using UnityEngine;

public sealed class Project3StatusPanel : MonoBehaviour
{
    [SerializeField] private Camera viewerCamera;
    [SerializeField] private GestureCommandRouter gestures;
    [SerializeField] private SpatialAnchorSurfaceAuthoring surfaces;
    [SerializeField] private AgentNavigator agent;
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private Vector3 viewportOffset = new Vector3(-0.58f, 0.38f, 1.2f);

    public void Configure(Camera camera, GestureCommandRouter gestureRouter, SpatialAnchorSurfaceAuthoring surfaceAuthoring, AgentNavigator navigator, TextMesh mesh)
    {
        viewerCamera = camera;
        gestures = gestureRouter;
        surfaces = surfaceAuthoring;
        agent = navigator;
        textMesh = mesh;
    }

    private void LateUpdate()
    {
        if (viewerCamera == null || textMesh == null)
        {
            return;
        }

        transform.position = viewerCamera.transform.TransformPoint(viewportOffset);
        transform.rotation = Quaternion.LookRotation(
            transform.position - viewerCamera.transform.position,
            viewerCamera.transform.up);

        var floorCount = surfaces != null ? surfaces.FloorCount : 0;
        var wallCount = surfaces != null ? surfaces.WallCount : 0;
        var gestureState = gestures != null ? gestures.InputStatus : "No gesture router";
        var agentState = agent != null ? agent.NavigationState : "No agent";

        textMesh.text =
            $"Surfaces: floor {floorCount}, walls {wallCount}\n" +
            $"Gesture: {gestureState}\n" +
            $"Agent: {agentState}";
    }
}
