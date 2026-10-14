using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Prepares a character FBX for use as a customer:
//   - Rig = Humanoid (avatar created from the model)
//   - embedded materials extracted to <model folder>/Materials and switched to URP Lit
//   - missing textures filled in from image files whose name starts with the material name
//     (…Diffuse/…Color/…_di → base map, …Normal/…_tn → normal map, …Opacity → baked into base alpha)
// Textures the naming rule can't find have to be assigned by hand on the extracted materials.
public static class CharacterModelSetup
{
    const string LitShader = "Universal Render Pipeline/Lit";

    static readonly string[] BaseTokens = { "diffuse", "color", "albedo", "basecolor", "_di", "_d" };
    static readonly string[] NormalTokens = { "normal", "_tn", "_n" };
    static readonly string[] OpacityTokens = { "opacity", "alpha" };
    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };

    [MenuItem("VRTattoo/Characters/Setup Selected Models")]
    static void SetupSelected()
    {
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (AssetImporter.GetAtPath(path) is ModelImporter)
                SetupModel(path);
        }
    }

    [MenuItem("VRTattoo/Characters/Setup Selected Models", true)]
    static bool SetupSelectedValidate() =>
        Selection.objects.Any(o => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(o)) is ModelImporter);

    public static void SetupModel(string modelPath)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();

        var folder = Path.GetDirectoryName(modelPath).Replace('\\', '/');
        var materialFolder = folder + "/Materials";
        ExtractMaterials(modelPath, materialFolder);

        // Downloads often put the model in source/ next to a textures/ folder.
        var imageFolder = Path.GetFileName(folder).ToLowerInvariant() == "source" ? Path.GetDirectoryName(folder).Replace('\\', '/') : folder;
        var images = FindImages(imageFolder);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { materialFolder }))
            SetupMaterial(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)), images, materialFolder);

        AssetDatabase.SaveAssets();
        Debug.Log($"[CharacterModelSetup] {modelPath}: Humanoid + URP materials in {materialFolder}");
    }

    static void ExtractMaterials(string modelPath, string materialFolder)
    {
        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(materialFolder), Path.GetFileName(materialFolder));

        var embedded = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToList();
        if (embedded.Count == 0)
            return;

        foreach (var material in embedded)
        {
            var target = AssetDatabase.GenerateUniqueAssetPath($"{materialFolder}/{Sanitize(material.name)}.mat");
            AssetDatabase.ExtractAsset(material, target);
        }
        AssetDatabase.WriteImportSettingsIfDirty(modelPath);
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
    }

    static void SetupMaterial(Material material, List<string> images, string materialFolder)
    {
        if (material == null)
            return;

        if (!material.shader.name.StartsWith("Universal Render Pipeline"))
            material.shader = Shader.Find(LitShader);

        var key = MaterialKey(material.name);
        var candidates = images.Where(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().StartsWith(key)).ToList();

        var baseMap = material.GetTexture("_BaseMap") as Texture2D;
        if (baseMap == null)
        {
            var found = Pick(candidates, key, BaseTokens);
            if (found != null)
            {
                baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(found);
                material.SetTexture("_BaseMap", baseMap);
            }
        }
        // FBX exporters often leave the colour black when a texture was supposed to supply it.
        if (baseMap != null && material.GetColor("_BaseColor").maxColorComponent < 0.01f)
            material.SetColor("_BaseColor", Color.white);

        if (material.GetTexture("_BumpMap") == null)
        {
            var found = Pick(candidates, key, NormalTokens);
            if (found != null)
            {
                MarkAsNormalMap(found);
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(found));
                material.EnableKeyword("_NORMALMAP");
            }
        }

        var opacity = Pick(candidates, key, OpacityTokens);
        if (opacity != null)
        {
            var combined = BakeAlpha(baseMap, AssetDatabase.LoadAssetAtPath<Texture2D>(opacity),
                $"{materialFolder}/{Sanitize(material.name)}_BaseAlpha.png");
            material.SetTexture("_BaseMap", combined);
            MakeCutout(material);
        }

        EditorUtility.SetDirty(material);
    }

    // Opaque surface with alpha clipping: cheap, sorts correctly, good enough for hair and lashes.
    public static void MakeCutout(Material material, float cutoff = 0.5f)
    {
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", cutoff);
        material.SetFloat("_Cull", 0f);
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
    }

    // Writes base RGB + opacity (red channel) as alpha into a new PNG and returns it.
    static Texture2D BakeAlpha(Texture2D baseMap, Texture2D opacity, string outPath)
    {
        int w = opacity.width, h = opacity.height;
        var color = ReadPixels(baseMap, w, h);
        var alpha = ReadPixels(opacity, w, h);
        for (int i = 0; i < color.Length; i++)
            color[i].a = alpha[i].r;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(color);
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(outPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
    }

    // Reads any texture (readable or not) at the given size through a temporary RenderTexture.
    static Color[] ReadPixels(Texture source, int w, int h)
    {
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        if (source != null)
            Graphics.Blit(source, rt);
        else
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.white);
            RenderTexture.active = previous;
        }

        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);

        var pixels = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return pixels;
    }

    public static void MarkAsNormalMap(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    static string Pick(List<string> candidates, string key, string[] tokens)
    {
        foreach (var token in tokens)
            foreach (var path in candidates)
            {
                var rest = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Substring(key.Length);
                if (rest.Contains(token))
                    return path;
            }
        return null;
    }

    // "Hair_Part01mat" → "hair_part01", "Material.005" stays as is.
    static string MaterialKey(string materialName)
    {
        var key = materialName.ToLowerInvariant();
        if (key.EndsWith("mat") && key.Length > 3)
            key = key.Substring(0, key.Length - 3);
        return key;
    }

    static List<string> FindImages(string folder) =>
        AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()) && !p.Contains("/Materials/"))
            .ToList();

    static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
