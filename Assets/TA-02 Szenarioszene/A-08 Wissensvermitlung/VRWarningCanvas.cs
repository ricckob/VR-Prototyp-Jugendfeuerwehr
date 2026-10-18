using System.Collections;
using UnityEngine;

public class VRWarningCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject warningPanel;

    [Header("Timing")]
    [SerializeField] private float showDuration = 5f;

    private Coroutine warningRoutine;

    private void Start()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    public void ShowWarning()
    {
        if (warningPanel == null)
        {
            Debug.LogError("[VRWarningCanvas] warningPanel fehlt!", this);
            return;
        }

        bool shouldCountWarning = !warningPanel.activeSelf;

        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        if (shouldCountWarning)
            GameSessionStats.RegisterWarning();

        warningRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        warningPanel.SetActive(true);

        yield return new WaitForSeconds(showDuration);

        warningPanel.SetActive(false);
        warningRoutine = null;
    }
}