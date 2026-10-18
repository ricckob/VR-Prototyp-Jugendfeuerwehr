using System;
using System.Collections.Generic;
using UnityEngine;

// Gemeinsamer Zustand aller Tutorial-Panels: aktuelle Seite und erledigte Schritte (per stepId).
public class TutorialPagerManager : MonoBehaviour
{
    public int CurrentPage { get; private set; }
    public int PageCount { get; private set; }

    public event Action OnStateChanged;

    [Header("Zweiter Feuerlöscher")]
    [SerializeField] private int extinguisherPageIndex = 3;
    [SerializeField] private GameObject secondExtinguisher;

    private readonly HashSet<string> completedSteps = new HashSet<string>();

    public void Initialize(int pageCount)
    {
        PageCount = pageCount;
        CurrentPage = 0;
        OnStateChanged?.Invoke();
    }

    public bool IsStepCompleted(string stepId)
    {
        return !string.IsNullOrWhiteSpace(stepId) && completedSteps.Contains(stepId);
    }

    public void SetStepCompleted(string stepId, bool completed)
    {
        if (string.IsNullOrWhiteSpace(stepId)) return;

        bool changed = completed ? completedSteps.Add(stepId) : completedSteps.Remove(stepId);
        if (changed)
            OnStateChanged?.Invoke();
    }

    public void ChangePage(int delta)
    {
        int page = CurrentPage + delta;
        if (page < 0 || page >= PageCount) return;

        CurrentPage = page;

        // Nur einmal aktivieren, beim Zurückblättern bleibt er stehen
        if (page == extinguisherPageIndex && secondExtinguisher != null && !secondExtinguisher.activeSelf)
            secondExtinguisher.SetActive(true);

        OnStateChanged?.Invoke();
    }
}
