using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Keeps Assets/_VRTattoo/Data/StencilLibrary.asset in sync with the PNGs in the Stencils folder
// and gives those PNGs the import settings the game needs (alpha only, readable for scoring).
class StencilLibraryPostprocessor : AssetPostprocessor
{
    const string LibraryPath = "Assets/_VRTattoo/Data/StencilLibrary.asset";

    static bool IsStencilPath(string path) =>
        path.StartsWith(StencilLibrary.Folder + "/") && path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);

    void OnPreprocessTexture()
    {
        if (!IsStencilPath(assetPath))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.SingleChannel;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.singleChannelComponent = TextureImporterSingleChannelComponent.Alpha;
        importer.SetTextureSettings(settings);
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.isReadable = true;  // TattooScorer reads the pixels
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Uncompressed;  // keep thin lines sharp
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsStencilPath))
            Rebuild();
    }

    [MenuItem("VRTattoo/Rebuild Stencil Library")]
    public static void Rebuild()
    {
        var library = AssetDatabase.LoadAssetAtPath<StencilLibrary>(LibraryPath);
        if (library == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
            library = ScriptableObject.CreateInstance<StencilLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.stencils.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { StencilLibrary.Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsStencilPath(path))
                continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!StencilLibrary.TryParseFileName(fileName, out var difficulty, out var displayName))
            {
                Debug.LogWarning($"[StencilLibrary] Skipped {path}: name must start with easy_, medium_ or hard_");
                continue;
            }

            library.stencils.Add(new StencilEntry
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                displayName = displayName,
                difficulty = difficulty,
            });
        }

        library.stencils.Sort((a, b) => a.difficulty != b.difficulty
            ? a.difficulty.CompareTo(b.difficulty)
            : string.CompareOrdinal(a.displayName, b.displayName));
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        Debug.Log($"[StencilLibrary] {library.stencils.Count} stencils");
    }
}
