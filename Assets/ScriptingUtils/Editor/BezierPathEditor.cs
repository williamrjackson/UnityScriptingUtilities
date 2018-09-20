using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wrj
{
    [CustomEditor(typeof(BezierPath))]
    public class BezierPathEditor : Editor
    {
        BezierPath connectedObjects;
        void OnEnable()
        {
            connectedObjects = target as BezierPath;
            if (connectedObjects.gameObject.GetComponentsInChildren<CurveGuide>().Length == 0)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = Vector3.one * .1f;
                go.transform.position = connectedObjects.transform.position;
                go.transform.parent = connectedObjects.transform;
                Wrj.Utils.EnsureComponent<CurveGuide>(go);
                go.name = "Node_0";
                go = Instantiate(go);
                go.transform.position = connectedObjects.transform.position + Vector3.right;
                go.transform.parent = connectedObjects.transform;
                go.name = "Node_1";
                go = Instantiate(go);
                go.transform.position = connectedObjects.transform.position + Vector3.right + Vector3.down;
                go.transform.parent = connectedObjects.transform;
                go.name = "Node_2";
            }
            SceneView.onSceneGUIDelegate = CurveEditor;
        }
        void CurveEditor(SceneView scene)
        {
            if (connectedObjects == null)
                return;

            CurveGuide[] points = connectedObjects.gameObject.GetComponentsInChildren<CurveGuide>();
            Vector3[] curve = connectedObjects.CurvePath(connectedObjects.res);
            if (curve != null)
            {

                Handles.color = Color.green;
                Handles.DrawAAPolyLine(2f, curve);

                Vector3[] pointsPos = new Vector3[points.Length];
                Handles.color = Color.yellow;
                for (int i = 0; i < points.Length; i++)
                {
                    pointsPos[i] = points[i].transform.position;
                    Handles.CubeHandleCap(i, points[i].transform.position, Quaternion.identity, .1f, EventType.Repaint);
                }
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(1f, pointsPos);
            }
        }
    }
}
