using System.Collections.Generic;
using UnityEngine;

// Macht ein Objekt brennbar. Ersetzt die alten Skripte FlammableObject, FirePointSpawner und BurnShaderController.
// 1. Beim Start werden FirePoints auf der Oberfläche der Collider verteilt.
// 2. Sobald der erste Punkt brennt, wird das Objekt über den Burn-Shader verkohlt.
// 3. Jede Brandstelle wird an den Shader übergeben (_BurnPositions).
public class FlammableObject : MonoBehaviour
{
    public enum SpawnMode
    {
        TopSurface,  // nur oben (z.B. Bett, Tisch)
        AllSurfaces  // alle Seiten (z.B. Vorhang, Schrank)
    }

    [Header("FirePoints")]
    public GameObject firePointPrefab;
    public Transform meshRoot;
    [Min(1)] public int firePointCount = 8;
    [Min(0.1f)] public float ignitionTime = 2f;
    public SpawnMode spawnMode = SpawnMode.TopSurface;

    [Header("Verkohlung (Burn-Shader)")]
    public string burnProperty = "_Burn";
    public string noiseScaleProperty = "_BurnNoiseScale";
    public string edgeWidthProperty = "_BurnEdgeWidth";
    [Range(0f, 2f)] public float activeNoiseScale = 0.6f;
    [Range(0f, 0.5f)] public float activeEdgeWidth = 0.1f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private const int MaxBurnPositions = 64; // muss zur Array-Größe im Shader passen
    private const int SpawnAttempts = 10;    // Versuche pro FirePoint, eine Oberfläche zu treffen

    private readonly List<FirePoint> firePoints = new List<FirePoint>();
    public IReadOnlyList<FirePoint> FirePoints => firePoints;

    // Materialkopien, die erst beim ersten Brennen erstellt werden
    private readonly List<Material> burnMaterials = new List<Material>();
    private readonly List<Material> ownedMaterials = new List<Material>(); // zum Aufräumen in OnDestroy
    private bool isBurning;

    // Brandstellen für den Shader
    private readonly Vector4[] burnPositions = new Vector4[MaxBurnPositions];
    private int burnPositionCount;
    private readonly HashSet<FirePoint> markedFirePoints = new HashSet<FirePoint>();

    private Transform Root => meshRoot != null ? meshRoot : transform;

    private void Start()
    {
        SpawnFirePoints();
    }

    private void OnDestroy()
    {
        // Selbst erstellte Materialkopien löschen, sonst bleiben sie im Speicher
        foreach (Material material in ownedMaterials)
            Destroy(material);
    }

    // ---------- FirePoints verteilen ----------

    private void SpawnFirePoints()
    {
        if (firePointPrefab == null)
        {
            Debug.LogError($"[{name}] Kein FirePoint-Prefab zugewiesen.", this);
            return;
        }

        List<Collider> colliders = GetValidColliders();
        if (colliders.Count == 0)
        {
            Debug.LogWarning($"[{name}] Keine gültigen Collider gefunden.", this);
            return;
        }

        int fireLayer = LayerMask.NameToLayer("Fire");

        for (int i = 0; i < firePointCount; i++)
        {
            Collider collider = colliders[Random.Range(0, colliders.Count)];

            if (TryGetSurfacePoint(collider, out Vector3 position))
                CreateFirePoint(position, $"FirePoint_{i}_{collider.name}", fireLayer);
        }

        if (showDebugLogs)
            Debug.Log($"[{name}] {firePoints.Count} FirePoints erstellt.", this);
    }

    private List<Collider> GetValidColliders()
    {
        var result = new List<Collider>();

        foreach (Collider collider in Root.GetComponentsInChildren<Collider>())
        {
            if (collider.enabled && collider.gameObject.activeInHierarchy)
                result.Add(collider);
        }

        return result;
    }

    // Sucht einen zufälligen Punkt auf der echten Oberfläche des Colliders.
    // Idee: zufälliger Punkt in der Bounding Box, dann von außen einen Strahl auf den Collider schießen.
    // So liegen die Punkte auch bei schrägen oder unregelmäßigen Formen auf dem Mesh und schweben nicht.
    private bool TryGetSurfacePoint(Collider collider, out Vector3 point)
    {
        Bounds bounds = collider.bounds;

        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            // Richtung, in die der Strahl schießt: TopSurface immer von oben nach unten
            Vector3 direction = spawnMode == SpawnMode.TopSurface ? Vector3.down : RandomAxisDirection();

            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            // Startpunkt knapp außerhalb der Box, auf der Seite gegenüber der Richtung
            float extent = Mathf.Abs(Vector3.Dot(bounds.extents, direction));
            float offset = Vector3.Dot(randomPoint - bounds.center, direction);
            Vector3 start = randomPoint - direction * (offset + extent + 0.1f);

            // collider.Raycast testet nur diesen einen Collider, nicht die ganze Szene
            if (collider.Raycast(new Ray(start, direction), out RaycastHit hit, extent * 2f + 0.2f))
            {
                point = hit.point;
                return true;
            }
        }

        point = default;
        return false;
    }

    private static Vector3 RandomAxisDirection()
    {
        switch (Random.Range(0, 6))
        {
            case 0: return Vector3.down;
            case 1: return Vector3.up;
            case 2: return Vector3.left;
            case 3: return Vector3.right;
            case 4: return Vector3.forward;
            default: return Vector3.back;
        }
    }

    private void CreateFirePoint(Vector3 position, string pointName, int fireLayer)
    {
        GameObject pointObject = Instantiate(firePointPrefab, position, Quaternion.identity, transform);
        pointObject.name = pointName;

        if (fireLayer != -1)
            pointObject.layer = fireLayer;

        FirePoint firePoint = pointObject.GetComponent<FirePoint>();
        if (firePoint == null)
        {
            Debug.LogError($"[{name}] Das Prefab besitzt kein FirePoint-Script.", this);
            Destroy(pointObject);
            return;
        }

        firePoint.ignitionTime = ignitionTime;
        firePoint.flammableParent = this;
        firePoints.Add(firePoint);
    }

    // ---------- Verkohlung ----------

    // Wird von FirePoint.Ignite aufgerufen
    public void OnFirePointIgnited(FirePoint firePoint)
    {
        if (!isBurning)
            StartBurning();

        // Jede Brandstelle nur einmal an den Shader geben
        if (markedFirePoints.Add(firePoint))
            AddBurnPosition(firePoint.transform.position);

        if (showDebugLogs)
            Debug.Log($"[{name}] Feuer entzündet: {firePoint.name}", this);
    }

    // Erstellt die Materialkopien erst jetzt. Objekte, die nie brennen, behalten ihre
    // gemeinsamen Materialien, das spart Speicher und Draw Calls auf der Quest.
    // Die Verkohlung bleibt auch nach dem Löschen bestehen.
    private void StartBurning()
    {
        isBurning = true;

        int burnId = Shader.PropertyToID(burnProperty);
        int noiseId = Shader.PropertyToID(noiseScaleProperty);
        int edgeId = Shader.PropertyToID(edgeWidthProperty);

        foreach (Renderer renderer in Root.GetComponentsInChildren<Renderer>())
        {
            // Renderer der FirePoints (Feuer-VFX) überspringen
            if (renderer.GetComponentInParent<FirePoint>() != null || !HasBurnMaterial(renderer, burnId))
                continue;

            // renderer.materials erstellt automatisch Kopien nur für diesen Renderer
            foreach (Material material in renderer.materials)
            {
                ownedMaterials.Add(material);

                if (!material.HasProperty(burnId))
                    continue;

                material.SetFloat(burnId, 1f);
                if (material.HasProperty(noiseId)) material.SetFloat(noiseId, activeNoiseScale);
                if (material.HasProperty(edgeId)) material.SetFloat(edgeId, activeEdgeWidth);
                burnMaterials.Add(material);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[{name}] Verkohlung gestartet, {burnMaterials.Count} Materialien.", this);
    }

    private static bool HasBurnMaterial(Renderer renderer, int burnId)
    {
        foreach (Material material in renderer.sharedMaterials)
        {
            if (material != null && material.HasProperty(burnId))
                return true;
        }

        return false;
    }

    private void AddBurnPosition(Vector3 worldPosition)
    {
        if (burnPositionCount >= MaxBurnPositions)
            return;

        burnPositions[burnPositionCount++] = new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1f);

        // Direkt ins Material statt über MaterialPropertyBlock,
        // sonst fällt das Objekt aus dem SRP Batcher
        foreach (Material material in burnMaterials)
        {
            material.SetVectorArray("_BurnPositions", burnPositions);
            material.SetFloat("_BurnPositionCount", burnPositionCount);
        }
    }

    private void OnDrawGizmos()
    {
        foreach (FirePoint firePoint in firePoints)
        {
            if (firePoint == null)
                continue;

            Gizmos.color = firePoint.isIgnited ? Color.red : firePoint.heat > 0f ? Color.yellow : Color.cyan;
            Gizmos.DrawSphere(firePoint.transform.position, 0.03f);
        }
    }
}
