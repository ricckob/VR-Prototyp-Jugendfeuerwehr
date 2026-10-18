using UnityEngine;
using UnityEngine.UI;

// Ablagefläche für eine Brandklassen-Karte. Zeigt die abgelegte Karte an;
// bei der richtigen Klasse wird die Karte entfernt, der Rahmen grün und der Tutorial-Schritt abgehakt.
public class BrandClassSlot : MonoBehaviour
{
    [Header("Welche Brandklasse ist hier richtig?")]
    [SerializeField] private FireClass expectedFireClass;

    [Header("Zielanzeige")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Image correctFrameImage;

    [Header("Tutorial Step")]
    [SerializeField] private TutorialStepToggle tutorialStepToggle;

    [Header("Rahmen")]
    [SerializeField] private Color correctFrameColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    private bool isSolved;

    public FireClass ExpectedFireClass => expectedFireClass;

    // Wird auch vom FireRoundManager genutzt: Rahmen in der Übungsrunde aus- und nach dem Löschen wieder einblenden.
    public void SetFrameVisible(bool visible)
    {
        if (correctFrameImage != null)
            correctFrameImage.enabled = visible;
    }

    private void Start()
    {
        if (targetImage != null)
            targetImage.enabled = false;

        if (correctFrameImage != null)
        {
            correctFrameImage.color = correctFrameColor;
            correctFrameImage.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSolved) return;

        BrandClassCard card = other.GetComponentInParent<BrandClassCard>();
        if (card == null) return;

        if (targetImage != null && card.CardSprite != null)
        {
            targetImage.sprite = card.CardSprite;
            targetImage.enabled = true;
        }

        if (card.FireClass != expectedFireClass) return;

        isSolved = true;
        Destroy(card.gameObject);

        if (tutorialStepToggle != null)
            tutorialStepToggle.MarkCompleted();

        SetFrameVisible(true);
    }
}
