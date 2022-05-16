using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wrj
{
    public class PathGuide : MonoBehaviour
    {
        private BezierPath ownerPath;
        private Vector3 lastPos;
        void Awake()
        {
            ownerPath = GetOwnerPath();
            if (ownerPath == null)
            {
                Destroy(this);
            }
        }
        void Update()
        {
            if (transform.position != lastPos)
            {
                ownerPath.RefreshPath();
                lastPos = transform.position;
            }
        }

        public void Duplicate(PathGuide toDupe)
        {
            #if UNITY_EDITOR
            Undo.SetCurrentGroupName("Duplicate Curve Node");
            GameObject dupe = Instantiate(toDupe.gameObject);
            Undo.RegisterCreatedObjectUndo(dupe, "");
            dupe.transform.parent = toDupe.transform.parent;
            dupe.transform.position = toDupe.transform.position;
            int index = (toDupe.transform.GetSiblingIndex() == 0) ? 0 : toDupe.transform.GetSiblingIndex() + 1;
            dupe.transform.SetSiblingIndex(index);
            Undo.RegisterFullObjectHierarchyUndo(dupe.transform.parent.gameObject, "");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            Selection.activeGameObject = dupe;
            GetOwnerPath().RefreshChildIndices();
            #endif
        }

        public BezierPath GetOwnerPath()
        {
            return transform.parent.GetComponent<BezierPath>();
        }
    }
}