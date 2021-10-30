using UnityEditor;
using UnityEngine;

namespace Wrj
{
    public class WrjSettings : ScriptableObject
    {
        [SerializeField]
        string _customScriptPath = string.Empty;

        [SerializeField]
        string _defaultNamespace = string.Empty;

        public static string CustomScriptPath
        {
            get
            {
                return EditorPrefs.GetString("_customScriptPath", string.Empty);
            }
            private set
            {
                EditorPrefs.SetString("_customScriptPath", value);
            }
        }
        public static string DefaultNamespace
        {
            get
            {
                return EditorPrefs.GetString("_defaultNamespace", Application.productName);
            }
            private set
            {
                EditorPrefs.SetString("_defaultNamespace", value);
            }
        }

        private void OnEnable()
        {
            Refresh(this);
        }

        public static void Refresh(WrjSettings instance)
        {
            CustomScriptPath = instance._customScriptPath;
            DefaultNamespace = instance._defaultNamespace;
        }
    }
}