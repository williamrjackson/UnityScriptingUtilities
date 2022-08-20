#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using UnityEditor;

class CoolorsSwatchEditorWindow : EditorWindow
{
    [MenuItem("Window/Coolor Swatch Generator")]
    static void Init()
    {
        CoolorsSwatchEditorWindow window = (CoolorsSwatchEditorWindow)EditorWindow.GetWindow(typeof(CoolorsSwatchEditorWindow), true, "Coolors Swatch Generator", true);
        window.Show();
    }

    private Vector2 scrollView;
    private bool isUrl = false;
    void OnGUI()
    {
        if (GUILayout.Button("Open Coolors Website"))
        {
            LaunchCoolors();
        }

        PaletteName = EditorGUILayout.TextField("Swatch Name", PaletteName);

        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty colorsProperty = so.FindProperty("colors");
        EditorGUILayout.PropertyField(colorsProperty, true);

        EditorGUILayout.LabelField("Paste Coolor Export Text or Palette URL:");
        scrollView = EditorGUILayout.BeginScrollView(scrollView);
        CoolorsExportText = EditorGUILayout.TextArea(CoolorsExportText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        if (isUrl)
        {
            EditorGUILayout.HelpBox("Url Pasted. Use Code Export to include color names.", MessageType.Info);
        }
        if (GUILayout.Button("Export Swatch"))
        {
            ExportUnitySwatch();
        }
    }

    public string _paletteName;
    public string PaletteName
    {
        get
        {
            if (string.IsNullOrEmpty(_paletteName))
            {
                _paletteName = $"{PlayerSettings.productName} Swatch";
            }
            return _paletteName;
        }
        set
        {
            _paletteName = value;
        }
    }
    public CoolorsPaletteObject[] colors;
    
    private string _coolorsExportText;
    public string CoolorsExportText
    {
         get
         {
             return _coolorsExportText;
         }
         set
         {
            if (_coolorsExportText == value) return;
             _coolorsExportText = value;
             ProcessCoolorsText();
         }
    }

    private void ClearColors()
    {
        colors = new CoolorsPaletteObject[0];
    }

    private void ProcessCoolorsText()
    {
        // Check for url
        isUrl = CoolorsExportText.StartsWith(@"https://coolors.co/palette/");
        if (string.IsNullOrWhiteSpace(CoolorsExportText))
        {
            if (colors.Length > 0)
                ClearColors();
            return;
        }
        if (isUrl)
        {
            string text = CoolorsExportText.Replace(@"https://coolors.co/palette/", "");
            var hexArray = text.Split('-');
            colors = new CoolorsPaletteObject[hexArray.Length];
            for (int i = 0; i < hexArray.Length; i++)
            {
                colors[i] = new CoolorsPaletteObject($"Color {i + 1}");
                if (!ColorUtility.TryParseHtmlString("#" + hexArray[i].Trim().Substring(0, 6), out colors[i].color))
                {
                    ClearColors();
                    return;
                }
            }
            //https://coolors.co/palette/264653-2a9d8f-e9c46a-f4a261-e76f51
            return;
        }
        var coolorsExport = _coolorsExportText.Split(new string[] { "/*" }, System.StringSplitOptions.None);
        foreach (var item in coolorsExport)
        {
            if (item.StartsWith(" Object"))
            {
                string block = item;
                block = block.Replace(" Object */", "");
                block = block.Replace("{", "");
                block = block.Replace("}", "");
                block = block.Replace("\"", "");
                var colorSetArray = block.Split(new string[] { "," }, System.StringSplitOptions.None);
                colors = new CoolorsPaletteObject[colorSetArray.Length];
                for (int i = 0; i < colorSetArray.Length; i++)
                {
                    var nameColor = colorSetArray[i].Split(':');
                    colors[i] = new CoolorsPaletteObject(nameColor[0].Trim());
                    if (!ColorUtility.TryParseHtmlString("#" + nameColor[1].Trim().Substring(0, 5), out colors[i].color))
                    {
                        ClearColors();
                        return;
                    }
                }
                return;
            }
        }
        ClearColors();
    }

    private void LaunchCoolors()
    {
        Application.OpenURL($"https://www.coolors.app/generate");
    }

    public void ExportUnitySwatch()
    {
        if (string.IsNullOrWhiteSpace(PaletteName))
        {
            Debug.LogWarning("Swatch Save Failed: Palette Name is Required.");
            return;
        }
        if (colors.Length == 0)
        {
            Debug.LogWarning("Swatch Save Failed: No Palette Colors found.");
            return;
        }
        string template = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &1
MonoBehaviour:
  m_ObjectHideFlags: 52
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 12323, guid: 0000000000000000e000000000000000, type: 0}
  m_Name: #NAME#
  m_EditorClassIdentifier: 
  m_Presets:";
        string presetTemplate = @"
  - m_Name: #NAME#
    m_Color: {r: #RVAL#, g: #GVAL#, b: #BVAL#, a: 1}";
        //0.15686275
        string output = template.Replace("#NAME#", PaletteName);
        for (int i = 0; i < colors.Length; i++)
        {
            string newColor = presetTemplate.Replace("#NAME#", colors[i].name);
            newColor = newColor.Replace("#RVAL#", colors[i].color.r.ToString());
            newColor = newColor.Replace("#GVAL#", colors[i].color.g.ToString());
            newColor = newColor.Replace("#BVAL#", colors[i].color.b.ToString());
            output += newColor;
        }
        string targetPath = Application.dataPath + "/Editor/";
        Directory.CreateDirectory(targetPath);
        string savePath = targetPath + PaletteName + ".colors";
        File.WriteAllText(savePath, output);
        Debug.Log("Saved to " + savePath);
    }

    [System.Serializable]
    public class CoolorsPaletteObject
    {
        public Color color;
        public string name;
        public CoolorsPaletteObject(string name)
        {
            this.color = Color.white;
            this.name = name;
        }
    }
}
#endif
