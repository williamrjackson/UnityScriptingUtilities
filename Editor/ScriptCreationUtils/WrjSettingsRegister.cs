using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Wrj
{
    public static class WrjSettingsRegister
    {
        const string SettingsPath = "Assets/UnityScriptingUtilities/Editor/ScriptCreationUtils/Wrj.Settings.asset";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Custom Script Utils Settings", SettingsScope.Project)
            {
                guiHandler = (searchContext) =>
                {
                    var settings = Load();
                    var serialized = new SerializedObject(settings);

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(serialized.FindProperty("_customScriptPath"), new GUIContent("Custom Script Path"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("_defaultNamespace"), new GUIContent("Default Namespace"));
                    EditorGUILayout.HelpBox("New scripts created directly on GameObjects or in Assets root will automatically move to the Custom Script Path. Leave blank to disable.\n\n" +
                        "Default Namespace requires a custom monobehavior script in `%EDITOR_PATH%\\Data\\Resources\\ScriptTemplates` containing the variable #NAMESPACE#.", MessageType.Info);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.ApplyModifiedProperties();
                        WrjSettings.Refresh(settings);
                    }
                },

                keywords = new HashSet<string>(new[] { "Custom scripts path", "Default namespace" })
            };
        }

        static WrjSettings Load()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WrjSettings>(SettingsPath);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WrjSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }
    }
}
