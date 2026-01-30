using System;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(
        menuName = "Transform Expressions/Presets/Align Pivot Or Bounds",
        fileName = "AlignPivotOrBoundsPreset")]
    public sealed class AlignPivotOrBoundsPreset : TransformPreset
{
    private enum AlignMode { Pivot, Bounds }
    private enum AnchorMode { FirstSelected, LastSelected }
    private enum BoundsSource { PreferRenderer, RendererOnly, ColliderOnly }
    private enum AlignPoint { Min, Center, Max }

    [Header("General")]
    [Tooltip("Align using transform pivot or bounding box.")]
    [SerializeField] private AlignMode mode = AlignMode.Bounds;

    [Tooltip("Which selected transform to use as the anchor/reference.")]
    [SerializeField] private AnchorMode anchorMode = AnchorMode.FirstSelected;

    [Tooltip("Include the anchor transform in the operation.")]
    [SerializeField] private bool includeAnchorInOperation = false;

    [Header("Bounds Settings")]
    [Tooltip("Source for calculating bounds: prefer renderer, renderer only, or collider only.")]
    [SerializeField] private BoundsSource boundsSource = BoundsSource.PreferRenderer;

    [Tooltip("Include child transforms when calculating bounds.")]
    [SerializeField] private bool includeChildren = true;

    [Header("Axis Rules")]
    [SerializeField] private AxisRule x = AxisRule.DefaultMatch();
    [SerializeField] private AxisRule y = AxisRule.DefaultMatch();
    [SerializeField] private AxisRule z = AxisRule.DefaultMatch();

    [Header("Offset (World Space)")]
    [Tooltip("Additional position offset applied to all targets.")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Serializable]
    private struct AxisRule
    {
        public bool enabled;
        public AlignPoint targetPoint;
        public AlignPoint anchorPoint;

        public static AxisRule DefaultMatch()
        {
            return new AxisRule
            {
                enabled = true,
                targetPoint = AlignPoint.Center,
                anchorPoint = AlignPoint.Center
            };
        }
    }

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Aligns selected transforms to an anchor transform using pivot or bounds points, with per-axis control.",
            MessageType.None);

        mode = (AlignMode)EditorGUILayout.EnumPopup("Mode", mode);
        anchorMode = (AnchorMode)EditorGUILayout.EnumPopup("Anchor", anchorMode);
        includeAnchorInOperation = EditorGUILayout.ToggleLeft("Include anchor in operation", includeAnchorInOperation);

        if (mode == AlignMode.Bounds)
        {
            boundsSource = (BoundsSource)EditorGUILayout.EnumPopup("Bounds Source", boundsSource);
            includeChildren = EditorGUILayout.ToggleLeft("Include children", includeChildren);
        }

        EditorGUILayout.Space(8);
        DrawShortcutsGUI();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Axis Rules", EditorStyles.miniBoldLabel);

        DrawAxisRuleRow("X", ref x);
        DrawAxisRuleRow("Y", ref y);
        DrawAxisRuleRow("Z", ref z);

        EditorGUILayout.Space(6);
        offset = EditorGUILayout.Vector3Field("Offset", offset);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Match Min/Min", GUILayout.Width(110)))
            {
                x = y = z = new AxisRule { enabled = true, targetPoint = AlignPoint.Min, anchorPoint = AlignPoint.Min };
                offset = Vector3.zero;
                EditorUtility.SetDirty(this);
            }

            if (GUILayout.Button("Match Center/Center", GUILayout.Width(130)))
            {
                x = y = z = new AxisRule { enabled = true, targetPoint = AlignPoint.Center, anchorPoint = AlignPoint.Center };
                offset = Vector3.zero;
                EditorUtility.SetDirty(this);
            }

            if (GUILayout.Button("Match Max/Max", GUILayout.Width(110)))
            {
                x = y = z = new AxisRule { enabled = true, targetPoint = AlignPoint.Max, anchorPoint = AlignPoint.Max };
                offset = Vector3.zero;
                EditorUtility.SetDirty(this);
            }
            
        }

        return EditorGUI.EndChangeCheck();
    }

    private void DrawShortcutsGUI()
    {
        EditorGUILayout.LabelField("Shortcuts", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox(
                "These buttons act on the current Selection immediately (one-click). " +
                "They do not require pressing Apply in the window.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0))
                {
                    if (GUILayout.Button("Put on Floor (to Anchor Top)"))
                        RunShortcut_BoundsAxisToAnchor(
                            axis: 1,
                            targetPoint: AlignPoint.Min,   // bottom of each target
                            anchorPoint: AlignPoint.Max,   // top of anchor (the floor)
                            undoName: "Put on Floor (to Anchor Top)"
                        );
                    if (GUILayout.Button("Put on Floor (Bounds Y Min → 0)"))
                        RunShortcut_BoundsAxisToWorld(1, AlignPoint.Min, 0f, "Put on Floor");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0))
                {
                    if (GUILayout.Button("Put on Ceiling (to Anchor Bottom)"))
                        RunShortcut_BoundsAxisToAnchor(
                            axis: 1,
                            targetPoint: AlignPoint.Max,   // Top of each target
                            anchorPoint: AlignPoint.Min,   // Bottom of anchor (the ceiling)
                            undoName: "Put on Ceiling (to Anchor Bottom)"
                        );
                    if (GUILayout.Button("Put on Ceiling (Bounds Y Max → 0)"))
                        RunShortcut_BoundsAxisToWorld(1, AlignPoint.Max, 0f, "Put on Ceiling");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0))
                {
                    if (GUILayout.Button("Put Against Left Wall (to Anchor Right)"))
                        RunShortcut_BoundsAxisToAnchor(
                            axis: 0,
                            targetPoint: AlignPoint.Max,   // right side of target
                            anchorPoint: AlignPoint.Min,   // left side of anchor
                            undoName: "Put Against Left Wall"
                        );
                    if (GUILayout.Button("Put Against Right Wall (to Anchor Left)"))
                        RunShortcut_BoundsAxisToAnchor(
                            axis: 0,
                            targetPoint: AlignPoint.Min,   // left side of target
                            anchorPoint: AlignPoint.Max,   // right side of anchor
                            undoName: "Put Against Right Wall"
                        );
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0))
                {
                    if (GUILayout.Button("Put Against Front Wall (to Anchor Back)"))
                        RunShortcut_BoundsAxisToAnchor(
                            axis: 2,
                            targetPoint: AlignPoint.Max,   // back side of target
                            anchorPoint: AlignPoint.Min,   // front side of anchor
                            undoName: "Put Against Front Wall"
                        );
                    if (GUILayout.Button("Put Against Back Wall (to Anchor Front)"))
                        RunShortcut_BoundsAxisToAnchor(
                            axis: 2,
                            targetPoint: AlignPoint.Min,   // front side of target
                            anchorPoint: AlignPoint.Max,   // back side of anchor
                            undoName: "Put Against Back Wall"
                        );
                }
            }
        }
    }

    private static void DrawAxisRuleRow(string label, ref AxisRule rule)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            rule.enabled = EditorGUILayout.ToggleLeft(label, rule.enabled, GUILayout.Width(28));

            using (new EditorGUI.DisabledScope(!rule.enabled))
            {
                rule.targetPoint = (AlignPoint)EditorGUILayout.EnumPopup(rule.targetPoint, GUILayout.Width(85));
                GUILayout.Label("→", GUILayout.Width(14));
                rule.anchorPoint = (AlignPoint)EditorGUILayout.EnumPopup(rule.anchorPoint, GUILayout.Width(85));
            }

            GUILayout.FlexibleSpace();
        }
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        if (targets == null || targets.Length == 0) return;

        int anchorIndex = anchorMode == AnchorMode.FirstSelected ? 0 : (targets.Length - 1);
        var anchor = targets[anchorIndex];
        if (!anchor) return;

        Bounds anchorBounds = default;
        if (mode == AlignMode.Bounds)
            anchorBounds = GetWorldBounds(anchor);

        Vector3 anchorPivot = anchor.position;

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            if (!includeAnchorInOperation && tr == anchor)
                continue;

            Vector3 delta = Vector3.zero;

            if (mode == AlignMode.Pivot)
            {
                if (x.enabled) delta.x = (anchorPivot.x - tr.position.x) + offset.x;
                if (y.enabled) delta.y = (anchorPivot.y - tr.position.y) + offset.y;
                if (z.enabled) delta.z = (anchorPivot.z - tr.position.z) + offset.z;

                tr.position += delta;
                continue;
            }

            Bounds trBounds = GetWorldBounds(tr);

            if (x.enabled)
            {
                float tp = Pick(trBounds, x.targetPoint, axis: 0);
                float ap = Pick(anchorBounds, x.anchorPoint, axis: 0);
                delta.x = (ap - tp) + offset.x;
            }
            if (y.enabled)
            {
                float tp = Pick(trBounds, y.targetPoint, axis: 1);
                float ap = Pick(anchorBounds, y.anchorPoint, axis: 1);
                delta.y = (ap - tp) + offset.y;
            }
            if (z.enabled)
            {
                float tp = Pick(trBounds, z.targetPoint, axis: 2);
                float ap = Pick(anchorBounds, z.anchorPoint, axis: 2);
                delta.z = (ap - tp) + offset.z;
            }

            tr.position += delta;
        }
    }

    // -------------------------
    // Shortcuts (act immediately on Selection)
    // -------------------------

    private void RunShortcut_BoundsAxisToAnchor(int axis, AlignPoint targetPoint, AlignPoint anchorPoint, string undoName)
    {
        var targets = Selection.transforms ?? Array.Empty<Transform>();
        if (targets.Length == 0) return;

        // Resolve anchor from selection using the same semantics as the preset.
        int anchorIndex = anchorMode == AnchorMode.FirstSelected ? 0 : (targets.Length - 1);
        var anchor = targets[anchorIndex];
        if (!anchor) return;

        Bounds anchorBounds = GetWorldBounds(anchor);
        float anchorValue = Pick(anchorBounds, anchorPoint, axis);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        Undo.RecordObjects(targets, undoName);

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            // If we’re excluding the anchor, skip it.
            if (!includeAnchorInOperation && i == anchorIndex)
                continue;

            Bounds b = GetWorldBounds(tr);
            float tp = Pick(b, targetPoint, axis);
            float d = anchorValue - tp;

            var p = tr.position;
            if (axis == 0) p.x += d;
            else if (axis == 1) p.y += d;
            else p.z += d;

            tr.position = p;
        }
    }

    private void RunShortcut_BoundsAxisToWorld(int axis, AlignPoint targetPoint, float worldValue, string undoName)
    {
        var targets = Selection.transforms ?? Array.Empty<Transform>();
        if (targets.Length == 0) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        Undo.RecordObjects(targets, undoName);

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            if (!includeAnchorInOperation)
            {
                // treat "anchor" as first/last selected, same as preset semantics
                int anchorIndex = anchorMode == AnchorMode.FirstSelected ? 0 : (targets.Length - 1);
                if (i == anchorIndex) continue;
            }

            Bounds b = GetWorldBounds(tr);
            float tp = Pick(b, targetPoint, axis);
            float d = worldValue - tp;

            var p = tr.position;
            if (axis == 0) p.x += d;
            else if (axis == 1) p.y += d;
            else p.z += d;

            tr.position = p;
        }
    }

    private void RunShortcut_BoundsXZCenterToWorldZero(string undoName)
    {
        var targets = Selection.transforms ?? Array.Empty<Transform>();
        if (targets.Length == 0) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        Undo.RecordObjects(targets, undoName);

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            if (!includeAnchorInOperation)
            {
                int anchorIndex = anchorMode == AnchorMode.FirstSelected ? 0 : (targets.Length - 1);
                if (i == anchorIndex) continue;
            }

            Bounds b = GetWorldBounds(tr);

            float dx = 0f - b.center.x;
            float dz = 0f - b.center.z;

            tr.position = new Vector3(tr.position.x + dx, tr.position.y, tr.position.z + dz);
        }
    }

    private void RunShortcut_PivotAxisToWorld(int axis, float worldValue, string undoName)
    {
        var targets = Selection.transforms ?? Array.Empty<Transform>();
        if (targets.Length == 0) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        Undo.RecordObjects(targets, undoName);

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            if (!includeAnchorInOperation)
            {
                int anchorIndex = anchorMode == AnchorMode.FirstSelected ? 0 : (targets.Length - 1);
                if (i == anchorIndex) continue;
            }

            var p = tr.position;
            float d = worldValue - (axis == 0 ? p.x : axis == 1 ? p.y : p.z);

            if (axis == 0) p.x += d;
            else if (axis == 1) p.y += d;
            else p.z += d;

            tr.position = p;
        }
    }

    // -------------------------
    // Bounds helpers
    // -------------------------

    private Bounds GetWorldBounds(Transform root)
    {
        Bounds b = new Bounds(root.position, Vector3.zero);
        bool hasAny = false;

        if (boundsSource == BoundsSource.PreferRenderer || boundsSource == BoundsSource.RendererOnly)
        {
            if (TryEncapsulateRenderers(root, ref b, ref hasAny))
                return b;

            if (boundsSource == BoundsSource.RendererOnly)
                return b;
        }

        if (boundsSource == BoundsSource.PreferRenderer || boundsSource == BoundsSource.ColliderOnly)
        {
            TryEncapsulateColliders(root, ref b, ref hasAny);
        }

        return b;
    }

    private bool TryEncapsulateRenderers(Transform root, ref Bounds b, ref bool hasAny)
    {
        var renderers = includeChildren
            ? root.GetComponentsInChildren<Renderer>(includeInactive: false)
            : root.GetComponents<Renderer>();

        if (renderers == null || renderers.Length == 0) return false;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            if (!hasAny)
            {
                b = r.bounds;
                hasAny = true;
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }

        return hasAny;
    }

    private bool TryEncapsulateColliders(Transform root, ref Bounds b, ref bool hasAny)
    {
        var colliders = includeChildren
            ? root.GetComponentsInChildren<Collider>(includeInactive: false)
            : root.GetComponents<Collider>();

        if (colliders == null || colliders.Length == 0) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            var c = colliders[i];
            if (!c) continue;

            if (!hasAny)
            {
                b = c.bounds;
                hasAny = true;
            }
            else
            {
                b.Encapsulate(c.bounds);
            }
        }

        return hasAny;
    }

    private static float Pick(Bounds b, AlignPoint p, int axis)
    {
        return p switch
        {
            AlignPoint.Min => axis == 0 ? b.min.x : axis == 1 ? b.min.y : b.min.z,
            AlignPoint.Center => axis == 0 ? b.center.x : axis == 1 ? b.center.y : b.center.z,
            AlignPoint.Max => axis == 0 ? b.max.x : axis == 1 ? b.max.y : b.max.z,
            _ => 0f
        };
    }
}
}
