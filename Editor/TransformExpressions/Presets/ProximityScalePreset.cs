using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Proximity Scale", fileName = "ProximityScalePreset")]
    public sealed class ProximityScalePreset : TransformPreset
{
    [Tooltip("Reference transform for proximity calculation.")]
    [SerializeField] private Transform target = null;

    [Tooltip("Scale when at the furthest distance from the target.")]
    [SerializeField] private Vector3 startScale = Vector3.one;

    [Tooltip("Scale when at the closest distance to the target.")]
    [SerializeField] private Vector3 endScale = Vector3.one * 2f;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Scales selected transforms based on their distance to a target.",
            MessageType.None);

        target = EditorGUILayout.ObjectField("Target", target, typeof(Transform), true) as Transform;
        startScale = EditorGUILayout.Vector3Field("Start Scale", startScale);
        endScale = EditorGUILayout.Vector3Field("End Scale", endScale);
        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;
        if (!target) return;
        Vector3 tPos = target.position;
        float maxDist = GetMaxDistance(targets, tPos);
        float minDist = GetMinDistance(targets, tPos);
        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;
            float dist = Vector3.Distance(tr.position, tPos);
            float t = (maxDist - minDist) < 1e-5f ? 0f : (dist - minDist) / (maxDist - minDist);
            tr.localScale = Vector3.Lerp(startScale, endScale, t);
        }
    }

    private float GetMaxDistance(Transform[] targets, Vector3 point)
    {
        float maxDistSqr = 0f;
        foreach (var tr in targets)
        {
            if (!tr) continue;
            float distSqr = (tr.position - point).sqrMagnitude;
            if (distSqr > maxDistSqr)
                maxDistSqr = distSqr;
        }
        return Mathf.Sqrt(maxDistSqr);
    }
    private float GetMinDistance(Transform[] targets, Vector3 point)
    {
        float minDistSqr = float.MaxValue;
        foreach (var tr in targets)
        {
            if (!tr) continue;
            float distSqr = (tr.position - point).sqrMagnitude;
            if (distSqr < minDistSqr)
                minDistSqr = distSqr;
        }
        return Mathf.Sqrt(minDistSqr);
    }
}
}
