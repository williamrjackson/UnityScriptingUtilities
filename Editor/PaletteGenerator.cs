#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class SwatchEditorWindow : EditorWindow
{
    [MenuItem("Window/Swatch Generator")]
    static void Init()
    {
        window.Show();
    }
    
    static SwatchEditorWindow _window;
    static SwatchEditorWindow window
    {
        get
        {
            if (_window == null)
            {
                _window = (SwatchEditorWindow)GetWindow(typeof(SwatchEditorWindow), true, "Swatch Generator", true);
            }
            return _window;
        }
    }
    
    private enum PaletteStyle {
        CoolorsCode,
        Complimentary,
        SplitComplimentary,
        Monochromatic,
        Analogous,
        Triadic,
        Tetradic
    }

    private Vector2 scrollView;
    private bool isUrl = false;
    private bool isGradient = false;
    private ScriptableObject target;
    void OnGUI()
    {
        target = this;
        SerializedObject so = new SerializedObject(target);

        EditorGUILayout.LabelField("Palette Style:");
        paletteStyle = (PaletteStyle)EditorGUILayout.EnumPopup(paletteStyle);
        if (paletteStyle == PaletteStyle.CoolorsCode)
        {
            if (GUILayout.Button("Open Coolors Website"))
            {
                LaunchCoolors();
            }
            EditorGUILayout.LabelField("Paste Coolor Export Text or URL:");
            scrollView = EditorGUILayout.BeginScrollView(scrollView);
            CoolorsExportText = EditorGUILayout.TextArea(CoolorsExportText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        else
        {
            BaseColor = EditorGUILayout.ColorField(BaseColor);
        }

        SerializedProperty colorsProperty = so.FindProperty("colors");
        bool gradientCapable = colors != null && colors.Length < 9;
        if (colors != null && colors.Length > 0)
        {
            if (gradientCapable)
            {
                float height = (paletteStyle == PaletteStyle.CoolorsCode) ? 60f : 100f;
                EditorGUILayout.PropertyField(colorsProperty, false);
                EditorGUILayout.GradientField(gradient, GUILayout.Height(height));
            }
            else
            {
                EditorGUILayout.PropertyField(colorsProperty, true);
            }
        }

        string exportType = (isGradient) ? "Gradient" : "Swatch";

        PaletteName = EditorGUILayout.TextField($"{exportType} Name", PaletteName);

        if (isUrl && !isGradient)
        {
            EditorGUILayout.HelpBox("Url Pasted. Use Code Export to include color names.", MessageType.Info);
        }
        if (GUILayout.Button($"Export {exportType}"))
        {
            ExportUnitySwatch();
            if (gradientCapable)
            {
                ExportUnityGradient();
            }
            AssetDatabase.Refresh();
        }
    }
    private Color _baseColor = Color.clear;
    private Color BaseColor
    {
        get
        {
            if (_baseColor.a == 0f)
            {
                BaseColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);
            }
            return _baseColor;
        }
        set
        {
            if (_baseColor == value) return;
            _baseColor = value;
            SetColorsForStyle();
        }
    }

    private PaletteStyle _paletteStyle = PaletteStyle.CoolorsCode;
    private PaletteStyle paletteStyle
    {
        get
        {
            return _paletteStyle;
        }
        set
        {
            _paletteStyle = value;
            if (_paletteStyle == PaletteStyle.CoolorsCode)
            {
                window.minSize = new Vector2(560f, 560f);
                window.maxSize = new Vector2(float.MaxValue, float.MaxValue);
            }
            else
            {
                window.minSize = new Vector2(280f, 230f);
                window.maxSize = new Vector2(360f, 230f);
            }
            SetColorsForStyle();
        }
    }

    public string _paletteName;
    public string PaletteName
    {
        get
        {
            if (string.IsNullOrEmpty(_paletteName))
            {
                int increment = 1;
                string incrementStr = "";
                while (File.Exists(targetPath + $"{PlayerSettings.productName} Swatch{incrementStr}.colors"))
                {
                    increment++;
                    incrementStr = $" {increment}";
                }
                _paletteName = $"{PlayerSettings.productName} Swatch{incrementStr}";
            }
            return _paletteName.Trim();
        }
        set
        {
            if (_paletteName == value) return;
            Undo.RecordObject(this, "Palette Name");
            _paletteName = value;
        }
    }

    [NonReorderable]
    public CoolorsPaletteObject[] colors;
    
    public string _coolorsExportText;
    public string CoolorsExportText
    {
         get
         {
             return _coolorsExportText;
         }
         set
         {
            if (_coolorsExportText == value) return;
            Undo.RecordObject(this, "Palette Text");
            _coolorsExportText = value;
            ProcessCoolorsText();
         }
    }

    private string palettePath
    {
        get
        {
            int increment = 1;
            string incrementStr = "";
            while (File.Exists(targetPath + PaletteName + incrementStr +  ".colors"))
            {
                increment++;
                incrementStr = $" {increment}";
            }
            return targetPath + PaletteName + ".colors";
        }
    }
    private string targetPath
    {
        get
        {
            var path = Application.dataPath + "/Editor/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }

    private Gradient _gradient;
    private Gradient gradient
    {
        get
        {
            if (_gradient == null)
            {
                _gradient = new Gradient();
            }
            return _gradient;
        }
        set { _gradient = value; }
    }

    private void ClearColors()
    {
        colors = new CoolorsPaletteObject[0];
    }

    private void SetColorsForStyle()
    {
        Color[] set;
        switch (paletteStyle)
        {
            case PaletteStyle.Complimentary:
                set = Wrj.ColorHarmony.Complementary(BaseColor);
                break;
            case PaletteStyle.Analogous:
                set = Wrj.ColorHarmony.Analogous(BaseColor);
                break;
            case PaletteStyle.Monochromatic:
                set = Wrj.ColorHarmony.Monochromatic(BaseColor);
                break;
            case PaletteStyle.SplitComplimentary:
                set = Wrj.ColorHarmony.SplitComplementary(BaseColor);
                break;
            case PaletteStyle.Triadic:
                set = Wrj.ColorHarmony.Triadic(BaseColor);
                break;
            case PaletteStyle.Tetradic:
                set = Wrj.ColorHarmony.Tetradic(BaseColor);
                break;
            default:
                ProcessCoolorsText();
                return;
        }
        colors = new CoolorsPaletteObject[set.Length];
        for (int i = 0; i < set.Length; i++)
        {
            colors[i] = new CoolorsPaletteObject($"Color {i + 1}")
            {
                color = set[i]
            };
        }
        ProduceGradient();
    }

    private void ProcessCoolorsText()
    {
        if (string.IsNullOrWhiteSpace(CoolorsExportText))
        {
            ClearColors();
            return;
        }
        // Check for url
        // https://coolors.co/palette/264653-2a9d8f-e9c46a-f4a261-e76f51
        // https://coolors.co/gradient/1cdce8-bb77ed-f34a62
        isUrl = CoolorsExportText.Trim().StartsWith(@"http");
        isGradient = CoolorsExportText.Contains(@"gradient");
        bool status;
        if (isUrl)
        {
            status = ProcessUrl(CoolorsExportText);
        }
        else
        {
            status = ProcessCode(CoolorsExportText);
        }
        if (status)
        {
            ProduceGradient();
        }
        else
        {
            ClearColors();
        }

    }
    private bool ProcessUrl(string url)
    {
        int startPos = url.LastIndexOf(@"/");
        while (startPos == url.Length - 1)
        {
            url = url.Substring(0, url.Length - 1);
            startPos = url.LastIndexOf(@"/");
        }
        startPos += 1;
        string text = url.Substring(startPos, url.Length - startPos);
        text = text.Replace("-", "");
        if (text.Length % 6 != 0)
        {
            return false;
        }
        colors = new CoolorsPaletteObject[text.Length / 6];
        int index = 0;
        for (int i = 0; i < text.Length; i+=6)
        {
            colors[index] = new CoolorsPaletteObject($"Color {index + 1}");
            string hex = text.Substring(i, 6);
            if (!ColorUtility.TryParseHtmlString("#" + hex, out colors[index].color))
            {
                Debug.Log($"{hex} failed");
                return false;
            }
            index++;
        }
        return true;
    }

    private bool ProcessCode(string code)
    {
        if (isGradient)
        {
            foreach(string line in code.Split("\n", StringSplitOptions.None))
            {
                if (line.StartsWith(@"background: linear-gradient("))
                {
                    string pattern = @"hsla\((\d+), (\d+)%, (\d+)%, (\d)\) (\d+)%";
                    Regex rg = new Regex(pattern);
                    MatchCollection matches = rg.Matches(line);
                    colors = new CoolorsPaletteObject[matches.Count];
                    for (int i = 0; i < matches.Count; i++)
                    {
                        float h = Mathf.InverseLerp(0, 360, int.Parse(matches[i].Groups[1].Value));
                        float s = int.Parse(matches[i].Groups[2].Value) * .01f;
                        float v = int.Parse(matches[i].Groups[3].Value) * .01f;
                        float a = float.Parse(matches[i].Groups[4].Value);
                        float stop = float.Parse(matches[i].Groups[5].Value) * .01f;
                        //Debug.Log($"{i} -- H:{h}, S:{s}, V:{v}, A:{a}, @{stop}");
                        colors[i] = new CoolorsPaletteObject($"Gradient Stop {i+1}");
                        colors[i].color = Color.HSVToRGB(h, s, v);
                        colors[i].color.a = a;
                        colors[i].stop = stop;
                    }
                    return true;
                }
            }
            return false;
        }
        else
        {
            var coolorsCode = code.Split(new string[] { "/*" }, StringSplitOptions.None);
            foreach (var item in coolorsCode)
            {
                if (item.StartsWith(" Object"))
                {
                    string block = item;
                    block = block.Replace(" Object */", "");
                    block = block.Replace("{", "");
                    block = block.Replace("}", "");
                    block = block.Replace("\"", "");
                    var colorSetArray = block.Split(new string[] { "," }, StringSplitOptions.None);
                    colors = new CoolorsPaletteObject[colorSetArray.Length];
                    for (int i = 0; i < colorSetArray.Length; i++)
                    {
                        var nameColor = colorSetArray[i].Split(':');
                        colors[i] = new CoolorsPaletteObject(nameColor[0].Trim());
                        if (!ColorUtility.TryParseHtmlString("#" + nameColor[1].Trim().Substring(0, 6), out colors[i].color))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }
        return false;
    }

    private void ProduceGradient()
    {
        GradientColorKey[] colorKeys = new GradientColorKey[Math.Min(colors.Length, 8)];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        if (isGradient)
        {
            gradient.mode = GradientMode.Blend;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                float stop = colors[i].stop >= 0f ? colors[i].stop : i.Remap(0, colorKeys.Length - 1, 0f, 1f);
                colorKeys[i] = new GradientColorKey(colors[i].color, stop);
            }
        }
        else
        {
            gradient.mode = GradientMode.Fixed;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i] = new GradientColorKey(colors[i].color, (i + 1).Remap(0, colorKeys.Length, 0f, 1f));
            }
        }
        gradient.SetKeys(colorKeys, alphaKeys);
    }
    private void LaunchCoolors()
    {
        Application.OpenURL($"https://www.coolors.co/generate");
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

        string output = colorPresetTemplate.Replace("#NAME#", PaletteName);
        for (int i = 0; i < colors.Length; i++)
        {
            string newColor = presetElementTemplate.Replace("#NAME#", colors[i].name);
            newColor = newColor.Replace("#RVAL#", colors[i].color.r.ToString());
            newColor = newColor.Replace("#GVAL#", colors[i].color.g.ToString());
            newColor = newColor.Replace("#BVAL#", colors[i].color.b.ToString());
            output += newColor;
        }
        File.WriteAllText(palettePath, output);
        Debug.Log("Saved to " + palettePath);
    }
    public void ExportUnityGradient()
    {
        string gradientPath = targetPath + "CoolorGradients.gradients";
        string input = string.Empty;
        List<string> existing = new List<string>();
        if (File.Exists(gradientPath))
        {
            input = File.ReadAllText(gradientPath);
            existing = input.Split("  - m_Name:").ToList();
            // Remove header template
            existing.RemoveAt(0);
            // Remove entries with the same name
            existing = existing.Where(x => x.Substring(0, x.IndexOf(@"m_Gradient")).Trim() != $"{PaletteName}").ToList();
        }
        string newGradient = gradientElementTemplate.Replace("#NAME#", PaletteName).Trim();
        newGradient = newGradient.Replace($"#COLORCOUNT#", Math.Min(colors.Length, 8).ToString());
        for (int i = 0; i < 8; i++)
        {
            if (i < colors.Length)
            {
                string stop = "0"; 
                if (colors[i].stop >= 0f)
                {
                    stop = Mathf.RoundToInt(colors[i].stop.Remap(0f, 1f, 0f, maxTime)).ToString();
                }
                else
                {
                    stop = Mathf.RoundToInt(i.Remap(0, colors.Length - 1, 0, maxTime)).ToString();
                }
                newGradient = newGradient.Replace($"#RVAL{i}#", colors[i].color.r.ToString());
                newGradient = newGradient.Replace($"#GVAL{i}#", colors[i].color.g.ToString());
                newGradient = newGradient.Replace($"#BVAL{i}#", colors[i].color.b.ToString());
                newGradient = newGradient.Replace($"#AVAL{i}#", colors[i].color.a.ToString());
                newGradient = newGradient.Replace($"#CTIME{i}#", stop);
            }
            else
            {
                newGradient = newGradient.Replace($"#RVAL{i}#", "0");
                newGradient = newGradient.Replace($"#GVAL{i}#", "0");
                newGradient = newGradient.Replace($"#BVAL{i}#", "0");
                newGradient = newGradient.Replace($"#AVAL{i}#", "0");
                newGradient = newGradient.Replace($"#CTIME{i}#", "0");
            }
        }
        var output = gradientPresetTemplate.Trim();
        if (existing.Count > 0)
        {
            foreach (var gradientBlock in existing)
            {
                output += Environment.NewLine;
                output += $"  - m_Name: {gradientBlock.Trim()}";
            }
        }
        output += Environment.NewLine;  // Linebreak
        output += "  ";                 // Indent
        output += newGradient;          // New content
        File.WriteAllText(gradientPath, output);
        Debug.Log($"Gradient added to {gradientPath}");
    }

    int maxTime = 65535;
    string colorPresetTemplate = @"%YAML 1.1
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
    string presetElementTemplate = @"
  - m_Name: #NAME#
    m_Color: {r: #RVAL#, g: #GVAL#, b: #BVAL#, a: 1}";
    string gradientPresetTemplate = @"%YAML 1.1
%YAML 1.1
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
  m_Script: {fileID: 12321, guid: 0000000000000000e000000000000000, type: 0}
  m_Name: CoolorGradients
  m_EditorClassIdentifier: 
  m_Presets:";
    string gradientElementTemplate = @"
  - m_Name: #NAME#
    m_Gradient:
      serializedVersion: 2
      key0: {r: #RVAL0#, g: #GVAL0#, b: #BVAL0#, a: #AVAL0#}
      key1: {r: #RVAL1#, g: #GVAL1#, b: #BVAL1#, a: #AVAL1#}
      key2: {r: #RVAL2#, g: #GVAL2#, b: #BVAL2#, a: #AVAL2#}
      key3: {r: #RVAL3#, g: #GVAL3#, b: #BVAL3#, a: #AVAL3#}
      key4: {r: #RVAL4#, g: #GVAL4#, b: #BVAL4#, a: #AVAL4#}
      key5: {r: #RVAL5#, g: #GVAL5#, b: #BVAL5#, a: #AVAL5#}
      key6: {r: #RVAL6#, g: #GVAL6#, b: #BVAL6#, a: #AVAL6#}
      key7: {r: #RVAL7#, g: #GVAL7#, b: #BVAL7#, a: #AVAL7#}
      ctime0: #CTIME0#
      ctime1: #CTIME1#
      ctime2: #CTIME2#
      ctime3: #CTIME3#
      ctime4: #CTIME4#
      ctime5: #CTIME5#
      ctime6: #CTIME6#
      ctime7: #CTIME7#
      atime0: 0
      atime1: 65535
      atime2: 0
      atime3: 0
      atime4: 0
      atime5: 0
      atime6: 0
      atime7: 0
      m_Mode: 0
      m_NumColorKeys: #COLORCOUNT#
      m_NumAlphaKeys: 2";
    [Serializable]
    public class CoolorsPaletteObject
    {
        public Color color;
        public string name;
        public float stop;
        public CoolorsPaletteObject(string name)
        {
            this.stop = -1f;
            this.color = Color.white;
            this.name = name;
        }
    }
}
#endif