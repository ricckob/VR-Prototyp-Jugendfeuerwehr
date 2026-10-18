using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Füllstandsanzeige vom Löscher, der gerade in der Hand ist
public class ExtinguisherBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;

    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color midColor = Color.yellow;
    [SerializeField] private Color lowColor = Color.red;

    private ExtinguisherAgent currentExtinguisher;

    private void Start()
    {
        panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (currentExtinguisher == null)
            return;

        float ratio = currentExtinguisher.Remaining01;
        fillImage.fillAmount = ratio;
        percentText.text = $"{Mathf.RoundToInt(ratio * 100f)}%";

        // Grün -> Gelb -> Rot
        if (ratio > 0.5f)
            fillImage.color = Color.Lerp(midColor, fullColor, (ratio - 0.5f) * 2f);
        else
            fillImage.color = Color.Lerp(lowColor, midColor, ratio * 2f);
    }

    public void ShowForExtinguisher(ExtinguisherAgent extinguisher)
    {
        currentExtinguisher = extinguisher;
        panelRoot.SetActive(extinguisher != null);
    }

    public void HideIfCurrent(ExtinguisherAgent extinguisher)
    {
        if (currentExtinguisher != extinguisher)
            return;

        currentExtinguisher = null;
        panelRoot.SetActive(false);
    }
}
