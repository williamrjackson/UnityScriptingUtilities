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
    void OnGUI()
    {
        paletteName = EditorGUILayout.TextField("Swatch Name", paletteName);
        if (GUILayout.Button("Export Swatch"))
        {
            ExportUnitySwatch();
        }
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty colorsProperty = so.FindProperty("colors");

        EditorGUILayout.PropertyField(colorsProperty, true);
        
        CoolorsExportText = EditorGUILayout.TextField("Paste Coolor Export Text:", CoolorsExportText);
    }
    public string paletteName;
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
             _coolorsExportText = value;
             ProcessCoolorsText();
         }
     }
    private void ProcessCoolorsText()
    {
        if (string.IsNullOrWhiteSpace(CoolorsExportText)) return;
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
                    ColorUtility.TryParseHtmlString("#" + nameColor[1].Trim(), out colors[i].color);
                }
                return;
            }
        }
    }
    public void ExportUnitySwatch()
    {
        if (string.IsNullOrWhiteSpace(paletteName))
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
        string output = template.Replace("#NAME#", paletteName);
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
        File.WriteAllText(targetPath + paletteName + ".colors", output);
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
