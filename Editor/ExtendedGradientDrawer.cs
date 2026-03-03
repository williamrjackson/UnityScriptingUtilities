using UnityEngine;
using UnityEditor;

namespace Wrj
{
    [CustomPropertyDrawer(typeof(ExtendedGradient))]
    public class ExtendedGradientDrawer : PropertyDrawer
    {
        private static readonly System.Collections.Generic.Dictionary<int, Texture2D> s_TextureCache = new System.Collections.Generic.Dictionary<int, Texture2D>();

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
                int width = Mathf.Max(1, Mathf.RoundToInt(textureRect.width));
                int cacheKey = property.serializedObject.targetObject.GetInstanceID() ^ fieldInfo.MetadataToken;
                if (!s_TextureCache.TryGetValue(cacheKey, out Texture2D texture) || texture == null || texture.width != width)
                {
                    if (texture != null)
                    {
                        Object.DestroyImmediate(texture);
                    }
                    texture = gradient.CreateTexture(width);
                    texture.hideFlags = HideFlags.HideAndDontSave;
                    s_TextureCache[cacheKey] = texture;
                }
                EditorGUI.DrawPreviewTexture(textureRect, texture);
            }
            EditorGUI.EndProperty();
        }
    }
}