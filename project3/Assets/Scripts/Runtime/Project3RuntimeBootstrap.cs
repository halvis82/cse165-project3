using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public static class Project3RuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureProject3Scene()
    {
        var existingAgent = Object.FindObjectOfType<AgentNavigator>();
        if (existingAgent != null &&
            Object.FindObjectOfType<SpatialAnchorSurfaceAuthoring>() != null &&
            Object.FindObjectOfType<GestureCommandRouter>() != null)
        {
            EnsureProceduralWalk(existingAgent);
            return;
        }

        var session = EnsureComponent<ARSession>("AR Session");
        _ = session;

        var originObject = GameObject.Find("XR Origin") ?? new GameObject("XR Origin");
        var xrOrigin = originObject.GetComponent<XROrigin>() ?? originObject.AddComponent<XROrigin>();

        var cameraOffset = FindOrCreateChild(originObject.transform, "Camera Offset");
        cameraOffset.localPosition = Vector3.zero;
        cameraOffset.localRotation = Quaternion.identity;
        cameraOffset.localScale = Vector3.one;
        xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

        var cameraObject = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraOffset, false);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;
        cameraObject.transform.localScale = Vector3.one;
        QuestCameraPoseUtility.EnsureHeadTrackedCamera(cameraObject);
        var camera = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
        if (cameraObject.GetComponent<AudioListener>() == null) cameraObject.AddComponent<AudioListener>();
        if (cameraObject.GetComponent<ARCameraManager>() == null) cameraObject.AddComponent<ARCameraManager>();
        if (cameraObject.GetComponent<ARCameraBackground>() == null) cameraObject.AddComponent<ARCameraBackground>();
        xrOrigin.Camera = camera;

        var planeManager = originObject.GetComponent<ARPlaneManager>() ?? originObject.AddComponent<ARPlaneManager>();
        var anchorManager = originObject.GetComponent<ARAnchorManager>() ?? originObject.AddComponent<ARAnchorManager>();
        var raycastManager = originObject.GetComponent<ARRaycastManager>() ?? originObject.AddComponent<ARRaycastManager>();

        var materialSet = RuntimeMaterialSet.Create();
        var agent = CreateAgent(materialSet.Agent);
        var destinationMarker = CreateDestinationMarker(materialSet.Target);
        var aimLine = CreateAimLine(materialSet.Target);

        var surfaceAuthoringObject = new GameObject("Spatial Anchor Surface Authoring");
        var surfaceAuthoring = surfaceAuthoringObject.AddComponent<SpatialAnchorSurfaceAuthoring>();
        surfaceAuthoring.Configure(planeManager, anchorManager, materialSet.Floor, materialSet.Wall, materialSet.Anchor);

        var gestureObject = new GameObject("Gesture Command Router");
        var gestureRouter = gestureObject.AddComponent<GestureCommandRouter>();
        gestureRouter.Configure(agent, destinationMarker.transform, aimLine, raycastManager, planeManager);

        var handDebug = new GameObject("Tracked Hand Joint Visualizer");
        handDebug.AddComponent<HandJointVisualizer>();

        CreateStatusPanel(camera, gestureRouter, surfaceAuthoring, agent);

#if UNITY_EDITOR
        CreateEditorTestRoom(materialSet.EditorRoom);
#endif
    }

    private static T EnsureComponent<T>(string objectName) where T : Component
    {
        var gameObject = GameObject.Find(objectName) ?? new GameObject(objectName);
        return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null) return child;
        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static AgentNavigator CreateAgent(Material fallbackMaterial)
    {
        var root = new GameObject("Embodied Agent");
        root.transform.position = new Vector3(0f, 0f, 1.5f);

        var controller = root.AddComponent<CharacterController>();
        controller.height = 1.6f;
        controller.radius = 0.22f;
        controller.center = new Vector3(0f, 0.8f, 0f);

        var navigator = root.AddComponent<AgentNavigator>();
        var mixamoCharacter = Resources.Load<GameObject>("MixamoBeetlejuice/Models/BeetleJuiceMixamo");
        var prototype = Resources.Load<GameObject>("PrototypeHumanoid/Character/Models/DefaultMale");
        var runtimeController = Resources.Load<RuntimeAnimatorController>("PrototypeHumanoid/Animation/zJog_SM");

        if (mixamoCharacter != null)
        {
            var visual = Object.Instantiate(mixamoCharacter, root.transform);
            visual.name = "Mixamo Beetlejuice Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var materialBinder = visual.GetComponent<MixamoCharacterMaterialBinder>() ?? visual.AddComponent<MixamoCharacterMaterialBinder>();
            materialBinder.Apply();
            AgentVisualUtility.FitVisualToHeight(visual.transform, controller.height * 0.96f);

            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            var controllerWithMixamoMotion = MixamoAnimationUtility.CreateOverrideController(runtimeController, "MixamoBeetlejuice/Models/BeetleJuiceMixamo");
            if (controllerWithMixamoMotion != null) animator.runtimeAnimatorController = controllerWithMixamoMotion;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var driver = root.AddComponent<AgentAnimationDriver>();
            driver.Configure(navigator, animator);
            AttachProceduralWalk(visual, navigator);
        }
        else if (prototype != null)
        {
            var visual = Object.Instantiate(prototype, root.transform);
            visual.name = "Animated Humanoid Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = runtimeController;
            animator.applyRootMotion = false;

            var driver = root.AddComponent<AgentAnimationDriver>();
            driver.Configure(navigator, animator);
            AttachProceduralWalk(visual, navigator);
        }
        else
        {
            CreateFallbackCapsule(root.transform, fallbackMaterial);
        }

        return navigator;
    }

    private static void EnsureProceduralWalk(AgentNavigator agent)
    {
        var binder = Object.FindObjectOfType<MixamoCharacterMaterialBinder>();
        if (binder != null)
        {
            binder.Apply();
            AttachProceduralWalk(binder.gameObject, agent);
        }
    }

    private static void AttachProceduralWalk(GameObject visual, AgentNavigator navigator)
    {
        if (visual == null || navigator == null) return;
        var proceduralWalk = visual.GetComponent<ProceduralMixamoWalkAnimator>() ?? visual.AddComponent<ProceduralMixamoWalkAnimator>();
        proceduralWalk.Configure(navigator);
    }

    private static void CreateFallbackCapsule(Transform parent, Material material)
    {
        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Fallback Capsule";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        visual.transform.localScale = new Vector3(0.45f, 0.8f, 0.45f);
        var collider = visual.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static GameObject CreateDestinationMarker(Material material)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Destination Marker";
        marker.transform.position = new Vector3(0f, 0.05f, 2f);
        marker.transform.localScale = Vector3.one * 0.14f;
        var collider = marker.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        marker.SetActive(false);
        return marker;
    }

    private static LineRenderer CreateAimLine(Material material)
    {
        var lineObject = new GameObject("Aim Line");
        var line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;
        line.sharedMaterial = material;
        line.enabled = false;
        return line;
    }

    private static void CreateStatusPanel(Camera camera, GestureCommandRouter gestures, SpatialAnchorSurfaceAuthoring surfaces, AgentNavigator agent)
    {
        var root = new GameObject("Project 3 Status Panel");
        var textMesh = root.AddComponent<TextMesh>();
        textMesh.characterSize = 0.035f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.color = Color.white;
        var panel = root.AddComponent<Project3StatusPanel>();
        panel.Configure(camera, gestures, surfaces, agent, textMesh);
    }

#if UNITY_EDITOR
    private static void CreateEditorTestRoom(Material material)
    {
        if (GameObject.Find("Editor Test Room") != null) return;
        var root = new GameObject("Editor Test Room");
        root.tag = "EditorOnly";
        CreateRoomPrimitive("Floor", root.transform, new Vector3(0f, -0.01f, 2f), new Vector3(5f, 0.02f, 5f), material, SpatialSurfaceKind.Floor);
        CreateRoomPrimitive("Back Wall", root.transform, new Vector3(0f, 1f, 4.5f), new Vector3(5f, 2f, 0.05f), material, SpatialSurfaceKind.Wall);
        CreateRoomPrimitive("Left Wall", root.transform, new Vector3(-2.5f, 1f, 2f), new Vector3(0.05f, 2f, 5f), material, SpatialSurfaceKind.Wall);
        CreateRoomPrimitive("Right Wall", root.transform, new Vector3(2.5f, 1f, 2f), new Vector3(0.05f, 2f, 5f), material, SpatialSurfaceKind.Wall);
    }

    private static void CreateRoomPrimitive(string name, Transform parent, Vector3 position, Vector3 scale, Material material, SpatialSurfaceKind kind)
    {
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.position = position;
        primitive.transform.localScale = scale;
        primitive.AddComponent<SpatialSurfaceProxy>().Configure(kind, default);
        var renderer = primitive.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }
#endif

    private sealed class RuntimeMaterialSet
    {
        public Material Floor { get; private set; }
        public Material Wall { get; private set; }
        public Material Anchor { get; private set; }
        public Material Agent { get; private set; }
        public Material Target { get; private set; }
        public Material EditorRoom { get; private set; }

        public static RuntimeMaterialSet Create()
        {
            return new RuntimeMaterialSet
            {
                Floor = CreateMaterial(new Color(0.16f, 0.68f, 0.72f, 0.35f)),
                Wall = CreateMaterial(new Color(0.92f, 0.48f, 0.21f, 0.35f)),
                Anchor = CreateMaterial(new Color(1f, 0.92f, 0.22f, 1f)),
                Agent = CreateMaterial(new Color(0.24f, 0.45f, 0.96f, 1f)),
                Target = CreateMaterial(new Color(0.28f, 1f, 0.56f, 1f)),
                EditorRoom = CreateMaterial(new Color(0.22f, 0.22f, 0.24f, 1f))
            };
        }

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            if (color.a < 1f)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            return material;
        }
    }
}
