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
            var customPath = WrjSettings.CustomScriptPath;

            if (string.IsNullOrWhiteSpace(customPath)) return;

            // Check for the saved path string
            string asset = EditorPrefs.GetString("scriptPath");
            if (!string.IsNullOrEmpty(asset))
            {
                // If it's found, delete it and move the asset
                EditorPrefs.DeleteKey("scriptPath");
                MoveAsset(asset, customPath);
            }
        }

        void OnPreprocessAsset()
        {
            // Bail if it's not in the Assets root dir.
            if (assetImporter.assetPath.Split('/').Length > 2)
                return;
            if (Path.GetExtension(assetImporter.assetPath).ToLower() == ".cs")
            {
                EditorPrefs.SetString("scriptPath", assetImporter.assetPath);
            }
        }

        static void MoveAsset(string assetPath, string subDir)
        {
            string filename = Path.GetFileName(assetPath);
            string filePath = "Assets/" + subDir + "/" + filename;

            // Create the folder if necessary
            (new FileInfo(filePath)).Directory.Create();

            // Move the asset.
            string error = AssetDatabase.MoveAsset(assetPath, filePath);
            if (!string.IsNullOrEmpty(error)) Debug.Log(error);
        }
    }
}