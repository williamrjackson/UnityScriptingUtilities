using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(
        menuName = "Transform Expressions/Selection Presets/Renumber",
        fileName = "RenumberSelectionPreset")]
    public sealed class RenumberSelectionPreset : SelectionPreset
{
    private enum SelectionOrder
    {
        UnitySelectionOrder,
        NameOrder,
        HierarchyOrder
    }

    [Header("Ordering")]
    [SerializeField] private SelectionOrder order = SelectionOrder.UnitySelectionOrder;

    [Header("Naming")]
    [SerializeField] private bool useEditorNamingScheme = true;
    [SerializeField, Min(1)] private int overrideDigits = 2;
    [SerializeField] private EditorSettings.NamingScheme overrideScheme = EditorSettings.NamingScheme.SpaceParenthesis;

    [Tooltip("Regex used to strip existing numeric suffix-like tokens from the name.")]
    [SerializeField] private string stripRegex = @"[_([.\s]*?\d+[)\]\s]*[^\S]*";

    [Header("Preview")]
    [SerializeField] private bool showPreview = true;

    public override bool DrawGUI()
    {
        EditorGUI.BeginChangeCheck();

        order = (SelectionOrder)EditorGUILayout.EnumPopup("Order", order);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Naming", EditorStyles.miniBoldLabel);

        useEditorNamingScheme = EditorGUILayout.ToggleLeft("Use Editor naming scheme/digits", useEditorNamingScheme);

        if (!useEditorNamingScheme)
        {
            overrideDigits = Mathf.Clamp(EditorGUILayout.IntField("Digits", overrideDigits), 1, 10);
            overrideScheme = (EditorSettings.NamingScheme)EditorGUILayout.EnumPopup("Scheme", overrideScheme);
        }
        else
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Editor Digits", EditorSettings.gameObjectNamingDigits);
                EditorGUILayout.EnumPopup("Editor Scheme", EditorSettings.gameObjectNamingScheme);
            }
        }

        stripRegex = EditorGUILayout.TextField(new GUIContent("Strip Regex", "Used to remove existing numbering tokens."), stripRegex);

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
                int digits = useEditorNamingScheme ? EditorSettings.gameObjectNamingDigits : overrideDigits;
                var scheme = useEditorNamingScheme ? EditorSettings.gameObjectNamingScheme : overrideScheme;

                int count = Mathf.Min(ordered.Length, 5);
                for (int i = 0; i < count; i++)
                {
                    var go = ordered[i];
                    if (!go) continue;

                    string baseName = Strip(go.name);
                    string num = FormatIndex(i, digits);
                    string proposed = ApplyScheme(baseName, num, scheme);

                    EditorGUILayout.LabelField($"{go.name}  →  {proposed}", EditorStyles.miniLabel);
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
        Undo.SetCurrentGroupName("Renumber");
        Undo.RecordObjects(ordered, "Renumber");

        int digits = useEditorNamingScheme ? EditorSettings.gameObjectNamingDigits : overrideDigits;
        var scheme = useEditorNamingScheme ? EditorSettings.gameObjectNamingScheme : overrideScheme;

        for (int i = 0; i < ordered.Length; i++)
        {
            var go = ordered[i];
            if (!go) continue;

            string output = Strip(go.name);
            string num = FormatIndex(i, digits);
            go.name = ApplyScheme(output, num, scheme);
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

    private string Strip(string name)
    {
        try
        {
            return Regex.Replace(name, stripRegex, string.Empty);
        }
        catch
        {
            // Bad regex -> safe fallback
            return name;
        }
    }

    private static string FormatIndex(int index, int digits)
    {
        string format = new string('0', Mathf.Max(1, digits));
        return index.ToString(format);
    }

    private static string ApplyScheme(string baseName, string digit, EditorSettings.NamingScheme scheme)
    {
        switch (scheme)
        {
            case EditorSettings.NamingScheme.Dot:
                return $"{baseName}.{digit}";
            case EditorSettings.NamingScheme.Underscore:
                return $"{baseName}_{digit}";
            default: // SpaceParenthesis
                return $"{baseName} ({digit})";
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
// /// Finds the first RenumberSelectionPreset asset and runs it.
// /// If you want explicit selection (registry asset), I can swap this.
// /// </summary>
// public static class RenumberSelectionPresetMenu
// {
//     [MenuItem("Edit/Renumber")]
//     [MenuItem("GameObject/Renumber", false, 0)]
//     private static void Renumber()
//     {
//         var preset = FindFirstPreset();
//         if (!preset)
//         {
//             EditorUtility.DisplayDialog(
//                 "Renumber preset not found",
//                 "Create a RenumberSelectionPreset asset via:\nCreate → Transform Expressions → Selection Presets → Renumber",
//                 "OK");
//             return;
//         }

//         preset.Apply(Selection.gameObjects);
//     }

//     [MenuItem("Edit/Renumber", true)]
//     [MenuItem("GameObject/Renumber", true)]
//     private static bool Validate() => Selection.gameObjects != null && Selection.gameObjects.Length > 1;

//     private static RenumberSelectionPreset FindFirstPreset()
//     {
//         var guids = AssetDatabase.FindAssets("t:RenumberSelectionPreset");
//         if (guids == null || guids.Length == 0) return null;
//         var path = AssetDatabase.GUIDToAssetPath(guids[0]);
//         return AssetDatabase.LoadAssetAtPath<RenumberSelectionPreset>(path);
//     }
// }
}

