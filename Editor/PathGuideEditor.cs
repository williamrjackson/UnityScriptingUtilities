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
        Color warningColor = Color.Lerp(Color.red, Color.yellow, .5f);    
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
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.D && !e.control) 
            // Ensure the CTRL key is not down. It collides with the Unity Editor object duplication command
            {
                PathGuide connectedObjects = target as PathGuide;
                if (connectedObjects == null)
                    return;

                connectedObjects.Duplicate(connectedObjects);
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.D && e.control)
            {
                CustomLog.Log("CTRL+D is the wrong way to duplicate curve nodes. Just type D.", warningColor);
            }                                                                                                                                                                                                                                                                           
        }
    }
    #endif
}