using System;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    [CreateAssetMenu(
        menuName = "Transform Expressions/Presets/HUD Frame Anchor (RectTransform → 3D)",
        fileName = "HudFrameAnchorPreset")]
    public sealed class HudFrameAnchorPreset : TransformPreset
    {
        private enum Anchor9
        {
            TopLeft, TopCenter, TopRight,
            MiddleLeft, MiddleCenter, MiddleRight,
            BottomLeft, BottomCenter, BottomRight
        }

        private enum DepthMode
        {
            KeepCurrentDepth,
            FixedDistanceFromCamera
        }

        [Header("HUD Frame (RectTransform)")]
        [SerializeField] private string rectGlobalId;   // GlobalObjectId string for scene RectTransform

        [Header("Camera")]
        [SerializeField] private string cameraGlobalId; // Optional GlobalObjectId string for scene Camera
        [SerializeField] private bool preferCanvasCamera = true;

        [Header("Layout")]
        [SerializeField] private Anchor9 anchor = Anchor9.TopRight;
        [SerializeField] private Vector2 pixelOffset = Vector2.zero; // pixels, +X right, +Y up

        [Header("Depth")]
        [SerializeField] private DepthMode depthMode = DepthMode.KeepCurrentDepth;
        [SerializeField] private float fixedDistance = 5f; // used when FixedDistanceFromCamera

        [Header("Rotation")]
        [SerializeField] private bool faceCamera = false;
        [SerializeField] private Vector3 faceCameraUp = Vector3.up;

        // Non-serialized GUI cache only (Unity won't keep scene refs in assets reliably)
        [NonSerialized] private RectTransform _rectCache;
        [NonSerialized] private Camera _cameraCache;

        public override bool DrawGUI(PresetContext ctx)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.HelpBox(
                "Anchors selected 3D objects to a RectTransform's bounds using pixel offsets.\n" +
                "This is like RectTransform anchoring, but applied to world Transforms.\n\n" +
                "Tip: Use 'Snap Offset From Selection' to set pixelOffset from the active object's current screen position.",
                MessageType.None);

            DrawRectField();
            DrawCameraField();

            EditorGUILayout.Space(6);
            anchor = (Anchor9)EditorGUILayout.EnumPopup("Anchor", anchor);
            pixelOffset = EditorGUILayout.Vector2Field(new GUIContent("Pixel Offset", "Pixels relative to anchor inside the RectTransform"), pixelOffset);

            EditorGUILayout.Space(6);
            depthMode = (DepthMode)EditorGUILayout.EnumPopup("Depth Mode", depthMode);
            using (new EditorGUI.DisabledScope(depthMode != DepthMode.FixedDistanceFromCamera))
            {
                fixedDistance = EditorGUILayout.FloatField(new GUIContent("Fixed Distance", "World distance from camera along its forward direction"), fixedDistance);
            }

            EditorGUILayout.Space(6);
            faceCamera = EditorGUILayout.ToggleLeft("Face Camera", faceCamera);
            using (new EditorGUI.DisabledScope(!faceCamera))
            {
                faceCameraUp = EditorGUILayout.Vector3Field(new GUIContent("Up", "Up direction when facing the camera"), faceCameraUp);
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Selection.activeTransform == null))
                {
                    if (GUILayout.Button("Snap Offset From Selection"))
                    {
                        TrySnapOffsetFromActiveSelection();
                        EditorUtility.SetDirty(this);
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear HUD Frame", GUILayout.Width(130)))
                {
                    rectGlobalId = null;
                    _rectCache = null;
                    EditorUtility.SetDirty(this);
                }
            }

            // Small preview for confidence
            DrawPreview();

            return EditorGUI.EndChangeCheck();
        }

        public override void Apply(PresetContext ctx, Transform[] targets)
        {
            if (targets == null || targets.Length == 0) return;

            RectTransform rect = ResolveRect();
            if (!rect)
            {
                EditorUtility.DisplayDialog("HUD Frame Anchor", "No HUD Frame (RectTransform) assigned or it could not be resolved.", "OK");
                return;
            }

            Camera cam = ResolveCamera(rect);
            if (!cam)
            {
                EditorUtility.DisplayDialog(
                    "HUD Frame Anchor",
                    "No Camera could be resolved.\n\n" +
                    "Assign a camera in the preset, or ensure Camera.main exists.\n" +
                    "Note: Even if your Canvas is Screen Space - Overlay, we still need a Camera to place 3D objects in world space.",
                    "OK");
                return;
            }

            // Compute rect bounds in SCREEN pixels (bottom-left / top-right)
            if (!TryGetRectScreenCorners(rect, cam, out Vector2 screenMin, out Vector2 screenMax))
            {
                EditorUtility.DisplayDialog("HUD Frame Anchor", "Could not compute screen bounds for the RectTransform.", "OK");
                return;
            }

            Vector2 anchorPx = ComputeAnchorPoint(screenMin, screenMax, anchor);
            Vector2 targetScreenPx = anchorPx + pixelOffset;

            for (int i = 0; i < targets.Length; i++)
            {
                Transform tr = targets[i];
                if (!tr) continue;

                // float depth = ComputeDepth(cam, tr, depthMode, fixedDistance);
                float depth = ResolveDepth(cam, tr); // IMPORTANT


                Vector3 world = cam.ScreenToWorldPoint(new Vector3(targetScreenPx.x, targetScreenPx.y, depth));
                tr.position = world;

                if (faceCamera)
                {
                    Vector3 forward = (tr.position - cam.transform.position);
                    if (forward.sqrMagnitude > 1e-8f)
                    {
                        Vector3 up = faceCameraUp.sqrMagnitude < 1e-8f ? Vector3.up : faceCameraUp.normalized;
                        tr.rotation = Quaternion.LookRotation(forward.normalized, up);
                    }
                }
            }
        }

        private static float ResolveDepth(Camera cam, Transform tr)
        {
            // distance along the camera's forward axis
            var toObj = tr.position - cam.transform.position;
            float d = Vector3.Dot(toObj, cam.transform.forward);

            // avoid 0/negative depths (behind camera / on camera)
            if (d < cam.nearClipPlane + 0.01f)
                d = cam.nearClipPlane + 0.5f;

            return d;
        }

        // -------------------------
        // GUI fields
        // -------------------------

        private void DrawRectField()
        {
            RectTransform current = ResolveRect(); // uses cached if possible

            EditorGUI.BeginChangeCheck();
            var picked = (RectTransform)EditorGUILayout.ObjectField(
                new GUIContent("HUD Frame", "RectTransform defining the pixel bounds for anchoring"),
                current,
                typeof(RectTransform),
                allowSceneObjects: true);

            if (EditorGUI.EndChangeCheck())
            {
                SetGlobalIdFromObject(ref rectGlobalId, picked);
                _rectCache = picked;
                EditorUtility.SetDirty(this);
            }
        }

        private void DrawCameraField()
        {
            Camera current = ResolveCamera(ResolveRect());

            EditorGUI.BeginChangeCheck();
            var picked = (Camera)EditorGUILayout.ObjectField(
                new GUIContent("Camera Override", "Optional. If null, we try Canvas camera (if enabled), else Camera.main."),
                ResolveCameraOverride(),
                typeof(Camera),
                allowSceneObjects: true);

            if (EditorGUI.EndChangeCheck())
            {
                SetGlobalIdFromObject(ref cameraGlobalId, picked);
                _cameraCache = picked;
                EditorUtility.SetDirty(this);
            }

            preferCanvasCamera = EditorGUILayout.ToggleLeft(
                new GUIContent("Prefer Canvas Camera", "If the HUD Frame is on a Canvas with a worldCamera, prefer that camera when override is empty."),
                preferCanvasCamera);
        }

        private void DrawPreview()
        {
            RectTransform rect = ResolveRect();
            if (!rect) return;

            Camera cam = ResolveCamera(rect);
            if (!cam) return;

            if (!TryGetRectScreenCorners(rect, cam, out Vector2 screenMin, out Vector2 screenMax))
                return;

            Vector2 anchorPx = ComputeAnchorPoint(screenMin, screenMax, anchor);
            Vector2 targetPx = anchorPx + pixelOffset;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Preview (pixels)", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Rect Min: {Fmt(screenMin)}   Rect Max: {Fmt(screenMax)}");
                EditorGUILayout.LabelField($"Rect Min: {Fmt(screenMin)}   Rect Max: {Fmt(screenMax)}");
                EditorGUILayout.LabelField($"Anchor:   {Fmt(anchorPx)}   Offset: {Fmt(pixelOffset)}");
                EditorGUILayout.LabelField($"Result:   {Fmt(targetPx)}");
            }
        }

        private static string Fmt(Vector2 v) => $"({Mathf.RoundToInt(v.x)}, {Mathf.RoundToInt(v.y)})";

        // -------------------------
        // Snap helper
        // -------------------------

        private void TrySnapOffsetFromActiveSelection()
        {
            Transform tr = Selection.activeTransform;
            if (!tr) return;

            RectTransform rect = ResolveRect();
            if (!rect)
            {
                EditorUtility.DisplayDialog("HUD Frame Anchor", "Assign a HUD Frame first.", "OK");
                return;
            }

            Camera cam = ResolveCamera(rect);
            if (!cam)
            {
                EditorUtility.DisplayDialog("HUD Frame Anchor", "No Camera could be resolved. Assign a Camera override or ensure Camera.main exists.", "OK");
                return;
            }

            if (!TryGetRectScreenCorners(rect, cam, out Vector2 screenMin, out Vector2 screenMax))
                return;

            Vector2 anchorPx = ComputeAnchorPoint(screenMin, screenMax, anchor);

            // Active transform's current screen position
            Vector3 sp = cam.WorldToScreenPoint(tr.position);
            pixelOffset = (Vector2)sp - anchorPx;
        }

        // -------------------------
        // Rect/camera resolution
        // -------------------------

        private RectTransform ResolveRect()
        {
            if (_rectCache) return _rectCache;

            if (!string.IsNullOrEmpty(rectGlobalId) && TryResolveGlobalId(rectGlobalId, out var obj))
            {
                _rectCache = obj as RectTransform;
                if (_rectCache) return _rectCache;
            }

            return null;
        }

        private Camera ResolveCameraOverride()
        {
            if (_cameraCache) return _cameraCache;

            if (!string.IsNullOrEmpty(cameraGlobalId) && TryResolveGlobalId(cameraGlobalId, out var obj))
            {
                _cameraCache = obj as Camera;
                if (_cameraCache) return _cameraCache;
            }

            return null;
        }

        private Camera ResolveCamera(RectTransform rect)
        {
            // 1) explicit override
            var cam = ResolveCameraOverride();
            if (cam) return cam;

            // 2) canvas camera (if enabled)
            if (preferCanvasCamera && rect)
            {
                var canvas = rect.GetComponentInParent<Canvas>();
                if (canvas)
                {
                    // Screen Space - Camera / World Space often set this
                    if (canvas.worldCamera) return canvas.worldCamera;

                    // Overlay canvases do not require a camera for UI, but we STILL need one for 3D placement.
                }
            }

            // 3) main camera
            return Camera.main;
        }

        // Screen rect corners in pixels (BL, TR)
        private static bool TryGetRectScreenCorners(RectTransform rect, Camera cam, out Vector2 screenMin, out Vector2 screenMax)
        {
            screenMin = default;
            screenMax = default;

            if (!rect) return false;

            // Get world corners then convert to screen pixels.
            // Note: For overlay canvases, RectTransformUtility.WorldToScreenPoint works with null,
            // but we pass cam anyway for consistency.
            Vector3[] wc = new Vector3[4];
            rect.GetWorldCorners(wc);

            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, wc[0]); // bottom-left
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, wc[2]); // top-right

            // Ensure correct ordering even if rect is rotated/flipped
            screenMin = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            screenMax = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return true;
        }

        private static Vector2 ComputeAnchorPoint(Vector2 min, Vector2 max, Anchor9 a)
        {
            float x = a switch
            {
                Anchor9.TopLeft or Anchor9.MiddleLeft or Anchor9.BottomLeft => min.x,
                Anchor9.TopCenter or Anchor9.MiddleCenter or Anchor9.BottomCenter => (min.x + max.x) * 0.5f,
                _ => max.x
            };

            float y = a switch
            {
                Anchor9.BottomLeft or Anchor9.BottomCenter or Anchor9.BottomRight => min.y,
                Anchor9.MiddleLeft or Anchor9.MiddleCenter or Anchor9.MiddleRight => (min.y + max.y) * 0.5f,
                _ => max.y
            };

            return new Vector2(x, y);
        }

        private static float ComputeDepth(Camera cam, Transform tr, DepthMode mode, float fixedDist)
        {
            if (!cam) return 1f;

            switch (mode)
            {
                case DepthMode.FixedDistanceFromCamera:
                    return Mathf.Max(0.01f, fixedDist);

                case DepthMode.KeepCurrentDepth:
                default:
                    // z is distance along camera forward in camera space
                    float z = cam.WorldToScreenPoint(tr.position).z;
                    // If object is behind camera, keep it barely in front to avoid flips
                    return Mathf.Max(0.01f, z);
            }
        }

        // -------------------------
        // GlobalObjectId helpers
        // -------------------------

        private static void SetGlobalIdFromObject(ref string idStr, UnityEngine.Object obj)
        {
    #if UNITY_2020_1_OR_NEWER
            if (!obj)
            {
                idStr = null;
                return;
            }

            var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            idStr = gid.ToString();
    #else
            // If you’re on an older Unity, we can swap to a fallback approach (path search),
            // but your toolchain looks modern enough that GlobalObjectId is the right move.
            idStr = null;
    #endif
        }

        private static bool TryResolveGlobalId(string idStr, out UnityEngine.Object obj)
        {
            obj = null;

    #if UNITY_2020_1_OR_NEWER
            if (string.IsNullOrEmpty(idStr)) return false;

            if (!GlobalObjectId.TryParse(idStr, out var gid))
                return false;

            obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            return obj != null;
    #else
            return false;
    #endif
        }
    }
}