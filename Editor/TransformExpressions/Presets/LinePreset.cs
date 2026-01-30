using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Line", fileName = "LinePreset")]
    public sealed class LinePreset : TransformPreset
{
    [Tooltip("Use the centroid of the selected transforms as the starting point.")]
    [SerializeField] private bool useSelectionCentroidAsStart = false;

    [Tooltip("Starting position in local space.")]
    [SerializeField] private Vector3 startLocal = Vector3.zero;

    [Tooltip("Ending position in local space.")]
    [SerializeField] private Vector3 endLocal = new Vector3(10, 0, 0);

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Arranges selected transforms along a straight line between start and end points.",
            MessageType.None);

        useSelectionCentroidAsStart = EditorGUILayout.ToggleLeft("Start = Selection Centroid", useSelectionCentroidAsStart);

        using (new EditorGUI.DisabledScope(useSelectionCentroidAsStart))
            startLocal = EditorGUILayout.Vector3Field("Start (Local)", startLocal);

        endLocal = EditorGUILayout.Vector3Field("End (Local)", endLocal);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 a = useSelectionCentroidAsStart ? ctx.ComputeLocalCentroid(targets) : startLocal;
        Vector3 b = endLocal;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            float t = (n <= 1) ? 0f : (float)i / (n - 1);
            tr.localPosition = Vector3.Lerp(a, b, t);
        }
    }
}
}
