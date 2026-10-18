using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Prüft regelmäßig, ob noch etwas brennt. Ist alles aus, ist das Szenario geschafft.
public class TriggerVictory : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float delayBeforeMainMenu = 3f;

    [Header("References")]
    [SerializeField] private LevelTimer levelTimer;

    [Header("Check")]
    [SerializeField] private float checkInterval = 0.25f;

    private bool victoryTriggered;
    private float checkTimer;

    // Alle FirePoints, die irgendwann gebrannt haben
    private readonly HashSet<FirePoint> seenBurning = new HashSet<FirePoint>();

    private void Start()
    {
        GameSessionStats.ResetRun();
    }

    private void Update()
    {
        if (victoryTriggered || FirePointManager.Instance == null)
            return;

        // Nicht jeden Frame prüfen
        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;

        bool anyBurning = false;

        foreach (FirePoint firePoint in FirePointManager.Instance.GetAllFirePoints())
        {
            if (firePoint.isIgnited)
            {
                seenBurning.Add(firePoint);
                anyBurning = true;
            }
        }

        // Gewonnen, wenn schon mal etwas gebrannt hat und jetzt alles aus ist
        if (seenBurning.Count > 0 && !anyBurning)
            TriggerWin();
    }

    private void TriggerWin()
    {
        victoryTriggered = true;

        float finalTime = 0f;

        if (levelTimer != null)
        {
            levelTimer.StopTimer();
            finalTime = levelTimer.ElapsedTime;
        }

        // Alles, was gebrannt hat, ist jetzt gelöscht
        GameSessionStats.SaveResults(finalTime, seenBurning.Count);

        StartCoroutine(LoadMainMenuAfterDelay());
    }

    private IEnumerator LoadMainMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delayBeforeMainMenu);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
