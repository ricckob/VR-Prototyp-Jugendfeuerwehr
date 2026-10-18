using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Zündet beim Start die Feuer für das im Hauptmenü gewählte Szenario
public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Start")]
    [SerializeField] private float maxWaitForFirePoints = 5f;
    [SerializeField] private float initialDelay = 0.25f;

    private IEnumerator Start()
    {
        FireClass[] fireClasses = ScenarioSelectionManager.pendingFireClasses;
        int fireCount = ScenarioSelectionManager.pendingFirePointCount;

        if (fireClasses == null || fireClasses.Length == 0)
        {
            Debug.LogWarning("Keine pendingFireClasses vorhanden.");
            yield break;
        }

        yield return new WaitForSeconds(initialDelay);

        if (FirePointManager.Instance == null)
        {
            Debug.LogWarning("Kein FirePointManager in der Szene.");
            yield break;
        }

        // Die FlammableObjects spawnen ihre FirePoints erst zur Laufzeit, also kurz darauf warten
        float timer = 0f;
        while (timer < maxWaitForFirePoints && FirePointManager.Instance.GetAllFirePoints().Count == 0)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Alle FirePoints der gewählten Brandklassen sammeln
        List<FirePoint> candidates = FirePointManager.Instance.GetAllFirePoints().FindAll(
            fp => fp.isActiveAndEnabled && System.Array.IndexOf(fireClasses, fp.fireClass) >= 0);

        int ignitedCount = 0;

        // Erst pro Brandklasse ein Feuer, damit jede Klasse sicher vorkommt
        foreach (FireClass fireClass in fireClasses)
        {
            List<FirePoint> ofClass = candidates.FindAll(fp => fp.fireClass == fireClass);

            if (ofClass.Count == 0)
            {
                Debug.LogWarning($"Kein aktiver FirePoint der Klasse {fireClass} in der Szene.");
                continue;
            }

            IgniteAndRemove(ofClass[Random.Range(0, ofClass.Count)], candidates);
            ignitedCount++;
        }

        // Dann zufällig auffüllen, bis die Anzahl aus dem Schwierigkeitsgrad erreicht ist
        while (ignitedCount < fireCount && candidates.Count > 0)
        {
            IgniteAndRemove(candidates[Random.Range(0, candidates.Count)], candidates);
            ignitedCount++;
        }
    }

    // Aus der Liste nehmen, damit derselbe Punkt nicht zweimal gewählt wird
    private static void IgniteAndRemove(FirePoint firePoint, List<FirePoint> candidates)
    {
        candidates.Remove(firePoint);
        firePoint.Ignite();
    }
}
