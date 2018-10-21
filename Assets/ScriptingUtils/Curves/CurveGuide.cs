using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
        public BezierPath GetOwnerPath()
        {
            return transform.parent.GetComponent<BezierPath>();
        }
    }
}