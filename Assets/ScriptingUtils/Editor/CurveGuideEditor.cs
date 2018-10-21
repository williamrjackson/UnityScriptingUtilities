using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Wrj
{
    [CustomEditor(typeof(CurveGuide))]
    public class CurveGuideEditor : Editor {
        public override void OnInspectorGUI()
        {
            CurveGuide connectedObjects = target as CurveGuide;
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
                CurveGuide connectedObjects = target as CurveGuide;
                if (connectedObjects == null)
                    return;

                connectedObjects.Duplicate(connectedObjects);
                Debug.Log("Dupe");
            }
        }
    }
}