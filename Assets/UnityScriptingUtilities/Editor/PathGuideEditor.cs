using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Wrj
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(PathGuide))]
    public class PathGuideEditor : Editor {
        public override void OnInspectorGUI()
        {
            PathGuide connectedObjects = target as PathGuide;
            if (connectedObjects == null)
                return;
            if (GUILayout.Button("Duplicate Node (D)", GUILayout.Width(255)))
            {
                connectedObjects.Duplicate(connectedObjects);
            }
        }
        void OnSceneGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.D)
            {
                PathGuide connectedObjects = target as PathGuide;
                if (connectedObjects == null)
                    return;

                connectedObjects.Duplicate(connectedObjects);
                Debug.Log("Dupe");
            }
        }
    }
    #endif
}