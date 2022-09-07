using UnityEngine;
using UnityEditor;

namespace Wrj
{
    [CustomPropertyDrawer(typeof(ExtendedGradient))]
    public class ExtendedGradientDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ExtendedGradient gradient = fieldInfo.GetValue(property.serializedObject.targetObject) as ExtendedGradient;
            if (gradient == null)
            {
                return;
            }

            Event guiEvent = Event.current;
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            EditorGUI.BeginProperty(position, label, property);
            Rect textureRect = new Rect(position.x, position.y, position.width, position.height);

            if (guiEvent.type == EventType.Repaint)
            {
                Texture2D texture = gradient.CreateTexture(Mathf.RoundToInt(textureRect.width));
                EditorGUI.DrawPreviewTexture(textureRect, texture);
            }
            EditorGUI.EndProperty();
        }
    }
}