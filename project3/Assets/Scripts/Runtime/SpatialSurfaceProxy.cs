using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public enum SpatialSurfaceKind
{
    Floor,
    Wall
}

public sealed class SpatialSurfaceProxy : MonoBehaviour
{
    [SerializeField] private SpatialSurfaceKind kind;
    [SerializeField] private TrackableId sourcePlaneId;

    public SpatialSurfaceKind Kind => kind;
    public TrackableId SourcePlaneId => sourcePlaneId;

    public void Configure(SpatialSurfaceKind surfaceKind, TrackableId planeId)
    {
        kind = surfaceKind;
        sourcePlaneId = planeId;
    }
}
