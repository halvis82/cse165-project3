using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class Project3SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Project3ARAgent.unity";
    private const string MaterialFolder = "Assets/Materials";
    private const string ProductName = "CSE165 Project 3";
    private const string AndroidPackageName = "edu.ucsd.cse165.project3.embodiedagentar";

    [MenuItem("CSE165 Project 3/Rebuild AR Agent Scene")]
    public static void RebuildScene()
    {
        EnsureDirectories();
        var materials = CreateMaterials();
        BuildScene(materials);
        ConfigureQuestOpenXR();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Built CSE 165 Project 3 scene at {ScenePath}.");
    }

    [MenuItem("CSE165 Project 3/Configure Quest OpenXR")]
    public static void ConfigureQuestOpenXR()
    {
        Directory.CreateDirectory("Assets/XR");
        Directory.CreateDirectory("Assets/XR/Settings");

        PlayerSettings.productName = ProductName;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidPackageName);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.colorSpace = ColorSpace.Linear;

        var buildTargetSettings = GetOrCreateXRBuildTargetSettings();
        if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        var androidSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
        androidSettings.InitManagerOnStart = true;

        var managerSettings = buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        managerSettings.automaticLoading = true;
        managerSettings.automaticRunning = true;
        XRPackageMetadataStore.AssignLoader(
            managerSettings,
            "UnityEngine.XR.OpenXR.OpenXRLoader",
            BuildTargetGroup.Android);

        var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXrSettings != null)
        {
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            EnableFeature<MetaQuestFeature>(openXrSettings);
            EnableFeature<HandTracking>(openXrSettings);
            EnableFeature<MetaHandTrackingAim>(openXrSettings);
            EnableFeature<ARSessionFeature>(openXrSettings);
            EnableFeature<ARCameraFeature>(openXrSettings);
            EnableFeature<ARPlaneFeature>(openXrSettings);
            EnableFeature<ARAnchorFeature>(openXrSettings);
            EnableFeature<ARRaycastFeature>(openXrSettings);
            EditorUtility.SetDirty(openXrSettings);
        }

        EditorUtility.SetDirty(buildTargetSettings);
        EditorUtility.SetDirty(androidSettings);
        EditorUtility.SetDirty(managerSettings);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("CSE165 Project 3/Build Quest APK")]
    public static void BuildQuestApk()
    {
        ConfigureQuestOpenXR();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/project3.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Quest build failed: {report.summary.result}");
        }
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Models/Mixamo");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Scripts/Editor");
        Directory.CreateDirectory("Assets/Scripts/Runtime");
    }

    private static MaterialSet CreateMaterials()
    {
        return new MaterialSet
        {
            floor = CreateOrUpdateMaterial("AnchoredFloor", new Color(0.16f, 0.68f, 0.72f, 0.35f)),
            wall = CreateOrUpdateMaterial("AnchoredWall", new Color(0.92f, 0.48f, 0.21f, 0.35f)),
            anchor = CreateOrUpdateMaterial("AnchorMarker", new Color(1f, 0.92f, 0.22f, 1f)),
            agent = CreateOrUpdateMaterial("AgentPlaceholder", new Color(0.24f, 0.45f, 0.96f, 1f)),
            target = CreateOrUpdateMaterial("DestinationMarker", new Color(0.28f, 1f, 0.56f, 1f)),
            editorRoom = CreateOrUpdateMaterial("EditorRoom", new Color(0.22f, 0.22f, 0.24f, 1f))
        };
    }

    private static Material CreateOrUpdateMaterial(string shortName, Color color)
    {
        var path = $"{MaterialFolder}/{shortName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = Shader.Find("Standard") ?? material.shader;
        material.color = color;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color);
            material.EnableKeyword("_EMISSION");
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        Project3MaterialUtility.ConfigureVisibility(material, color.a < 1f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildScene(MaterialSet materials)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sessionObject = new GameObject("AR Session");
        sessionObject.AddComponent<ARSession>();

        var originObject = new GameObject("XR Origin");
        var xrOrigin = originObject.AddComponent<XROrigin>();

        var cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(originObject.transform, false);
        xrOrigin.CameraFloorOffsetObject = cameraOffset;
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraOffset.transform, false);
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<ARCameraManager>();
        cameraObject.AddComponent<ARCameraBackground>();
        QuestCameraPoseUtility.EnsureHeadTrackedCamera(cameraObject);
        xrOrigin.Camera = camera;

        var planeManager = originObject.AddComponent<ARPlaneManager>();
        var anchorManager = originObject.AddComponent<ARAnchorManager>();
        originObject.AddComponent<ARRaycastManager>();

        var destinationMarker = CreateDestinationMarker(materials.target);
        var aimLine = CreateAimLine(materials.target);
        var agent = CreateAgent(materials.agent);

        var surfaceAuthoringObject = new GameObject("Spatial Anchor Surface Authoring");
        var surfaceAuthoring = surfaceAuthoringObject.AddComponent<SpatialAnchorSurfaceAuthoring>();
        surfaceAuthoring.Configure(planeManager, anchorManager, materials.floor, materials.wall, materials.anchor);

        var gestureObject = new GameObject("Gesture Command Router");
        var gestureRouter = gestureObject.AddComponent<GestureCommandRouter>();
        gestureRouter.Configure(agent, destinationMarker.transform, aimLine, originObject.GetComponent<ARRaycastManager>(), planeManager);

        var handDebugObject = new GameObject("Tracked Hand Joint Visualizer");
        handDebugObject.AddComponent<HandJointVisualizer>();

        CreateStatusPanel(camera, gestureRouter, surfaceAuthoring, agent);

        CreateEditorOnlyRoom(materials.editorRoom);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScene(ScenePath);
        Selection.activeGameObject = agent.gameObject;
    }

    private static AgentNavigator CreateAgent(Material material)
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

            var materialBinder = visual.GetComponent<MixamoCharacterMaterialBinder>() ??
                                 visual.AddComponent<MixamoCharacterMaterialBinder>();
            materialBinder.Apply();
            AgentVisualUtility.FitVisualToHeight(visual.transform, controller.height * 0.96f);

            var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = runtimeController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var driver = root.AddComponent<AgentAnimationDriver>();
            driver.Configure(navigator, animator);
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
        }
        else
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Fallback Capsule";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            visual.transform.localScale = new Vector3(0.45f, 0.8f, 0.45f);

            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        return navigator;
    }

    private static GameObject CreateDestinationMarker(Material material)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Destination Marker";
        marker.transform.position = new Vector3(0f, 0.05f, 2f);
        marker.transform.localScale = Vector3.one * 0.14f;

        var collider = marker.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

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

    private static void CreateStatusPanel(
        Camera camera,
        GestureCommandRouter gestures,
        SpatialAnchorSurfaceAuthoring surfaces,
        AgentNavigator agent)
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

    private static void CreateEditorOnlyRoom(Material material)
    {
        var root = new GameObject("Editor Test Room");
        root.tag = "EditorOnly";

        CreateRoomPrimitive("Floor", root.transform, new Vector3(0f, -0.01f, 2f), new Vector3(5f, 0.02f, 5f), material, SpatialSurfaceKind.Floor);
        CreateRoomPrimitive("Back Wall", root.transform, new Vector3(0f, 1f, 4.5f), new Vector3(5f, 2f, 0.05f), material, SpatialSurfaceKind.Wall);
        CreateRoomPrimitive("Left Wall", root.transform, new Vector3(-2.5f, 1f, 2f), new Vector3(0.05f, 2f, 5f), material, SpatialSurfaceKind.Wall);
        CreateRoomPrimitive("Right Wall", root.transform, new Vector3(2.5f, 1f, 2f), new Vector3(0.05f, 2f, 5f), material, SpatialSurfaceKind.Wall);
    }

    private static void CreateRoomPrimitive(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material,
        SpatialSurfaceKind kind)
    {
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.position = position;
        primitive.transform.localScale = scale;

        var surfaceProxy = primitive.AddComponent<SpatialSurfaceProxy>();
        surfaceProxy.Configure(kind, default);

        var renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void SetBuildScene(string scenePath)
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        var sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        Selection.activeObject = sceneAsset;
        Debug.Log($"Build scene set to {scenePath} ({sceneGuid}).");
    }

    private static void EnableFeature<TFeature>(OpenXRSettings openXrSettings)
        where TFeature : OpenXRFeature
    {
        var feature = openXrSettings.GetFeature<TFeature>();
        if (feature == null)
        {
            Debug.LogWarning($"OpenXR feature {typeof(TFeature).Name} was not found.");
            return;
        }

        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    private static XRGeneralSettingsPerBuildTarget GetOrCreateXRBuildTargetSettings()
    {
        if (EditorBuildSettings.TryGetConfigObject(
                XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget existingSettings) &&
            existingSettings != null)
        {
            return existingSettings;
        }

        var guid = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget").FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(guid))
        {
            existingSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (existingSettings != null)
            {
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, existingSettings, true);
                return existingSettings;
            }
        }

        Directory.CreateDirectory("Assets/XR/Settings");
        var createdSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        const string settingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
        AssetDatabase.CreateAsset(createdSettings, settingsPath);
        AssetDatabase.SaveAssets();
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, createdSettings, true);
        return createdSettings;
    }

    private sealed class MaterialSet
    {
        public Material floor;
        public Material wall;
        public Material anchor;
        public Material agent;
        public Material target;
        public Material editorRoom;
    }
}
