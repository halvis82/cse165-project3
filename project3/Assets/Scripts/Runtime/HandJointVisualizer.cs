using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public sealed class HandJointVisualizer : MonoBehaviour
{
    [SerializeField] private float jointRadiusMeters = 0.018f;
    [SerializeField] private Color leftHandColor = new Color(0.15f, 0.65f, 1f, 0.95f);
    [SerializeField] private Color rightHandColor = new Color(1f, 0.74f, 0.16f, 0.95f);

    private readonly XRHandJointID[] trackedJoints =
    {
        XRHandJointID.Wrist,
        XRHandJointID.Palm,
        XRHandJointID.ThumbTip,
        XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip,
        XRHandJointID.RingTip,
        XRHandJointID.LittleTip
    };

    private readonly Dictionary<XRHandJointID, Transform> leftMarkers = new Dictionary<XRHandJointID, Transform>();
    private readonly Dictionary<XRHandJointID, Transform> rightMarkers = new Dictionary<XRHandJointID, Transform>();

    private Material leftMaterial;
    private Material rightMaterial;

    private void Awake()
    {
        leftMaterial = CreateMaterial(leftHandColor);
        rightMaterial = CreateMaterial(rightHandColor);
        CreateMarkers("Left Joint", leftMarkers, leftMaterial);
        CreateMarkers("Right Joint", rightMarkers, rightMaterial);
    }

    private void LateUpdate()
    {
        if (!HandPoseUtility.TryGetRunningSubsystem(out var subsystem))
        {
            SetVisible(leftMarkers, false);
            SetVisible(rightMarkers, false);
            return;
        }

        UpdateMarkers(subsystem.leftHand, leftMarkers);
        UpdateMarkers(subsystem.rightHand, rightMarkers);
    }

    private void CreateMarkers(
        string prefix,
        Dictionary<XRHandJointID, Transform> markers,
        Material material)
    {
        for (var i = 0; i < trackedJoints.Length; i++)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{prefix} {trackedJoints[i]}";
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * (jointRadiusMeters * 2f);

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            marker.SetActive(false);
            markers.Add(trackedJoints[i], marker.transform);
        }
    }

    private static Material CreateMaterial(Color color)
    {
        return new Material(Shader.Find("Standard"))
        {
            color = color
        };
    }

    private void UpdateMarkers(XRHand hand, Dictionary<XRHandJointID, Transform> markers)
    {
        for (var i = 0; i < trackedJoints.Length; i++)
        {
            var jointId = trackedJoints[i];
            var marker = markers[jointId];
            if (HandPoseUtility.TryGetJointPose(hand, jointId, out var pose))
            {
                marker.gameObject.SetActive(true);
                marker.position = pose.position;
                marker.rotation = pose.rotation;
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }
    }

    private static void SetVisible(Dictionary<XRHandJointID, Transform> markers, bool visible)
    {
        foreach (var marker in markers.Values)
        {
            marker.gameObject.SetActive(visible);
        }
    }
}
