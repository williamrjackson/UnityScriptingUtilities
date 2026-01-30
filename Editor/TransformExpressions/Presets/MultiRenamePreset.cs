using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Selection Presets/Multi-Rename", fileName = "MultiRenamePreset")]
    public sealed class MultiRenamePreset : SelectionPreset
{
    [SerializeField] private string oldToken;
    [SerializeField] private string newToken;

    [Header("Tokens")]
    [SerializeField] private bool allowParentToken = true;
    [SerializeField] private bool allowSceneToken = true;
    [SerializeField] private bool allowProjectToken = true;
    [SerializeField] private bool allowTagToken = true;
    [SerializeField] private bool allowLayerToken = true;

    // Optional: prevent 1–2 character “common substring” suggestions
    [SerializeField] private int minSuggestedOldTokenLength = 3;

    // --- Preview UI ---
    [Header("Preview")]
    [SerializeField] private bool previewFoldout = true;
    [SerializeField] private int previewMaxItems = 20;

    // non-serialized UI state
    [NonSerialized] private Vector2 _previewScroll;
    [NonSerialized] private int _lastPreviewHash;
    [NonSerialized] private List<(string from, string to)> _previewRows = new();

    public override bool DrawGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Use tokens in New Token: {parent}, {scene}, {project}, {tag}, {layer}\n\n" +
            "Old Token empty: rename all to New Token.\n" +
            "Old Token \"*\": inserts the original name wherever \"*\" appears in New Token.",
            MessageType.None);

        oldToken = EditorGUILayout.TextField("Old Token", oldToken);
        newToken = EditorGUILayout.TextField("New Token", newToken);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length < 2))
            {
                if (GUILayout.Button("Suggest Old Token"))
                {
                    var suggested = SuggestOldToken(Selection.gameObjects, minSuggestedOldTokenLength);
                    if (!string.IsNullOrEmpty(suggested))
                    {
                        oldToken = suggested;
                        EditorUtility.SetDirty(this);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Multi-Rename", "No meaningful common substring found.", "OK");
                    }
                }
            }

            GUILayout.FlexibleSpace();

            minSuggestedOldTokenLength = EditorGUILayout.IntField(
                new GUIContent("Min Len", "Minimum length for suggested common substring"),
                Mathf.Clamp(minSuggestedOldTokenLength, 1, 32),
                GUILayout.Width(140));
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Enable Tokens", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            allowParentToken = EditorGUILayout.ToggleLeft("{parent}", allowParentToken, GUILayout.Width(90));
            allowSceneToken = EditorGUILayout.ToggleLeft("{scene}", allowSceneToken, GUILayout.Width(80));
            allowProjectToken = EditorGUILayout.ToggleLeft("{project}", allowProjectToken, GUILayout.Width(90));
            allowTagToken = EditorGUILayout.ToggleLeft("{tag}", allowTagToken, GUILayout.Width(70));
            allowLayerToken = EditorGUILayout.ToggleLeft("{layer}", allowLayerToken, GUILayout.Width(80));
        }

        EditorGUILayout.Space(8);
        DrawPreviewArea();

        return EditorGUI.EndChangeCheck();
    }

    private void DrawPreviewArea()
    {
        var gos = Selection.gameObjects ?? Array.Empty<GameObject>();

        previewFoldout = EditorGUILayout.Foldout(previewFoldout, "Preview", true);
        if (!previewFoldout) return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                previewMaxItems = EditorGUILayout.IntField(
                    new GUIContent("Max Items", "Limit preview rows for large selections"),
                    Mathf.Clamp(previewMaxItems, 1, 500),
                    GUILayout.Width(220));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(gos.Length == 0))
                {
                    if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                        _lastPreviewHash = 0; // force rebuild
                }
            }

            if (gos.Length == 0)
            {
                EditorGUILayout.HelpBox("Select GameObjects to preview the rename result.", MessageType.Info);
                return;
            }

            if (string.IsNullOrEmpty(newToken) && string.IsNullOrEmpty(oldToken))
            {
                EditorGUILayout.HelpBox("Provide Old Token and/or New Token to preview.", MessageType.Warning);
                return;
            }

            // Build / rebuild preview only when needed
            int hash = ComputePreviewHash(gos);
            if (hash != _lastPreviewHash)
            {
                _lastPreviewHash = hash;
                RebuildPreview(gos);
            }

            int shown = Mathf.Min(_previewRows.Count, previewMaxItems);
            if (shown <= 0)
            {
                EditorGUILayout.HelpBox("No preview rows (nothing would change).", MessageType.Info);
                return;
            }

            // Header row
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Current", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Result", EditorStyles.miniBoldLabel);
            }

            const float rowHeight = 18f;
            float viewHeight = Mathf.Min(220f, (shown + 1) * (rowHeight + 2f));

            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(viewHeight));
            for (int i = 0; i < shown; i++)
            {
                var (from, to) = _previewRows[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(from, GUILayout.Height(rowHeight));
                    EditorGUILayout.SelectableLabel(to, GUILayout.Height(rowHeight));
                }
            }
            EditorGUILayout.EndScrollView();

            if (_previewRows.Count > shown)
            {
                EditorGUILayout.HelpBox($"Showing {shown} of {_previewRows.Count} rows. Increase Max Items to see more.", MessageType.None);
            }
        }
    }

    private int ComputePreviewHash(GameObject[] gos)
    {
        unchecked
        {
            int h = 17;
            h = (h * 31) ^ gos.Length;

            // selection identity
            for (int i = 0; i < gos.Length; i++)
                h = (h * 31) ^ (gos[i] ? gos[i].GetInstanceID() : 0);

            // inputs
            h = (h * 31) ^ (oldToken?.GetHashCode() ?? 0);
            h = (h * 31) ^ (newToken?.GetHashCode() ?? 0);

            h = (h * 31) ^ (allowParentToken ? 1 : 0);
            h = (h * 31) ^ (allowSceneToken ? 1 : 0);
            h = (h * 31) ^ (allowProjectToken ? 1 : 0);
            h = (h * 31) ^ (allowTagToken ? 1 : 0);
            h = (h * 31) ^ (allowLayerToken ? 1 : 0);

            h = (h * 31) ^ previewMaxItems;

            // include these in case you want them to force refresh too
            h = (h * 31) ^ (minSuggestedOldTokenLength);

            return h;
        }
    }

    private void RebuildPreview(GameObject[] gos)
    {
        _previewRows.Clear();

        foreach (var go in gos)
        {
            if (!go) continue;

            string from = go.name;
            string to = ComputeNewName(go, from);

            // Optional: only include rows that would actually change.
            // If you prefer to show all, remove this if.
            if (to != from)
                _previewRows.Add((from, to));
        }
    }

    private string ComputeNewName(GameObject go, string currentName)
    {
        // matches Apply() behavior, but returns the resulting name without mutating
        string replacement = ExpandTokens(go, newToken);

        if (string.IsNullOrEmpty(oldToken))
        {
            return replacement ?? string.Empty;
        }

        if (oldToken == "*")
        {
            if (replacement == null) replacement = string.Empty;

            // Ensure replacement contains '*', then substitute original name
            if (!replacement.Contains("*"))
                replacement = "*" + replacement;

            replacement = replacement.Replace("*", currentName);
            return replacement;
        }

        if (!string.IsNullOrEmpty(oldToken) && currentName.Contains(oldToken))
        {
            return currentName.Replace(oldToken, replacement);
        }

        // no-op if oldToken doesn't match
        return currentName;
    }

    public override void Apply(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0) return;

        if (string.IsNullOrEmpty(newToken) && string.IsNullOrEmpty(oldToken))
        {
            EditorUtility.DisplayDialog("Multi-Rename", "Provide Old Token and/or New Token.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Multi-Rename");
        Undo.RecordObjects(targets, "Multi-Rename");

        foreach (var go in targets)
        {
            if (!go) continue;

            string objName = go.name;
            string replacement = ExpandTokens(go, newToken);

            if (string.IsNullOrEmpty(oldToken))
            {
                go.name = replacement;
                continue;
            }

            if (oldToken == "*")
            {
                // Ensure replacement contains '*', then substitute original name
                if (!replacement.Contains("*"))
                    replacement = "*" + replacement;

                replacement = replacement.Replace("*", objName);
                go.name = replacement;
                continue;
            }

            if (objName.Contains(oldToken))
            {
                go.name = objName.Replace(oldToken, replacement);
            }
        }
    }

    private string ExpandTokens(GameObject go, string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        if (allowParentToken && s.Contains("{parent}"))
        {
            string parentName = go.transform.parent ? go.transform.parent.name : "Root";
            s = s.Replace("{parent}", parentName);
        }
        if (allowSceneToken && s.Contains("{scene}"))
        {
            s = s.Replace("{scene}", SceneManager.GetActiveScene().name);
        }
        if (allowProjectToken && s.Contains("{project}"))
        {
            s = s.Replace("{project}", Application.productName);
        }
        if (allowTagToken && s.Contains("{tag}"))
        {
            s = s.Replace("{tag}", go.tag);
        }
        if (allowLayerToken && s.Contains("{layer}"))
        {
            s = s.Replace("{layer}", LayerMask.LayerToName(go.layer));
        }

        return s;
    }

    private static string SuggestOldToken(GameObject[] gos, int minLen)
    {
        if (gos == null || gos.Length < 2) return string.Empty;

        string subStr = gos[0] ? gos[0].name : string.Empty;
        for (int i = 1; i < gos.Length; i++)
        {
            if (!gos[i]) continue;
            subStr = LongestCommonSubstring(subStr, gos[i].name);
            if (string.IsNullOrEmpty(subStr)) break;
        }

        return (subStr.Length >= minLen) ? subStr : string.Empty;
    }

    // Same algorithm you used (kept for fidelity), but static + slightly cleaned up
    private static string LongestCommonSubstring(string X, string Y)
    {
        if (string.IsNullOrEmpty(X) || string.IsNullOrEmpty(Y)) return string.Empty;

        int m = X.Length;
        int n = Y.Length;
        int[,] LCSuff = new int[m + 1, n + 1];

        int len = 0;
        int row = 0, col = 0;

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (X[i - 1] == Y[j - 1])
                {
                    LCSuff[i, j] = LCSuff[i - 1, j - 1] + 1;
                    if (LCSuff[i, j] > len)
                    {
                        len = LCSuff[i, j];
                        row = i;
                        col = j;
                    }
                }
                else
                {
                    LCSuff[i, j] = 0;
                }
            }
        }

        if (len == 0) return string.Empty;

        // Reconstruct
        char[] result = new char[len];
        int k = len - 1;

        while (row > 0 && col > 0 && LCSuff[row, col] != 0)
        {
            result[k--] = X[row - 1];
            row--;
            col--;
        }

        return new string(result);
    }
}
}
