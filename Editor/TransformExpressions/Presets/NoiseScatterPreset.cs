using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Noise Scatter", fileName = "NoiseScatterPreset")]
    public sealed class NoiseScatterPreset : TransformPreset
{
    [Tooltip("Random seed for reproducible noise.")]
    [SerializeField] private int seed = 12345;

    [Header("Noise Field")]
    [Tooltip("Higher = more variation over short distances.")]
    [SerializeField] private float noiseFrequency = 0.25f;

    [Tooltip("Offset magnitude (local units) per axis.")]
    [SerializeField] private Vector3 strength = new Vector3(1f, 0.2f, 1f);

    [Header("Space")]
    [Tooltip("If ON: add noise to current positions. If OFF: position is origin + noise.")]
    [SerializeField] private bool additive = true;

    [Tooltip("If additive is OFF, origin is used as the center.")]
    [SerializeField] private bool originIsSelectionCentroid = true;

    [SerializeField] private Vector3 originLocal = Vector3.zero;

    [Header("Distribution")]
    [Tooltip("If ON: noise uses each object's current position as input. If OFF: uses selection index order.")]
    [SerializeField] private bool sampleFromPosition = true;

    [Tooltip("Used when sampleFromPosition is OFF.")]
    [SerializeField] private float indexStep = 0.33f;

    [Header("Apply To")]
    [Tooltip("Apply noise to X axis.")]
    [SerializeField] private bool applyX = true;

    [Tooltip("Apply noise to Y axis.")]
    [SerializeField] private bool applyY = true;

    [Tooltip("Apply noise to Z axis.")]
    [SerializeField] private bool applyZ = true;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Scatters selected transforms using Perlin noise, creating natural-looking variation.",
            MessageType.None);

        seed = EditorGUILayout.IntField("Seed", seed);
        noiseFrequency = EditorGUILayout.FloatField("Noise Frequency", noiseFrequency);
        strength = EditorGUILayout.Vector3Field("Strength", strength);

        EditorGUILayout.Space(4);
        additive = EditorGUILayout.ToggleLeft("Additive (offset current positions)", additive);

        using (new EditorGUI.DisabledScope(additive))
        {
            originIsSelectionCentroid = EditorGUILayout.ToggleLeft("Origin = Selection Centroid", originIsSelectionCentroid);
            using (new EditorGUI.DisabledScope(originIsSelectionCentroid))
                originLocal = EditorGUILayout.Vector3Field("Origin (Local)", originLocal);
        }

        EditorGUILayout.Space(4);
        sampleFromPosition = EditorGUILayout.ToggleLeft("Sample from current position", sampleFromPosition);
        using (new EditorGUI.DisabledScope(sampleFromPosition))
            indexStep = EditorGUILayout.FloatField("Index Step", indexStep);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Apply Axes", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            applyX = EditorGUILayout.ToggleLeft("X", applyX, GUILayout.Width(40));
            applyY = EditorGUILayout.ToggleLeft("Y", applyY, GUILayout.Width(40));
            applyZ = EditorGUILayout.ToggleLeft("Z", applyZ, GUILayout.Width(40));
        }

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 origin = originLocal;
        if (!additive && originIsSelectionCentroid)
            origin = ctx.ComputeLocalCentroid(targets);

        // Seed offsets for decorrelated channels
        float sx = seed * 0.1031f + 11.1f;
        float sy = seed * 0.1379f + 37.7f;
        float sz = seed * 0.1733f + 73.3f;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            Vector3 basePos = additive ? tr.localPosition : origin;

            // Inputs to noise
            float ix, iy;
            if (sampleFromPosition)
            {
                // Use current local position as input (coherent field)
                var p = tr.localPosition;
                ix = p.x * noiseFrequency;
                iy = p.z * noiseFrequency; // use XZ as "ground plane" input by default
            }
            else
            {
                // Use selection index (ordered)
                float t = i * indexStep;
                ix = t * noiseFrequency;
                iy = (t + 19.19f) * noiseFrequency;
            }

            // 0..1 -> -1..1
            float nx = (Mathf.PerlinNoise(ix + sx, iy + sx) * 2f) - 1f;
            float ny = (Mathf.PerlinNoise(ix + sy, iy + sy) * 2f) - 1f;
            float nz = (Mathf.PerlinNoise(ix + sz, iy + sz) * 2f) - 1f;

            Vector3 delta = new Vector3(nx * strength.x, ny * strength.y, nz * strength.z);

            Vector3 next = basePos;
            if (applyX) next.x = basePos.x + delta.x;
            if (applyY) next.y = basePos.y + delta.y;
            if (applyZ) next.z = basePos.z + delta.z;

            tr.localPosition = next;
        }
    }
}
}
