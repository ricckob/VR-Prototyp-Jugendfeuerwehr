using UnityEngine;

// Zeigt eine durchsichtige Vorschau, wo der Deckel einrastet
public class LidSnapGhost : MonoBehaviour
{
    [Header("References")]
    public GameObject ghostVisual;
    public Transform snapPoint;

    private void Start()
    {
        HideGhost();
    }

    public void ShowGhost()
    {
        if (ghostVisual != null && snapPoint != null)
            ghostVisual.SetActive(true);
    }

    public void HideGhost()
    {
        if (ghostVisual != null)
            ghostVisual.SetActive(false);
    }

    // Vorschau folgt dem Snap-Punkt, falls sich der Topf bewegt
    private void LateUpdate()
    {
        if (ghostVisual != null && snapPoint != null && ghostVisual.activeSelf)
            ghostVisual.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
    }
}
