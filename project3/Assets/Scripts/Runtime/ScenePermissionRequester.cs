using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// Requests the Quest runtime permissions that gate spatial (room) data and
// hand tracking. On Quest, com.oculus.permission.USE_SCENE must be granted at
// runtime before ARPlaneManager surfaces any real room walls/floor; declaring
// it in the manifest is not enough. Without this the plane subsystem returns
// zero trackables, which is why the project previously fell back to fake walls.
public sealed class ScenePermissionRequester : MonoBehaviour
{
    public const string ScenePermission = "com.oculus.permission.USE_SCENE";
    public const string HandTrackingPermission = "com.oculus.permission.HAND_TRACKING";

    private SpatialAnchorSurfaceAuthoring surfaceAuthoring;
    private bool sceneGranted;

    public bool SceneGranted => sceneGranted;

    public void Configure(SpatialAnchorSurfaceAuthoring authoring)
    {
        surfaceAuthoring = authoring;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void Start()
    {
        RequestPermission(HandTrackingPermission);
        RequestScenePermission();
    }

    private void RequestScenePermission()
    {
        if (Permission.HasUserAuthorizedPermission(ScenePermission))
        {
            OnSceneGranted(ScenePermission);
            return;
        }

        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += OnSceneGranted;
        callbacks.PermissionDenied += OnSceneDenied;
        Permission.RequestUserPermission(ScenePermission, callbacks);
    }

    private static void RequestPermission(string permission)
    {
        if (!Permission.HasUserAuthorizedPermission(permission))
        {
            Permission.RequestUserPermission(permission);
        }
    }

    private void OnSceneGranted(string permission)
    {
        sceneGranted = true;
        Debug.Log($"Project3 scene permission granted: {permission}. Rescanning room planes.");
        if (surfaceAuthoring != null)
        {
            surfaceAuthoring.Rescan();
        }
    }

    private void OnSceneDenied(string permission)
    {
        Debug.LogWarning($"Project3 scene permission denied: {permission}. Real room walls will not appear.");
    }
#else
    private void Start()
    {
        sceneGranted = true;
    }
#endif
}
