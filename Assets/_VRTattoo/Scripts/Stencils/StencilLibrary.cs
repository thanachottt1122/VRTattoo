using System;
using System.Collections.Generic;
using UnityEngine;

public enum StencilDifficulty { Easy, Medium, Hard }

[Serializable]
public class StencilEntry
{
    public Texture2D texture;
    public string displayName;
    public StencilDifficulty difficulty;
}

// Every tattoo design in Assets/_VRTattoo/Stencils. The list is rebuilt automatically by
// StencilLibraryPostprocessor whenever a PNG is added, removed or renamed there, so it is never
// edited by hand. File names are <easy|medium|hard>_<name>.png.
[CreateAssetMenu(menuName = "VRTattoo/Stencil Library")]
public class StencilLibrary : ScriptableObject
{
    public const string Folder = "Assets/_VRTattoo/Stencils";

    public List<StencilEntry> stencils = new List<StencilEntry>();

    public static float Multiplier(StencilDifficulty difficulty) => difficulty switch
    {
        StencilDifficulty.Medium => 1.5f,
        StencilDifficulty.Hard => 2f,
        _ => 1f,
    };

    // "medium_crown_seven" -> Medium, "crown seven"
    public static bool TryParseFileName(string fileNameWithoutExtension, out StencilDifficulty difficulty, out string displayName)
    {
        difficulty = StencilDifficulty.Easy;
        displayName = null;

        int split = fileNameWithoutExtension.IndexOf('_');
        if (split <= 0 || split == fileNameWithoutExtension.Length - 1)
            return false;

        switch (fileNameWithoutExtension.Substring(0, split).ToLowerInvariant())
        {
            case "easy": difficulty = StencilDifficulty.Easy; break;
            case "medium": difficulty = StencilDifficulty.Medium; break;
            case "hard": difficulty = StencilDifficulty.Hard; break;
            default: return false;
        }

        displayName = fileNameWithoutExtension.Substring(split + 1).Replace('_', ' ');
        return true;
    }
}
