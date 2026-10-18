using UnityEngine;

// Einfacher Schlauch-Controller mit 5 Kontrollpunkten.
// 0 und 1 bleiben am Feuerlöscher fest.
// 4 bleibt am Griff fest.
// Nur 2 und 3 werden simuliert.
[RequireComponent(typeof(LineRenderer))]
public class FireHose : MonoBehaviour
{
    [SerializeField] private Transform[] points = new Transform[5];

    [Header("Line")]
    [SerializeField, Min(2)] private int curveSegments = 6;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.03f;

    [Header("Motion")]
    [SerializeField, Range(1, 12)] private int iterations = 6;
    [SerializeField, Range(0f, 1f)] private float damping = 0.92f;
    [SerializeField, Range(0f, 1f)] private float bendStrength = 0.18f;
    [SerializeField, Range(0f, 1f)] private float followStrength = 0.20f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawCurvePreview = true;
    [SerializeField] private float gizmoPointRadius = 0.015f;
    [SerializeField] private Color pinnedPointColor = Color.red;
    [SerializeField] private Color freePointColor = Color.cyan;
    [SerializeField] private Color lineColor = Color.yellow;
    [SerializeField] private Color curvePreviewColor = Color.green;

    private LineRenderer lineRenderer;
    private Vector3[] simulated;
    private Vector3[] previous;
    private Vector3[] restOffsets;
    private float[] segmentLengths;
    private Vector3[] renderPoints;

    private void Awake()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        if (!IsValid())
            return;

        if (simulated == null || simulated.Length != 5)
            Refresh();

        Simulate();
        DrawLine();
    }

    private void OnValidate()
    {
        curveSegments = Mathf.Max(2, curveSegments);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        gizmoPointRadius = Mathf.Max(0.001f, gizmoPointRadius);
        iterations = Mathf.Max(1, iterations);

        if (!Application.isPlaying && IsValid())
            Refresh();
    }

    // Baut alles neu auf und setzt die Linie direkt korrekt.
    private void Refresh()
    {
        if (!IsValid())
            return;

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false; //Wolrd space false WICHTIG -> damit bei instanzen vom Prefab nicht die koodoanten vom linerenderer kopiert werden
        lineRenderer.widthMultiplier = lineWidth;

        simulated = new Vector3[5];
        previous = new Vector3[5];
        restOffsets = new Vector3[5];
        segmentLengths = new float[4];

        SnapToCurrentPose();

        for (int i = 0; i < 4; i++)
            segmentLengths[i] = Vector3.Distance(points[i].position, points[i + 1].position);

        CacheRestOffsets();
        DrawLine();
    }

    // Setzt alle Simulationspunkte sofort auf die aktuellen Transform-Positionen.
    public void SnapToCurrentPose()
    {
        if (!IsValid() || simulated == null || previous == null)
            return;

        for (int i = 0; i < 5; i++)
        {
            Vector3 p = points[i].position;
            simulated[i] = p;
            previous[i] = p;
        }
    }

    // Prüft, ob genau 5 Punkte vorhanden und gesetzt sind.
    private bool IsValid()
    {
        if (points == null || points.Length != 5)
            return false;

        for (int i = 0; i < 5; i++)
            if (points[i] == null)
                return false;

        return true;
    }

    // Merkt sich die Grundform relativ zur Linie von Punkt 1 bis Punkt 4.
    private void CacheRestOffsets()
    {
        Vector3 start = points[1].position;
        Vector3 end = points[4].position;

        for (int i = 0; i < 5; i++)
        {
            float t = Mathf.InverseLerp(1f, 4f, i);
            Vector3 linePos = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            restOffsets[i] = points[i].position - linePos;
        }
    }

    // Setzt die festen Punkte jedes Mal zurück.
    private void PinEnds()
    {
        simulated[0] = previous[0] = points[0].position;
        simulated[1] = previous[1] = points[1].position;
        simulated[4] = previous[4] = points[4].position;
    }

    // Simuliert nur die beiden mittleren Punkte.
    private void Simulate()
    {
        PinEnds();

        for (int i = 2; i <= 3; i++)
        {
            Vector3 current = simulated[i];
            Vector3 velocity = (current - previous[i]) * damping;
            previous[i] = current;
            simulated[i] = current + velocity;
        }

        for (int n = 0; n < iterations; n++)
        {
            PinEnds();
            PullTowardRest();
            SolveDistances();
            SolveBend();
        }
    }

    // Zieht die Mitte leicht zurück in ihre gemerkte Grundform.
    private void PullTowardRest()
    {
        Vector3 start = simulated[1];
        Vector3 end = simulated[4];

        for (int i = 2; i <= 3; i++)
        {
            float t = Mathf.InverseLerp(1f, 4f, i);
            Vector3 linePos = Vector3.Lerp(start, end, t);
            simulated[i] = Vector3.Lerp(simulated[i], linePos + restOffsets[i], followStrength);
        }
    }

    // Hält die Segmentlängen ungefähr konstant.
    private void SolveDistances()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 delta = simulated[i + 1] - simulated[i];
            float distance = delta.magnitude;

            if (distance < 0.0001f)
                continue;

            Vector3 correction = delta / distance * (distance - segmentLengths[i]);

            bool aPinned = (i == 0 || i == 1);
            bool bPinned = (i + 1 == 1 || i + 1 == 4);

            if (aPinned && !bPinned) simulated[i + 1] -= correction;
            else if (!aPinned && bPinned) simulated[i] += correction;
            else if (!aPinned)
            {
                simulated[i] += correction * 0.5f;
                simulated[i + 1] -= correction * 0.5f;
            }
        }
    }

    // Glättet die mittleren Punkte etwas.
    private void SolveBend()
    {
        simulated[2] = Vector3.Lerp(simulated[2], (simulated[1] + simulated[3]) * 0.5f, bendStrength);
        simulated[3] = Vector3.Lerp(simulated[3], (simulated[2] + simulated[4]) * 0.5f, bendStrength);
    }

    // Baut aus den Punkten eine glatte Kurve für den LineRenderer.
    private void DrawLine()
    {
        if (lineRenderer == null || simulated == null)
            return;

        int totalPoints = 4 * curveSegments + 1;

        if (renderPoints == null || renderPoints.Length != totalPoints)
            renderPoints = new Vector3[totalPoints];

        int index = 0;
        Transform lineSpace = lineRenderer.transform;

        for (int i = 0; i < 4; i++)
        {
            Vector3 p0 = simulated[Mathf.Max(i - 1, 0)];
            Vector3 p1 = simulated[i];
            Vector3 p2 = simulated[i + 1];
            Vector3 p3 = simulated[Mathf.Min(i + 2, 4)];

            for (int j = 0; j < curveSegments; j++)
            {
                float t = j / (float)curveSegments;
                Vector3 world = CatmullRom(p0, p1, p2, p3, t);
                renderPoints[index++] = lineSpace.InverseTransformPoint(world);
            }
        }

        renderPoints[index] = lineSpace.InverseTransformPoint(simulated[4]);
        lineRenderer.positionCount = totalPoints;
        lineRenderer.SetPositions(renderPoints);
    }

    // Zeichnet Waypoints und Vorschau im Editor.
    private void OnDrawGizmos()
    {
        if (!drawGizmos || !IsValid())
            return;

        for (int i = 0; i < 5; i++)
        {
            Gizmos.color = (i == 0 || i == 1 || i == 4) ? pinnedPointColor : freePointColor;
            Gizmos.DrawSphere(points[i].position, gizmoPointRadius);
        }

        Gizmos.color = lineColor;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(points[i].position, points[i + 1].position);

        if (!drawCurvePreview)
            return;

        Gizmos.color = curvePreviewColor;
        Vector3 prev = points[0].position;

        for (int i = 0; i < 4; i++)
        {
            Vector3 p0 = points[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = points[i].position;
            Vector3 p2 = points[i + 1].position;
            Vector3 p3 = points[Mathf.Min(i + 2, 4)].position;

            for (int j = 1; j <= curveSegments; j++)
            {
                float t = j / (float)curveSegments;
                Vector3 current = CatmullRom(p0, p1, p2, p3, t);
                Gizmos.DrawLine(prev, current);
                prev = current;
            }
        }
    }

    // Berechnet einen glatten Punkt auf der Catmull-Rom-Kurve.
    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}