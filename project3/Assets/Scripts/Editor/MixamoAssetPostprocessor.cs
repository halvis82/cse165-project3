using UnityEditor;

public sealed class MixamoAssetPostprocessor : AssetPostprocessor
{
    private const string MixamoRoot = "Assets/Resources/MixamoBeetlejuice";
    private const string MixamoModelPath = MixamoRoot + "/Models/BeetleJuiceMixamo.fbx";

    private void OnPreprocessModel()
    {
        if (assetPath != MixamoModelPath)
        {
            return;
        }

        var importer = (ModelImporter)assetImporter;
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.globalScale = 1f;

        var clips = importer.defaultClipAnimations;
        if (clips != null && clips.Length > 0)
        {
            for (var i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
            }

            importer.clipAnimations = clips;
        }
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(MixamoRoot + "/Textures/"))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.mipmapEnabled = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.maxTextureSize = 2048;

        if (assetPath.EndsWith("_N.png"))
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
        }
        else
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
        }
    }
}
