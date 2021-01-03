using UnityEngine;

namespace Wrj
{
    public class BezierPath : MonoBehaviour
    {
        public Transform slider;
        [Range(0, 1)]
        public float percent = 0f;
        [Range(2, 50)]
        public int res = 15;

        private int m_cachedRes = -1;
        private PathGuide[] m_Points;
        private Vector3[] m_Curve;

        public Vector3[] Curve { get => m_Curve; }

        void Awake()
        {
            RefreshPath();
            // Disable mesh renderers on PathGuides
            foreach (MeshRenderer rend in GetComponentsInChildren<MeshRenderer>())
            {
                if (rend.GetComponent<PathGuide>())
                    rend.enabled = false;
            }
        }

        void Update()
        {
            if (res != m_cachedRes)
            {
                RefreshPath();
            }
            if (slider != null)
            {
                Vector3 look = Vector3.zero;
                slider.position = GetPointOnCurve(m_Curve, percent, ref look);
                slider.LookAt(look);
            }
        }

        // Build up a new curve with latest curveguides
        public void RefreshPath()
        {
            m_cachedRes = res;
            m_Curve = CurvePath(res);
        }
        public Vector3 GetPointOnCurve(float t, ref Vector3 lookAt)
        {
              return GetPointOnCurve(m_Curve, t, ref lookAt);
        }

        public static Vector3 GetPointOnCurve(Vector3[] path, float t, ref Vector3 lookAt)
        {
            if (path.Length < 2)
            {
                return Vector3.zero;
            }

            // Get total length of path
            float posOnLine = GetCurveLength(path) * t;

            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 p0 = path[i];
                Vector3 p1 = path[i + 1];
                float currentDistance = Vector3.Distance(p1, p0);

                // If the remaining distance is greater than the distance between these vectors, subtract the current distance and proceed
                if (currentDistance < posOnLine)
                {
                    posOnLine -= currentDistance;
                    continue;
                }

                lookAt = Vector3.Lerp(p0, p1, posOnLine / currentDistance + .1f);
                return Vector3.Lerp(p0, p1, posOnLine / currentDistance);
            }
            // made it to the end
            lookAt = Vector3.Lerp(path[path.Length - 2], path[path.Length - 1], 1f);
            return path[path.Length - 1];
        }

        // Returns total length of the curve
        public float GetCurveLength()
        {
            return GetCurveLength(m_Curve);
        }

        public static float GetCurveLength(Vector3[] path)
        {
            if (path.Length < 1)
                return 0;

            float length = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                length += Vector3.Distance(path[i + 1], path[i]);
            }
            return length;
        }

        // Cunstruct a curve path.
        public Vector3[] CurvePath(int resolution = 15)
        {
            // Collect PathGuides
            m_Points = GetComponentsInChildren<PathGuide>();
            // Require at least 3, for start, influence, and end.
            if (m_Points.Length < 3)
                return null;

            // Create vector array to hold the results (-2 is to accommodate the first and last)
            Vector3[] finalPoints = new Vector3[(m_Points.Length - 2) * resolution];
            
            // Start at the front of the path
            Vector3 currentPos = m_Points[0].transform.position;
            int finalPointIndex = 0;

            // Connect multiple quadratic curves from the mid-point between each PathGuide node
            for (int i = 1; i < m_Points.Length - 2; i += 1)
            {
                Vector3 p0 = m_Points[i].transform.position;
                Vector3 p1 = m_Points[i + 1].transform.position;
                // Get the midpoint between p0 & p1
                Vector3 mid = Vector3.Lerp(p0, p1, .5f);
                finalPointIndex = (i * resolution) - resolution;
                foreach (Vector3 p in Wrj.Utils.QuadraticBezierCurve(currentPos, p0, mid, resolution))
                {
                    finalPoints[finalPointIndex] = p;
                    finalPointIndex++;
                }
                currentPos = mid;
            }
            finalPointIndex = (m_Points.Length - 2) * resolution - resolution;
            foreach (Vector3 p in Wrj.Utils.QuadraticBezierCurve(currentPos, m_Points[m_Points.Length - 2].transform.position, m_Points[m_Points.Length - 1].transform.position, resolution))
            {
                finalPoints[finalPointIndex] = p;
                finalPointIndex++;
            }
            return finalPoints;
        }

        // Renumber curveguide children
        public void RefreshChildIndices()
        {
            foreach (PathGuide cg in transform.GetComponentsInChildren<PathGuide>())
            {
                cg.name = "Node_" + cg.transform.GetSiblingIndex();
            }
        }
    }
}