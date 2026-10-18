using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Übungsrunde: zündet in zufälliger Reihenfolge alle 'interval' Sekunden einen FirePoint pro Brandklasse.
// Beim Start werden die grünen Rahmen der Brandklassen-Slots ausgeblendet.
// Sind alle Feuer einer gezündeten Klasse gelöscht, wird ihr Schritt abgehakt (grün) und ihr Rahmen wieder eingeblendet.
public class FireRoundManager : MonoBehaviour
{
    [Serializable]
    private class ClassEntry
    {
        public FireClass fireClass;
        public Toggle toggle;
    }

    [Header("Timing")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float interval = 5f;

    [Header("Brandklassen")]
    [SerializeField] private ClassEntry[] classes;

    [Header("UI")]
    [SerializeField] private Toggle startToggle;

    private readonly HashSet<FireClass> pendingClasses = new HashSet<FireClass>();
    private bool isRoundRunning;

    private void Awake()
    {
        if (startToggle != null)
            startToggle.SetIsOnWithoutNotify(false);

        ResetAll();
    }

    private void OnEnable() => FirePoint.Extinguished += OnFirePointExtinguished;
    private void OnDisable() => FirePoint.Extinguished -= OnFirePointExtinguished;


    public void StartRoundFromToggle(bool isOn)
    {
        if (isRoundRunning) return;

        ResetAll();

        foreach (ClassEntry entry in classes)
            SetSlotFramesVisible(entry.fireClass, false);

        StartCoroutine(RunRound());
    }

    private void ResetAll()
    {
        pendingClasses.Clear();

        foreach (ClassEntry entry in classes)
        {
            if (entry.toggle == null) continue;

            entry.toggle.interactable = false;
            entry.toggle.SetIsOnWithoutNotify(false);

            // TutorialStepToggle liegt auf demselben Objekt wie der Toggle und färbt ihn
            if (entry.toggle.TryGetComponent(out TutorialStepToggle step))
                step.ResetStep();
        }
    }

    // Slots können auf gerade ausgeblendeten Tutorial-Seiten liegen, deshalb auch inaktive suchen
    private static void SetSlotFramesVisible(FireClass fireClass, bool visible)
    {
        foreach (BrandClassSlot slot in FindObjectsByType<BrandClassSlot>(FindObjectsInactive.Include))
            if (slot.ExpectedFireClass == fireClass)
                slot.SetFrameVisible(visible);
    }

    private IEnumerator RunRound()
    {
        isRoundRunning = true;
        if (startToggle != null) startToggle.interactable = false;

        yield return new WaitForSeconds(startDelay);

        // Reihenfolge mischen (Fisher-Yates)
        List<ClassEntry> order = new List<ClassEntry>(classes);
        for (int i = 0; i < order.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, order.Count);
            (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (ClassEntry entry in order)
        {
            FirePoint target = GetRandomColdFirePoint(entry.fireClass);

            if (target != null)
            {
                pendingClasses.Add(entry.fireClass);
                target.Ignite();
            }
            else
            {
                Debug.LogWarning($"FireRoundManager: Kein freier FirePoint für Klasse {entry.fireClass} gefunden.", this);
            }

            yield return new WaitForSeconds(interval);
        }

        isRoundRunning = false;

        if (startToggle != null)
        {
            startToggle.interactable = true;
            startToggle.SetIsOnWithoutNotify(false);
        }
    }

    private void OnFirePointExtinguished(FirePoint firePoint)
    {
        FireClass fireClass = firePoint.fireClass;
        if (!pendingClasses.Contains(fireClass)) return;

        foreach (FirePoint fp in FindObjectsByType<FirePoint>())
            if (fp.fireClass == fireClass && fp.isIgnited) return;

        pendingClasses.Remove(fireClass);
        SetSlotFramesVisible(fireClass, true);

        foreach (ClassEntry entry in classes)
        {
            if (entry.fireClass != fireClass || entry.toggle == null) continue;

            if (entry.toggle.TryGetComponent(out TutorialStepToggle step))
                step.MarkCompleted();
            else
                entry.toggle.SetIsOnWithoutNotify(true);
        }
    }

    private static FirePoint GetRandomColdFirePoint(FireClass fireClass)
    {
        List<FirePoint> candidates = new List<FirePoint>();

        foreach (FirePoint fp in FindObjectsByType<FirePoint>())
            if (fp.fireClass == fireClass && fp.isActiveAndEnabled && !fp.isIgnited)
                candidates.Add(fp);

        return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
    }
}
