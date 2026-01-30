using System.IO;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    public static class MultiRenameMenuBridge
{
    private const string DefaultAssetPath = "Assets/TransformExpressions/Presets/MultiRenamePreset.asset";

    [MenuItem("Edit/Multi-Rename")]
    [MenuItem("GameObject/Multi-Rename", false, 0)]
    private static void OpenMultiRenameConfig()
    {
        var preset = FindFirstPreset();

        if (!preset)
        {
            // Optionally auto-create instead of “no-op”
            if (EditorUtility.DisplayDialog(
                    "Multi-Rename preset not found",
                    "No MultiRenamePreset asset exists yet.\n\nCreate one now?",
                    "Create",
                    "Cancel"))
            {
                preset = CreatePresetAsset(DefaultAssetPath);
            }
            else
            {
                return;
            }
        }

        // Configure-first behavior: open window + focus preset (NO apply)
        TransformExpressionsWindow.OpenAndFocusPreset(preset, scrollTo: true);
    }

    [MenuItem("Edit/Multi-Rename", true)]
    [MenuItem("GameObject/Multi-Rename", true)]
    private static bool Validate()
        => Selection.gameObjects != null && Selection.gameObjects.Length > 1;

    private static MultiRenamePreset FindFirstPreset()
    {
        // IMPORTANT: this must match your concrete class name exactly
        var guids = AssetDatabase.FindAssets("t:MultiRenamePreset");
        if (guids == null || guids.Length == 0) return null;

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<MultiRenamePreset>(path);
    }

    private static MultiRenamePreset CreatePresetAsset(string assetPath)
    {
        var dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var preset = ScriptableObject.CreateInstance<MultiRenamePreset>();
        AssetDatabase.CreateAsset(preset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // // This might be stealing selection?
        // EditorGUIUtility.PingObject(preset);
        // Selection.activeObject = preset; 

        return preset;
    }
}
}
