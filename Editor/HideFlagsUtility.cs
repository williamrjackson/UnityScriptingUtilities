using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class HideFlagsUtility
{
    [MenuItem("Edit/Hide Flags/Show All In Hierarchy")]
    private static void ShowAll()
    {
        var allGameObjects = Object.FindObjectsOfType<GameObject>();
        List<Object> newSelection = new List<Object>();
        int shown = 0;
        foreach (var go in allGameObjects)
        {
            if (go.hideFlags.HasFlag(HideFlags.HideInHierarchy))
            {
                go.hideFlags &= ~HideFlags.HideInHierarchy;
                newSelection.Add(go);
                shown++;
            }
        }
        if (shown == 0)
        {
            Debug.Log("No hidden objects found in hierarchy.");
            return;
        }
        Selection.objects = newSelection.ToArray();
    }

    [MenuItem("Edit/Hide Flags/Hide Selected Objects")]
    private static void HideSelected()
    {
        foreach (var go in Selection.gameObjects)
        {
            go.hideFlags |= HideFlags.HideInHierarchy;
        }
    }

    [MenuItem("Edit/Hide Flags/Log Hidden Objects")]
    private static void LogHidden()
    {
        var allGameObjects = Object.FindObjectsOfType<GameObject>();
        int hiddenFound = 0;

        foreach (var go in allGameObjects)
        {
            if (go.hideFlags.HasFlag(HideFlags.HideInHierarchy))
            {
                Debug.Log($"Hidden Object {++hiddenFound}: {go.name}");
            }
        }
        if (hiddenFound == 0)
        {
            Debug.Log("No hidden objects found in hierarchy.");
            return;
        }
    }
}