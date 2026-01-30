using System;
using UnityEditor;
using UnityEngine;
namespace Wrj.TransformExpressions
{
[CreateAssetMenu(
    menuName = "Transform Expressions/Presets/Feet & Inches",
    fileName = "FeetInchesPreset")]
    public sealed class FeetInchesPreset : TransformPreset
    {
        private const float MetersPerFoot = 0.3048f;
        private const float MetersPerInch = 0.0254f;

        [Header("Edit Toggles")]
        [SerializeField] private bool editPosition = true;
        [SerializeField] private bool editScale = true;
        [SerializeField] private bool linkScale = true;

        [Header("Position (World)")]
        [SerializeField] private FeetInchesVector pos;

        [Header("Lossy Scale")]
        [SerializeField] private FeetInchesVector scale;

        [Serializable]
        private struct FeetInchesValue
        {
            public int feet;
            public float inches;

            public float ToMeters()
            {
                return feet * MetersPerFoot + inches * MetersPerInch;
            }

            public static FeetInchesValue FromMeters(float meters)
            {
                float totalFeet = meters / MetersPerFoot;
                int wholeFeet = Mathf.FloorToInt(totalFeet);
                float remainingFeet = totalFeet - wholeFeet;
                float inches = remainingFeet * 12f;

                return new FeetInchesValue
                {
                    feet = wholeFeet,
                    inches = inches
                };
            }
        }

        [Serializable]
        private struct FeetInchesVector
        {
            public FeetInchesValue x;
            public FeetInchesValue y;
            public FeetInchesValue z;

            public Vector3 ToMeters()
            {
                return new Vector3(
                    x.ToMeters(),
                    y.ToMeters(),
                    z.ToMeters());
            }

            public static FeetInchesVector FromMeters(Vector3 v)
            {
                return new FeetInchesVector
                {
                    x = FeetInchesValue.FromMeters(v.x),
                    y = FeetInchesValue.FromMeters(v.y),
                    z = FeetInchesValue.FromMeters(v.z)
                };
            }
        }

        public override bool DrawGUI(PresetContext ctx)
        {
            EditorGUI.BeginChangeCheck();

            editPosition = EditorGUILayout.ToggleLeft("Edit World Position", editPosition);
            editScale = EditorGUILayout.ToggleLeft("Edit Lossy Scale", editScale);

            EditorGUILayout.Space(6);

            if (Selection.transforms.Length > 0)
            {
                var tr = Selection.transforms[0];

                if (editPosition)
                {
                    EditorGUILayout.LabelField("World Position", EditorStyles.miniBoldLabel);
                    pos = FeetInchesVector.FromMeters(tr.position);
                    DrawVectorGUI(ref pos);
                }

                EditorGUILayout.Space(6);

                if (editScale)
                {
                    EditorGUILayout.LabelField("Lossy Scale (World)", EditorStyles.miniBoldLabel);

                    // show current scale based on first selected tr's lossy scale
                    scale = FeetInchesVector.FromMeters(tr.lossyScale);

                    // Link toggle for uniform scaling across axes (icon)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Link Scale Axes", GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        GUIContent linkContent = new GUIContent(linkScale ? "🔗" : "🔓", linkScale ? "Linked" : "Unlinked");
                        if (GUILayout.Button(linkContent, EditorStyles.miniButton, GUILayout.Width(24)))
                        {
                            linkScale = !linkScale;
                        }
                    }

                    // Draw fields and detect changes
                    EditorGUI.BeginChangeCheck();
                    DrawVectorGUI(ref scale);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Apply changes immediately to the currently selected transforms (live preview)
                        var targets = ctx?.GetOrderedSelection?.Invoke();
                        if (targets != null && targets.Length > 0)
                        {
                            ctx.RecordUndo(targets, DisplayName + " (live)");
                            Vector3 desired = scale.ToMeters();
                            foreach (var target in targets)
                            {
                                if (!target) continue;
                                ApplyWorldScale(target, desired);
                            }
                        }
                    }
                }
            }

            return EditorGUI.EndChangeCheck();
        }

        private void DrawVectorGUI(ref FeetInchesVector vec)
        {
            if (linkScale)
            {
                // If linked, any axis edit will propagate to the others
                EditorGUI.BeginChangeCheck();
                DrawAxisGUI("X", ref vec.x);
                if (EditorGUI.EndChangeCheck())
                {
                    vec.y = vec.x;
                    vec.z = vec.x;
                }

                EditorGUI.BeginChangeCheck();
                DrawAxisGUI("Y", ref vec.y);
                if (EditorGUI.EndChangeCheck())
                {
                    vec.x = vec.y;
                    vec.z = vec.y;
                }

                EditorGUI.BeginChangeCheck();
                DrawAxisGUI("Z", ref vec.z);
                if (EditorGUI.EndChangeCheck())
                {
                    vec.x = vec.z;
                    vec.y = vec.z;
                }
            }
            else
            {
                DrawAxisGUI("X", ref vec.x);
                DrawAxisGUI("Y", ref vec.y);
                DrawAxisGUI("Z", ref vec.z);
            }
        }

        private void DrawAxisGUI(string label, ref FeetInchesValue v)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(18));
                v.feet = EditorGUILayout.IntField(v.feet, GUILayout.Width(50));
                GUILayout.Label("ft", GUILayout.Width(18));
                v.inches = EditorGUILayout.FloatField(v.inches, GUILayout.Width(60));
                GUILayout.Label("in");
            }
        }

        public override void Apply(PresetContext ctx, Transform[] targets)
        {
            if (targets == null || targets.Length == 0)
                return;

            Undo.RecordObjects(targets, DisplayName);

            foreach (var tr in targets)
            {
                if (!tr) continue;

                if (editPosition)
                {
                    tr.position = pos.ToMeters();
                }

                if (editScale)
                {
                    ApplyWorldScale(tr, scale.ToMeters());
                }
            }
        }

        private static void ApplyWorldScale(Transform tr, Vector3 desiredWorldScale)
        {
            if (!tr.parent)
            {
                tr.localScale = desiredWorldScale;
                return;
            }

            Vector3 parentScale = tr.parent.lossyScale;

            Vector3 newLocal = new Vector3(
                SafeDivide(desiredWorldScale.x, parentScale.x),
                SafeDivide(desiredWorldScale.y, parentScale.y),
                SafeDivide(desiredWorldScale.z, parentScale.z)
            );

            tr.localScale = newLocal;
        }

        private static float SafeDivide(float a, float b)
        {
            if (Mathf.Approximately(b, 0f)) return 0f;
            return a / b;
        }
    }
}