using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class SpatialAnchorSurfaceAuthoring : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material anchorMaterial;
    [SerializeField] private float minimumPlaneAreaSquareMeters = 0.35f;
    [SerializeField] private int maximumWallCount = 16;
    [SerializeField] private bool autoCaptureFloor = true;
    [SerializeField] private bool autoCaptureWalls = true;

    private readonly HashSet<TrackableId> capturedPlanes = new HashSet<TrackableId>();
    private readonly List<SpatialSurfaceProxy> capturedSurfaces = new List<SpatialSurfaceProxy>();
    private bool floorCaptured;
    private int wallCount;

    public event Action<SpatialSurfaceProxy> SurfaceCaptured;

    public int FloorCount => floorCaptured ? 1 : 0;
    public int WallCount => wallCount;
    public IReadOnlyList<SpatialSurfaceProxy> CapturedSurfaces => capturedSurfaces;

    public void Configure(
        ARPlaneManager planes,
        ARAnchorManager anchors,
        Material floor,
        Material wall,
        Material anchor)
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= HandlePlanesChanged;
        }

        planeManager = planes;
        anchorManager = anchors;
        floorMaterial = floor;
        wallMaterial = wall;
        anchorMaterial = anchor;
        maximumWallCount = Mathf.Max(maximumWallCount, 16);

        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            if (isActiveAndEnabled)
            {
                planeManager.planesChanged += HandlePlanesChanged;
            }
        }
    }

    // Re-evaluate every currently tracked plane. Called after the USE_SCENE
    // permission is granted, since planes that existed before the grant are
    // not re-delivered through planesChanged.
    public void Rescan()
    {
        if (planeManager == null)
        {
            return;
        }

        foreach (var plane in planeManager.trackables)
        {
            TryCapturePlane(plane);
        }
    }

    private void Awake()
    {
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
        }
    }

    private void OnEnable()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged += HandlePlanesChanged;
        }
    }

    private void OnDisable()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= HandlePlanesChanged;
        }
    }

    private void Start()
    {
        if (planeManager == null)
        {
            return;
        }

        foreach (var plane in planeManager.trackables)
        {
            TryCapturePlane(plane);
        }
    }

    public bool TryCapturePlane(ARPlane plane)
    {
        if (!CanCapturePlane(plane, out var kind))
        {
            return false;
        }

        var localCenter = new Vector3(plane.center.x, 0f, plane.center.y);
        var worldCenter = plane.transform.TransformPoint(localCenter);
        var anchorPose = new Pose(worldCenter, plane.transform.rotation);
        var anchor = CreateAnchor(plane, anchorPose);

        // If the anchor service is unavailable, still build the collider and
        // parent it to the plane itself so the room surface is never dropped.
        // The anchor sits at the plane center, so its local offset is zero;
        // the plane transform sits at its own origin, so offset to the center.
        var anchored = anchor != null;
        var proxyRoot = anchored ? anchor.transform : plane.transform;
        var localOffset = anchored ? Vector3.zero : localCenter;
        var proxy = CreateProxySurface(proxyRoot, localOffset, plane, kind);
        capturedPlanes.Add(plane.trackableId);
        capturedSurfaces.Add(proxy);

        if (kind == SpatialSurfaceKind.Floor)
        {
            floorCaptured = true;
        }
        else
        {
            wallCount++;
        }

        SurfaceCaptured?.Invoke(proxy);
        return true;
    }

    public void RegisterFallbackSurface(SpatialSurfaceProxy proxy)
    {
        if (proxy == null || capturedSurfaces.Contains(proxy))
        {
            return;
        }

        capturedSurfaces.Add(proxy);

        if (proxy.Kind == SpatialSurfaceKind.Floor)
        {
            floorCaptured = true;
        }
        else
        {
            wallCount++;
        }

        SurfaceCaptured?.Invoke(proxy);
    }

    private void HandlePlanesChanged(ARPlanesChangedEventArgs args)
    {
        for (var i = 0; i < args.added.Count; i++)
        {
            TryCapturePlane(args.added[i]);
        }

        for (var i = 0; i < args.updated.Count; i++)
        {
            TryCapturePlane(args.updated[i]);
        }
    }

    private bool CanCapturePlane(ARPlane plane, out SpatialSurfaceKind kind)
    {
        kind = SpatialSurfaceKind.Floor;

        if (plane == null ||
            capturedPlanes.Contains(plane.trackableId) ||
            plane.size.x * plane.size.y < minimumPlaneAreaSquareMeters)
        {
            return false;
        }

        var isHorizontal = plane.alignment == PlaneAlignment.HorizontalUp;
        var isVertical = plane.alignment == PlaneAlignment.Vertical;

        if (isHorizontal)
        {
            kind = SpatialSurfaceKind.Floor;
            return autoCaptureFloor && !floorCaptured;
        }

        if (isVertical)
        {
            kind = SpatialSurfaceKind.Wall;
            return autoCaptureWalls && wallCount < maximumWallCount;
        }

        return false;
    }

    private ARAnchor CreateAnchor(ARPlane plane, Pose pose)
    {
        if (anchorManager == null)
        {
            return null;
        }

        var anchor = anchorManager.AttachAnchor(plane, pose);
        if (anchor != null)
        {
            return anchor;
        }

#pragma warning disable 618
        return anchorManager.AddAnchor(pose);
#pragma warning restore 618
    }

    private SpatialSurfaceProxy CreateProxySurface(
        Transform anchorRoot,
        Vector3 localOffset,
        ARPlane plane,
        SpatialSurfaceKind kind)
    {
        var proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        proxy.name = kind == SpatialSurfaceKind.Floor ? "Anchored Floor Proxy" : "Anchored Wall Proxy";
        proxy.transform.SetParent(anchorRoot, false);
        proxy.transform.localPosition = localOffset;
        proxy.transform.localRotation = Quaternion.identity;
        // Walls are thickened to 0.12 m so the CharacterController (skin width
        // ~0.08 m) collides solidly instead of tunneling through a thin slab.
        var thickness = kind == SpatialSurfaceKind.Wall ? 0.12f : 0.02f;
        proxy.transform.localScale = new Vector3(
            Mathf.Max(0.05f, plane.size.x),
            thickness,
            Mathf.Max(0.05f, plane.size.y));

        var surfaceProxy = proxy.AddComponent<SpatialSurfaceProxy>();
        surfaceProxy.Configure(kind, plane.trackableId);

        var renderer = proxy.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = kind == SpatialSurfaceKind.Floor ? floorMaterial : wallMaterial;
        }

        var anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        anchorMarker.name = "Anchor Marker";
        anchorMarker.transform.SetParent(anchorRoot, false);
        anchorMarker.transform.localPosition = localOffset;
        anchorMarker.transform.localScale = Vector3.one * 0.06f;

        var markerCollider = anchorMarker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        var markerRenderer = anchorMarker.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.sharedMaterial = anchorMaterial;
        }

        return surfaceProxy;
    }
}
