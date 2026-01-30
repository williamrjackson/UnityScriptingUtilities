using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Progressive Scale", fileName = "ProgressiveScalePreset")]
    public sealed class ProgressiveScalePreset : TransformPreset
{
    [Tooltip("Scale at the first selected transform.")]
    [SerializeField] private Vector3 startScale = Vector3.one * 0.5f;

    [Tooltip("Scale at the last selected transform.")]
    [SerializeField] private Vector3 endScale = Vector3.one * 2f;

    [Tooltip("Curve to control scale progression.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Clamp curve values to 0..1 range.")]
    [SerializeField] private bool clampCurve01 = true;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Progressively scales selected transforms from start to end scale across the selection.",
            MessageType.None);

        startScale = EditorGUILayout.Vector3Field("Start Scale", startScale);
        endScale = EditorGUILayout.Vector3Field("End Scale", endScale);
        curve = EditorGUILayout.CurveField("Curve", curve);
        clampCurve01 = EditorGUILayout.ToggleLeft("Clamp Curve 0..1", clampCurve01);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            float t = (n <= 1) ? 0f : (float)i / (n - 1);
            float u = curve != null ? curve.Evaluate(t) : t;
            if (clampCurve01) u = Mathf.Clamp01(u);

            tr.localScale = Vector3.Lerp(startScale, endScale, u);
        }
    }
}
}
