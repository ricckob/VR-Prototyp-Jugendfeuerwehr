using UnityEngine;
using Oculus.Interaction;

public class LidSnapFireExtinguisher : MonoBehaviour
{
    [SerializeField] private SnapInteractable snapZone;

    [SerializeField] private Transform potObject;

    // Wird zur Laufzeit gefunden
    private FirePoint firePoint;

    private void OnEnable()
    {
        snapZone.WhenSelectingInteractorViewAdded += OnLidSnapped;
        snapZone.WhenSelectingInteractorViewRemoved += OnLidRemoved;
    }

    private void OnDisable()
    {
        snapZone.WhenSelectingInteractorViewAdded -= OnLidSnapped;
        snapZone.WhenSelectingInteractorViewRemoved -= OnLidRemoved;
    }

    private void OnLidSnapped(IInteractorView interactor)
    {
        // FirePoint erst jetzt suchen – er existiert jetzt zur Laufzeit
        firePoint = potObject.GetComponentInChildren<FirePoint>();

        if (firePoint == null)
        {
            Debug.LogWarning("[LidSnap] ⚠️ Kein FirePoint am Topf gefunden – ist der Fettbrand aktiv?");
            return;
        }

        firePoint.ExtinguishByLid();
    }

    private void OnLidRemoved(IInteractorView interactor)
    {
        if (firePoint == null) return;

        firePoint.CancelLidExtinguish();
    }
}