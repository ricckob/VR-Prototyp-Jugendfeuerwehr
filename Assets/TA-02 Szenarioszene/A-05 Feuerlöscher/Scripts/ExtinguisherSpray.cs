using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class ExtinguisherSpray : MonoBehaviour, IHandGrabUseDelegate
{
    [Header("Hebel")]
    [SerializeField] private Transform trigger;
    [SerializeField] private Grabbable triggerGrabbable;
    [SerializeField] private float maxAngle = 20f;

    [Header("DÃ¼se")]
    [SerializeField] private Grabbable hoseGrabbable;

    [Header("Partikel und Ton")]
    [SerializeField] private ExtinguisherSprayParticles sprayVFX;

    [Header("LÃ¶schmittel")]
    [SerializeField] private ExtinguisherAgent extinguisherAgent;

    [Header("Tutorial")]
    [SerializeField] private TutorialStepToggle sprayStep;
    [SerializeField] private bool completeOnFirstSpray = true;

    [Header("Training")]
    [SerializeField] private ExtinguishingEffect extinguishingEffect;
    [SerializeField] private VRWarningCanvas longPressWarningCanvas;
    [SerializeField] private float burstMaxDuration = 1f;
    [SerializeField] private float longPressWarningAfter = 3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs;

    private Vector3 startRotation;
    private bool isSpraying;
    private bool hasCompletedSprayStep;
    private float currentSprayDuration;
    private bool longPressActive;

    private void Awake()
    {
        if (trigger != null)
            startRotation = trigger.localEulerAngles;
    }

    public void BeginUse() { }

    public void EndUse()
    {
        RotateTrigger(0f);
        StopSpray();
        ResetTrainingState();
    }

    public float ComputeUseStrength(float strength)
    {
        if (!IsTriggerGrabbed())
        {
            RotateTrigger(0f);
            StopSpray();
            ResetTrainingState();
            return 0f;
        }

        float clampedStrength = Mathf.Clamp01(strength);
        RotateTrigger(clampedStrength);

        bool wantsToSpray = clampedStrength > 0f && IsHoseGrabbed();
        bool hasAgent = ConsumeAgentIfNeeded(wantsToSpray);
        bool shouldSpray = wantsToSpray && hasAgent;

        if (shouldSpray)
            StartOrUpdateSpray(clampedStrength);
        else
            StopSpray();

        UpdateTrainingState(shouldSpray);
        CompleteTutorialStepIfNeeded(shouldSpray);

        return clampedStrength;
    }

    private bool IsTriggerGrabbed()
    {
        return triggerGrabbable != null && triggerGrabbable.SelectingPointsCount > 0;
    }

    private bool IsHoseGrabbed()
    {
        return hoseGrabbable != null && hoseGrabbable.SelectingPointsCount > 0;
    }

    private bool ConsumeAgentIfNeeded(bool wantsToSpray)
    {
        if (!wantsToSpray || extinguisherAgent == null)
            return true;

        return extinguisherAgent.Consume(Time.deltaTime);
    }

    private void StartOrUpdateSpray(float strength)
    {
        if (!isSpraying && showDebugLogs)
            Debug.Log("[ExtinguisherSpray] Spray START");

        isSpraying = true;

        if (sprayVFX != null)
            sprayVFX.Play(strength);
    }

    private void StopSpray()
    {
        if (isSpraying && showDebugLogs)
            Debug.Log("[ExtinguisherSpray] Spray STOP");

        isSpraying = false;

        if (sprayVFX != null)
            sprayVFX.Stop();
    }

    private void RotateTrigger(float progress)
    {
        if (trigger == null)
            return;

        float targetX = startRotation.x - maxAngle * progress;
        trigger.localRotation = Quaternion.Euler(targetX, startRotation.y, startRotation.z);
    }

    private void CompleteTutorialStepIfNeeded(bool shouldSpray)
    {
        if (!shouldSpray || sprayStep == null || !completeOnFirstSpray || hasCompletedSprayStep)
            return;

        hasCompletedSprayStep = true;
        sprayStep.MarkCompleted();
    }

private void UpdateTrainingState(bool shouldSpray)
{
    if (extinguishingEffect == null)
        return;

    if (!shouldSpray)
    {
        ResetTrainingState();
        return;
    }

    currentSprayDuration += Time.deltaTime;

    bool burstBonus = currentSprayDuration <= burstMaxDuration;
    bool longPressPenalty = currentSprayDuration >= longPressWarningAfter;

    extinguishingEffect.SetTrainingEfficiency(burstBonus, longPressPenalty);

    if (longPressPenalty && !longPressActive)
    {
        longPressActive = true;
        longPressWarningCanvas?.ShowWarning();
    }
}

    private void ResetTrainingState()
    {
        currentSprayDuration = 0f;
        longPressActive = false;

        if (extinguishingEffect != null)
            extinguishingEffect.SetTrainingEfficiency(false, false);
    }

    public void OnGrabbed() { }

    public void OnReleased()
    {
        RotateTrigger(0f);
        StopSpray();
        ResetTrainingState();
    }
}
