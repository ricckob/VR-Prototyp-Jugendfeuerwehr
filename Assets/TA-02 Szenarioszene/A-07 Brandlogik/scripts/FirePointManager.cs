using System.Collections.Generic;
using UnityEngine;

// Zentrale Liste aller FirePoints in der Szene. Darf nur einmal vorhanden sein.
// Der Feuerlöscher (ExtinguishingEffect) holt sich hier alle Feuer zum Löschen.
public class FirePointManager : MonoBehaviour
{
    public static FirePointManager Instance { get; private set; }

    // static, damit sich FirePoints auch dann eintragen können, wenn der Manager
    // noch kein Awake hatte (Reihenfolge beim Szenenstart ist in Unity nicht festgelegt)
    private static readonly List<FirePoint> firePoints = new List<FirePoint>();

    // Jeder FirePoint, der mindestens einmal gebrannt hat (für eine spätere Auswertung)
    private static readonly HashSet<FirePoint> burnedFirePoints = new HashSet<FirePoint>();

    public int BurnedFirePointCount => burnedFirePoints.Count;

    private void Awake()
    {
        // Falls es aus Versehen zwei Manager gibt, den zweiten entfernen
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Wird von FirePoint in OnEnable / OnDisable aufgerufen
    public static void Register(FirePoint firePoint)
    {
        if (!firePoints.Contains(firePoint))
            firePoints.Add(firePoint);
    }

    public static void Unregister(FirePoint firePoint)
    {
        firePoints.Remove(firePoint);
        burnedFirePoints.Remove(firePoint);
    }

    public List<FirePoint> GetAllFirePoints() => firePoints;

    // Wird beim Entzünden aufgerufen. HashSet zählt jeden Punkt nur einmal.
    public void RegisterBurnedFirePoint(FirePoint firePoint) => burnedFirePoints.Add(firePoint);

    public void ResetEvaluation() => burnedFirePoints.Clear();
}
