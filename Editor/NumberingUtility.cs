using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Linq;

public static class NumberingUtility
{
    [MenuItem("Edit/Renumber")]
    [MenuItem("GameObject/Renumber", false, 0)]
    private static void Renumber()
    {
        var selection = Selection.objects.OfType<GameObject>().ToArray();
        for (int i = 0; i < selection.Length; i++)
        {
            GameObject go = selection[i];
            string output = Regex.Replace(go.name, @"[_([.\s]*?\d+[)\]\s]*[^\S]*", string.Empty);

            string format = new string('0', EditorSettings.gameObjectNamingDigits);
            string digit = i.ToString(format);
            switch (EditorSettings.gameObjectNamingScheme)
            {
                case EditorSettings.NamingScheme.Dot:
                    go.name = $"{output}.{digit}";
                    break;
                case EditorSettings.NamingScheme.Underscore:
                    go.name = $"{output}_{digit}";
                    break;
                default: // spaceParenthesis
                    go.name = $"{output} ({digit})";
                    break;
            }
        }
    }
    [MenuItem("Edit/Renumber", true)]
    [MenuItem("GameObject/Renumber", true)]
    private static bool MultiSelectValidate() => Selection.gameObjects.Length > 1;
}