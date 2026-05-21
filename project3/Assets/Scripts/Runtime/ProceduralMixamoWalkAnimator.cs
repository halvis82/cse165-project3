using System;
using UnityEngine;

public sealed class ProceduralMixamoWalkAnimator : MonoBehaviour
{
    [SerializeField] private AgentNavigator navigator;
    [SerializeField] private float stepFrequency = 1.85f;
    [SerializeField] private float legSwingDegrees = 24f;
    [SerializeField] private float kneeSwingDegrees = 28f;
    [SerializeField] private float armSwingDegrees = 18f;
    [SerializeField] private float hipBobMeters = 0.035f;
    [SerializeField] private float blendSpeed = 10f;

    private Transform hips;
    private Transform spine;
    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightLowerLeg;
    private Transform leftFoot;
    private Transform rightFoot;
    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftForeArm;
    private Transform rightForeArm;

    private Vector3 hipsRestLocalPosition;
    private Quaternion hipsRestRotation;
    private Quaternion spineRestRotation;
    private Quaternion leftUpperLegRestRotation;
    private Quaternion rightUpperLegRestRotation;
    private Quaternion leftLowerLegRestRotation;
    private Quaternion rightLowerLegRestRotation;
    private Quaternion leftFootRestRotation;
    private Quaternion rightFootRestRotation;
    private Quaternion leftUpperArmRestRotation;
    private Quaternion rightUpperArmRestRotation;
    private Quaternion leftForeArmRestRotation;
    private Quaternion rightForeArmRestRotation;

    private float phase;
    private float weight;
    private bool cached;

    public void Configure(AgentNavigator agentNavigator)
    {
        navigator = agentNavigator;
    }

    private void Awake()
    {
        CacheBones();
    }

    private void LateUpdate()
    {
        if (!cached)
        {
            CacheBones();
        }

        if (hips == null)
        {
            return;
        }

        var moving = navigator != null && navigator.IsMoving;
        var targetWeight = moving ? 1f : 0f;
        weight = Mathf.MoveTowards(weight, targetWeight, blendSpeed * Time.deltaTime);

        if (moving)
        {
            phase += Time.deltaTime * stepFrequency * Mathf.PI * 2f;
        }

        ApplyPose(weight);
    }

    private void CacheBones()
    {
        hips = FindBone("Hips");
        spine = FindBone("Spine");
        leftUpperLeg = FindBone("LeftUpLeg", "LeftUpperLeg");
        rightUpperLeg = FindBone("RightUpLeg", "RightUpperLeg");
        leftLowerLeg = FindBone("LeftLeg", "LeftLowerLeg");
        rightLowerLeg = FindBone("RightLeg", "RightLowerLeg");
        leftFoot = FindBone("LeftFoot");
        rightFoot = FindBone("RightFoot");
        leftUpperArm = FindBone("LeftArm", "LeftUpperArm");
        rightUpperArm = FindBone("RightArm", "RightUpperArm");
        leftForeArm = FindBone("LeftForeArm", "LeftLowerArm");
        rightForeArm = FindBone("RightForeArm", "RightLowerArm");

        if (hips == null)
        {
            return;
        }

        hipsRestLocalPosition = hips.localPosition;
        hipsRestRotation = hips.localRotation;
        spineRestRotation = Rest(spine);
        leftUpperLegRestRotation = Rest(leftUpperLeg);
        rightUpperLegRestRotation = Rest(rightUpperLeg);
        leftLowerLegRestRotation = Rest(leftLowerLeg);
        rightLowerLegRestRotation = Rest(rightLowerLeg);
        leftFootRestRotation = Rest(leftFoot);
        rightFootRestRotation = Rest(rightFoot);
        leftUpperArmRestRotation = Rest(leftUpperArm);
        rightUpperArmRestRotation = Rest(rightUpperArm);
        leftForeArmRestRotation = Rest(leftForeArm);
        rightForeArmRestRotation = Rest(rightForeArm);
        cached = true;
    }

    private void ApplyPose(float poseWeight)
    {
        var left = Mathf.Sin(phase);
        var right = -left;
        var bob = Mathf.Abs(Mathf.Sin(phase * 2f)) * hipBobMeters;

        hips.localPosition = Vector3.Lerp(
            hips.localPosition,
            hipsRestLocalPosition + Vector3.up * (bob * poseWeight),
            poseWeight > 0f ? 0.85f : 0.25f);

        ApplyRotation(hips, hipsRestRotation, Quaternion.Euler(0f, Mathf.Sin(phase) * 2f, 0f), poseWeight);
        ApplyRotation(spine, spineRestRotation, Quaternion.Euler(0f, -Mathf.Sin(phase) * 2f, 0f), poseWeight);

        ApplyRotation(leftUpperLeg, leftUpperLegRestRotation, Quaternion.Euler(left * legSwingDegrees, 0f, 0f), poseWeight);
        ApplyRotation(rightUpperLeg, rightUpperLegRestRotation, Quaternion.Euler(right * legSwingDegrees, 0f, 0f), poseWeight);
        ApplyRotation(leftLowerLeg, leftLowerLegRestRotation, Quaternion.Euler(Mathf.Max(0f, -left) * kneeSwingDegrees, 0f, 0f), poseWeight);
        ApplyRotation(rightLowerLeg, rightLowerLegRestRotation, Quaternion.Euler(Mathf.Max(0f, -right) * kneeSwingDegrees, 0f, 0f), poseWeight);
        ApplyRotation(leftFoot, leftFootRestRotation, Quaternion.Euler(Mathf.Max(0f, left) * 10f, 0f, 0f), poseWeight);
        ApplyRotation(rightFoot, rightFootRestRotation, Quaternion.Euler(Mathf.Max(0f, right) * 10f, 0f, 0f), poseWeight);

        ApplyRotation(leftUpperArm, leftUpperArmRestRotation, Quaternion.Euler(right * armSwingDegrees, 0f, 0f), poseWeight);
        ApplyRotation(rightUpperArm, rightUpperArmRestRotation, Quaternion.Euler(left * armSwingDegrees, 0f, 0f), poseWeight);
        ApplyRotation(leftForeArm, leftForeArmRestRotation, Quaternion.Euler(Mathf.Abs(right) * 7f, 0f, 0f), poseWeight);
        ApplyRotation(rightForeArm, rightForeArmRestRotation, Quaternion.Euler(Mathf.Abs(left) * 7f, 0f, 0f), poseWeight);
    }

    private static Quaternion Rest(Transform bone)
    {
        return bone != null ? bone.localRotation : Quaternion.identity;
    }

    private static void ApplyRotation(Transform bone, Quaternion rest, Quaternion offset, float poseWeight)
    {
        if (bone == null)
        {
            return;
        }

        bone.localRotation = Quaternion.Slerp(rest, rest * offset, poseWeight);
    }

    private Transform FindBone(params string[] suffixes)
    {
        var children = GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < children.Length; i++)
        {
            var childName = children[i].name;
            for (var j = 0; j < suffixes.Length; j++)
            {
                if (childName.EndsWith(suffixes[j], StringComparison.OrdinalIgnoreCase) ||
                    childName.EndsWith(":" + suffixes[j], StringComparison.OrdinalIgnoreCase))
                {
                    return children[i];
                }
            }
        }

        return null;
    }
}
