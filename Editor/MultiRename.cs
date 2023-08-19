using UnityEngine;
using UnityEditor;

public class MultiRename : ScriptableWizard
{
    public string oldToken;
    public string newToken;

    [MenuItem("Edit/Multi Rename")]
    [MenuItem("GameObject/Multi Rename", false, 0)]
    public static void CreateWizard()
    {
        var wizardWindow = ScriptableWizard.DisplayWizard("Multi Rename", typeof(MultiRename), "Okay", "Apply");
        wizardWindow.minSize = new Vector2(300f, 150f);
    }

    [MenuItem("Edit/Multi Rename", true)]
    [MenuItem("GameObject/Multi Rename", true)]
    private static bool MultiSelectValidate() => Selection.gameObjects.Length > 1;

    private void OnWizardCreate()
    {
        Rename();
        Close();
    }
    private void OnWizardOtherButton() 
    {
        Rename();
    }
    private void Rename()
    {
        foreach (var selectedObject in Selection.gameObjects)
        {
            string objName = selectedObject.name;
            if (objName.Contains(oldToken))
            {
                Undo.RecordObject(selectedObject, "Multi Rename");
                selectedObject.name = objName.Replace(oldToken, newToken);
            }
        }
    }

    bool _hasPrePopulated = false;
    bool _hasChangeListener = false;
    void OnWizardUpdate()
    {
        // Only try to do this once or if selection has changed
        if (!_hasPrePopulated) 
        {
            _hasPrePopulated = true;
            PrePopulateOld();
        }
        if (!_hasChangeListener)
        {
            Selection.selectionChanged += () => 
            {
                _hasChangeListener = true;
                PrePopulateOld();
            };
        }
        if (string.IsNullOrEmpty(oldToken)) isValid = false;
        else isValid = true;
    }

    void PrePopulateOld()
    {
        if (Selection.gameObjects.Length < 2) return;

        string subStr = Selection.gameObjects[0].name;

        for (int i = 1; i < Selection.gameObjects.Length; i++)
        {
            subStr = CommonSubstring(subStr, Selection.gameObjects[i].name);
        }
        if (!string.IsNullOrEmpty(subStr))
        {
            oldToken = subStr;
            Repaint();
        }
    }

    // Stolen from https://www.geeksforgeeks.org/print-longest-common-substring/
    string CommonSubstring(string X, string Y)
    {
        // Create a table to store lengths of longest common
        // suffixes of substrings. Note that LCSuff[i][j]
        // contains length of longest common suffix of X[0..i-1]
        // and Y[0..j-1]. The first row and first column entries
        // have no logical meaning, they are used only for
        // simplicity of program
        int m = X.Length;
        int n = Y.Length;
        int[, ] LCSuff = new int[m + 1, n + 1];
 
        // To store length of the longest common substring
        int len = 0;
 
        // To store the index of the cell which contains the
        // maximum value. This cell's index helps in building
        // up the longest common substring from right to left.
        int row = 0, col = 0;
 
        /* Following steps build LCSuff[m+1][n+1] in bottom
        up fashion. */
        for (int i = 0; i <= m; i++) {
            for (int j = 0; j <= n; j++) {
                if (i == 0 || j == 0)
                    LCSuff[i, j] = 0;
 
                else if (X[i - 1] == Y[j - 1]) {
                    LCSuff[i, j] = LCSuff[i - 1, j - 1] + 1;
                    if (len < LCSuff[i, j]) {
                        len = LCSuff[i, j];
                        row = i;
                        col = j;
                    }
                }
                else
                    LCSuff[i, j] = 0;
            }
        }
 
        // if true, then no common substring exists
        if (len == 0) {
            return string.Empty;
        }
 
        // allocate space for the longest common substring
        string resultStr = string.Empty;
 
        // traverse up diagonally form the (row, col) cell
        // until LCSuff[row][col] != 0
        while (LCSuff[row, col] != 0) {
            resultStr = X[row - 1] + resultStr; // or Y[col-1]
            --len;
 
            // move diagonally up to previous cell
            row--;
            col--;
        }
 
        // longest common substring. 
        // Fail on 1 or 2 character substrings.
        return (resultStr.Length > 2) ? resultStr : string.Empty;
    }
}