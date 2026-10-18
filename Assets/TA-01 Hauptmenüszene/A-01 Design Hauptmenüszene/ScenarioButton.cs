using UnityEngine;

public class ScenarioButton : MonoBehaviour
{
    [Header("Scenario Info")]
    public string scenarioName;
    public string sceneName;
    
    [Header("Fire Classes")]
    public FireClass[] initialFireClasses;
    
    [Header("Visuals")]
    public GameObject selectedOutline;

    [Header("Detail Panels")]
    public GameObject[] allDetailPanels;
    public GameObject myDetailPanel;

    public void OnScenarioClicked()
    {
        if (ScenarioSelectionManager.Instance == null)
        {
            Debug.LogWarning("[ScenarioButton] ScenarioSelectionManager.Instance ist null.");
            return;
        }

        ScenarioSelectionManager.Instance.SelectScenario(this);
        ShowMyDetailPanel();
    }

    public void Select()
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(true);
    }

    public void Deselect()
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(false);
    }

    public void ShowMyDetailPanel()
    {
        if (allDetailPanels != null)
        {
            foreach (var panel in allDetailPanels)
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }

        if (myDetailPanel != null)
            myDetailPanel.SetActive(true);
    }
}