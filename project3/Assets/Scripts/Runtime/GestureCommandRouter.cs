using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Hands;

public sealed class GestureCommandRouter : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private AgentNavigator agent;
    [SerializeField] private Transform destinationMarker;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARPlaneManager arPlaneManager;

    [Header("Gesture Recognition")]
    [SerializeField] private float pinchEnterDistanceMeters = 0.03f;
    [SerializeField] private float pinchExitDistanceMeters = 0.045f;
    [SerializeField] private float commandCooldownSeconds = 0.35f;

    [Header("Destination Resolution")]
    [SerializeField] private LayerMask physicsSurfaceMask = ~0;
    [SerializeField] private float maximumAimDistanceMeters = 4f;
#if UNITY_EDITOR
    [SerializeField] private float editorMarkerSpeedMetersPerSecond = 1.25f;
#endif

    private readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();
    private bool leftPinchLatched;
    private bool hasAimedDestination;
    private float cooldownRemaining;
    private Vector3 aimedDestination;

    public bool HandsTracked { get; private set; }
    public bool HasPointingRay { get; private set; }
    public bool HasAimedDestination => hasAimedDestination;
    public bool LeftPinchActive => leftPinchLatched;
    public Vector3 AimedDestination => aimedDestination;
    public string InputStatus { get; private set; } = "Waiting for hands";

    public void Configure(
        AgentNavigator navigator,
        Transform marker,
        LineRenderer line,
        ARRaycastManager raycasts,
        ARPlaneManager planes)
    {
        agent = navigator;
        destinationMarker = marker;
        aimLine = line;
        arRaycastManager = raycasts;
        arPlaneManager = planes;
    }

    private void Update()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
        }

        if (HandPoseUtility.TryGetRunningSubsystem(out var subsystem))
        {
            UpdateFromHands(subsystem);
            return;
        }

        HandsTracked = false;
        HasPointingRay = false;
        leftPinchLatched = false;
        InputStatus = "No tracked hands";
        SetAimVisuals(false, default, default);

#if UNITY_EDITOR
        UpdateEditorFallback();
#endif
    }

    private void UpdateFromHands(XRHandSubsystem subsystem)
    {
        HandsTracked = subsystem.leftHand.isTracked && subsystem.rightHand.isTracked;
        HasPointingRay = HandPoseUtility.TryGetHandAimRay(subsystem.rightHand, out var aimRay);
        hasAimedDestination = false;

        if (HasPointingRay)
        {
            hasAimedDestination = TryResolveDestination(aimRay, out aimedDestination);
            SetAimVisuals(hasAimedDestination, aimRay.origin, hasAimedDestination ? aimedDestination : aimRay.GetPoint(maximumAimDistanceMeters));
        }
        else
        {
            SetAimVisuals(false, default, default);
        }

        var pinchDistance = HandPoseUtility.TryGetPinchDistance(subsystem.leftHand, out var distance)
            ? distance
            : float.PositiveInfinity;

        if (!leftPinchLatched && pinchDistance <= pinchEnterDistanceMeters)
        {
            leftPinchLatched = true;
            if (cooldownRemaining <= 0f && hasAimedDestination && agent != null)
            {
                agent.SetDestination(aimedDestination);
                cooldownRemaining = commandCooldownSeconds;
                InputStatus = "Command sent";
            }
        }
        else if (leftPinchLatched && pinchDistance >= pinchExitDistanceMeters)
        {
            leftPinchLatched = false;
        }

        if (!HandsTracked)
        {
            InputStatus = "Hands not fully tracked";
        }
        else if (!HasPointingRay)
        {
            InputStatus = "Aim your right hand";
        }
        else if (!hasAimedDestination)
        {
            InputStatus = "Aim at a floor surface";
        }
        else if (!leftPinchLatched)
        {
            InputStatus = "Pinch left thumb + index to move";
        }
    }

    private bool TryResolveDestination(Ray aimRay, out Vector3 destination)
    {
        if (arRaycastManager != null &&
            arRaycastManager.Raycast(aimRay, arHits, TrackableType.PlaneWithinPolygon))
        {
            for (var i = 0; i < arHits.Count; i++)
            {
                var plane = arPlaneManager != null ? arPlaneManager.GetPlane(arHits[i].trackableId) : null;
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    destination = arHits[i].pose.position;
                    return true;
                }
            }
        }

        if (Physics.Raycast(
                aimRay,
                out var hit,
                maximumAimDistanceMeters,
                physicsSurfaceMask,
                QueryTriggerInteraction.Ignore) &&
            hit.normal.y >= 0.35f)
        {
            destination = hit.point;
            return true;
        }

        var floorY = agent != null ? agent.transform.position.y : 0f;
        var floorPlane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
        if (floorPlane.Raycast(aimRay, out var enter) && enter <= maximumAimDistanceMeters)
        {
            destination = aimRay.GetPoint(enter);
            return true;
        }

        destination = default;
        return false;
    }

    private void SetAimVisuals(bool visible, Vector3 start, Vector3 end)
    {
        if (destinationMarker != null)
        {
            destinationMarker.gameObject.SetActive(visible);
            if (visible)
            {
                destinationMarker.position = aimedDestination;
            }
        }

        if (aimLine != null)
        {
            aimLine.enabled = visible;
            if (visible)
            {
                aimLine.positionCount = 2;
                aimLine.SetPosition(0, start);
                aimLine.SetPosition(1, end);
            }
        }
    }

#if UNITY_EDITOR
    private void UpdateEditorFallback()
    {
        if (destinationMarker == null || agent == null)
        {
            return;
        }

        if (!destinationMarker.gameObject.activeSelf)
        {
            destinationMarker.gameObject.SetActive(true);
        }

        var input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical"));

        if (input.sqrMagnitude > 0.0001f)
        {
            destinationMarker.position += input.normalized * editorMarkerSpeedMetersPerSecond * Time.deltaTime;
        }

        hasAimedDestination = true;
        aimedDestination = destinationMarker.position;
        InputStatus = "Editor fallback: WASD + Space";

        if (Input.GetKeyDown(KeyCode.Space))
        {
            agent.SetDestination(destinationMarker.position);
        }
    }
#endif
}
