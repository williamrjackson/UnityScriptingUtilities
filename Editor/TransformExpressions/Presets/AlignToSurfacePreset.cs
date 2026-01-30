using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(menuName = "Transform Expressions/Presets/Align To Surface", fileName = "AlignToSurfacePreset")]
    public sealed class AlignToSurfacePreset : TransformPreset
{
    private enum RayDirection { Down, Up, Forward, Back, Right, Left }

    [Header("Raycast")]
    [Tooltip("Direction to cast the ray for surface detection.")]
    [SerializeField] private RayDirection direction = RayDirection.Down;

    [Tooltip("Maximum distance for the raycast.")]
    [SerializeField] private float maxDistance = 100f;

    [Tooltip("Layers to include in the raycast.")]
    [SerializeField] private LayerMask layerMask = ~0;

    [Tooltip("How to handle trigger colliders.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Position")]
    [Tooltip("Move transforms to the hit point on the surface.")]
    [SerializeField] private bool setPosition = true;

    [Tooltip("Additional offset along the surface normal.")]
    [SerializeField] private float offsetAlongNormal = 0f;

    [Tooltip("Offset so that the object's renderer bounds abut the surface (makes it sit on the surface).")]
    [SerializeField] private bool offsetToAbutBounds = false;

    [Header("Rotation")]
    [SerializeField] private bool alignRotationToNormal = true;

    [Tooltip("Object local axis that should point 'out of' the surface normal.")]
    [SerializeField] private Vector3 localUpAxis = Vector3.up;

    [Tooltip("Optional: preserve current yaw around the normal by projecting forward.")]
    [SerializeField] private bool preserveYaw = false;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Casts rays from selected transforms to detect surfaces and optionally positions/rotates them to align.",
            MessageType.None);

        direction = (RayDirection)EditorGUILayout.EnumPopup("Ray Direction", direction);
        maxDistance = EditorGUILayout.FloatField("Max Distance", maxDistance);

        layerMask = LayerMaskField("Layer Mask", layerMask);
        triggerInteraction = (QueryTriggerInteraction)EditorGUILayout.EnumPopup("Triggers", triggerInteraction);

        EditorGUILayout.Space(4);
        setPosition = EditorGUILayout.ToggleLeft("Set Position to Hit", setPosition);
        offsetAlongNormal = EditorGUILayout.FloatField("Offset Along Normal", offsetAlongNormal);
        offsetToAbutBounds = EditorGUILayout.ToggleLeft("Offset to Abut Bounds (Sit on Surface)", offsetToAbutBounds);

        EditorGUILayout.Space(4);
        alignRotationToNormal = EditorGUILayout.ToggleLeft("Align Rotation to Normal", alignRotationToNormal);
        using (new EditorGUI.DisabledScope(!alignRotationToNormal))
        {
            localUpAxis = EditorGUILayout.Vector3Field("Local Up Axis", localUpAxis);
            preserveYaw = EditorGUILayout.ToggleLeft("Preserve Yaw", preserveYaw);
        }

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        if (targets.Length == 0) return;

        Vector3 worldDir = GetWorldDirection(direction);

        for (int i = 0; i < targets.Length; i++)
        {
            var tr = targets[i];
            if (!tr) continue;

            Vector3 origin = tr.position;
            if (Physics.Raycast(origin, worldDir, out RaycastHit hit, maxDistance, layerMask, triggerInteraction))
            {
                Vector3 pos = hit.point + hit.normal * offsetAlongNormal;

                if (offsetToAbutBounds)
                {
                    Bounds bounds = GetWorldBounds(tr);
                    float boundsOffset = Vector3.Dot(bounds.extents, hit.normal.normalized);
                    pos += hit.normal * boundsOffset;
                }

                if (setPosition)
                    tr.position = pos;

                if (alignRotationToNormal)
                {
                    Vector3 n = hit.normal.normalized;

                    Vector3 localUp = localUpAxis.sqrMagnitude < 1e-8f ? Vector3.up : localUpAxis.normalized;
                    Quaternion fromTo = Quaternion.FromToRotation(tr.TransformDirection(localUp), n);

                    if (!preserveYaw)
                    {
                        tr.rotation = fromTo * tr.rotation;
                    }
                    else
                    {
                        // Project current forward onto tangent plane to keep yaw-ish orientation
                        Vector3 fwd = tr.forward;
                        Vector3 projected = Vector3.ProjectOnPlane(fwd, n);
                        if (projected.sqrMagnitude < 1e-8f)
                            projected = Vector3.Cross(n, Vector3.right);

                        tr.rotation = Quaternion.LookRotation(projected.normalized, n);
                    }
                }
            }
        }
    }

    private static Vector3 GetWorldDirection(RayDirection dir)
    {
        return dir switch
        {
            RayDirection.Down => Vector3.down,
            RayDirection.Up => Vector3.up,
            RayDirection.Forward => Vector3.forward,
            RayDirection.Back => Vector3.back,
            RayDirection.Right => Vector3.right,
            RayDirection.Left => Vector3.left,
            _ => Vector3.down
        };
    }

    private static Bounds GetWorldBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        // Fallback to colliders if no renderers
        var colliders = root.GetComponentsInChildren<Collider>(includeInactive: false);
        if (colliders != null && colliders.Length > 0)
        {
            Bounds b = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                b.Encapsulate(colliders[i].bounds);
            }
            return b;
        }

        // Last resort: just the transform position with zero size
        return new Bounds(root.position, Vector3.zero);
    }

    // Nice IMGUI LayerMask field helper
    private static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        var layers = InternalEditorUtility.layers;
        int maskWithoutEmpty = 0;

        // Convert layerMask to field mask
        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);
            if (((1 << layer) & layerMask.value) != 0)
                maskWithoutEmpty |= (1 << i);
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

        // Convert back
        int mask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
            {
                int layer = LayerMask.NameToLayer(layers[i]);
                mask |= (1 << layer);
            }
        }

        layerMask.value = mask;
        return layerMask;
    }
}
}
