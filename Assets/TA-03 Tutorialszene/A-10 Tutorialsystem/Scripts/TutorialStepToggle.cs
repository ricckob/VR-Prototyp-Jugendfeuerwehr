using UnityEngine;
using UnityEngine.UI;

// Ein abhakbarer Tutorial-Schritt. Der Zustand liegt im TutorialPagerManager (per stepId),
// Jeder Schritt hat seine eindeutige ID.
public class TutorialStepToggle : MonoBehaviour
{
    [Header("Gemeinsame ID auf beiden Canvases, z.B. fireextinguisher")]
    public string stepId;

    [Header("Referenzen")]
    public TutorialPagerManager manager;
    public Toggle uiToggle;
    public Image backgroundImage;

    [Header("Farben")]
    public Color incompleteColor = Color.white;
    public Color completeColor = Color.green;

    [Header("Sound")]
    public AudioSource audioSource;

    public bool IsCompleted => manager != null && manager.IsStepCompleted(stepId);

    public void MarkCompleted()
    {
        if (manager == null || string.IsNullOrWhiteSpace(stepId) || IsCompleted) return;

        manager.SetStepCompleted(stepId, true);

        PlaySound();
        RefreshVisual();
    }

    // Schritt wieder auf "nicht erledigt" setzen, z.B. beim Neustart einer Übungsrunde.
    public void ResetStep()
    {
        if (manager != null && !string.IsNullOrWhiteSpace(stepId))
            manager.SetStepCompleted(stepId, false);

        RefreshVisual();
    }

    // Seiten, die gerade nicht angezeigt werden, sind deaktiviert – dort spielt eine AudioSource nichts ab.
    // Dann den Clip stattdessen frei an der Position abspielen.
    private void PlaySound()
    {
        if (audioSource == null) return;

        if (audioSource.isActiveAndEnabled)
            audioSource.Play();
        else if (audioSource.clip != null)
            AudioSource.PlayClipAtPoint(audioSource.clip, audioSource.transform.position, audioSource.volume);
    }

    public void RefreshVisual()
    {
        bool completed = IsCompleted;

        if (uiToggle != null)
            uiToggle.SetIsOnWithoutNotify(completed);

        if (backgroundImage != null)
            backgroundImage.color = completed ? completeColor : incompleteColor;
    }
}
