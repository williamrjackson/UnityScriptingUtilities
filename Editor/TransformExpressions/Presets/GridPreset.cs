using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Grid", fileName = "GridPreset")]
    public sealed class GridPreset : TransformPreset
{
    [Tooltip("Number of columns in the grid.")]
    [SerializeField] private int columns = 5;

    [Tooltip("Size of each cell in the grid.")]
    [SerializeField] private Vector2 cellSize = new Vector2(2f, 2f);

    private enum Plane { XY, XZ, YZ }

    [Tooltip("The plane in which to arrange the grid.")]
    [SerializeField] private Plane plane = Plane.XZ;

    [Tooltip("Use the centroid of the selected transforms as the origin.")]
    [SerializeField] private bool useSelectionCentroidAsOrigin = true;

    [Tooltip("Origin position in local space.")]
    [SerializeField] private Vector3 originLocal = Vector3.zero;

    [Tooltip("Center the grid around the origin point.")]
    [SerializeField] private bool centerGrid = true;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Arranges selected transforms in a rectangular grid pattern.",
            MessageType.None);

        columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
        cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);
        plane = (Plane)EditorGUILayout.EnumPopup("Plane", plane);

        useSelectionCentroidAsOrigin = EditorGUILayout.ToggleLeft("Origin = Selection Centroid", useSelectionCentroidAsOrigin);
        using (new EditorGUI.DisabledScope(useSelectionCentroidAsOrigin))
            originLocal = EditorGUILayout.Vector3Field("Origin (Local)", originLocal);

        centerGrid = EditorGUILayout.ToggleLeft("Center Grid Around Origin", centerGrid);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 origin = useSelectionCentroidAsOrigin ? ctx.ComputeLocalCentroid(targets) : originLocal;

        int cols = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt(n / (float)cols);

        // Compute offsets so grid is centered around origin if requested.
        float ox = 0f, oy = 0f;
        if (centerGrid)
        {
            ox = (cols - 1) * cellSize.x * 0.5f;
            oy = (rows - 1) * cellSize.y * 0.5f;
        }

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            int x = i % cols;
            int y = i / cols;

            float dx = x * cellSize.x - ox;
            float dy = y * cellSize.y - oy;

            Vector3 p = origin;
            switch (plane)
            {
                case Plane.XY: p.x += dx; p.y += dy; break;
                case Plane.XZ: p.x += dx; p.z += dy; break;
                case Plane.YZ: p.y += dx; p.z += dy; break;
            }

            tr.localPosition = p;
        }
    }
}
}
