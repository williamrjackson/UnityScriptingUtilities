using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Look At Point", fileName = "LookAtPointPreset")]
    public sealed class LookAtPointPreset : TransformPreset
{
    [Tooltip("Use the centroid of the selected transforms as the target point to look at.")]
    [SerializeField] private bool useSelectionCentroidAsTarget = true;

    [Tooltip("Specific transform to look at (ignored if using selection centroid).")]
    [SerializeField] private Transform target = null;

    [Tooltip("World up direction for the look rotation.")]
    [SerializeField] private Vector3 worldUp = Vector3.up;

    [Tooltip("Allow rotation around the X-axis.")]
    [SerializeField] private bool allowRotationX = true;

    [Tooltip("Allow rotation around the Y-axis.")]
    [SerializeField] private bool allowRotationY = true;

    [Tooltip("Allow rotation around the Z-axis.")]
    [SerializeField] private bool allowRotationZ = true;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Makes selected transforms look at a target point, with optional axis constraints.",
            MessageType.None);

        useSelectionCentroidAsTarget = EditorGUILayout.ToggleLeft("Use Selection Centroid as Target", useSelectionCentroidAsTarget);
        using (new EditorGUI.DisabledScope(useSelectionCentroidAsTarget))
            target = EditorGUILayout.ObjectField("Target", target, typeof(Transform), true) as Transform;

        worldUp = EditorGUILayout.Vector3Field("World Up", worldUp);

        EditorGUILayout.LabelField("Rotation Axes", EditorStyles.boldLabel);
        allowRotationX = EditorGUILayout.Toggle("Allow X Axis Rotation", allowRotationX);
        allowRotationY = EditorGUILayout.Toggle("Allow Y Axis Rotation", allowRotationY);
        allowRotationZ = EditorGUILayout.Toggle("Allow Z Axis Rotation", allowRotationZ);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        if (targets.Length == 0) return;

        Vector3 tWorld;
        if (useSelectionCentroidAsTarget)
        {
            // centroid in world space
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var tr in targets)
            {
                if (!tr) continue;
                sum += tr.position;
                count++;
            }
            tWorld = count > 0 ? sum / count : Vector3.zero;
        }
        else
        {
            tWorld = target.position;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            Vector3 dir = (tWorld - tr.position);
            if (dir.sqrMagnitude < 1e-8f) continue;

            Quaternion desiredRot = Quaternion.LookRotation(dir.normalized, worldUp.sqrMagnitude < 1e-8f ? Vector3.up : worldUp.normalized);
            Vector3 desiredEuler = desiredRot.eulerAngles;
            Vector3 currentEuler = tr.rotation.eulerAngles;

            if (allowRotationX) currentEuler.x = desiredEuler.x;
            if (allowRotationY) currentEuler.y = desiredEuler.y;
            if (allowRotationZ) currentEuler.z = desiredEuler.z;

            tr.rotation = Quaternion.Euler(currentEuler);
        }
    }
}
}
