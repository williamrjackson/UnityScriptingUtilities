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
    // If no texture, preserves volume by equalizing all axes (resets distorted spheres to original shape)
    [MenuItem("CONTEXT/Transform/Scale All (Preserve Aspect Area or Volume)")]
    static void ScaleBothPreserveArea(MenuCommand command)
    {
        Transform tform = (Transform)command.context;
        var renderer = tform.GetComponent<Renderer>();
        if (renderer == null) return;

        Vector3 lossy = tform.lossyScale;
        Vector3 local = tform.localScale;
        var size = renderer.bounds.size;

        bool hasTexture = renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null;

        float newLocalX, newLocalY, newLocalZ;

        if (hasTexture)
        {
            // Texture mode: preserve X×Y area, match aspect, leave Z unchanged
            float aspect = GetAspect(renderer); // w/h
            if (aspect <= 0f) return;

            float worldW = Mathf.Max(1e-6f, size.x);
            float worldH = Mathf.Max(1e-6f, size.y);
            float area = worldW * worldH;

            float desiredWorldW = Mathf.Sqrt(area * aspect);
            float desiredWorldH = desiredWorldW / aspect;

            float parentScaleX = Mathf.Approximately(local.x, 0f) ? 1f : (lossy.x / local.x);
            float parentScaleY = Mathf.Approximately(local.y, 0f) ? 1f : (lossy.y / local.y);

            newLocalX = desiredWorldW / parentScaleX;
            newLocalY = desiredWorldH / parentScaleY;
            newLocalZ = local.z;
        }
        else
        {
            // No texture: preserve volume by distributing evenly across all axes (resets to cube/sphere shape)
            float worldW = Mathf.Max(1e-6f, size.x);
            float worldH = Mathf.Max(1e-6f, size.y);
            float worldD = Mathf.Max(1e-6f, size.z);
            float volume = worldW * worldH * worldD;

            // Each axis gets the cube root of the volume (uniform scaling)
            float desiredWorldDim = Mathf.Pow(volume, 1f/3f);

            float parentScaleX = Mathf.Approximately(local.x, 0f) ? 1f : (lossy.x / local.x);
            float parentScaleY = Mathf.Approximately(local.y, 0f) ? 1f : (lossy.y / local.y);
            float parentScaleZ = Mathf.Approximately(local.z, 0f) ? 1f : (lossy.z / local.z);

            newLocalX = desiredWorldDim / parentScaleX;
            newLocalY = desiredWorldDim / parentScaleY;
            newLocalZ = desiredWorldDim / parentScaleZ;
        }

        Undo.RecordObject(tform, "Scale Both To Aspect (Preserve Area)");
        tform.localScale = new Vector3(newLocalX, newLocalY, newLocalZ);
        EditorUtility.SetDirty(tform);
    }

    // Open an editor window to set a target world width/height (meters), maintaining texture aspect
    [MenuItem("CONTEXT/Transform/Set World Width...")]
    static void SetWorldWidth(MenuCommand command)
    {
        WorldSizeSetter.Open((Transform)command.context, WorldSizeSetter.Mode.Width);
    }
    [MenuItem("CONTEXT/Transform/Set World Height...")]
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
        Vector3 lossy = target.lossyScale;
        Vector3 local = target.localScale;

        // Compute uniform scale factor based on the dimension being changed
        float scaleFactor;
        if (mode == Mode.Width)
        {
            float currentWorldWidth = lossy.x;
            scaleFactor = valueMeters / Mathf.Max(1e-6f, currentWorldWidth);
        }
        else // Height
        {
            float currentWorldHeight = lossy.y;
            scaleFactor = valueMeters / Mathf.Max(1e-6f, currentWorldHeight);
        }

        // Apply uniform scaling to all axes in world space, then convert to local
        float parentScaleX = Mathf.Approximately(local.x, 0f) ? 1f : (lossy.x / local.x);
        float parentScaleY = Mathf.Approximately(local.y, 0f) ? 1f : (lossy.y / local.y);
        float parentScaleZ = Mathf.Approximately(local.z, 0f) ? 1f : (lossy.z / local.z);

        float newLocalX = (lossy.x * scaleFactor) / parentScaleX;
        float newLocalY = (lossy.y * scaleFactor) / parentScaleY;
        float newLocalZ = (lossy.z * scaleFactor) / parentScaleZ;

        Undo.RecordObject(target, mode == Mode.Width ? "Set World Width" : "Set World Height");
        target.localScale = new Vector3(newLocalX, newLocalY, newLocalZ);
        EditorUtility.SetDirty(target);
    }
}