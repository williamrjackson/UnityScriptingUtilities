using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Wrj
{
    public class CurveGuide : MonoBehaviour
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
                ownerPath.RefeshCurve();
                lastPos = transform.position;
            }
        }

        public void Duplicate(CurveGuide toDupe)
        {
            GameObject dupe = Instantiate(toDupe.gameObject);
            Undo.RegisterCreatedObjectUndo(dupe, "Duplicate Curve Node");
            dupe.transform.parent = toDupe.transform.parent;
            dupe.transform.position = toDupe.transform.position;
            int index = (toDupe.transform.GetSiblingIndex() == 0) ? 0 : toDupe.transform.GetSiblingIndex() + 1;
            dupe.transform.SetSiblingIndex(index);
            Selection.activeGameObject = dupe;
            GetOwnerPath().RefreshChildIndexes();
        }

        public BezierPath GetOwnerPath()
        {
            return transform.parent.GetComponent<BezierPath>();
        }
    }
}