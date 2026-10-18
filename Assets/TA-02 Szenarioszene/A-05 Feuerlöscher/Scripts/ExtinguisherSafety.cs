using UnityEngine;

// Reihenfolge beim Löscher: greifen -> Stift ziehen -> Druckknopf -> Hebel.
// Die Komponenten für den nächsten Schritt werden erst freigeschaltet, wenn der vorige erledigt ist.
public class ExtinguisherSafety : MonoBehaviour
{
    [Header("Sicherheitsstift")]
    [SerializeField] private Transform safetyPin;
    [SerializeField] private Rigidbody safetyPinRigidbody;

    [Header("Freigabe nach Hauptgriff")]
    [SerializeField] private Behaviour[] enableAfterExtinguisherGrabbed;

    [Header("Freigabe nach gezogenem Sicherheitsstift")]
    [SerializeField] private Behaviour[] enableAfterPinRemoved;

    [Header("Freigabe nach aktiviertem Druckknopf (optional)")]
    [SerializeField] private Behaviour[] enableAfterPressureButtonActivated;

    private bool isGrabbed;
    private bool isPinRemoved;
    private bool isButtonPressed;

    private void Awake()
    {
        // Stift hängt fest, bis er gezogen wird
        if (safetyPinRigidbody != null)
        {
            safetyPinRigidbody.isKinematic = true;
            safetyPinRigidbody.useGravity = false;
        }

        ApplySafetyState();
    }

    public void OnExtinguisherGrabbed()
    {
        isGrabbed = true;
        ApplySafetyState();
    }

    public void OnExtinguisherReleased()
    {
        isGrabbed = false;
        ApplySafetyState();
    }

    public void RemoveSafetyPin()
    {
        if (!isGrabbed || isPinRemoved)
            return;

        isPinRemoved = true;

        // Stift vom Löscher lösen und runterfallen lassen
        if (safetyPin != null)
            safetyPin.SetParent(null, true);

        if (safetyPinRigidbody != null)
        {
            safetyPinRigidbody.isKinematic = false;
            safetyPinRigidbody.useGravity = true;
        }

        ApplySafetyState();
    }

    public void ActivatePressureButton()
    {
        if (!isGrabbed || !isPinRemoved)
            return;

        isButtonPressed = true;
        ApplySafetyState();
    }

    private void ApplySafetyState()
    {
        SetEnabled(enableAfterExtinguisherGrabbed, isGrabbed);
        SetEnabled(enableAfterPinRemoved, isGrabbed && isPinRemoved);
        SetEnabled(enableAfterPressureButtonActivated, isGrabbed && isPinRemoved && isButtonPressed);
    }

    private static void SetEnabled(Behaviour[] behaviours, bool enabledState)
    {
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabledState;
        }
    }
}
