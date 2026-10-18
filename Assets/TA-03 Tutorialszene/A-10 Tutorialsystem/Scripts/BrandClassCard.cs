using UnityEngine;

// Greifbare Brandklassen-Karte. Beim ersten Greifen (über den Unity Event Wrapper unter Select) wird eine neue Karte erzeugt.
public class BrandClassCard : MonoBehaviour
{
    [Header("Karte")]
    [SerializeField] private FireClass fireClass;
    [SerializeField] private Sprite cardSprite;

    [Header("Respawn")]
    [SerializeField] private GameObject cardPrefab;

    public FireClass FireClass => fireClass;
    public Sprite CardSprite => cardSprite;

    // Startplatz merken, die Ersatzkarte erscheint immer dort
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool hasSpawnedReplacement;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void SpawnReplacement()
    {
        if (hasSpawnedReplacement) return;

        Instantiate(cardPrefab, startPosition, startRotation);
        hasSpawnedReplacement = true;
    }
}
