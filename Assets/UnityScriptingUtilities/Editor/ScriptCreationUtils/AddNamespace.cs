#if true
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class AddNamespace : UnityEditor.AssetModificationProcessor {

	public static void OnWillCreateAsset(string path) 
	{
		// Find the name of the file this meta represents
		path = path.Replace(".meta", "");
		// Bail if it's not a .cs file
		if(!path.EndsWith(".cs")) return;
		// Read contents
		string file = System.IO.File.ReadAllText(path);

		// Replace #NAMESPACE# keyword with cleaned up product name
		string _namespace = Wrj.WrjSettings.DefaultNamespace;
		_namespace = ConvertToIdentifier(_namespace);
		file = file.Replace("#NAMESPACE#", _namespace);

		// Rewrite the file and refresh the database
		System.IO.File.WriteAllText(path, file);
		AssetDatabase.Refresh();
	}
	
	private static string ConvertToIdentifier(string ns)
	{
		// Strip to alphanumeric
		Regex rgx = new Regex("[^a-zA-Z0-9]");
		string result = rgx.Replace(ns, "");
		// Don't allow a digit in the front.
		if (char.IsDigit(result.ToCharArray()[0]))
		{
			result = "_" + result;
		}
		return result;
	}
}
#endif