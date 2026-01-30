using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Helix", fileName = "HelixPreset")]
    public sealed class HelixPreset : TransformPreset
{
    private enum Plane { XY, XZ, YZ }
    private enum Origin { Bottom, Center, Top }

    [Tooltip("The plane in which the helical circle is arranged (XY, XZ, or YZ).")]
    [SerializeField] private Plane plane = Plane.XZ;

    [Tooltip("Radius of the helix.")]
    [SerializeField] private float radius = 5f;

    [Tooltip("Use the centroid of the selected transforms as the center.")]
    [SerializeField] private bool useSelectionCentroidAsCenter = true;

    [Tooltip("Center position in local space.")]
    [SerializeField] private Vector3 centerLocal = Vector3.zero;

    [Tooltip("Number of full turns in the helix.")]
    [SerializeField] private float turns = 2f;

    [Tooltip("Total height of the helix.")]
    [SerializeField] private float height = 5f;

    [Tooltip("Angle offset in degrees.")]
    [SerializeField] private float angleOffsetDeg = 0f;

    [Tooltip("Where the helix starts relative to the center: Bottom (starts at center), Center (centered), Top (starts above center).")]
    [SerializeField] private Origin origin = Origin.Bottom;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Arranges selected transforms in a helical spiral around a center point.",
            MessageType.None);

        plane = (Plane)EditorGUILayout.EnumPopup("Plane", plane);
        radius = EditorGUILayout.FloatField("Radius", radius);

        useSelectionCentroidAsCenter = EditorGUILayout.ToggleLeft("Center = Selection Centroid", useSelectionCentroidAsCenter);
        using (new EditorGUI.DisabledScope(useSelectionCentroidAsCenter))
            centerLocal = EditorGUILayout.Vector3Field("Center (Local)", centerLocal);

        turns = EditorGUILayout.FloatField("Turns", turns);
        height = EditorGUILayout.FloatField("Height", height);
        angleOffsetDeg = EditorGUILayout.FloatField("Angle Offset (deg)", angleOffsetDeg);

        origin = (Origin)EditorGUILayout.EnumPopup("Origin", origin);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 center = useSelectionCentroidAsCenter ? ctx.ComputeLocalCentroid(targets) : centerLocal;

        float offsetRad = angleOffsetDeg * Mathf.Deg2Rad;
        float totalAngle = Mathf.PI * 2f * turns;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            float t = (n <= 1) ? 0f : (float)i / (n - 1);
            float ang = totalAngle * t + offsetRad;

            float cx = Mathf.Cos(ang) * radius;
            float cy = Mathf.Sin(ang) * radius;
            float h = 0f;
            switch (origin)
            {
                case Origin.Bottom: h = height * t; break;
                case Origin.Center: h = height * (t - 0.5f); break;
                case Origin.Top: h = height * (t - 1f); break;
            }

            Vector3 p = center;
            switch (plane)
            {
                case Plane.XY: p.x += cx; p.y += cy; p.z += h; break;
                case Plane.XZ: p.x += cx; p.z += cy; p.y += h; break;
                case Plane.YZ: p.y += cx; p.z += cy; p.x += h; break;
            }

            tr.localPosition = p;
        }
    }
}
}
