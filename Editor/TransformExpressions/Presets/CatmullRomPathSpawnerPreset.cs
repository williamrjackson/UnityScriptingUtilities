using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(
        menuName = "Transform Expressions/Presets/Catmull-Rom Path Spawner",
        fileName = "CatmullRomPathSpawnerPreset")]
    public sealed class CatmullRomPathSpawnerPreset : TransformPreset
{
    // Sampling
    private enum SpacingMode
    {
        SamplesPerSegment,
        EveryDistance
    }

    private struct SplineSample
    {
        public Vector3 pos;
        public Vector3 tangent;
    }

    [Header("Template")]
    [SerializeField] private SpacingMode spacingMode = SpacingMode.SamplesPerSegment;

    [Tooltip("Samples per segment (between each pair of selected points). Higher = smoother/more objects.")]
    [Min(1)]
    [SerializeField] private int samplesPerSegment = 10;

    [Tooltip("Place an object every X units along the spline (approx).")]
    [Min(0.0001f)]
    [SerializeField] private float distanceStep = 1.0f;

    [Tooltip("Internal subdivision used to approximate arc-length when using distance spacing. Higher = more accurate, slower.")]
    [Range(8, 256)]
    [SerializeField] private int arcLengthSubdivisionsPerSegment = 32;
    
    [Header("Path Options")]
    [Tooltip("If enabled, the path connects the last selected object back to the first.")]
    [SerializeField] private bool closedLoop = false;

    [Tooltip("If enabled, does NOT place objects exactly at the selected control points (knot points).")]
    [SerializeField] private bool skipControlPoints = true;

    [Header("Transform Options")]
    [Tooltip("If true, aligns rotation to the spline tangent (keeps template's up).")]
    [SerializeField] private bool alignToTangent = false;

    private enum SpaceMode
    {
        World,
        LocalToCommonParent
    }

    [Tooltip("World: use world positions. LocalToCommonParent: if all selected share the same parent, use that local space.")]
    [SerializeField] private SpaceMode spaceMode = SpaceMode.World;

    [Header("Hierarchy Options")]
    [Tooltip("If enabled, creates a container GameObject and parents all spawned objects under it.")]
    [SerializeField] private bool createContainer = true;

    [Tooltip("Name for the container. If empty, defaults to \"<TemplateName>_Path\".")]
    [SerializeField] private string containerName = "";

    [Tooltip("If enabled, creates a single GameObject with a LineRenderer showing the path instead of spawning objects.")]
    [SerializeField] private bool createLineRendererInstead = false;

    [SerializeField] private bool optionsFoldout = true;

    private GameObject pathObject = null;

    public override bool DrawGUI(PresetContext ctx)
    {
        EditorGUI.BeginChangeCheck();

        pathObject = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Path Object", "Prefab or scene object duplicated along the spline. If null, uses first selected."),
            pathObject, typeof(GameObject), false);

        spacingMode = (SpacingMode)EditorGUILayout.EnumPopup("Spacing", spacingMode);

        using (new EditorGUI.IndentLevelScope())
        {
            if (spacingMode == SpacingMode.SamplesPerSegment)
            {
                samplesPerSegment = EditorGUILayout.IntField(
                    new GUIContent("Samples/Segment", "How many placements between each pair of points."),
                    Mathf.Max(1, samplesPerSegment));
            }
            else
            {
                distanceStep = EditorGUILayout.FloatField(
                    new GUIContent("Distance Step", "Place an object approximately every X units along the spline."),
                    Mathf.Max(0.0001f, distanceStep));

                arcLengthSubdivisionsPerSegment = EditorGUILayout.IntSlider(
                    new GUIContent("Arc Subdiv/Seg", "Higher = better distance accuracy."),
                    arcLengthSubdivisionsPerSegment, 8, 256);
            }
        }

        optionsFoldout = EditorGUILayout.Foldout(optionsFoldout, "Options", true);
        if (optionsFoldout)
        {
            closedLoop = EditorGUILayout.Toggle(new GUIContent("Closed Loop"), closedLoop);
            skipControlPoints = EditorGUILayout.Toggle(new GUIContent("Skip Control Points"), skipControlPoints);

            EditorGUILayout.Space(3);
            alignToTangent = EditorGUILayout.Toggle(new GUIContent("Align To Tangent"), alignToTangent);
            spaceMode = (SpaceMode)EditorGUILayout.EnumPopup(new GUIContent("Space"), spaceMode);

            EditorGUILayout.Space(3);
            createContainer = EditorGUILayout.Toggle(new GUIContent("Create Container"), createContainer);
            if (createContainer)
            {
                containerName = EditorGUILayout.TextField(
                    new GUIContent("Container Name"),
                    containerName);
            }

            createLineRendererInstead = EditorGUILayout.Toggle(new GUIContent("Create Line Renderer Instead"), createLineRendererInstead);
        }

        EditorGUILayout.HelpBox(
            "Creates objects along a Catmull–Rom spline through the ordered selection.\n" +
            "Enable 'Create Line Renderer Instead' to visualize the path without spawning objects.\n\n" +
            "- Closed Loop connects last->first\n" +
            "- Skip Control Points avoids placing objects at knots\n" +
            "- Distance spacing uses arc-length approximation\n" +
            "- LocalToCommonParent only works when all points share the same parent",
            MessageType.None);

        return EditorGUI.EndChangeCheck();
    }

    public override void Apply(PresetContext ctx, Transform[] targets)
    {
        if (targets == null || targets.Length < 2)
        {
            EditorUtility.DisplayDialog(DisplayName, "Select at least 2 objects to form a path.", "OK");
            return;
        }

        // Determine template (Path Object)
        GameObject template = pathObject;
        if (!template)
            template = targets[0] ? targets[0].gameObject : null;

        if (!template)
        {
            EditorUtility.DisplayDialog(DisplayName, "No Path Object available (template null and first selection null).", "OK");
            return;
        }

        // Filter points + build positions
        var pointTransforms = new List<Transform>(targets.Length);
        for (int i = 0; i < targets.Length; i++)
            if (targets[i]) pointTransforms.Add(targets[i]);

        if (pointTransforms.Count < 2)
        {
            EditorUtility.DisplayDialog(DisplayName, "Not enough valid Transforms in selection.", "OK");
            return;
        }

        // Decide coordinate space
        Transform commonParent = null;
        if (spaceMode == SpaceMode.LocalToCommonParent)
        {
            commonParent = pointTransforms[0].parent;
            for (int i = 1; i < pointTransforms.Count; i++)
            {
                if (pointTransforms[i].parent != commonParent)
                {
                    commonParent = null;
                    break;
                }
            }
        }

        bool useLocal = (spaceMode == SpaceMode.LocalToCommonParent) && commonParent != null;

        var points = new List<Vector3>(pointTransforms.Count);
        for (int i = 0; i < pointTransforms.Count; i++)
        {
            var tr = pointTransforms[i];
            points.Add(useLocal ? tr.localPosition : tr.position);
        }

        // Parent selection for spawned objects:
        // - If container enabled: container is sibling of template if possible
        // - Otherwise: spawned are siblings of template (same parent) when possible
        Transform templateParent = template.transform.parent; // may be null
        Transform spawnParent = templateParent;
        int siblingInsertIndex = -1;

        if (templateParent != null)
            siblingInsertIndex = template.transform.GetSiblingIndex() + 1;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(DisplayName);
        int undoGroup = Undo.GetCurrentGroup();

        GameObject container = null;
        var spawned = new List<GameObject>();
        var linePositions = new List<Vector3>();

        try
        {
            if (createContainer)
            {
                string name = string.IsNullOrWhiteSpace(containerName) ? $"{template.name}_Path" : containerName;
                container = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(container, DisplayName);

                // Parent container to template parent (so it's a sibling of template)
                container.transform.SetParent(templateParent, worldPositionStays: true);
                if (templateParent != null && siblingInsertIndex >= 0)
                    container.transform.SetSiblingIndex(siblingInsertIndex++);

                spawnParent = container.transform;
                siblingInsertIndex = -1; // under container we usually don't need sibling indexing
            }

            // Create placements or collect positions
            if (createLineRendererInstead)
            {
                if (spacingMode == SpacingMode.SamplesPerSegment)
                    CollectPositionsBySamples(points, linePositions);
                else
                    CollectPositionsByDistance(points, linePositions);

                // Create LineRenderer object
                CreateLineRenderer(container ?? spawnParent?.gameObject, linePositions, useLocal, commonParent);
            }
            else
            {
                if (spacingMode == SpacingMode.SamplesPerSegment)
                    SpawnBySamples(points, template, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
                else
                    SpawnByDistance(points, template, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
            }

            // Optional: ping container or first spawned
            if (container) EditorGUIUtility.PingObject(container);
            else if (spawned.Count > 0) EditorGUIUtility.PingObject(spawned[0]);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    // -------------------------
    // Sampling strategies
    // -------------------------

    private void SpawnBySamples(
        List<Vector3> points,
        GameObject template,
        bool useLocal,
        Transform commonParent,
        Transform spawnParent,
        ref int siblingInsertIndex,
        List<GameObject> spawned)
    {
        int n = points.Count;
        int segmentCount = closedLoop ? n : (n - 1);
        int res = Mathf.Max(1, samplesPerSegment);

        for (int seg = 0; seg < segmentCount; seg++)
        {
            // Segment from i -> i+1 (wrapped if loop)
            int i1 = seg;
            int i2 = (seg + 1) % n;

            Vector3 p0 = points[(seg - 1 + n) % n];
            Vector3 p1 = points[i1];
            Vector3 p2 = points[i2];
            Vector3 p3 = points[(seg + 2) % n];

            // Non-loop endpoints: clamp by duplication
            if (!closedLoop)
            {
                p0 = points[Mathf.Max(seg - 1, 0)];
                p1 = points[seg];
                p2 = points[seg + 1];
                p3 = points[Mathf.Min(seg + 2, n - 1)];
            }

            // Decide which samples to include to avoid duplicates
            // - We usually skip t=0 for seg>0 to avoid knot duplicates.
            // - If skipControlPoints: also avoid t=0 and t=1 entirely.
            int startS = 0;
            int endS = res; // exclusive; we'll optionally add endpoints separately

            for (int s = startS; s < endS; s++)
            {
                float t = s / (float)res;

                if (skipControlPoints)
                {
                    if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f)) continue;
                }
                else
                {
                    // avoid duplicates at knots between segments
                    if (seg > 0 && Mathf.Approximately(t, 0f)) continue;
                }

                PlaceOne(template, p0, p1, p2, p3, t, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
            }
        }

        // Add final endpoint if not loop and not skipping control points
        if (!closedLoop && !skipControlPoints)
        {
            Vector3 last = points[n - 1];
            PlaceEndpoint(template, last, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
        }
    }

    private void CollectPositionsBySamples(List<Vector3> points, List<Vector3> positions)
    {
        int n = points.Count;
        int segmentCount = closedLoop ? n : (n - 1);
        int res = Mathf.Max(1, samplesPerSegment);

        for (int seg = 0; seg < segmentCount; seg++)
        {
            // Segment from i -> i+1 (wrapped if loop)
            int i1 = seg;
            int i2 = (seg + 1) % n;

            Vector3 p0 = points[(seg - 1 + n) % n];
            Vector3 p1 = points[i1];
            Vector3 p2 = points[i2];
            Vector3 p3 = points[(seg + 2) % n];

            // Non-loop endpoints: clamp by duplication
            if (!closedLoop)
            {
                p0 = points[Mathf.Max(seg - 1, 0)];
                p1 = points[seg];
                p2 = points[seg + 1];
                p3 = points[Mathf.Min(seg + 2, n - 1)];
            }

            // Decide which samples to include to avoid duplicates
            // - We usually skip t=0 for seg>0 to avoid knot duplicates.
            // - If skipControlPoints: also avoid t=0 and t=1 entirely.
            int startS = 0;
            int endS = res; // exclusive; we'll optionally add endpoints separately

            for (int s = startS; s < endS; s++)
            {
                float t = s / (float)res;

                if (skipControlPoints)
                {
                    if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f)) continue;
                }
                else
                {
                    // avoid duplicates at knots between segments
                    if (seg > 0 && Mathf.Approximately(t, 0f)) continue;
                }

                Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
                positions.Add(pos);
            }
        }

        // Add final endpoint if not loop and not skipping control points
        if (!closedLoop && !skipControlPoints)
        {
            Vector3 last = points[n - 1];
            positions.Add(last);
        }
    }

    private void SpawnByDistance(
        List<Vector3> points,
        GameObject template,
        bool useLocal,
        Transform commonParent,
        Transform spawnParent,
        ref int siblingInsertIndex,
        List<GameObject> spawned)
    {
        int n = points.Count;
        int segmentCount = closedLoop ? n : (n - 1);

        // Build a polyline approximation of the spline at high resolution.
        var samples = new List<SplineSample>(segmentCount * arcLengthSubdivisionsPerSegment + 8);

        for (int seg = 0; seg < segmentCount; seg++)
        {
            int i1 = seg;
            int i2 = (seg + 1) % n;

            Vector3 p0 = points[(seg - 1 + n) % n];
            Vector3 p1 = points[i1];
            Vector3 p2 = points[i2];
            Vector3 p3 = points[(seg + 2) % n];

            if (!closedLoop)
            {
                p0 = points[Mathf.Max(seg - 1, 0)];
                p1 = points[seg];
                p2 = points[seg + 1];
                p3 = points[Mathf.Min(seg + 2, n - 1)];
            }

            int sub = Mathf.Clamp(arcLengthSubdivisionsPerSegment, 8, 256);

            // Include t=0 for the very first segment only (unless skipControlPoints)
            for (int s = 0; s <= sub; s++)
            {
                float t = s / (float)sub;

                if (skipControlPoints)
                {
                    if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f)) continue;
                }
                else
                {
                    if (seg > 0 && Mathf.Approximately(t, 0f)) continue;
                }

                Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
                Vector3 tan = CatmullRomTangent(p0, p1, p2, p3, t);

                samples.Add(new SplineSample
                {
                    pos = pos,
                    tangent = tan
                });
            }
        }

        if (samples.Count < 2)
        {
            EditorUtility.DisplayDialog(DisplayName, "Not enough spline samples to place objects. Try disabling Skip Control Points or increase subdivisions.", "OK");
            return;
        }

        // Walk along samples and place every distanceStep.
        float step = Mathf.Max(0.0001f, distanceStep);

        float nextAt = 0f;

        // Place at the first sample (unless skipping control points makes the first sample mid-segment; that's fine)
        // If you *really* want to avoid the first placement too, you can set nextAt = step initially.
        nextAt = 0f;

        Vector3 prev = samples[0].pos;
        float travelled = 0f;

        // We’ll use linear interpolation between sample points for placement.
        PlaceFromSample(template, samples[0], useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);

        for (int i = 1; i < samples.Count; i++)
        {
            Vector3 cur = samples[i].pos;
            float segLen = Vector3.Distance(prev, cur);
            if (segLen <= 1e-8f)
            {
                prev = cur;
                continue;
            }

            while (travelled + segLen >= nextAt + step)
            {
                nextAt += step;
                float distInto = nextAt - travelled; // how far into this segment
                float alpha = Mathf.Clamp01(distInto / segLen);

                Vector3 pos = Vector3.Lerp(prev, cur, alpha);
                Vector3 tan = Vector3.Lerp(samples[i - 1].tangent, samples[i].tangent, alpha);

                PlaceFromRaw(template, pos, tan, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
            }

            travelled += segLen;
            prev = cur;
        }

        // If not loop and not skipping control points, ensure last endpoint exists.
        if (!closedLoop && !skipControlPoints)
        {
            Vector3 last = points[n - 1];
            PlaceEndpoint(template, last, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
        }
    }

    private void CollectPositionsByDistance(List<Vector3> points, List<Vector3> positions)
    {
        int n = points.Count;
        int segmentCount = closedLoop ? n : (n - 1);

        // Build a polyline approximation of the spline at high resolution.
        var samples = new List<SplineSample>(segmentCount * arcLengthSubdivisionsPerSegment + 8);

        for (int seg = 0; seg < segmentCount; seg++)
        {
            int i1 = seg;
            int i2 = (seg + 1) % n;

            Vector3 p0 = points[(seg - 1 + n) % n];
            Vector3 p1 = points[i1];
            Vector3 p2 = points[i2];
            Vector3 p3 = points[(seg + 2) % n];

            if (!closedLoop)
            {
                p0 = points[Mathf.Max(seg - 1, 0)];
                p1 = points[seg];
                p2 = points[seg + 1];
                p3 = points[Mathf.Min(seg + 2, n - 1)];
            }

            int sub = Mathf.Clamp(arcLengthSubdivisionsPerSegment, 8, 256);

            // Include t=0 for the very first segment only (unless skipControlPoints)
            for (int s = 0; s <= sub; s++)
            {
                float t = s / (float)sub;

                if (skipControlPoints)
                {
                    if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f)) continue;
                }
                else
                {
                    // avoid duplicates at knots between segments
                    if (seg > 0 && Mathf.Approximately(t, 0f)) continue;
                }

                Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
                Vector3 tan = CatmullRomTangent(p0, p1, p2, p3, t);
                samples.Add(new SplineSample { pos = pos, tangent = tan });
            }
        }

        // Walk along samples and collect every distanceStep.
        float step = Mathf.Max(0.0001f, distanceStep);

        float nextAt = 0f;

        // Collect at the first sample
        nextAt = 0f;

        Vector3 prev = samples[0].pos;
        positions.Add(prev);
        float travelled = 0f;

        for (int i = 1; i < samples.Count; i++)
        {
            Vector3 cur = samples[i].pos;
            float segLen = Vector3.Distance(prev, cur);
            if (segLen <= 1e-8f)
            {
                prev = cur;
                continue;
            }

            while (travelled + segLen >= nextAt + step)
            {
                nextAt += step;
                float distInto = nextAt - travelled; // how far into this segment
                float alpha = Mathf.Clamp01(distInto / segLen);

                Vector3 pos = Vector3.Lerp(prev, cur, alpha);
                positions.Add(pos);
            }

            travelled += segLen;
            prev = cur;
        }

        // If not loop and not skipping control points, ensure last endpoint exists.
        if (!closedLoop && !skipControlPoints)
        {
            Vector3 last = points[n - 1];
            positions.Add(last);
        }
    }

    // -------------------------
    // Placement helpers
    // -------------------------

    private void PlaceOne(
        GameObject template,
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        float t,
        bool useLocal,
        Transform commonParent,
        Transform spawnParent,
        ref int siblingInsertIndex,
        List<GameObject> spawned)
    {
        Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
        Vector3 tan = CatmullRomTangent(p0, p1, p2, p3, t);

        PlaceFromRaw(template, pos, tan, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
    }

    private void PlaceEndpoint(
        GameObject template,
        Vector3 pos,
        bool useLocal,
        Transform commonParent,
        Transform spawnParent,
        ref int siblingInsertIndex,
        List<GameObject> spawned)
    {
        PlaceFromRaw(template, pos, Vector3.forward, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
    }

    private void PlaceFromSample(
        GameObject template,
        SplineSample sample,
        bool useLocal,
        Transform commonParent,
        Transform spawnParent,
        ref int siblingInsertIndex,
        List<GameObject> spawned)
    {
        PlaceFromRaw(template, sample.pos, sample.tangent, useLocal, commonParent, spawnParent, ref siblingInsertIndex, spawned);
    }

    private void PlaceFromRaw(
        GameObject template,
        Vector3 pos,
        Vector3 tangent,
        bool useLocal,
        Transform commonParent,
        Transform spawnParent,
        ref int siblingInsertIndex,
        List<GameObject> spawned)
    {
        var go = InstantiateTemplate(template);
        Undo.RegisterCreatedObjectUndo(go, DisplayName);

        // Parent first
        go.transform.SetParent(spawnParent, worldPositionStays: !useLocal);

        // Apply transform
        if (useLocal)
            go.transform.localPosition = pos;
        else
            go.transform.position = pos;

        Quaternion rot = template.transform.rotation;
        if (alignToTangent && tangent.sqrMagnitude > 1e-8f)
        {
            // Keep template's up as reference (predictable)
            rot = Quaternion.LookRotation(tangent.normalized, template.transform.up);
        }

        go.transform.rotation = rot;
        go.transform.localScale = template.transform.localScale;

        if (spawnParent != null && siblingInsertIndex >= 0)
            go.transform.SetSiblingIndex(siblingInsertIndex++);

        go.name = $"{template.name}_Path_{spawned.Count:000}";
        spawned.Add(go);
    }

    private static GameObject InstantiateTemplate(GameObject template)
    {
        if (PrefabUtility.IsPartOfPrefabAsset(template))
        {
            var instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
            return instance;
        }

        return UnityEngine.Object.Instantiate(template);
    }

    // -------------------------
    // Catmull–Rom spline
    // -------------------------

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
    private void CreateLineRenderer(GameObject parent, List<Vector3> positions, bool useLocal, Transform commonParent)
    {
        GameObject lineObj = new GameObject("Path Line");
        Undo.RegisterCreatedObjectUndo(lineObj, DisplayName);

        if (parent)
        {
            lineObj.transform.SetParent(parent.transform, worldPositionStays: false);
        }
        else
        {
            // If no container, place as sibling of first selected
            // But since we're not spawning, just place at origin
        }

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = positions.Count;
        lr.SetPositions(positions.ToArray());

        // Basic styling
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.loop = closedLoop;

        // If using local space, the positions are already in local space relative to commonParent
        if (useLocal && commonParent)
        {
            lineObj.transform.SetParent(commonParent, worldPositionStays: false);
        }
    }
    private static Vector3 CatmullRomTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;

        return 0.5f * (
            (-p0 + p2) +
            2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
            3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2
        );
    }
}
}
