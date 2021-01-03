using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Wrj
{
    [CustomEditor(typeof(BezierPath))]
    [InitializeOnLoad]
    public class BezierPathEditor : Editor
    {
        BezierPath connectedObject;

        static BezierPathEditor()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private static void UndoRedoPerformed()
        {
            foreach ( BezierPath path in FindObjectsOfType<BezierPath>())
            {
                path.RefreshChildIndices();
            }
        }

        void OnEnable()
        {
            connectedObject = target as BezierPath;
            if (connectedObject == null)
                return;

            if (connectedObject.gameObject.GetComponentsInChildren<PathGuide>().Length == 0)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = Vector3.one * .1f;
                go.transform.position = connectedObject.transform.position;
                go.transform.parent = connectedObject.transform;
                Wrj.Utils.EnsureComponent<PathGuide>(go);
                go.name = "Node_0";
                go = Instantiate(go);
                go.transform.position = connectedObject.transform.position + Vector3.right;
                go.transform.parent = connectedObject.transform;
                go.name = "Node_1";
                go = Instantiate(go);
                go.transform.position = connectedObject.transform.position + Vector3.right + Vector3.down;
                go.transform.parent = connectedObject.transform;
                go.name = "Node_2";
            }
            SceneView.duringSceneGui += CurveEditor;
        }
        void CurveEditor(SceneView scene)
        {
            if (connectedObject == null)
                return;

            PathGuide[] points = connectedObject.gameObject.GetComponentsInChildren<PathGuide>();
            Vector3[] curve = connectedObject.CurvePath(connectedObject.res);
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
