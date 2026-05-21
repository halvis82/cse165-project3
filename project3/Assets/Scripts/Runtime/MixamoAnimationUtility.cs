using System.Collections.Generic;
using UnityEngine;

public static class MixamoAnimationUtility
{
    public static RuntimeAnimatorController CreateOverrideController(
        RuntimeAnimatorController baseController,
        string animationResourcePath)
    {
        if (baseController == null || string.IsNullOrWhiteSpace(animationResourcePath))
        {
            return baseController;
        }

        var mixamoClip = FindUsableClip(animationResourcePath);
        if (mixamoClip == null)
        {
            return baseController;
        }

        var overrideController = new AnimatorOverrideController(baseController)
        {
            name = "Mixamo Beetlejuice Motion Controller"
        };

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (var i = 0; i < overrides.Count; i++)
        {
            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                overrides[i].Key,
                mixamoClip);
        }

        overrideController.ApplyOverrides(overrides);
        return overrideController;
    }

    private static AnimationClip FindUsableClip(string resourcePath)
    {
        var clips = Resources.LoadAll<AnimationClip>(resourcePath);
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        AnimationClip fallback = null;
        for (var i = 0; i < clips.Length; i++)
        {
            var clip = clips[i];
            if (clip == null || clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = clip;
            }

            if (clip.length > 0.05f)
            {
                return clip;
            }
        }

        return fallback;
    }
}
