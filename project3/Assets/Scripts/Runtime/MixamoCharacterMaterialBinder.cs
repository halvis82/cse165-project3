using UnityEngine;

public sealed class MixamoCharacterMaterialBinder : MonoBehaviour
{
    private const string BodyTexturePath = "MixamoBeetlejuice/Textures/Body";
    private const string HeadTexturePath = "MixamoBeetlejuice/Textures/Head";

    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material headMaterial;
    [SerializeField] private bool applyOnAwake = true;

    public Material BodyMaterial => bodyMaterial;
    public Material HeadMaterial => headMaterial;

    public void Configure(Material body, Material head)
    {
        bodyMaterial = body;
        headMaterial = head;
    }

    private void Awake()
    {
        if (applyOnAwake)
        {
            Apply();
        }
    }

    public void Apply()
    {
        if (bodyMaterial == null)
        {
            bodyMaterial = CreateMaterial("Beetlejuice Body Material", BodyTexturePath);
        }

        if (headMaterial == null)
        {
            headMaterial = CreateMaterial("Beetlejuice Head Material", HeadTexturePath);
        }

        ForceOpaqueDoubleSided(bodyMaterial);
        ForceOpaqueDoubleSided(headMaterial);

        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        for (var i = 0; i < renderers.Length; i++)
        {
            ConfigureRendererForQuest(renderers[i]);
            var targetMaterial = IsBodyRenderer(renderers[i], i) ? bodyMaterial : headMaterial;
            AssignMaterial(renderers[i], targetMaterial, headMaterial, i == 0 && renderers.Length == 1);
        }
    }

    private static Material CreateMaterial(string materialName, string albedoResourcePath)
    {
        var albedo = Resources.Load<Texture2D>(albedoResourcePath);
        var material = Project3MaterialUtility.CreateUnlitTexture(materialName, albedo);
        ForceOpaqueDoubleSided(material);
        return material;
    }

    private static void ForceOpaqueDoubleSided(Material material)
    {
        if (material == null)
        {
            return;
        }

        // Force the genuinely double-sided unlit shader. A material that came
        // from the baked scene may still be on Standard, whose hidden Cull Back
        // would render the ripped mesh inside-out. Switching shaders preserves
        // the _MainTex/_Color values since both use the same property names.
        var doubleSided = Shader.Find("CSE165/DoubleSidedUnlit");
        if (doubleSided != null && material.shader != doubleSided)
        {
            material.shader = doubleSided;
        }

        material.color = Color.white;
        Project3MaterialUtility.ConfigureVisibility(material, false);
    }

    private static void ConfigureRendererForQuest(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            skinnedMeshRenderer.updateWhenOffscreen = true;
            skinnedMeshRenderer.localBounds = new Bounds(Vector3.up * 0.9f, new Vector3(2.5f, 2.5f, 2.5f));
        }
    }

    private static bool IsBodyRenderer(Renderer renderer, int rendererIndex)
    {
        var rendererName = renderer != null ? renderer.name : string.Empty;
        var meshName = string.Empty;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
        {
            meshName = skinnedMeshRenderer.sharedMesh.name;
        }
        else
        {
            var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            if (filter != null && filter.sharedMesh != null)
            {
                meshName = filter.sharedMesh.name;
            }
        }

        var combinedName = $"{rendererName} {meshName}";

        if (combinedName.Contains("Model001_Material001") ||
            combinedName.Contains("Mesh-0164.rip_7_Mesh-0164.rip_7_Mesh-0164.rip") ||
            combinedName.Contains("Mesh-0164.rip-7-Mesh-0164.rip-7-Mesh-0164.rip"))
        {
            return true;
        }

        if (combinedName.Contains("Model001_Material002") ||
            combinedName.Contains("Model001_Material003") ||
            combinedName.Contains("Model001_Material004") ||
            combinedName.Contains("Mesh-0166") ||
            combinedName.Contains("Mesh-0170"))
        {
            return false;
        }

        return rendererIndex == 0;
    }

    private static void AssignMaterial(Renderer renderer, Material material, Material head, bool singleRendererFallback)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        var materialCount = GetMaterialSlotCount(renderer);

        if (singleRendererFallback && materialCount >= 4)
        {
            renderer.sharedMaterials = new[] { material, head, head, head };
            return;
        }

        var materials = new Material[materialCount];
        for (var i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }

        renderer.sharedMaterials = materials;
    }

    private static int GetMaterialSlotCount(Renderer renderer)
    {
        var materialCount = renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials.Length : 0;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
        {
            materialCount = Mathf.Max(materialCount, skinnedMeshRenderer.sharedMesh.subMeshCount);
        }
        else
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                materialCount = Mathf.Max(materialCount, filter.sharedMesh.subMeshCount);
            }
        }

        return Mathf.Max(1, materialCount);
    }
}
