using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.IO;

namespace Wrj
{
    class WrjSettingsProvider : SettingsProvider
    {
        public const string k_WrjSettingsPath = "Assets/Editor/Wrj.Settings.asset";
        private SerializedObject m_WrjSettings;

        public static GUIContent CustomScriptPath = new GUIContent("Custom Script Path");
        
        public WrjSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }
 
        
        public static bool IsSettingsAvailable()
        {
            return File.Exists(k_WrjSettingsPath);
        }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_WrjSettings = WrjSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        { 
            m_WrjSettings.Update();
            EditorGUILayout.HelpBox("New scripts created in the root assets folder will be automatically moved to the path specified here.", MessageType.None);
            EditorGUILayout.PropertyField(m_WrjSettings.FindProperty("_customScriptPath"), CustomScriptPath);
            m_WrjSettings.ApplyModifiedProperties();
        }

        [SettingsProvider]
        public static SettingsProvider CreateWrjSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                var provider = new WrjSettingsProvider("Project/Custom Script Utils Settings", SettingsScope.Project);
                provider.keywords = new HashSet<string>(new[] { "Custom scripts path" });
                return provider;
            }
            return null;
        }
    }
}
