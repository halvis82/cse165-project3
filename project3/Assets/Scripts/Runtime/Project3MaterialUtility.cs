using UnityEngine;

public static class Project3MaterialUtility
{
    public static Material CreateUnlitColor(string materialName, Color color, bool transparent = false)
    {
        var shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        ApplyColor(material, color);
        ConfigureStandardSurface(material);
        ConfigureVisibility(material, transparent);
        return material;
    }

    public static Material CreateUnlitTexture(string materialName, Texture texture)
    {
        var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
        var material = new Material(shader)
        {
            name = materialName,
            color = Color.white
        };

        ApplyColor(material, Color.white);
        if (texture != null)
        {
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", texture);
                material.EnableKeyword("_EMISSION");
            }

            material.mainTexture = texture;
        }

        ConfigureStandardSurface(material);
        ConfigureVisibility(material, false);
        return material;
    }

    public static void ConfigureVisibility(Material material, bool transparent)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (transparent)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            SetFloatIfPresent(material, "_Mode", 3f);
            SetIntIfPresent(material, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            SetIntIfPresent(material, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            SetIntIfPresent(material, "_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return;
        }

        material.SetOverrideTag("RenderType", "Opaque");
        SetFloatIfPresent(material, "_Mode", 0f);
        SetIntIfPresent(material, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        SetIntIfPresent(material, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        SetIntIfPresent(material, "_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
    }

    private static void ApplyColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color);
            material.EnableKeyword("_EMISSION");
        }
    }

    private static void ConfigureStandardSurface(Material material)
    {
        if (material == null)
        {
            return;
        }

        SetFloatIfPresent(material, "_Glossiness", 0f);
        SetFloatIfPresent(material, "_Metallic", 0f);
        SetFloatIfPresent(material, "_SmoothnessTextureChannel", 0f);
    }

    private static void SetIntIfPresent(Material material, string property, int value)
    {
        if (material.HasProperty(property))
        {
            material.SetInt(property, value);
        }
    }

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }
}
