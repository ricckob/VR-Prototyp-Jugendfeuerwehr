using UnityEngine;
using UnityEngine.UI;

// Zeigt die aktuelle Seite des TutorialPagerManagers an. "Weiter" geht erst, Toggles der aktuellen Seite erledigt sind.
public class TutorialPagerView : MonoBehaviour
{
    public TutorialPagerManager manager;
    public GameObject[] pages;
    public Toggle prevToggle;
    public Toggle nextToggle;

    private TutorialStepToggle[][] pageSteps;

    private void Start()
    {
        if (manager == null)
        {
            Debug.LogError($"{name}: TutorialPagerManager fehlt.", this);
            return;
        }

        // Schritte einmal pro Seite suchen und mit dem Manager verbinden
        pageSteps = new TutorialStepToggle[pages.Length][];
        for (int i = 0; i < pages.Length; i++)
        {
            pageSteps[i] = pages[i] != null
                ? pages[i].GetComponentsInChildren<TutorialStepToggle>(true)
                : new TutorialStepToggle[0];

            foreach (TutorialStepToggle step in pageSteps[i])
                step.manager = manager;
        }

        if (prevToggle != null) prevToggle.onValueChanged.AddListener(OnPrevToggled);
        if (nextToggle != null) nextToggle.onValueChanged.AddListener(OnNextToggled);

        manager.OnStateChanged += RefreshView;

        if (manager.PageCount == 0)
            manager.Initialize(pages.Length);

        RefreshView();
    }

    private void OnDestroy()
    {
        if (manager != null) manager.OnStateChanged -= RefreshView;
        if (prevToggle != null) prevToggle.onValueChanged.RemoveListener(OnPrevToggled);
        if (nextToggle != null) nextToggle.onValueChanged.RemoveListener(OnNextToggled);
    }

    private void OnPrevToggled(bool isOn)
    {
        if (!isOn) return;

        prevToggle.SetIsOnWithoutNotify(false);
        manager.ChangePage(-1);
    }

    private void OnNextToggled(bool isOn)
    {
        if (!isOn) return;

        nextToggle.SetIsOnWithoutNotify(false);
        if (AreAllStepsCompletedOnPage(manager.CurrentPage))
            manager.ChangePage(1);
    }

    private void RefreshView()
    {
        int current = manager.CurrentPage;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == current);

            foreach (TutorialStepToggle step in pageSteps[i])
                step.RefreshVisual();
        }

        if (prevToggle != null)
            prevToggle.interactable = current > 0;

        if (nextToggle != null)
            nextToggle.interactable = current < manager.PageCount - 1 && AreAllStepsCompletedOnPage(current);
    }

    private bool AreAllStepsCompletedOnPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= pageSteps.Length) return true;

        foreach (TutorialStepToggle step in pageSteps[pageIndex])
            if (!step.IsCompleted) return false;

        return true;
    }
}
