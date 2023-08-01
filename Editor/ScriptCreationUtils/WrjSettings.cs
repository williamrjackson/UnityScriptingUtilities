using UnityEditor;
using UnityEngine;
using System.IO;

namespace Wrj
{
    public class WrjSettings : ScriptableObject
    {
        [SerializeField]
        string _customScriptPath = string.Empty;

        internal static WrjSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WrjSettings>(WrjSettingsProvider.k_WrjSettingsPath);
            if (settings == null)
            {
                (new FileInfo(WrjSettingsProvider.k_WrjSettingsPath)).Directory.Create();
                settings = ScriptableObject.CreateInstance<WrjSettings>();
                settings._customScriptPath = "";
                AssetDatabase.CreateAsset(settings, WrjSettingsProvider.k_WrjSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}