using UnityEngine;
using TMPro;

public class MainMenuResultsPanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timeText;
    public TMP_Text firePointsText;
    public TMP_Text warningsText;
    public GameObject resultsPanel;

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(GameSessionStats.hasResult);

        if (!GameSessionStats.hasResult)
            return;

        int minutes = Mathf.FloorToInt(GameSessionStats.lastRunTime / 60f);
        int seconds = Mathf.FloorToInt(GameSessionStats.lastRunTime % 60f);

        if (timeText != null)
            timeText.text = $"Zeit: {minutes:00}:{seconds:00}";

        if (firePointsText != null)
            firePointsText.text = $"Gelöschte Feuerpunkte: {GameSessionStats.lastClearedFirePoints}";

        if (warningsText != null)
            warningsText.text = $"Warnungen: {GameSessionStats.lastWarnings}";
    }
}