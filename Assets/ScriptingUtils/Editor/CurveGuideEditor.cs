using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wrj
{
    [CustomEditor(typeof(CurveGuide))]
    public class CurveGuideEditor : Editor {

        public override void OnInspectorGUI()
        {
            CurveGuide connectedObjects = target as CurveGuide;
            if (connectedObjects == null)
                return;
            if (GUILayout.Button("Duplicate Node", GUILayout.Width(255)))
            {
                GameObject obj = Instantiate(connectedObjects.gameObject);
                obj.transform.parent = connectedObjects.transform.parent;
                obj.transform.position = connectedObjects.transform.position;
                int index = (connectedObjects.transform.GetSiblingIndex() == 0) ? 0 : connectedObjects.transform.GetSiblingIndex() + 1;
                obj.transform.SetSiblingIndex(index);
                foreach (CurveGuide cg in connectedObjects.transform.parent.GetComponentsInChildren<CurveGuide>())
                {
                    cg.name = "Node_" + cg.transform.GetSiblingIndex();
                }
                Selection.activeGameObject = obj;
            }
        }
    }
}