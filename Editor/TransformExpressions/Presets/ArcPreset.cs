using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Arc", fileName = "ArcPreset")]
    public sealed class ArcPreset : TransformPreset
{
    private enum Plane { XY, XZ, YZ }

    [Tooltip("The plane in which to arrange the arc.")]
    [SerializeField] private Plane plane = Plane.XZ;

    [Tooltip("Radius of the arc.")]
    [SerializeField] private float radius = 5f;

    [Tooltip("Use the centroid of the selected transforms as the center.")]
    [SerializeField] private bool useSelectionCentroidAsCenter = true;

    [Tooltip("Center position in local space.")]
    [SerializeField] private Vector3 centerLocal = Vector3.zero;

    [Tooltip("Starting angle in degrees.")]
    [SerializeField] private float startAngleDeg = 0f;

    [Tooltip("Ending angle in degrees.")]
    [SerializeField] private float endAngleDeg = 180f;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Arranges selected transforms along a circular arc.",
            MessageType.None);

        plane = (Plane)EditorGUILayout.EnumPopup("Plane", plane);
        radius = EditorGUILayout.FloatField("Radius", radius);

        useSelectionCentroidAsCenter = EditorGUILayout.ToggleLeft("Center = Selection Centroid", useSelectionCentroidAsCenter);
        using (new EditorGUI.DisabledScope(useSelectionCentroidAsCenter))
            centerLocal = EditorGUILayout.Vector3Field("Center (Local)", centerLocal);

        startAngleDeg = EditorGUILayout.FloatField("Start Angle (deg)", startAngleDeg);
        endAngleDeg = EditorGUILayout.FloatField("End Angle (deg)", endAngleDeg);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 center = useSelectionCentroidAsCenter ? ctx.ComputeLocalCentroid(targets) : centerLocal;

        float a0 = startAngleDeg * Mathf.Deg2Rad;
        float a1 = endAngleDeg * Mathf.Deg2Rad;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            float t = (n <= 1) ? 0f : (float)i / (n - 1);
            float ang = Mathf.Lerp(a0, a1, t);

            float cx = Mathf.Cos(ang) * radius;
            float cy = Mathf.Sin(ang) * radius;

            Vector3 p = center;
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
