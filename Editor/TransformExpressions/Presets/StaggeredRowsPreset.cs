using UnityEngine;
using UnityEditor;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Staggered Rows", fileName = "StaggeredRowsPreset")]
    public sealed class StaggeredRowsPreset : TransformPreset
{
    private enum Plane { XY, XZ, YZ }
    private enum StaggerMode { EveryOtherRow, EveryOtherColumn, Checkerboard }

    [SerializeField] private Plane plane = Plane.XZ;

    [SerializeField] private int columns = 6;
    [SerializeField] private Vector2 cellSize = new Vector2(2f, 2f);

    [Tooltip("Stagger offset as a fraction of cellSize.x (or the primary axis in the plane). Typical = 0.5")]
    [SerializeField] private float staggerFraction = 0.5f;

    [SerializeField] private StaggerMode staggerMode = StaggerMode.EveryOtherRow;

    [SerializeField] private bool originIsSelectionCentroid = true;
    [SerializeField] private Vector3 originLocal = Vector3.zero;
    [SerializeField] private bool centerGridAroundOrigin = true;

    [SerializeField] private bool snakeOrder = false;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        plane = (Plane)EditorGUILayout.EnumPopup("Plane", plane);

        columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
        cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);

        staggerMode = (StaggerMode)EditorGUILayout.EnumPopup("Stagger Mode", staggerMode);
        staggerFraction = EditorGUILayout.Slider("Stagger Fraction", staggerFraction, -1f, 1f);

        snakeOrder = EditorGUILayout.ToggleLeft("Snake Order (zig-zag rows)", snakeOrder);

        EditorGUILayout.Space(4);
        originIsSelectionCentroid = EditorGUILayout.ToggleLeft("Origin = Selection Centroid", originIsSelectionCentroid);
        using (new EditorGUI.DisabledScope(originIsSelectionCentroid))
            originLocal = EditorGUILayout.Vector3Field("Origin (Local)", originLocal);

        centerGridAroundOrigin = EditorGUILayout.ToggleLeft("Center Around Origin", centerGridAroundOrigin);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        int n = targets.Length;
        if (n == 0) return;

        Vector3 origin = originIsSelectionCentroid ? ctx.ComputeLocalCentroid(targets) : originLocal;

        int cols = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt(n / (float)cols);

        // Base centering offsets
        float ox = 0f, oy = 0f;
        if (centerGridAroundOrigin)
        {
            ox = (cols - 1) * cellSize.x * 0.5f;
            oy = (rows - 1) * cellSize.y * 0.5f;
        }

        float stagger = cellSize.x * staggerFraction;

        for (int i = 0; i < n; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            int x = i % cols;
            int y = i / cols;

            // Optional snake ordering (nice for seating/rows)
            int xEff = x;
            if (snakeOrder && (y % 2 == 1))
                xEff = (cols - 1) - x;

            float dx = xEff * cellSize.x - ox;
            float dy = y * cellSize.y - oy;

            bool doStagger = staggerMode switch
            {
                StaggerMode.EveryOtherRow => (y % 2 == 1),
                StaggerMode.EveryOtherColumn => (xEff % 2 == 1),
                StaggerMode.Checkerboard => ((xEff + y) % 2 == 1),
                _ => false
            };

            if (doStagger)
                dx += stagger;

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
