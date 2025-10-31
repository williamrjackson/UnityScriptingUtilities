using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class TransformExtensions
{
    // Helper: get texture width/height from Texture2D or RenderTexture
    static bool TryGetTextureDims(Renderer renderer, out int texW, out int texH)
    {
        texW = texH = 0;
        if (renderer == null || renderer.sharedMaterial == null) return false;
        var tex = renderer.sharedMaterial.mainTexture;
        if (tex == null) return false;
        if (tex is Texture2D t2d)
        {
            texW = t2d.width; texH = t2d.height; return true;
        }
        if (tex is RenderTexture rt)
        {
            texW = rt.width; texH = rt.height; return true;
        }
        // Fallback: Texture base class exposes width/height in recent Unity versions
        texW = tex.width; texH = tex.height; return texW > 0 && texH > 0;
    }

    public static float GetAspect(Renderer renderer)
    {
        if (TryGetTextureDims(renderer, out int w, out int h) && h != 0) return (float)w / h;
        return 1f;
    }

    // Add a menu item called "ScaleXToSourceAspectRatio" to a Transform's context menu.
    [MenuItem("CONTEXT/Transform/Scale X To Aspect Ratio")]
    static void ScaleXToSourceAspectRatio(MenuCommand command)
    {
        Transform tform = (Transform)command.context;
        var renderer = tform.GetComponent<Renderer>();
        if (renderer == null) return;

        float aspect = GetAspect(renderer);

        // Compute desired WORLD width based on current WORLD height and aspect
        Vector3 lossy = tform.lossyScale;
        Vector3 local = tform.localScale;
        float worldHeight = lossy.y;
        float desiredWorldWidth = worldHeight * aspect;

        // Convert desired WORLD width back to local, factoring parent scaling
        float parentScaleX = Mathf.Approximately(local.x, 0f) ? 1f : (lossy.x / local.x);
        float newLocalX = desiredWorldWidth / parentScaleX;

        Undo.RecordObject(tform, "Scale X To Aspect Ratio");
        tform.localScale = new Vector3(newLocalX, local.y, local.z);
        EditorUtility.SetDirty(tform);
    }
    [MenuItem("CONTEXT/Transform/Scale X To Aspect Ratio", true)]
    static bool ValidateScaleXToSourceAspectRatio(MenuCommand command)
    {
        Transform tform = (Transform)command.context;
        var r = tform.GetComponent<Renderer>();
        return r != null && r.sharedMaterial != null && r.sharedMaterial.mainTexture != null;
    }
    // Add a menu item called "ScaleYToSourceAspectRatio" to a Transform's context menu.
    [MenuItem("CONTEXT/Transform/Scale Y To Aspect Ratio")]
    static void ScaleYToSourceAspectRatio(MenuCommand command)
    {
        Transform tform = (Transform)command.context;
        var renderer = tform.GetComponent<Renderer>();
        if (renderer == null) return;

        float aspect = GetAspect(renderer);

        // Compute desired WORLD height based on current WORLD width and aspect
        Vector3 lossy = tform.lossyScale;
        Vector3 local = tform.localScale;
        float worldWidth = lossy.x;
        float desiredWorldHeight = worldWidth / aspect;

        // Convert desired WORLD height back to local, factoring parent scaling
        float parentScaleY = Mathf.Approximately(local.y, 0f) ? 1f : (lossy.y / local.y);
        float newLocalY = desiredWorldHeight / parentScaleY;

        Undo.RecordObject(tform, "Scale Y To Aspect Ratio");
        tform.localScale = new Vector3(local.x, newLocalY, local.z);
        EditorUtility.SetDirty(tform);
    }
    [MenuItem("CONTEXT/Transform/Scale Y To Aspect Ratio", true)]
    static bool ValidateScaleYToSourceAspectRatio(MenuCommand command)
    {
        Transform tform = (Transform)command.context;
        var r = tform.GetComponent<Renderer>();
        return r != null && r.sharedMaterial != null && r.sharedMaterial.mainTexture != null;
    }

    // Preserve area while matching texture aspect: scales both X and Y so width/height == aspect and area stays constant in world space
    [MenuItem("CONTEXT/Transform/Scale Both To Aspect (Preserve Area)")]
    static void ScaleBothPreserveArea(MenuCommand command)
    {
        Transform tform = (Transform)command.context;
        var renderer = tform.GetComponent<Renderer>();
        if (renderer == null) return;

        float aspect = GetAspect(renderer); // w/h
        if (aspect <= 0f) return;

        // Current world size from bounds
        var size = renderer.bounds.size;
        float worldW = Mathf.Max(1e-6f, size.x);
        float worldH = Mathf.Max(1e-6f, size.y);
        float area = worldW * worldH;

        float desiredWorldW = Mathf.Sqrt(area * aspect);
        float desiredWorldH = desiredWorldW / aspect;

        // Convert world sizes back to local scales using parent factors
        Vector3 lossy = tform.lossyScale;
        Vector3 local = tform.localScale;
        float parentScaleX = Mathf.Approximately(local.x, 0f) ? 1f : (lossy.x / local.x);
        float parentScaleY = Mathf.Approximately(local.y, 0f) ? 1f : (lossy.y / local.y);

        float newLocalX = desiredWorldW / parentScaleX;
        float newLocalY = desiredWorldH / parentScaleY;

        Undo.RecordObject(tform, "Scale Both To Aspect (Preserve Area)");
        tform.localScale = new Vector3(newLocalX, newLocalY, local.z);
        EditorUtility.SetDirty(tform);
    }

    // Open an editor window to set a target world width/height (meters), maintaining texture aspect
    [MenuItem("CONTEXT/Transform/Set World Width (m)...")]
    static void SetWorldWidth(MenuCommand command)
    {
        WorldSizeSetter.Open((Transform)command.context, WorldSizeSetter.Mode.Width);
    }
    [MenuItem("CONTEXT/Transform/Set World Height (m)...")]
    static void SetWorldHeight(MenuCommand command)
    {
        WorldSizeSetter.Open((Transform)command.context, WorldSizeSetter.Mode.Height);
    }
}

public class WorldSizeSetter : EditorWindow
{
    public enum Mode { Width, Height }
    Transform target;
    Mode mode;
    float valueMeters = 1f;

    public static void Open(Transform t, Mode mode)
    {
        var win = CreateInstance<WorldSizeSetter>();
        win.target = t;
        win.mode = mode;
        win.valueMeters = mode == Mode.Width ? t.lossyScale.x : t.lossyScale.y;
        win.titleContent = new GUIContent(mode == Mode.Width ? "Set World Width" : "Set World Height");
        win.minSize = new Vector2(320, 90);
        win.maxSize = new Vector2(320, 90);
        win.ShowUtility();
    }

    void OnGUI()
    {
        if (!target)
        {
            EditorGUILayout.HelpBox("No target Transform.", MessageType.Error);
            if (GUILayout.Button("Close")) Close();
            return;
        }

        var renderer = target.GetComponent<Renderer>();
        if (!renderer || renderer.sharedMaterial == null || renderer.sharedMaterial.mainTexture == null)
        {
            EditorGUILayout.HelpBox("Target requires a Renderer with a mainTexture.", MessageType.Error);
            if (GUILayout.Button("Close")) Close();
            return;
        }

        EditorGUILayout.LabelField("Target", target.name);
        valueMeters = EditorGUILayout.FloatField(mode == Mode.Width ? "World Width (m)" : "World Height (m)", valueMeters);
        if (valueMeters <= 0f) valueMeters = 0.001f;

        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply", GUILayout.Height(24)))
            {
                Apply();
            }
            if (GUILayout.Button("Close", GUILayout.Height(24)))
            {
                Close();
            }
        }
    }

    void Apply()
    {
        var renderer = target.GetComponent<Renderer>();
        float aspect = TransformExtensions.GetAspect(renderer);

        Vector3 lossy = target.lossyScale;
        Vector3 local = target.localScale;
        float parentScaleX = Mathf.Approximately(local.x, 0f) ? 1f : (lossy.x / local.x);
        float parentScaleY = Mathf.Approximately(local.y, 0f) ? 1f : (lossy.y / local.y);

        float desiredWorldW, desiredWorldH;
        if (mode == Mode.Width)
        {
            desiredWorldW = valueMeters;
            desiredWorldH = valueMeters / Mathf.Max(1e-6f, aspect);
        }
        else
        {
            desiredWorldH = valueMeters;
            desiredWorldW = valueMeters * Mathf.Max(1e-6f, aspect);
        }

        float newLocalX = desiredWorldW / parentScaleX;
        float newLocalY = desiredWorldH / parentScaleY;

        Undo.RecordObject(target, mode == Mode.Width ? "Set World Width" : "Set World Height");
        target.localScale = new Vector3(newLocalX, newLocalY, local.z);
        EditorUtility.SetDirty(target);
    }
}