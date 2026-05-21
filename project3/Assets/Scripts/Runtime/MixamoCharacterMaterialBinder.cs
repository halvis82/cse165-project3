using UnityEngine;

public sealed class MixamoCharacterMaterialBinder : MonoBehaviour
{
    private const string BodyTexturePath = "MixamoBeetlejuice/Textures/Body";
    private const string BodyNormalPath = "MixamoBeetlejuice/Textures/Body_N";
    private const string HeadTexturePath = "MixamoBeetlejuice/Textures/Head";
    private const string HeadNormalPath = "MixamoBeetlejuice/Textures/Head_N";

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
            bodyMaterial = CreateMaterial(
                "Beetlejuice Body Material",
                BodyTexturePath,
                BodyNormalPath);
        }

        if (headMaterial == null)
        {
            headMaterial = CreateMaterial(
                "Beetlejuice Head Material",
                HeadTexturePath,
                HeadNormalPath);
        }

        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        for (var i = 0; i < renderers.Length; i++)
        {
            var targetMaterial = IsBodyRenderer(renderers[i], i) ? bodyMaterial : headMaterial;
            AssignMaterial(renderers[i], targetMaterial, headMaterial, i == 0 && renderers.Length == 1);
        }
    }

    private static Material CreateMaterial(
        string materialName,
        string albedoResourcePath,
        string normalResourcePath)
    {
        var material = new Material(Shader.Find("Standard"))
        {
            name = materialName
        };

        var albedo = Resources.Load<Texture2D>(albedoResourcePath);
        if (albedo != null)
        {
            material.mainTexture = albedo;
            material.SetTexture("_MainTex", albedo);
        }

        var normal = Resources.Load<Texture2D>(normalResourcePath);
        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        material.SetFloat("_Glossiness", 0.25f);
        return material;
    }

    private static bool IsBodyRenderer(Renderer renderer, int rendererIndex)
    {
        var rendererName = renderer != null ? renderer.name : string.Empty;
        var meshName = string.Empty;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer &&
            skinnedMeshRenderer.sharedMesh != null)
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

    private static void AssignMaterial(
        Renderer renderer,
        Material material,
        Material head,
        bool singleRendererFallback)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        var materialCount = GetMaterialSlotCount(renderer);

        if (singleRendererFallback && materialCount >= 4)
        {
            renderer.sharedMaterials = new[]
            {
                material,
                head,
                head,
                head
            };
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
        var materialCount = renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
            ? renderer.sharedMaterials.Length
            : 0;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer &&
            skinnedMeshRenderer.sharedMesh != null)
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
