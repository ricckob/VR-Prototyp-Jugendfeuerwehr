using System.Collections.Generic;
using UnityEngine;

public enum ExtinguisherType
{
    KlasseABC,
    KlasseA,
    KlasseB,
    KlasseC,
    KlasseD,
    KlasseF
}

// Hängt am Partikelsystem vom Löscher. Trifft ein Partikel ein Feuer, wird es schwächer.
[RequireComponent(typeof(ParticleSystem))]
public class ExtinguishingEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private VRWarningCanvas wrongTypeWarningCanvas;

    [Header("Extinguisher Type")]
    [SerializeField] private ExtinguisherType extinguisherType = ExtinguisherType.KlasseABC;

    [Header("Extinguishing Strength")]
    [SerializeField] private float baseDamagePerParticle = 12f;
    [SerializeField] private int multiHitThreshold = 3;
    [SerializeField] private float multiHitBonus = 1.5f;
    [SerializeField] private float maxSearchRadius = 1.2f;

    [Header("Warning")]
    [SerializeField] private float warningCooldown = 1f;

    private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    private float nextWarningTime;
    private float efficiencyMultiplier = 1f;

    private void Awake()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();

        if (!particleSystem.collision.enabled || !particleSystem.collision.sendCollisionMessages)
            Debug.LogWarning($"[{name}] Collision Module oder Send Collision Messages ist deaktiviert.", this);
    }

    // Kurze Sprühstöße löschen besser, Dauersprühen schlechter
    public void SetTrainingEfficiency(bool burstActive, bool continuousPenaltyActive)
    {
        efficiencyMultiplier = 1f;

        if (burstActive)
            efficiencyMultiplier *= 1.2f;

        if (continuousPenaltyActive)
            efficiencyMultiplier *= 0.75f;
    }

    private void OnParticleCollision(GameObject other)
    {
        int hitCount = particleSystem.GetCollisionEvents(other, collisionEvents);
        if (hitCount <= 0)
            return;

        Vector3 hitPosition = collisionEvents[0].intersection;
        FirePoint firePoint = FindFirePoint(other, hitPosition);

        if (firePoint == null)
            return;

        if (!CanExtinguish(firePoint.fireClass))
        {
            ShowWrongTypeWarning();
            return;
        }

        float damage = baseDamagePerParticle * hitCount;

        if (hitCount >= multiHitThreshold)
            damage *= multiHitBonus;

        // Je weiter weg vom Feuer, desto schwächer (aber mindestens halbe Wirkung)
        float distance = Vector3.Distance(hitPosition, firePoint.transform.position);
        float falloff = Mathf.Max(0.5f, 1f - distance / maxSearchRadius);

        firePoint.ReduceFireIntensity(damage * falloff * efficiencyMultiplier);
    }

    // Erst das getroffene Objekt prüfen, sonst das nächste brennende Feuer im Umkreis nehmen
    private FirePoint FindFirePoint(GameObject hitObject, Vector3 hitPosition)
    {
        FirePoint hit = hitObject.GetComponentInParent<FirePoint>();
        if (IsBurning(hit))
            return hit;

        if (FirePointManager.Instance == null)
            return null;

        FirePoint nearest = null;
        float closestDistance = maxSearchRadius * maxSearchRadius;

        foreach (FirePoint candidate in FirePointManager.Instance.GetAllFirePoints())
        {
            if (!IsBurning(candidate))
                continue;

            float distance = (candidate.transform.position - hitPosition).sqrMagnitude;
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private static bool IsBurning(FirePoint firePoint)
    {
        return firePoint != null && firePoint.isActiveAndEnabled && firePoint.isIgnited;
    }

    private bool CanExtinguish(FireClass fireClass)
    {
        switch (extinguisherType)
        {
            case ExtinguisherType.KlasseABC: return fireClass == FireClass.A || fireClass == FireClass.B || fireClass == FireClass.C;
            case ExtinguisherType.KlasseA: return fireClass == FireClass.A;
            case ExtinguisherType.KlasseB: return fireClass == FireClass.B;
            case ExtinguisherType.KlasseC: return fireClass == FireClass.C;
            case ExtinguisherType.KlasseD: return fireClass == FireClass.D;
            case ExtinguisherType.KlasseF: return fireClass == FireClass.F;
            default: return false;
        }
    }

    // Cooldown, damit die Warnung nicht jeden Frame neu kommt
    private void ShowWrongTypeWarning()
    {
        if (wrongTypeWarningCanvas == null || Time.time < nextWarningTime)
            return;

        nextWarningTime = Time.time + warningCooldown;
        wrongTypeWarningCanvas.ShowWarning();
    }
}
