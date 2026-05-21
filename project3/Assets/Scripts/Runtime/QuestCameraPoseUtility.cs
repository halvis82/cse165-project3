using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

public static class QuestCameraPoseUtility
{
    public static void EnsureHeadTrackedCamera(GameObject cameraObject)
    {
        if (cameraObject == null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var driver = cameraObject.GetComponent<TrackedPoseDriver>() ??
                     cameraObject.AddComponent<TrackedPoseDriver>();
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        driver.ignoreTrackingState = false;

        var position = CreateAction("HMD Position", "<XRHMD>/centerEyePosition", "Vector3");
        var rotation = CreateAction("HMD Rotation", "<XRHMD>/centerEyeRotation", "Quaternion");
        var trackingState = CreateAction("HMD Tracking State", "<XRHMD>/trackingState", "Integer");

        driver.positionInput = new InputActionProperty(position);
        driver.rotationInput = new InputActionProperty(rotation);
        driver.trackingStateInput = new InputActionProperty(trackingState);

        position.Enable();
        rotation.Enable();
        trackingState.Enable();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static InputAction CreateAction(string name, string binding, string expectedControlType)
    {
        var action = new InputAction(
            name,
            InputActionType.Value,
            binding,
            expectedControlType: expectedControlType);
        return action;
    }
#endif
}
