using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(
        menuName = "Transform Expressions/Selection Presets/Reorder Hierarchy To Selection",
        fileName = "ReorderHierarchyToSelectionPreset")]
    public sealed class ReorderHierarchyToSelectionPreset : SelectionPreset
{
    private enum SelectionOrder
    {
        UnitySelectionOrder,
        NameOrder,
        HierarchyOrder
    }

    [Header("Ordering")]
    [SerializeField] private SelectionOrder order = SelectionOrder.UnitySelectionOrder;

    [Header("Safety")]
    [Tooltip("If enabled, only objects that share the same parent as the first target will be reordered.")]
    [SerializeField] private bool onlyIfSameParent = true;

    [Header("Preview")]
    [SerializeField] private bool showPreview = true;

    public override bool DrawGUI()
    {
        EditorGUI.BeginChangeCheck();

        order = (SelectionOrder)EditorGUILayout.EnumPopup("Order", order);
        onlyIfSameParent = EditorGUILayout.ToggleLeft("Only if same parent", onlyIfSameParent);

        EditorGUILayout.Space(6);
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview", true);
        if (showPreview)
        {
            var gos = Selection.gameObjects ?? Array.Empty<GameObject>();
            if (gos.Length == 0)
            {
                EditorGUILayout.LabelField("Select 1+ GameObjects to preview.", EditorStyles.miniLabel);
            }
            else
            {
                var ordered = GetOrdered(gos);

                int count = Mathf.Min(ordered.Length, 8);
                for (int i = 0; i < count; i++)
                {
                    var go = ordered[i];
                    if (!go) continue;
                    EditorGUILayout.LabelField($"{i}: {go.name}", EditorStyles.miniLabel);
                }

                if (ordered.Length > count)
                    EditorGUILayout.LabelField($"… +{ordered.Length - count} more", EditorStyles.miniLabel);
            }
        }

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0) return;

        var ordered = GetOrdered(targets);
        if (ordered.Length == 0) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Reorder Hierarchy To Selection");
        Undo.RecordObjects(ordered.Select(g => g.transform).ToArray(), "Reorder Hierarchy");

        Transform parent = ordered[0].transform.parent;

        for (int i = 0; i < ordered.Length; i++)
        {
            var go = ordered[i];
            if (!go) continue;

            if (!onlyIfSameParent || go.transform.parent == parent)
                go.transform.SetSiblingIndex(i);
        }
    }

    // -----------------------
    // Helpers
    // -----------------------

    private GameObject[] GetOrdered(GameObject[] gos)
    {
        var filtered = gos.Where(g => g).ToArray();

        switch (order)
        {
            case SelectionOrder.UnitySelectionOrder:
                return filtered;

            case SelectionOrder.NameOrder:
                return filtered
                    .OrderBy(g => g.name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(g => g.GetInstanceID())
                    .ToArray();

            case SelectionOrder.HierarchyOrder:
                return filtered
                    .OrderBy(g => GetHierarchyPath(g.transform), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(g => g.transform.GetSiblingIndex())
                    .ToArray();

            default:
                return filtered;
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        var stack = new System.Collections.Generic.Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }
}

// /// <summary>
// /// Menu bridge that preserves the legacy context-menu UX.
// /// Finds the first ReorderHierarchyToSelectionPreset asset and runs it.
// /// </summary>
// public static class ReorderHierarchyToSelectionPresetMenu
// {
//     [MenuItem("Edit/Reorder Hierarchy To Selection")]
//     [MenuItem("GameObject/Reorder Hierarchy To Selection", false, 0)]
//     private static void Reorder()
//     {
//         var preset = FindFirstPreset();
//         if (!preset)
//         {
//             EditorUtility.DisplayDialog(
//                 "Reorder preset not found",
//                 "Create a ReorderHierarchyToSelectionPreset asset via:\nCreate → Transform Expressions → Selection Presets → Reorder Hierarchy To Selection",
//                 "OK");
//             return;
//         }

//         preset.Apply(Selection.gameObjects);
//     }

//     [MenuItem("Edit/Reorder Hierarchy To Selection", true)]
//     [MenuItem("GameObject/Reorder Hierarchy To Selection", true)]
//     private static bool Validate() => Selection.gameObjects != null && Selection.gameObjects.Length > 1;

//     private static ReorderHierarchyToSelectionPreset FindFirstPreset()
//     {
//         var guids = AssetDatabase.FindAssets("t:ReorderHierarchyToSelectionPreset");
//         if (guids == null || guids.Length == 0) return null;
//         var path = AssetDatabase.GUIDToAssetPath(guids[0]);
//         return AssetDatabase.LoadAssetAtPath<ReorderHierarchyToSelectionPreset>(path);
//     }
// }
}
