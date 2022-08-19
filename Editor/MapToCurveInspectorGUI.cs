using UnityEditor;
using UnityEngine;

namespace Wrj
{
    [CustomPropertyDrawer(typeof(Utils.MapToCurve))]
    public class MapToCurveInspectorGUI : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + 35;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var bMinMax = property.FindPropertyRelative("_isMinMaxCurveMode");
            // Calculate rects
            var toggleLabel = new GUIContent("Use Min/Max");
            var toggleRect = new Rect(position.x, position.y, position.width, 20);
            var curveRect = new Rect(position.x, position.y + 25, position.width, 30);

            // Draw fields
            EditorGUI.PropertyField(toggleRect, bMinMax, toggleLabel);
            if (bMinMax.boolValue)
            {
                var minMaxCurve = property.FindPropertyRelative("_minMaxCurve");
                EditorGUI.PropertyField(curveRect, minMaxCurve, GUIContent.none);
                var mode = minMaxCurve.FindPropertyRelative("m_Mode");
                var curveMin = minMaxCurve.FindPropertyRelative("m_CurveMin");
                var curveMax = minMaxCurve.FindPropertyRelative("m_CurveMax");
                mode.enumValueIndex = 2;
                if (curveMin.animationCurveValue.keys.Length == 0)
                {
                    curveMax.animationCurveValue = Utils.MapToCurve.EaseInCurve;
                    curveMin.animationCurveValue = Utils.MapToCurve.EaseOutCurve;
                }
            }
            else
            {
                EditorGUI.PropertyField(curveRect, property.FindPropertyRelative("_curve"), GUIContent.none);
            }
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

    }
}