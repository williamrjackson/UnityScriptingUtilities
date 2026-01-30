using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Mirror", fileName = "MirrorPreset")]
    public sealed class MirrorPreset : TransformPreset
{
    private enum Axis { X, Y, Z }

    [Tooltip("Axis perpendicular to the mirror plane.")]
    [SerializeField] private Axis axis = Axis.X;

    [Tooltip("Mirror around the centroid of the selected transforms.")]
    [SerializeField] private bool mirrorAroundSelectionCentroid = true;

    [Tooltip("Position of the mirror plane along the axis in local space (if not using centroid).")]
    [SerializeField] private float mirrorPlaneLocal = 0f;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Mirrors selected transforms across a plane defined by an axis.",
            MessageType.None);

        axis = (Axis)EditorGUILayout.EnumPopup("Axis", axis);
        mirrorAroundSelectionCentroid = EditorGUILayout.ToggleLeft("Plane = Selection Centroid", mirrorAroundSelectionCentroid);

        using (new EditorGUI.DisabledScope(mirrorAroundSelectionCentroid))
            mirrorPlaneLocal = EditorGUILayout.FloatField("Plane (Local axis value)", mirrorPlaneLocal);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        if (targets.Length == 0) return;

        float planeVal = mirrorPlaneLocal;
        if (mirrorAroundSelectionCentroid)
        {
            var c = ctx.ComputeLocalCentroid(targets);
            planeVal = axis switch
            {
                Axis.X => c.x,
                Axis.Y => c.y,
                Axis.Z => c.z,
                _ => planeVal
            };
        }

        foreach (var tr in targets)
        {
            if (!tr) continue;

            var p = tr.localPosition;
            switch (axis)
            {
                case Axis.X: p.x = planeVal - (p.x - planeVal); break;
                case Axis.Y: p.y = planeVal - (p.y - planeVal); break;
                case Axis.Z: p.z = planeVal - (p.z - planeVal); break;
            }
            tr.localPosition = p;
        }
    }
}
}
