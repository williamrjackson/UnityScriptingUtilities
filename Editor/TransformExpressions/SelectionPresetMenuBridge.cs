using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    public static class SelectionPresetMenuBridge
{
    private const string RegistryAssetSearch = "t:SelectionPresetRegistry";
    // --- Renumber ---
    [MenuItem("Edit/Renumber")]
    [MenuItem("GameObject/Renumber", false, 0)]
    private static void Renumber()
        => RunFromRegistry(r => r.renumberPreset, "Renumber Preset not assigned.");

    [MenuItem("Edit/Renumber", true)]
    [MenuItem("GameObject/Renumber", true)]
    private static bool Renumber_Validate()
        => Selection.gameObjects != null && Selection.gameObjects.Length > 1;

    // --- Reorder Hierarchy To Selection (if you want it as its own menu item) ---
    [MenuItem("Edit/Reorder Hierarchy To Selection")]
    [MenuItem("GameObject/Reorder Hierarchy To Selection", false, 0)]
    private static void ReorderHierarchyToSelection()
        => RunFromRegistry(r => r.renumberPreset, "Renumber Preset not assigned (this preset also contains Reorder).");

    [MenuItem("Edit/Reorder Hierarchy To Selection", true)]
    [MenuItem("GameObject/Reorder Hierarchy To Selection", true)]
    private static bool ReorderHierarchyToSelection_Validate()
        => Selection.gameObjects != null && Selection.gameObjects.Length > 1;

    // -----------------------
    // Shared dispatch
    // -----------------------
    private static void RunFromRegistry(System.Func<SelectionPresetRegistry, SelectionPreset> selector, string errorMsg)
    {
        var registry = FindRegistry();
        if (!registry)
        {
            EditorUtility.DisplayDialog(
                "Selection Preset Registry Missing",
                "Create a SelectionPresetRegistry asset and assign your presets.\n\n" +
                "Create → Transform Expressions → Selection Presets → Selection Preset Registry",
                "OK");
            return;
        }

        var preset = selector(registry);
        if (!preset)
        {
            EditorUtility.DisplayDialog("Preset Missing", errorMsg, "OK");
            // Selection.activeObject = registry;
            // EditorGUIUtility.PingObject(registry);
            return;
        }

        preset.Apply(Selection.gameObjects);
    }

    private static SelectionPresetRegistry FindRegistry()
    {
        var guids = AssetDatabase.FindAssets(RegistryAssetSearch);
        if (guids == null || guids.Length == 0) return null;

        // If multiple registries exist, just take the first (or you can add logic here).
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<SelectionPresetRegistry>(path);
    }
}
}
