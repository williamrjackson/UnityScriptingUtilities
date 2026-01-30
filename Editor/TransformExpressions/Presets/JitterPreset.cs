using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Jitter", fileName = "JitterPreset")]
    public sealed class JitterPreset : TransformPreset
{
    [Tooltip("Random seed for reproducible jitter.")]
    [SerializeField] private int seed = 12345;

    [Header("Position (Local)")]
    [Tooltip("Random position offset per axis.")]
    [SerializeField] private Vector3 posJitter = new Vector3(0.5f, 0.0f, 0.5f);

    [Header("Rotation (Euler, degrees)")]
    [Tooltip("Random rotation offset per axis in degrees.")]
    [SerializeField] private Vector3 rotJitterEuler = new Vector3(0f, 15f, 0f);

    [Header("Scale (Uniform factor jitter)")]
    [Tooltip("Random scale factor jitter (applied uniformly).")]
    [SerializeField] private float uniformScaleJitter = 0.1f;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Adds random jitter to position, rotation, and scale of selected transforms.",
            MessageType.None);

        seed = EditorGUILayout.IntField("Seed", seed);
        posJitter = EditorGUILayout.Vector3Field("Pos Jitter", posJitter);
        rotJitterEuler = EditorGUILayout.Vector3Field("Rot Jitter", rotJitterEuler);
        uniformScaleJitter = EditorGUILayout.FloatField("Uniform Scale Jitter", uniformScaleJitter);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        var rnd = new System.Random(seed);

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            Vector3 dp = new Vector3(
                RandRange(rnd, -posJitter.x, posJitter.x),
                RandRange(rnd, -posJitter.y, posJitter.y),
                RandRange(rnd, -posJitter.z, posJitter.z)
            );

            Vector3 dr = new Vector3(
                RandRange(rnd, -rotJitterEuler.x, rotJitterEuler.x),
                RandRange(rnd, -rotJitterEuler.y, rotJitterEuler.y),
                RandRange(rnd, -rotJitterEuler.z, rotJitterEuler.z)
            );

            float ds = RandRange(rnd, -uniformScaleJitter, uniformScaleJitter);

            tr.localPosition += dp;
            tr.localRotation = Quaternion.Euler(tr.localEulerAngles + dr);
            tr.localScale *= (1f + ds);
        }
    }

    private static float RandRange(System.Random r, float min, float max)
        => (float)(min + (max - min) * r.NextDouble());
}
}
