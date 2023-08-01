using UnityEngine;
using UnityEditor;
using System.IO;

namespace Wrj
{
    public class NewScriptHandler : AssetPostprocessor
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var settings = WrjSettings.GetSerializedSettings();
            //Debug.Log($"Settings: {settings.ToString()}");
            SerializedProperty customPathProp = settings.FindProperty("_customScriptPath");
            string customPath = customPathProp.stringValue;
            //Debug.Log($"Custom Script Path: {customPath}");
            if (string.IsNullOrWhiteSpace(customPath)) return;

            // Check for the saved path string
            string asset = EditorPrefs.GetString("temp_scriptPath");
            if (!string.IsNullOrEmpty(asset))
            {
                // If it's found, delete it and move the asset
                EditorPrefs.DeleteKey("temp_scriptPath");
                MoveAsset(asset, customPath);
            }
        }

        void OnPreprocessAsset()
        {
            //Debug.Log($"New Script: {assetImporter.assetPath}");
            // Bail if it's not in the Assets root dir.
            if (assetImporter.assetPath.Split('/').Length > 2)
                return;
            if (Path.GetExtension(assetImporter.assetPath).ToLower() == ".cs")
            {
                //Debug.Log("Saving");
                EditorPrefs.SetString("temp_scriptPath", assetImporter.assetPath);
            }
        }

        static void MoveAsset(string assetPath, string subDir)
        {
            string filename = Path.GetFileName(assetPath);
            string filePath = Path.Combine("Assets", subDir, filename).ToString();
            //Debug.Log($"New path: {filePath}");
            // Create the folder if necessary
            (new FileInfo(filePath)).Directory.Create();

            // Move the asset.
            string error = AssetDatabase.MoveAsset(assetPath, filePath);
            if (!string.IsNullOrEmpty(error)) Debug.Log(error);
        }
    }
}