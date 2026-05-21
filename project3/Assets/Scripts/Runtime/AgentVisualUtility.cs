using UnityEngine;

public static class AgentVisualUtility
{
    public static void FitVisualToHeight(Transform visualRoot, float targetHeightMeters)
    {
        if (visualRoot == null || targetHeightMeters <= 0f)
        {
            return;
        }

        if (!TryGetRendererBounds(visualRoot, out var bounds) || bounds.size.y <= 0.001f)
        {
            return;
        }

        var scale = targetHeightMeters / bounds.size.y;
        if (scale > 0.001f && scale < 1000f)
        {
            visualRoot.localScale *= scale;
        }

        if (TryGetRendererBounds(visualRoot, out bounds))
        {
            var floorY = visualRoot.parent != null ? visualRoot.parent.position.y : 0f;
            visualRoot.position += Vector3.up * (floorY - bounds.min.y);
        }
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var hasBounds = false;

        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}
