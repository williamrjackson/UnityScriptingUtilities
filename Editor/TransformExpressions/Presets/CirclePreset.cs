using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Circle", fileName = "CirclePreset")]
    public sealed class CirclePreset : TransformPreset
{
    private enum Plane { XY, XZ, YZ }

    [Tooltip("The plane in which to arrange the circle (XY, XZ, or YZ).")]
    [SerializeField] private Plane plane = Plane.XZ;

    [Tooltip("Radius of the circle.")]
    [SerializeField] private float radius = 5f;

    [Tooltip("Center position in local space.")]
    [SerializeField] private Vector3 center = Vector3.zero;

    [Tooltip("Angle offset in degrees.")]
    [SerializeField] private float angleOffsetDeg = 0f;

    [Tooltip("Use the centroid of the selected transforms as the center.")]
    [SerializeField] private bool useSelectionCentroidAsCenter = false;

    [Tooltip("Arrange transforms counter-clockwise around the circle.")]
    [SerializeField] private bool counterClockwise = true;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Arranges selected transforms in a circle around a center point.",
            MessageType.None);

        plane = (Plane)EditorGUILayout.EnumPopup("Plane", plane);
        radius = EditorGUILayout.FloatField("Radius", radius);

        using (new EditorGUILayout.HorizontalScope())
        {
            useSelectionCentroidAsCenter = EditorGUILayout.ToggleLeft("Use Selection Centroid", useSelectionCentroidAsCenter, GUILayout.Width(180));

            using (new EditorGUI.DisabledScope(!useSelectionCentroidAsCenter))
            {
                if (GUILayout.Button("Capture Now", GUILayout.Width(100)))
                {
                    var targets = ctx.GetOrderedSelection();
                    center = ctx.ComputeLocalCentroid(targets);
                    return true;
                }
            }
        }

        using (new EditorGUI.DisabledScope(useSelectionCentroidAsCenter))
        {
            center = EditorGUILayout.Vector3Field("Center (Local)", center);
        }

        angleOffsetDeg = EditorGUILayout.FloatField("Angle Offset (deg)", angleOffsetDeg);
        counterClockwise = EditorGUILayout.Toggle("Counter-Clockwise", counterClockwise);

        return false;
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        float offsetRad = angleOffsetDeg * Mathf.Deg2Rad;

        Vector3 finalCenter = useSelectionCentroidAsCenter
            ? ctx.ComputeLocalCentroid(targets)
            : center;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            float tt = (float)i / n;
            float sign = counterClockwise ? 1f : -1f;
            float angle = sign * tt * Mathf.PI * 2f + offsetRad;

            float cx = Mathf.Cos(angle) * radius;
            float cy = Mathf.Sin(angle) * radius;

            var p = finalCenter;
            switch (plane)
            {
                case Plane.XY: p.x += cx; p.y += cy; break;
                case Plane.XZ: p.x += cx; p.z += cy; break;
                case Plane.YZ: p.y += cx; p.z += cy; break;
            }

            tr.localPosition = p;
        }
    }
}
}
