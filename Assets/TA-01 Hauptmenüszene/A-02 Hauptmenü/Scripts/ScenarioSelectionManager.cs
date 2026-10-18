using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Merkt sich das gewählte Szenario und lädt die Szene.
// Die pending-Werte sind static, damit der ScenarioManager sie in der neuen Szene lesen kann.
public class ScenarioSelectionManager : MonoBehaviour
{
    public static ScenarioSelectionManager Instance;

    public static FireClass[] pendingFireClasses;
    public static int pendingFirePointCount;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [Header("Scene Loading")]
    [SerializeField] private float minimumLoadingTime = 5f;
    [SerializeField] private GameObject loadingScreen;

    private ScenarioButton selectedScenario;
    private bool isLoading;

    private void Awake()
    {
        Instance = this;

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    public void SelectScenario(ScenarioButton scenarioButton)
    {
        if (selectedScenario != null)
            selectedScenario.Deselect();

        selectedScenario = scenarioButton;
        selectedScenario.Select();
    }

    public void StartSelectedScene()
    {
        if (isLoading || selectedScenario == null)
            return;

        pendingFireClasses = selectedScenario.initialFireClasses;

        // Leicht = 1 Feuer, Mittel = 2, Schwer = 3
        pendingFirePointCount = difficultyDropdown != null ? difficultyDropdown.value + 1 : 2;

        StartCoroutine(LoadSceneAsync(selectedScenario.sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;

        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        float startTime = Time.realtimeSinceStartup;

        // Szene im Hintergrund laden, aber erst nach der Mindestladezeit wechseln
        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
        loading.allowSceneActivation = false;

        // Bei allowSceneActivation = false bleibt progress bei 0.9 stehen
        while (loading.progress < 0.9f)
            yield return null;

        float remainingTime = minimumLoadingTime - (Time.realtimeSinceStartup - startTime);
        if (remainingTime > 0f)
            yield return new WaitForSecondsRealtime(remainingTime);

        loading.allowSceneActivation = true;
    }
}
