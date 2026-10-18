using System.Collections;
using UnityEngine;

public class HoseReturn : MonoBehaviour
{
    [SerializeField] private Transform gripTransform;
    [SerializeField, Min(0.01f)] private float returnDuration = 0.6f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private Coroutine returnRoutine;

    private void Awake()
    {
        if (gripTransform == null)
            gripTransform = transform;

        startLocalPosition = gripTransform.localPosition;
        startLocalRotation = gripTransform.localRotation;
    }

    public void ReturnToRest()
    {
        if (gripTransform == null)
            return;

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        returnRoutine = StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        Vector3 fromPos = gripTransform.localPosition;
        Quaternion fromRot = gripTransform.localRotation;

        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float eased = returnCurve.Evaluate(t);

            Vector3 pos = Vector3.Lerp(fromPos, startLocalPosition, eased);
            Quaternion rot = Quaternion.Slerp(fromRot, startLocalRotation, eased);

            gripTransform.SetLocalPositionAndRotation(pos, rot);
            yield return null;
        }

        gripTransform.SetLocalPositionAndRotation(startLocalPosition, startLocalRotation);
        returnRoutine = null;
    }
}