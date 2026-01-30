using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Wave", fileName = "WavePreset")]
    public sealed class WavePreset : TransformPreset
{
    private enum Axis { X, Y, Z }

    [Tooltip("Axis along which the wave propagates.")]
    [SerializeField] private Axis alongAxis = Axis.X;

    [Tooltip("Axis perpendicular to the propagation along which the wave oscillates.")]
    [SerializeField] private Axis waveAxis = Axis.Y;

    [Tooltip("Total length along the propagation axis.")]
    [SerializeField] private float length = 10f;

    [Tooltip("Amplitude of the wave oscillation.")]
    [SerializeField] private float amplitude = 2f;

    [Tooltip("Number of complete wave cycles.")]
    [SerializeField] private float cycles = 1f;

    [Tooltip("Phase offset in degrees.")]
    [SerializeField] private float phaseDeg = 0f;

    [Tooltip("Use the centroid of the selected transforms as the origin.")]
    [SerializeField] private bool useSelectionCentroidAsOrigin = true;

    [Tooltip("Origin position in local space.")]
    [SerializeField] private Vector3 originLocal = Vector3.zero;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Arranges selected transforms in a sinusoidal wave pattern.",
            MessageType.None);
        {
            EditorGUI.BeginChangeCheck();

            alongAxis = (Axis)EditorGUILayout.EnumPopup("Along Axis", alongAxis);
            waveAxis = (Axis)EditorGUILayout.EnumPopup("Wave Axis", waveAxis);

            length = EditorGUILayout.FloatField("Length", length);
            amplitude = EditorGUILayout.FloatField("Amplitude", amplitude);
            cycles = EditorGUILayout.FloatField("Cycles", cycles);
            phaseDeg = EditorGUILayout.FloatField("Phase (deg)", phaseDeg);

            useSelectionCentroidAsOrigin = EditorGUILayout.ToggleLeft("Origin = Selection Centroid", useSelectionCentroidAsOrigin);
            using (new EditorGUI.DisabledScope(useSelectionCentroidAsOrigin))
                originLocal = EditorGUILayout.Vector3Field("Origin (Local)", originLocal);

            return EditorGUI.EndChangeCheck();
        }
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 origin = useSelectionCentroidAsOrigin ? ctx.ComputeLocalCentroid(targets) : originLocal;
        float phase = phaseDeg * Mathf.Deg2Rad;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            float t = (n <= 1) ? 0f : (float)i / (n - 1);

            float along = (t - 0.5f) * length;
            float wave = Mathf.Sin((Mathf.PI * 2f * cycles * t) + phase) * amplitude;

            Vector3 p = origin;
            AddAxis(ref p, alongAxis, along);
            AddAxis(ref p, waveAxis, wave);

            tr.localPosition = p;
        }
    }

    private static void AddAxis(ref Vector3 v, Axis axis, float add)
    {
        switch (axis)
        {
            case Axis.X: v.x += add; break;
            case Axis.Y: v.y += add; break;
            case Axis.Z: v.z += add; break;
        }
    }
}
}