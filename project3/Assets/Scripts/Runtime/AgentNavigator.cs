using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class AgentNavigator : MonoBehaviour
{
    [Header("Locomotion")]
    [SerializeField] private float moveSpeedMetersPerSecond = 0.75f;
    [SerializeField] private float rotationSpeedDegreesPerSecond = 360f;
    [SerializeField] private float stoppingDistanceMeters = 0.08f;

    [Header("Environment Awareness")]
    [SerializeField] private float collisionPaddingMeters = 0.04f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private CharacterController characterController;
    private Vector3 destination;
    private bool hasDestination;
    private float blockedTimer;

    public event Action<Vector3> DestinationAccepted;
    public event Action<Vector3> DestinationReached;
    public event Action DestinationBlocked;

    public bool HasDestination => hasDestination;
    public bool IsMoving { get; private set; }
    public bool IsBlocked { get; private set; }
    public Vector3 Destination => destination;
    public float CurrentSpeedMetersPerSecond { get; private set; }
    public string NavigationState { get; private set; } = "Idle";

    public void ConfigureObstacleMask(LayerMask mask)
    {
        obstacleMask = mask;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void SetDestination(Vector3 worldPoint)
    {
        destination = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);
        hasDestination = true;
        IsBlocked = false;
        NavigationState = "Moving";
        DestinationAccepted?.Invoke(destination);
    }

    public void ClearDestination()
    {
        hasDestination = false;
        IsMoving = false;
        IsBlocked = false;
        CurrentSpeedMetersPerSecond = 0f;
        NavigationState = "Idle";
    }

    private void Update()
    {
        CurrentSpeedMetersPerSecond = 0f;
        IsMoving = false;

        if (!hasDestination || characterController == null)
        {
            return;
        }

        var toDestination = destination - transform.position;
        toDestination.y = 0f;

        if (toDestination.magnitude <= stoppingDistanceMeters)
        {
            hasDestination = false;
            IsBlocked = false;
            NavigationState = "Arrived";
            DestinationReached?.Invoke(destination);
            return;
        }

        var direction = toDestination.normalized;
        RotateToward(direction);

        var desiredDistance = Mathf.Min(moveSpeedMetersPerSecond * Time.deltaTime, toDestination.magnitude);
        var allowedDistance = GetAllowedTravelDistance(direction, desiredDistance);
        if (allowedDistance <= 0f)
        {
            hasDestination = false;
            IsBlocked = true;
            NavigationState = "Blocked by room surface";
            DestinationBlocked?.Invoke();
            return;
        }

        var positionBeforeMove = transform.position;
        var flags = characterController.Move(direction * allowedDistance);

        // The CharacterController physically stops at wall colliders even when
        // the look-ahead sphere cast missed (SphereCast ignores a wall it is
        // already touching). Detect that case by comparing intended vs actual
        // travel: if he is pressed against a wall and not progressing, stop so
        // he does not run in place forever.
        var actualTravel = Vector3.ProjectOnPlane(transform.position - positionBeforeMove, Vector3.up).magnitude;
        var hitSide = (flags & CollisionFlags.Sides) != 0;
        if ((hitSide || actualTravel < allowedDistance * 0.25f) && allowedDistance > 0.0005f)
        {
            blockedTimer += Time.deltaTime;
        }
        else
        {
            blockedTimer = 0f;
        }

        if (blockedTimer >= 0.2f)
        {
            blockedTimer = 0f;
            hasDestination = false;
            IsBlocked = true;
            IsMoving = false;
            CurrentSpeedMetersPerSecond = 0f;
            NavigationState = "Blocked by room surface";
            DestinationBlocked?.Invoke();
            return;
        }

        CurrentSpeedMetersPerSecond = Time.deltaTime > 0f ? actualTravel / Time.deltaTime : 0f;
        IsMoving = CurrentSpeedMetersPerSecond > 0.01f;
        NavigationState = "Moving";
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeedDegreesPerSecond * Time.deltaTime);
    }

    private float GetAllowedTravelDistance(Vector3 direction, float desiredDistance)
    {
        var center = transform.TransformPoint(characterController.center);
        var radius = Mathf.Max(0.01f, characterController.radius * 0.95f);
        var hits = Physics.SphereCastAll(
            center,
            radius,
            direction,
            desiredDistance + collisionPaddingMeters,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        var nearestDistance = float.PositiveInfinity;
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null ||
                hit.collider.transform.IsChildOf(transform) ||
                hit.collider.GetComponentInParent<SpatialSurfaceProxy>() == null)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
            }
        }

        if (float.IsPositiveInfinity(nearestDistance))
        {
            return desiredDistance;
        }

        return Mathf.Max(0f, Mathf.Min(desiredDistance, nearestDistance - collisionPaddingMeters));
    }
}
