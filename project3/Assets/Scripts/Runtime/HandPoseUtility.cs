using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public static class HandPoseUtility
{
    private static readonly List<XRHandSubsystem> HandSubsystems = new List<XRHandSubsystem>();

    public static bool TryGetRunningSubsystem(out XRHandSubsystem subsystem)
    {
        SubsystemManager.GetSubsystems(HandSubsystems);
        for (var i = 0; i < HandSubsystems.Count; i++)
        {
            if (HandSubsystems[i] != null && HandSubsystems[i].running)
            {
                subsystem = HandSubsystems[i];
                return true;
            }
        }

        subsystem = null;
        return false;
    }

    public static bool TryGetJointPose(XRHand hand, XRHandJointID jointId, out Pose pose)
    {
        return hand.GetJoint(jointId).TryGetPose(out pose);
    }

    public static bool TryGetPinchDistance(XRHand hand, out float distanceMeters)
    {
        distanceMeters = float.PositiveInfinity;
        if (!TryGetJointPose(hand, XRHandJointID.ThumbTip, out var thumbTip) ||
            !TryGetJointPose(hand, XRHandJointID.IndexTip, out var indexTip))
        {
            return false;
        }

        distanceMeters = Vector3.Distance(thumbTip.position, indexTip.position);
        return true;
    }

    public static bool IsThumbIndexPinching(XRHand hand, float pinchDistanceMeters = 0.035f)
    {
        return TryGetPinchDistance(hand, out var distanceMeters) &&
               distanceMeters <= pinchDistanceMeters;
    }

    // Aim ray straight along the hand/arm, regardless of finger pose (finger,
    // fist, open hand - all work). Origin at the knuckles, direction from the
    // wrist toward the middle knuckle, i.e. wherever the hand/arm is aimed.
    public static bool TryGetHandAimRay(XRHand hand, out Ray ray)
    {
        ray = default;

        if (!TryGetJointPose(hand, XRHandJointID.Wrist, out var wrist) ||
            !TryGetJointPose(hand, XRHandJointID.MiddleProximal, out var middleKnuckle))
        {
            return false;
        }

        var forward = middleKnuckle.position - wrist.position;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        ray = new Ray(middleKnuckle.position, forward.normalized);
        return true;
    }

    // Aim ray for a closed fist: all four fingers curled toward the palm. The
    // ray starts at the knuckles and points along the hand's forward axis
    // (wrist -> middle knuckle), i.e. where the front of the fist is aimed.
    public static bool TryGetFistAimRay(XRHand hand, out Ray ray)
    {
        ray = default;

        if (!TryGetJointPose(hand, XRHandJointID.Palm, out var palm) ||
            !TryGetJointPose(hand, XRHandJointID.Wrist, out var wrist) ||
            !TryGetJointPose(hand, XRHandJointID.MiddleProximal, out var middleKnuckle) ||
            !TryGetJointPose(hand, XRHandJointID.IndexTip, out var indexTip) ||
            !TryGetJointPose(hand, XRHandJointID.MiddleTip, out var middleTip) ||
            !TryGetJointPose(hand, XRHandJointID.RingTip, out var ringTip) ||
            !TryGetJointPose(hand, XRHandJointID.LittleTip, out var littleTip))
        {
            return false;
        }

        // All four fingertips pulled in close to the palm => curled fist.
        const float curledThreshold = 0.075f;
        var fistClosed =
            Vector3.Distance(palm.position, indexTip.position) <= curledThreshold &&
            Vector3.Distance(palm.position, middleTip.position) <= curledThreshold &&
            Vector3.Distance(palm.position, ringTip.position) <= curledThreshold &&
            Vector3.Distance(palm.position, littleTip.position) <= curledThreshold;

        if (!fistClosed)
        {
            return false;
        }

        var forward = middleKnuckle.position - wrist.position;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        ray = new Ray(middleKnuckle.position, forward.normalized);
        return true;
    }

    public static bool TryGetPointingRay(XRHand hand, out Ray ray)
    {
        ray = default;

        if (!TryGetJointPose(hand, XRHandJointID.Palm, out var palm) ||
            !TryGetJointPose(hand, XRHandJointID.IndexProximal, out var indexProximal) ||
            !TryGetJointPose(hand, XRHandJointID.IndexTip, out var indexTip) ||
            !TryGetJointPose(hand, XRHandJointID.MiddleTip, out var middleTip) ||
            !TryGetJointPose(hand, XRHandJointID.RingTip, out var ringTip) ||
            !TryGetJointPose(hand, XRHandJointID.LittleTip, out var littleTip))
        {
            return false;
        }

        var indexDirection = indexTip.position - indexProximal.position;
        if (indexDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        var palmToIndex = Vector3.Distance(palm.position, indexTip.position);
        var palmToMiddle = Vector3.Distance(palm.position, middleTip.position);
        var palmToRing = Vector3.Distance(palm.position, ringTip.position);
        var palmToLittle = Vector3.Distance(palm.position, littleTip.position);

        var indexExtended = palmToIndex >= 0.08f;
        var otherFingersFolded =
            palmToMiddle <= palmToIndex * 0.88f &&
            palmToRing <= palmToIndex * 0.88f &&
            palmToLittle <= palmToIndex * 0.88f;

        if (!indexExtended || !otherFingersFolded)
        {
            return false;
        }

        ray = new Ray(indexTip.position, indexDirection.normalized);
        return true;
    }
}
