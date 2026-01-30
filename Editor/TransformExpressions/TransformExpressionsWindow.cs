using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Wrj.TransformExpressions
{
    public class TransformExpressionsWindow : EditorWindow
    {
        // --- Selection caching ---
        private Transform[] _selection = Array.Empty<Transform>();
        private int _selectionHash;

        // --- Driver ---
        private enum OrderMode { UnitySelectionOrder, HierarchyOrder, NameOrder }
        private OrderMode _orderMode = OrderMode.UnitySelectionOrder;

        private bool _livePreview;
        private double _nextPreviewTime;
        private const double PreviewHz = 30.0;

        private int _seed = 12345;

        private enum TargetAxis
        {
            PosX, PosY, PosZ,
            RotX, RotY, RotZ,
            ScaleX, ScaleY, ScaleZ,
            ScaleXYZ
        }

        private enum DriverMode
        {
            SetConstant,
            Add,
            Multiply,
            Linear,
            Random,
            Curve
        }

        private TargetAxis _targetAxis = TargetAxis.PosX;
        private TargetAxis _prevTargetAxis = TargetAxis.PosX;

        private DriverMode _driverMode = DriverMode.Linear;
        private DriverMode _prevDriverMode = DriverMode.Linear;

        private float _a = -5f;
        private float _b = 5f;

        // Curve driver
        private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private bool _curveFoldout = true;

        // UI
        private Vector2 _scroll;

        // ✅ Pending scroll request (used for "focus preset" scroll that actually works)
        private float _pendingScrollY = -1f;

        // --- Live Preview session state (ONE undo per gesture/session) ---
        private bool _previewSessionActive;
        private Transform[] _previewSessionTargets = Array.Empty<Transform>();
        private string _previewSessionName = "Transform Driver";

        // Baseline cache for cumulative modes (keyed by transform instance id)
        private readonly Dictionary<int, float> _baselineById = new();
        private TargetAxis _baselineAxis; // axis the baseline corresponds to

        // Presets framework (assets)
        [SerializeField] private TransformPreset[] _transformPresetAssets = Array.Empty<TransformPreset>();
        [SerializeField] private SelectionPreset[] _selectionPresetAssets = Array.Empty<SelectionPreset>();
        private PresetContext _presetCtx;

        // --- External focus request (from menu items) ---
        private static UnityEngine.Object s_focusPresetAsset;
        private static bool s_focusPresetRequestScroll;

        // Track previous inputs to detect “meaningful change” and start a session
        private struct DriverInputs
        {
            public OrderMode order;
            public int seed;
            public TargetAxis axis;
            public DriverMode mode;
            public float a;
            public float b;
            public int curveHash;

            public override bool Equals(object obj)
            {
                if (obj is not DriverInputs o) return false;
                return order == o.order &&
                    seed == o.seed &&
                    axis == o.axis &&
                    mode == o.mode &&
                    Mathf.Approximately(a, o.a) &&
                    Mathf.Approximately(b, o.b) &&
                    curveHash == o.curveHash;
            }

            public override int GetHashCode() => HashCode.Combine(order, seed, axis, mode, a, b, curveHash);
        }

        private DriverInputs _lastInputs;

        [MenuItem("Tools/Transform Expressions")]
        public static void Open() => GetWindow<TransformExpressionsWindow>("Transform Expressions");

        /// <summary>
        /// Used by menu items for "configure-first" flows.
        /// Opens the window, refreshes preset list, expands + scrolls to the preset asset.
        /// </summary>
        public static void OpenAndFocusPreset(UnityEngine.Object presetAsset, bool scrollTo = true)
        {
            var w = GetWindow<TransformExpressionsWindow>("Transform Expressions");
            w.Show();
            w.Focus();

            s_focusPresetAsset = presetAsset;
            s_focusPresetRequestScroll = scrollTo;

            w.RefreshPresetAssets();
            w.Repaint();

            // ✅ Ping only (doesn't steal selection)
            if (presetAsset)
                EditorGUIUtility.PingObject(presetAsset);
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            RebuildSelection(force: true);
            EditorApplication.update += OnEditorUpdate;

            _lastInputs = ReadInputs();
            _presetCtx = new PresetContext(GetOrderedSelection);

            RefreshPresetAssets();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
            EndPreviewSession();
        }

        private void OnSelectionChanged()
        {
            EndPreviewSession();
            RebuildSelection(force: true);
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_livePreview) return;
            if (!_previewSessionActive) return;
            if (_selection == null || _selection.Length == 0) return;

            var t = EditorApplication.timeSinceStartup;
            if (t < _nextPreviewTime) return;
            _nextPreviewTime = t + (1.0 / PreviewHz);

            ApplyDriver_NoUndo();
        }

        private void RebuildSelection(bool force)
        {
            var transforms = Selection.transforms ?? Array.Empty<Transform>();

            int hash = transforms.Length;
            unchecked
            {
                for (int i = 0; i < transforms.Length; i++)
                    hash = (hash * 397) ^ (transforms[i] ? transforms[i].GetInstanceID() : 0);
            }

            if (!force && hash == _selectionHash) return;
            _selectionHash = hash;

            _selection = transforms;
        }

        private void OnGUI()
        {
            RebuildSelection(force: false);

            // End session on mouse-up (good approximation of “gesture ended”)
            var e = Event.current;
            if (e.type == EventType.MouseUp || e.type == EventType.Ignore)
                EndPreviewSession();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSelectionHeader();

            EditorGUILayout.Space(10);
            DrawDriverPanel();

            EditorGUILayout.Space(10);
            DrawPresetsPanel();

            EditorGUILayout.EndScrollView();

            // ✅ Apply pending scroll AFTER EndScrollView so it actually takes effect
            if (_pendingScrollY >= 0f)
            {
                _scroll.y = _pendingScrollY;
                _pendingScrollY = -1f;
                Repaint();
            }

            // If any driver input changed and Live Preview is on, start a session if needed
            var currentInputs = ReadInputs();
            if (!_lastInputs.Equals(currentInputs))
            {
                if (_livePreview && !_previewSessionActive)
                    BeginPreviewSession($"Transform Driver ({_driverMode})");

                _lastInputs = currentInputs;
            }
        }

        public void RefreshPresetAssets()
        {
            // Transform presets
            {
                var guids = AssetDatabase.FindAssets("t:TransformPreset");
                var list = new List<TransformPreset>(guids.Length);

                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var preset = AssetDatabase.LoadAssetAtPath<TransformPreset>(path);
                    if (preset) list.Add(preset);
                }

                list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                _transformPresetAssets = list.ToArray();
            }

            // Selection presets
            {
                var guids = AssetDatabase.FindAssets("t:SelectionPreset");
                var list = new List<SelectionPreset>(guids.Length);

                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var preset = AssetDatabase.LoadAssetAtPath<SelectionPreset>(path);
                    if (preset) list.Add(preset);
                }

                list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                _selectionPresetAssets = list.ToArray();
            }
        }

        private void DrawSelectionHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

                if (_selection == null || _selection.Length == 0)
                {
                    EditorGUILayout.HelpBox("Select one or more GameObjects to use the driver/presets.", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField($"{_selection.Length} Transform(s) selected");
                _orderMode = (OrderMode)EditorGUILayout.EnumPopup("Order", _orderMode);
                _seed = EditorGUILayout.IntField("Seed", _seed);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool newLive = EditorGUILayout.ToggleLeft("Live Preview", _livePreview, GUILayout.Width(140));

                    if (newLive != _livePreview)
                    {
                        _livePreview = newLive;

                        if (_livePreview)
                            BeginPreviewSession($"Transform Driver ({_driverMode})");
                        else
                            EndPreviewSession();
                    }
                }

                EditorGUILayout.HelpBox(
                    "Live Preview records ONE Undo step per continuous edit session. " +
                    "Cumulative modes (Add/Multiply) use an edit-start baseline so they never run away.",
                    MessageType.None
                );
            }
        }

        private void DrawDriverPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Driver", EditorStyles.boldLabel);

                if (_selection == null || _selection.Length == 0)
                {
                    EditorGUILayout.HelpBox("No selection.", MessageType.Info);
                    return;
                }

                _prevTargetAxis = _targetAxis;
                _targetAxis = (TargetAxis)EditorGUILayout.EnumPopup("Target", _targetAxis);

                _prevDriverMode = _driverMode;
                _driverMode = (DriverMode)EditorGUILayout.EnumPopup("Mode", _driverMode);

                // Rule: disable Live Preview when target changes
                if (_livePreview && _targetAxis != _prevTargetAxis)
                {
                    _livePreview = false;
                    EndPreviewSession();
                }

                // Optional: same for mode changes (prevents surprise baseline semantics changes mid-session)
                if (_livePreview && _driverMode != _prevDriverMode)
                {
                    _livePreview = false;
                    EndPreviewSession();
                }

                // Switching Target (or Mode) should not retain confusing delta/factor from prior target.
                if (_targetAxis != _prevTargetAxis || _driverMode != _prevDriverMode)
                {
                    ResetDriverParamsForMode();

                    // We just changed inputs programmatically; treat as the new baseline state for change detection.
                    _lastInputs = ReadInputs();
                    Repaint();
                }

                switch (_driverMode)
                {
                    case DriverMode.SetConstant:
                        _a = EditorGUILayout.FloatField("Value", _a);
                        break;

                    case DriverMode.Add:
                        _a = EditorGUILayout.FloatField("Delta", _a);
                        break;

                    case DriverMode.Multiply:
                        _a = EditorGUILayout.FloatField("Factor", _a);
                        break;

                    case DriverMode.Linear:
                        _a = EditorGUILayout.FloatField("Start (a)", _a);
                        _b = EditorGUILayout.FloatField("End (b)", _b);
                        break;

                    case DriverMode.Random:
                        _a = EditorGUILayout.FloatField("Min (a)", _a);
                        _b = EditorGUILayout.FloatField("Max (b)", _b);
                        break;

                    case DriverMode.Curve:
                        _a = EditorGUILayout.FloatField("Start (a)", _a);
                        _b = EditorGUILayout.FloatField("End (b)", _b);

                        _curveFoldout = EditorGUILayout.Foldout(_curveFoldout, "Curve", true);
                        if (_curveFoldout)
                        {
                            _curve = EditorGUILayout.CurveField("Shape", _curve);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Reset Curve", GUILayout.Width(110)))
                                {
                                    _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                                    _lastInputs = ReadInputs();
                                    Repaint();
                                }
                            }
                        }
                        break;
                }

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Apply"))
                        ApplyDriverOnce();

                    using (new EditorGUI.DisabledScope(!_livePreview))
                    {
                        if (GUILayout.Button("Set Baseline (Zero Delta)"))
                            SetBaselineFromCurrent();
                    }
                }
            }
        }

        private void DrawPresetsPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Refresh Presets", GUILayout.Width(130)))
                        RefreshPresetAssets();

                    GUILayout.FlexibleSpace();
                }

                DrawPresetSection_TransformPresets();
                EditorGUILayout.Space(10);
                DrawPresetSection_SelectionPresets();
            }
        }

        private void DrawPresetSection_TransformPresets()
        {
            EditorGUILayout.LabelField("Transform Presets", EditorStyles.miniBoldLabel);

            if (_transformPresetAssets == null || _transformPresetAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("No TransformPreset assets found. Create one via Create menu.", MessageType.Info);
                return;
            }

            foreach (var preset in _transformPresetAssets)
            {
                if (!preset || !preset.EnabledInWindow) continue;

                HandleExternalFocus(preset);

                preset.IsExpanded = EditorGUILayout.Foldout(preset.IsExpanded, preset.DisplayName, true);
                if (!preset.IsExpanded) continue;

                Rect contentRectAfterDraw = default;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUI.BeginChangeCheck();
                    preset.DrawGUI(_presetCtx);
                    bool changed = EditorGUI.EndChangeCheck();

                    if (changed)
                        EditorUtility.SetDirty(preset);

                    // Capture a rect AFTER drawing the preset GUI so we can scroll to the content
                    contentRectAfterDraw = GUILayoutUtility.GetLastRect();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Apply"))
                        {
                            var targets = GetOrderedSelection();
                            if (targets.Length == 0) return;

                            _presetCtx.RecordUndo(targets, preset.DisplayName);
                            preset.Apply(_presetCtx, targets);
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                // Scroll to the *content*, not the foldout header
                HandleExternalScrollAfterContentIfRequested(preset, contentRectAfterDraw);

                EditorGUILayout.Space(6);
            }
        }

        private void DrawPresetSection_SelectionPresets()
        {
            EditorGUILayout.LabelField("Selection Presets", EditorStyles.miniBoldLabel);

            if (_selectionPresetAssets == null || _selectionPresetAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("No SelectionPreset assets found. Create one via Create menu.", MessageType.Info);
                return;
            }

            foreach (var preset in _selectionPresetAssets)
            {
                if (!preset || !preset.EnabledInWindow) continue;

                HandleExternalFocus(preset);

                preset.IsExpanded = EditorGUILayout.Foldout(preset.IsExpanded, preset.DisplayName, true);
                if (!preset.IsExpanded) continue;

                Rect contentRectAfterDraw = default;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUI.BeginChangeCheck();
                    preset.DrawGUI();
                    bool changed = EditorGUI.EndChangeCheck();

                    if (changed)
                        EditorUtility.SetDirty(preset);

                    // Capture a rect AFTER drawing the preset GUI so we can scroll to the content
                    contentRectAfterDraw = GUILayoutUtility.GetLastRect();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Apply"))
                        {
                            var gos = Selection.gameObjects ?? Array.Empty<GameObject>();
                            if (gos.Length == 0) return;

                            Undo.IncrementCurrentGroup();
                            Undo.SetCurrentGroupName(preset.DisplayName);

                            preset.Apply(gos);
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                // Scroll to the *content*, not the foldout header
                HandleExternalScrollAfterContentIfRequested(preset, contentRectAfterDraw);

                EditorGUILayout.Space(6);
            }
        }

        private static void HandleExternalFocus(UnityEngine.Object preset)
        {
            if (!s_focusPresetAsset) return;
            if (preset != s_focusPresetAsset) return;

            // Ensure expanded so config UI is visible immediately
            switch (preset)
            {
                case TransformPreset tp:
                    tp.IsExpanded = true;
                    break;
                case SelectionPreset sp:
                    sp.IsExpanded = true;
                    break;
            }
        }

        private void HandleExternalScrollAfterContentIfRequested(UnityEngine.Object preset, Rect contentRectAfterDraw)
        {
            if (!s_focusPresetAsset) return;
            if (preset != s_focusPresetAsset) return;
            if (!s_focusPresetRequestScroll) return;

            // Rects are only trustworthy during repaint.
            if (Event.current.type != EventType.Repaint)
                return;

            // Back up a bit so header + first fields are visible (tweak to taste)
            _pendingScrollY = Mathf.Max(0f, contentRectAfterDraw.y - 120f);

            s_focusPresetRequestScroll = false;
            s_focusPresetAsset = null;
        }

        // -------------------------
        // Preview session logic
        // -------------------------

        private static bool IsCumulative(DriverMode mode)
            => mode == DriverMode.Add || mode == DriverMode.Multiply;

        private void BeginPreviewSession(string name)
        {
            if (_previewSessionActive) return;

            var targets = GetOrderedSelection();
            if (targets.Length == 0) return;

            _previewSessionActive = true;
            _previewSessionTargets = targets;
            _previewSessionName = name;

            // Record ONCE: Undo restores pre-session state
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(_previewSessionName);
            Undo.RecordObjects(_previewSessionTargets, _previewSessionName);

            // Capture baseline for cumulative modes so we never compound,
            // AND so we don't "jump" when the session begins with a non-neutral delta/factor.
            _baselineById.Clear();
            _baselineAxis = _targetAxis;

            if (IsCumulative(_driverMode))
            {
                foreach (var tr in _previewSessionTargets)
                {
                    if (!tr) continue;

                    float current = GetAxis(tr, _baselineAxis);
                    float baseline = current;

                    if (_driverMode == DriverMode.Add)
                    {
                        // current == baseline + delta  => baseline = current - delta
                        baseline = current - _a;
                    }
                    else if (_driverMode == DriverMode.Multiply)
                    {
                        // current == baseline * factor => baseline = current / factor
                        baseline = Mathf.Approximately(_a, 0f) ? current : (current / _a);
                    }

                    _baselineById[tr.GetInstanceID()] = baseline;
                }
            }
        }

        private void EndPreviewSession()
        {
            if (!_previewSessionActive) return;

            _previewSessionActive = false;
            _previewSessionTargets = Array.Empty<Transform>();
            _previewSessionName = "Transform Driver";
            _baselineById.Clear();
        }

        /// <summary>
        /// Commits the current state as the new baseline for the active session,
        /// then resets the delta/factor to neutral so further edits start from that baseline.
        /// </summary>
        private void SetBaselineFromCurrent()
        {
            if (!_livePreview) return;

            // Ensure we have a session so ordering is stable.
            if (!_previewSessionActive)
                BeginPreviewSession($"Transform Driver ({_driverMode})");

            // 1) Capture new baseline from current scene state
            _baselineById.Clear();
            _baselineAxis = _targetAxis;

            foreach (var tr in _previewSessionTargets)
            {
                if (!tr) continue;
                _baselineById[tr.GetInstanceID()] = GetAxis(tr, _baselineAxis);
            }

            // 2) Reset driver inputs to neutral so we don't immediately re-apply the old delta/factor
            if (_driverMode == DriverMode.Add)
                _a = 0f;
            else if (_driverMode == DriverMode.Multiply)
                _a = 1f;

            _lastInputs = ReadInputs();
            Repaint();
        }

        private void ResetDriverParamsForMode()
        {
            switch (_driverMode)
            {
                case DriverMode.Add:
                    _a = 0f;
                    break;

                case DriverMode.Multiply:
                    _a = 1f;
                    break;

                // For SetConstant/Linear/Random/Curve we keep existing values by default.
            }
        }

        private void ApplyDriverOnce()
        {
            var targets = GetOrderedSelection();
            if (targets.Length == 0) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Transform Driver ({_driverMode})");
            Undo.RecordObjects(targets, $"Transform Driver ({_driverMode})");

            // One-shot: apply directly
            ApplyDriver_NoUndo(targets, baselineMode: false);
        }

        private DriverInputs ReadInputs() => new DriverInputs
        {
            order = _orderMode,
            seed = _seed,
            axis = _targetAxis,
            mode = _driverMode,
            a = _a,
            b = _b,
            curveHash = ComputeCurveHash(_curve)
        };

        private static int ComputeCurveHash(AnimationCurve c)
        {
            if (c == null) return 0;

            unchecked
            {
                int h = 17;
                var keys = c.keys;
                h = (h * 31) ^ keys.Length;

                for (int i = 0; i < keys.Length; i++)
                {
                    var k = keys[i];
                    h = (h * 31) ^ k.time.GetHashCode();
                    h = (h * 31) ^ k.value.GetHashCode();
                    h = (h * 31) ^ k.inTangent.GetHashCode();
                    h = (h * 31) ^ k.outTangent.GetHashCode();
    #if UNITY_2020_1_OR_NEWER
                    h = (h * 31) ^ k.inWeight.GetHashCode();
                    h = (h * 31) ^ k.outWeight.GetHashCode();
                    h = (h * 31) ^ (int)k.weightedMode;
    #endif
                }

                h = (h * 31) ^ (int)c.preWrapMode;
                h = (h * 31) ^ (int)c.postWrapMode;

                return h;
            }
        }

        // -------------------------
        // Apply logic (NO Undo inside)
        // -------------------------

        private void ApplyDriver_NoUndo()
        {
            // Keep ordering stable during an active session
            var targets = _previewSessionActive ? _previewSessionTargets : GetOrderedSelection();

            bool useBaseline = _previewSessionActive && IsCumulative(_driverMode) && _baselineAxis == _targetAxis;

            ApplyDriver_NoUndo(targets, baselineMode: useBaseline);
        }

        private void ApplyDriver_NoUndo(Transform[] ordered, bool baselineMode)
        {
            int n = ordered.Length;
            if (n == 0) return;

            for (int i = 0; i < n; i++)
            {
                var tr = ordered[i];
                if (!tr) continue;

                float tt = (n <= 1) ? 0f : (float)i / (n - 1);

                float value = EvaluateDriverValue(i, tt);

                if (baselineMode)
                {
                    int id = tr.GetInstanceID();
                    if (!_baselineById.TryGetValue(id, out float baseline))
                        baseline = GetAxis(tr, _targetAxis);

                    SetAxisFromBaseline(tr, _targetAxis, baseline, value, _driverMode);
                }
                else
                {
                    SetAxis(tr, _targetAxis, value, _driverMode);
                }
            }
        }

        private float EvaluateDriverValue(int index, float t)
        {
            return _driverMode switch
            {
                DriverMode.SetConstant => _a,
                DriverMode.Add => _a,        // delta
                DriverMode.Multiply => _a,   // factor
                DriverMode.Linear => Mathf.Lerp(_a, _b, t),
                DriverMode.Random => DeterministicRandomRange(_a, _b, _seed, index, (int)_targetAxis),
                DriverMode.Curve => EvaluateCurveDriver(t),
                _ => 0f
            };
        }

        private float EvaluateCurveDriver(float t)
        {
            float u = 0f;

            if (_curve != null)
                u = _curve.Evaluate(Mathf.Clamp01(t));

            u = Mathf.Clamp01(u); // Clamp to avoid accidental overshoot
            return Mathf.Lerp(_a, _b, u);
        }

        private static float DeterministicRandomRange(float min, float max, int seed, int index, int salt)
        {
            unchecked
            {
                int h = seed;
                h = (h * 397) ^ index;
                h = (h * 397) ^ salt;
                h ^= (h << 13);
                h ^= (h >> 17);
                h ^= (h << 5);

                uint uh = (uint)h;
                float u01 = (uh & 0x00FFFFFF) / (float)0x01000000;
                return Mathf.Lerp(min, max, u01);
            }
        }

        private Transform[] GetOrderedSelection()
        {
            if (_selection == null) return Array.Empty<Transform>();

            switch (_orderMode)
            {
                case OrderMode.UnitySelectionOrder:
                    return _selection.Where(t => t).ToArray();

                case OrderMode.NameOrder:
                    return _selection.Where(t => t)
                        .OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(t => t.GetInstanceID())
                        .ToArray();

                case OrderMode.HierarchyOrder:
                    return _selection.Where(t => t)
                        .OrderBy(GetHierarchyPath, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(t => t.GetSiblingIndex())
                        .ToArray();

                default:
                    return _selection.Where(t => t).ToArray();
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        private static float GetAxis(Transform tr, TargetAxis axis)
        {
            return axis switch
            {
                TargetAxis.PosX => tr.localPosition.x,
                TargetAxis.PosY => tr.localPosition.y,
                TargetAxis.PosZ => tr.localPosition.z,

                TargetAxis.RotX => tr.localEulerAngles.x,
                TargetAxis.RotY => tr.localEulerAngles.y,
                TargetAxis.RotZ => tr.localEulerAngles.z,

                TargetAxis.ScaleX => tr.localScale.x,
                TargetAxis.ScaleY => tr.localScale.y,
                TargetAxis.ScaleZ => tr.localScale.z,
                TargetAxis.ScaleXYZ => tr.localScale.x, // treat uniform as x

                _ => 0f
            };
        }

        private static void SetAxisFromBaseline(Transform tr, TargetAxis axis, float baseline, float value, DriverMode mode)
        {
            float next = mode switch
            {
                DriverMode.Add => baseline + value,
                DriverMode.Multiply => baseline * value,
                _ => value
            };

            SetAxisAbsolute(tr, axis, next);
        }

        private static void SetAxis(Transform tr, TargetAxis axis, float value, DriverMode mode)
        {
            if (!tr) return;

            if (mode == DriverMode.Add || mode == DriverMode.Multiply)
            {
                float current = GetAxis(tr, axis);
                float next = mode == DriverMode.Add ? current + value : current * value;
                SetAxisAbsolute(tr, axis, next);
                return;
            }

            SetAxisAbsolute(tr, axis, value);
        }

        private static void SetAxisAbsolute(Transform tr, TargetAxis axis, float absoluteValue)
        {
            if (!tr) return;

            bool isPosition = axis == TargetAxis.PosX || axis == TargetAxis.PosY || axis == TargetAxis.PosZ;
            bool isRotation = axis == TargetAxis.RotX || axis == TargetAxis.RotY || axis == TargetAxis.RotZ;

            if (isPosition)
            {
                var v = tr.localPosition;
                switch (axis)
                {
                    case TargetAxis.PosX: v.x = absoluteValue; break;
                    case TargetAxis.PosY: v.y = absoluteValue; break;
                    case TargetAxis.PosZ: v.z = absoluteValue; break;
                }
                tr.localPosition = v;
            }
            else if (isRotation)
            {
                var e = tr.localEulerAngles;
                switch (axis)
                {
                    case TargetAxis.RotX: e.x = absoluteValue; break;
                    case TargetAxis.RotY: e.y = absoluteValue; break;
                    case TargetAxis.RotZ: e.z = absoluteValue; break;
                }
                tr.localEulerAngles = e;
            }
            else
            {
                var s = tr.localScale;

                if (axis == TargetAxis.ScaleXYZ)
                {
                    s.x = absoluteValue;
                    s.y = absoluteValue;
                    s.z = absoluteValue;
                }
                else
                {
                    switch (axis)
                    {
                        case TargetAxis.ScaleX: s.x = absoluteValue; break;
                        case TargetAxis.ScaleY: s.y = absoluteValue; break;
                        case TargetAxis.ScaleZ: s.z = absoluteValue; break;
                    }
                }

                tr.localScale = s;
            }
        }
    }
}