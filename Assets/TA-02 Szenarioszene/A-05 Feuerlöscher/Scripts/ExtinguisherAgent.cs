using System;
using UnityEngine;

// Eigene Klasse nur, damit die im Inspector eingestellten Werte erhalten bleiben
[Serializable]
public class ExtinguisherResource
{
    public float maxAgentSeconds = 20f;
}

// Löschmittelvorrat: so viele Sekunden kann gesprüht werden
public class ExtinguisherAgent : MonoBehaviour
{
    [SerializeField] private ExtinguisherResource resource = new ExtinguisherResource();

    private float remainingSeconds;

    public float Remaining01 => resource.maxAgentSeconds > 0f ? remainingSeconds / resource.maxAgentSeconds : 0f;

    private void Awake()
    {
        remainingSeconds = resource.maxAgentSeconds;
    }

    // Gibt false zurück, wenn der Löscher leer ist
    public bool Consume(float deltaTime)
    {
        if (remainingSeconds <= 0f)
            return false;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - deltaTime);
        return remainingSeconds > 0f;
    }
}
