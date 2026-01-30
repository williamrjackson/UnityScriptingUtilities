using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(
        menuName = "Transform Expressions/Presets/Match Transform",
        fileName = "MatchTransformPreset")]
    public sealed class MatchTransformPreset : TransformPreset
{
    [Header("Source")]
    [Tooltip("Use the first selected transform as the source to copy from.")]
    [SerializeField] private bool useFirstSelectedAsSource = true;

    [Tooltip("Specific transform to copy from (ignored if using first selected).")]
    [SerializeField] private Transform explicitSource;

    [Header("What To Copy")]
    [Tooltip("Copy position from source to targets.")]
    [SerializeField] private bool copyPosition = true;

    [Tooltip("Copy rotation from source to targets.")]
    [SerializeField] private bool copyRotation = true;

    [Tooltip("Copy scale from source to targets.")]
    [SerializeField] private bool copyScale = false;

    [Header("Space")]
    [Tooltip("Use world space for position/rotation, or local space.")]
    [SerializeField] private bool useWorldSpace = true;

    [Header("Axis Masks (Position)")]
    [Tooltip("Copy X position.")]
    [SerializeField] private bool posX = true;

    [Tooltip("Copy Y position.")]
    [SerializeField] private bool posY = true;

    [Tooltip("Copy Z position.")]
    [SerializeField] private bool posZ = true;

    [Header("Axis Masks (Rotation)")]
    [Tooltip("Copy X rotation.")]
    [SerializeField] private bool rotX = true;

    [Tooltip("Copy Y rotation.")]
    [SerializeField] private bool rotY = true;

    [Tooltip("Copy Z rotation.")]
    [SerializeField] private bool rotZ = true;

    [Header("Axis Masks (Scale)")]
    [Tooltip("Copy X scale.")]
    [SerializeField] private bool scaleX = true;

    [Tooltip("Copy Y scale.")]
    [SerializeField] private bool scaleY = true;

    [Tooltip("Copy Z scale.")]
    [SerializeField] private bool scaleZ = true;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.HelpBox(
            "Copies transform values from a source object to the rest of the selection.\n\n" +
            "If 'Use First Selected' is enabled, the first selected Transform becomes the source.",
            MessageType.None);

        useFirstSelectedAsSource =
            EditorGUILayout.Toggle("Use First Selected", useFirstSelectedAsSource);

        if (!useFirstSelectedAsSource)
        {
            explicitSource = (Transform)EditorGUILayout.ObjectField(
                "Source",
                explicitSource,
                typeof(Transform),
                true);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Components", EditorStyles.miniBoldLabel);
        copyPosition = EditorGUILayout.ToggleLeft("Position", copyPosition);
        copyRotation = EditorGUILayout.ToggleLeft("Rotation", copyRotation);
        copyScale = EditorGUILayout.ToggleLeft("Scale", copyScale);

        EditorGUILayout.Space();

        useWorldSpace = EditorGUILayout.Toggle("Use World Space", useWorldSpace);

        if (copyPosition)
        {
            EditorGUILayout.LabelField("Position Axes", EditorStyles.miniBoldLabel);
            DrawAxisRow(ref posX, ref posY, ref posZ);
        }

        if (copyRotation)
        {
            EditorGUILayout.LabelField("Rotation Axes", EditorStyles.miniBoldLabel);
            DrawAxisRow(ref rotX, ref rotY, ref rotZ);
        }

        if (copyScale)
        {
            EditorGUILayout.LabelField("Scale Axes", EditorStyles.miniBoldLabel);
            DrawAxisRow(ref scaleX, ref scaleY, ref scaleZ);
        }

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        if (targets == null || targets.Length < 2)
        {
            EditorUtility.DisplayDialog("Match Transform",
                "Select at least 2 Transforms.", "OK");
            return;
        }

        Transform source = ResolveSource(targets);
        if (!source)
        {
            EditorUtility.DisplayDialog("Match Transform",
                "No valid source Transform found.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Match Transform");

        foreach (var tr in targets)
        {
            if (!tr) continue;
            if (tr == source) continue;

            Undo.RecordObject(tr, "Match Transform");

            ApplyToTarget(tr, source);
        }
    }

    private Transform ResolveSource(Transform[] targets)
    {
        if (useFirstSelectedAsSource)
        {
            return targets[0];
        }

        return explicitSource;
    }

    private void ApplyToTarget(Transform target, Transform source)
    {
        // POSITION
        if (copyPosition)
        {
            Vector3 srcPos = useWorldSpace
                ? source.position
                : source.localPosition;

            Vector3 dstPos = useWorldSpace
                ? target.position
                : target.localPosition;

            if (posX) dstPos.x = srcPos.x;
            if (posY) dstPos.y = srcPos.y;
            if (posZ) dstPos.z = srcPos.z;

            if (useWorldSpace)
                target.position = dstPos;
            else
                target.localPosition = dstPos;
        }

        // ROTATION
        if (copyRotation)
        {
            Vector3 srcEuler = useWorldSpace
                ? source.eulerAngles
                : source.localEulerAngles;

            Vector3 dstEuler = useWorldSpace
                ? target.eulerAngles
                : target.localEulerAngles;

            if (rotX) dstEuler.x = srcEuler.x;
            if (rotY) dstEuler.y = srcEuler.y;
            if (rotZ) dstEuler.z = srcEuler.z;

            if (useWorldSpace)
                target.eulerAngles = dstEuler;
            else
                target.localEulerAngles = dstEuler;
        }

        // SCALE
        if (copyScale)
        {
            Vector3 srcScale = useWorldSpace
                ? source.lossyScale
                : source.localScale;

            Vector3 dstScale = target.localScale;

            if (scaleX) dstScale.x = srcScale.x;
            if (scaleY) dstScale.y = srcScale.y;
            if (scaleZ) dstScale.z = srcScale.z;

            target.localScale = dstScale;
        }
    }

    private static void DrawAxisRow(ref bool x, ref bool y, ref bool z)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            x = EditorGUILayout.ToggleLeft("X", x, GUILayout.Width(40));
            y = EditorGUILayout.ToggleLeft("Y", y, GUILayout.Width(40));
            z = EditorGUILayout.ToggleLeft("Z", z, GUILayout.Width(40));
        }
    }
}
}
