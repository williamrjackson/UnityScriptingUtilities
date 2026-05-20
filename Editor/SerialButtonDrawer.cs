#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Wrj
{
    [CustomPropertyDrawer(typeof(ArduinoTestConfig.SerialButton))]
    public class SerialButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var mode = property.FindPropertyRelative("mode");
            var command = property.FindPropertyRelative("command");
            var keyCombo = property.FindPropertyRelative("keyCombo");

            position.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(position, mode);

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if ((ArduinoTestConfig.ButtonMode)mode.enumValueIndex == ArduinoTestConfig.ButtonMode.Serial)
                {
                    if (command != null)
                        EditorGUI.PropertyField(position, command);
                }
                else
                {
                    if (keyCombo != null)
                        EditorGUI.PropertyField(position, keyCombo, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var mode = property.FindPropertyRelative("mode");

            float height = EditorGUIUtility.singleLineHeight; // foldout
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // mode

            if ((ArduinoTestConfig.ButtonMode)mode.enumValueIndex == ArduinoTestConfig.ButtonMode.Keypress)
            {
                var keyCombo = property.FindPropertyRelative("keyCombo");
                height += (keyCombo != null ? EditorGUI.GetPropertyHeight(keyCombo, true) : EditorGUIUtility.singleLineHeight) + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
#endif
