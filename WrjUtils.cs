using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
    public class Utils : MonoBehaviour
    {
        /// Returns a component of Type T by finding the existing one, or by instantiating one if not found.
        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            if (!go.GetComponent<T>())
            {
                go.AddComponent<T>();
            }
            return go.GetComponent<T>();
        }

        /// Swap items
        public static void Switcheroo<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        /// Supress a compiler warning about unused variables
        public static void SupressUnusedVarWarning<T>(T sink)
        {
            // Do nothing, but trick the compiler into thinking otherwise.
        }
        
        /// Call a function for a game object and all of its children.
        public delegate void GameObjectAffector(GameObject gObject);
        public static void AffectGORecursively(GameObject go, GameObjectAffector goa, bool skipParent = false)
        {
            if (go == null)
            {
                return;
            }

            if (!skipParent)
                goa(go);

            foreach (Transform t in go.transform)
            {
                AffectGORecursively(t.gameObject, goa, false);
            }
        }
	    
        /// Ensure an angle in degrees is within 0 - 360.
        public static float GetPositiveAngle(float angle)
        {
            while (angle <= 0)
            {
                angle += 360;
            }
            return angle % 360;
        }
        /// Ensure an angle in degrees is within 0 - 360.
        public static Vector3 GetPositiveAngle(Vector3 angles)
        {
            return new Vector3(GetPositiveAngle(angles.x), GetPositiveAngle(angles.y), GetPositiveAngle(angles.z));
        }

        /// Linear remap; Simplified interface, rather than solving the Lerp/InverseLerp puzzle every time.
        public static float Remap(float value, float sourceMin, float sourceMax, float destMin, float destMax)
        {
            return Mathf.Lerp(destMin, destMax, Mathf.InverseLerp(sourceMin, sourceMax, value));
        }

        /// Get an array of points representing a quadratic bezier curve.
        /// https://upload.wikimedia.org/wikipedia/commons/3/3d/B%C3%A9zier_2_big.gif
        public static Vector3[] QuadraticBezierCurve(Vector3 origin, Vector3 influence, Vector3 destination, int pointCount, bool throughInfluence = false)
        {
            Vector3[] result = new Vector3[pointCount];
            if (pointCount == 0)
                return result;

            if (throughInfluence)
            {
                influence = influence * 2 - (origin + destination) / 2;
            }

            result[0] = origin;
            for (int i = 1; i < pointCount - 1; i++)
            {
                float t = (1f / pointCount) * i;
                Vector3 point1 = Vector3.Lerp(origin, influence, t);
                Vector3 point2 = Vector3.Lerp(influence, destination, t);
                result[i] = Vector3.Lerp(point1, point2, t);
            }
            result[pointCount - 1] = destination;

            return result;
        }

        /// Get an array of points representing a cubic bezier curve.
        /// https://upload.wikimedia.org/wikipedia/commons/d/db/Bézier_3_big.gif
        public static Vector3[] CubicBezierCurve(Vector3 origin, Vector3 influenceA, Vector3 influenceB, Vector3 destination, int pointCount)
        {
            Vector3[] result = new Vector3[pointCount];
            if (pointCount == 0)
                return result;

            result[0] = origin;
            for (int i = 1; i < pointCount - 1; i++)
            {
                float t = (1f / pointCount) * i;
                Vector3 point1 = Vector3.Lerp(origin, influenceA, t);
                Vector3 point2 = Vector3.Lerp(influenceA, influenceB, t);
                Vector3 point3 = Vector3.Lerp(influenceB, destination, t);

                Vector3 point4 = Vector3.Lerp(point1, point2, t);
                Vector3 point5 = Vector3.Lerp(point2, point3, t);

                result[i] = Vector3.Lerp(point4, point5, t);
            }
            result[pointCount - 1] = destination;

            return result;
        }

        /// Run a method after a delay.
        ///
        /// Usage:
        /// DeferredExecution(3f, () => Debug.Log("This is a test"));
        public static void DeferredExecution(float delay, System.Action methodWithParameters)
        {
            MapToCurve.Linear.Delay(delay, methodWithParameters);
        }

        public static void SafeTry(System.Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.Log("Exeption: " + ex.Message);
            }
        }

        /// Get feet in Unity units/meters
        public static float FromFeet(float feet)
        {
            return feet * 0.3048f;
        }
        /// Get Unity units/meters in feet
        public static float ToFeet(float meters)
        {
            return meters * 3.2808399f;
        }

        /// Get inches in Unity units/meters
        public static float FromInches(float inches)
        {
            return inches * 0.0254f;
        }
        /// Get Unity units/meters in inches
        public static float ToInches(float meters)
        {
            return meters * 39.3700787f;
        }

        public static float Invert(float f)
        {
            if (f > 0f)
            {
                return f * -1f;
            }
            return Mathf.Abs(f);
        }
		
	    /// <summary>
	    ///  Get difference between two values
	    /// </summary>
	    public static float Difference(float val1, float val2)
	    {
	    	return Mathf.Abs(val1 - val2);	
	    }
	    public static int Difference(int val1, int val2)
	    {
	    	return Mathf.Abs(val1 - val2);	
	    }
	    
        public static bool CoinFlip
        {
            get { return (Random.Range(-1f, 1f) > 0) ? true : false; }
        }

        // Coroutine list management stuff...
        public static Utils wrjInstance;
        private List<MapToCurve.Manipulation> m_ManipulationList;
        private void InitializeCoroList()
        {
            m_ManipulationList = new List<MapToCurve.Manipulation>();
        }
        private void AddToCoroList(MapToCurve.Manipulation mcp)
        {
            m_ManipulationList.Add(mcp);
        }
        private void CleanCoroList()
        {
            m_ManipulationList.RemoveAll(x => x.coroutine == null);
        }
        private void CancelAll()
        {
            foreach (MapToCurve.Manipulation mcp in m_ManipulationList)
            {
                StopCoroutine(mcp.coroutine);
                mcp.coroutine = null;
            }
            CleanCoroList();
        }
        private void CancelByTransform(Transform t)
        {
            foreach (MapToCurve.Manipulation mcp in m_ManipulationList)
            {
                if (mcp.transform == t && mcp.coroutine != null)
                {
                    StopCoroutine(mcp.coroutine);
                    mcp.coroutine = null;
                }
            }
            CleanCoroList();
        }

        // Get a value as plotted on an Animation Curve
        // Used like Mathf.Lerp, except not linear
        // Handy functions to manipulate transforms/audio over time.
        // Example:
        //      Wrj.Utils.MapToCurve map = new WrjUtils.MapToCurve();
        //      map.ScaleTransform(transform, transform.localScale * .25f, 5, onDone: SomeMethodThatReturnsVoid, pingPong: 3);

        [System.Serializable]
        public class MapToCurve
        {
            public AnimationCurve curve;
            public delegate void OnDone();

            public static MapToCurve Linear = new MapToCurve(AnimationCurve.Linear(0, 0, 1, 1));
            public static MapToCurve Ease = new MapToCurve(AnimationCurve.EaseInOut(0, 0, 1, 1));
            public static MapToCurve EaseIn = new MapToCurve(AnimationCurve.EaseInOut(0, 0, 2, 2));
            public static MapToCurve EaseOut = new MapToCurve(AnimationCurve.EaseInOut(-1, -1, 1, 1));

            public class Manipulation
            {
                public Coroutine coroutine;
                public Transform transform;
                public int iterationCount = 0;
                private string type;

                public Manipulation(string _type, Transform _transform)
                {
                    type = _type;
                    transform = _transform;
                    if (wrjInstance == null)
                        return;
                    foreach(Manipulation mcp in wrjInstance.m_ManipulationList)
                    {
                        if (mcp.transform == transform && mcp.type == type && type != "")
                        {
                            mcp.Stop();
                        }
                    }
                }
                public void Stop()
                {
                    if (coroutine != null)
                    {
                        wrjInstance.StopCoroutine(coroutine);
                        coroutine = null;
                    }
                }
            }
            public MapToCurve()
            {
                curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            public MapToCurve(AnimationCurve c)
            {
                curve = c;
            }

            public static void StopAll()
            {
                if (wrjInstance != null)
                    wrjInstance.CancelAll();
            }
            public static void StopAllOnTransform(Transform tform)
            {
                if (wrjInstance != null)
                    wrjInstance.CancelByTransform(tform);
            }

            public float CurvedRemap(float value, float sourceMin, float sourceMax, float destMin, float destMax)
            {
                return Lerp(destMin, destMax, Mathf.InverseLerp(sourceMin, sourceMax, value));
            }
            public float Lerp(float a, float b, float time)
            {
                return Mathf.LerpUnclamped(a, b, curve.Evaluate(time));
            }
            public Vector3 Lerp(Vector3 a, Vector3 b, float time)
            {
                return Vector3.LerpUnclamped(a, b, curve.Evaluate(time));
            }
            public Quaternion Lerp(Quaternion a, Quaternion b, float time)
            {
                return Quaternion.LerpUnclamped(a, b, curve.Evaluate(time));
            }
            public Color Lerp(Color a, Color b, float time)
            {
                return Color.Lerp(a, b, curve.Evaluate(time));
            }
            public float MirrorLerp(float a, float b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Mathf.LerpUnclamped(a, b, Remap(curve.Evaluate(t), -1, 2, 2, -1));
            }
            public Vector3 MirrorLerp(Vector3 a, Vector3 b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Vector3.LerpUnclamped(a, b, Remap(curve.Evaluate(t), -1, 2, 2, -1));
            }
            public Quaternion MirrorLerp(Quaternion a, Quaternion b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Quaternion.LerpUnclamped(a, b, Remap(curve.Evaluate(t), -1, 2, 2, -1));
            }
            public Color MirrorLerp(Color a, Color b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Color.LerpUnclamped(a, b, Remap(curve.Evaluate(t), -1, 2, 2, -1));
            }
            public static float Lerp(AnimationCurve c, float a, float b, float time)
            {
                return Mathf.LerpUnclamped(a, b, c.Evaluate(time));
            }
            public static float MirrorLerp(AnimationCurve c, float a, float b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Remap(Mathf.LerpUnclamped(a, b, c.Evaluate(t)), -1, 2, 2, -1);
            }

            // Period-based Manipulation Coroutines...
            public Manipulation Scale(Transform tform, Vector3 to, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Scale", tform);
                mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator ScaleLocal(Manipulation mcp, Transform tform, Vector3 to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Vector3 from = tform.localScale;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        tform.localScale = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        tform.localScale = Lerp(from, to, scrubPos);
                    }
                }
                tform.localScale = to;
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    tform.localScale = from;
                    mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation Move(Transform tform, Vector3 to, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, bool pendulum = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Move", tform);
                mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, pendulum, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator MoveLocal(Manipulation mcp, Transform tform, Vector3 to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Vector3 from = tform.localPosition;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        tform.localPosition = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        tform.localPosition = Lerp(from, to, scrubPos);
                    }
                }
                tform.localPosition = Lerp(from, to, 1f);

                if (pendulum && mcp.iterationCount % 2 == 0)
                {
                    from = to - from;
                }
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, pendulum, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, pendulum, onDone));
                }
                else if (loop != 0)
                {
                    tform.localPosition = from;
                    mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, pendulum, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation MoveWorld(Transform tform, Vector3 to, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, bool pendulum = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Move", tform);
                mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, pendulum, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator MoveWorldspace(Manipulation mcp, Transform tform, Vector3 to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Vector3 from = tform.position;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        tform.position = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        tform.position = Lerp(from, to, scrubPos);
                    }
                }
                tform.position = Lerp(from, to, 1f);

                if (pendulum && mcp.iterationCount % 2 == 0)
                {
                    from = to - from;
                }

                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, pendulum, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, pendulum, onDone));
                }
                else if (loop != 0)
                {
                    tform.position = from;
                    mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, pendulum, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation MoveAlongPath(Transform tform, BezierPath path, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, bool inverse = false, bool align = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Move", tform);
                mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path.Curve, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, inverse, align, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }

            public Manipulation MoveAlongPath(Transform tform, Vector3[] path, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, bool inverse = false, bool align = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Move", tform);
                mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, inverse, align, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator MovePath(Manipulation mcp, Transform tform, Vector3[] path, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, bool inverse, bool align, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = inverse ? Remap(elapsedTime, 0, duration, 1, 0) : Remap(elapsedTime, 0, duration, 0, 1);
                    Vector3 look = Vector3.zero;
                    if (mirrorCurve)
                    {
                        tform.position = BezierPath.GetPointOnCurve(path, MirrorLerp(0, 1, scrubPos), ref look);
                    }
                    else
                    {
                        tform.position = BezierPath.GetPointOnCurve(path, Lerp(0, 1, scrubPos), ref look);
                    }
                    if (align && look != tform.position)
                        tform.rotation = Quaternion.LookRotation(tform.position - look);
                }
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, !inverse, align, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, !inverse, align, onDone));
                }
                else if (loop != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, mirrorCurve, --loop, 0, 0, useTimeScale, inverse, align, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            public Manipulation Rotate(Transform tform, Vector3 eulerTo, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, bool shortestPath = true, bool pendulum = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Rotate", tform);
                if (shortestPath)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, tform.rotation, Quaternion.Euler(eulerTo.x, eulerTo.y, eulerTo.z), duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, pendulum, onDone));
                }
                else
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, tform.localEulerAngles, eulerTo, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, pendulum, onDone));
                }
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator RotateLocalQuaternionLerp(Manipulation mcp, Transform tform, Quaternion from, Quaternion to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        tform.localRotation = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        tform.localRotation = Lerp(from, to, scrubPos);
                    }
                }
                tform.localRotation = Lerp(from, to, 1f);

                if (pendulum && mcp.iterationCount % 2 == 0)
                {
                    from = Quaternion.Euler(to.eulerAngles - from.eulerAngles);
                }
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, to, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, pendulum, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, to, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, pendulum, onDone));
                }
                else if (loop != 0)
                {
                    tform.localRotation = from;
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, from, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, pendulum, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            private IEnumerator RotateLocal(Manipulation mcp, Transform tform, Vector3 from, Vector3 to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        tform.localEulerAngles = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        tform.localEulerAngles = Lerp(from, to, scrubPos);
                    }
                }
                tform.localEulerAngles = Lerp(from, to, 1f);

                if (pendulum && mcp.iterationCount % 2 == 0)
                {
                    from = to - from;
                }

                if (pingPong != 0) 
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, to, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, pendulum, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, to, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, pendulum, onDone));
                }
                else if (loop != 0)
                {
                    tform.localEulerAngles = from;
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, from, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, pendulum, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            public delegate void FloatReceiver(float f);
            public Manipulation ManipulateFloat(FloatReceiver receiver, float init, float target, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Float", UtilObject().transform);
                mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, init, target, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator FloatManip(Manipulation mcp, FloatReceiver receiver, float init, float target, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();

                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        receiver(MirrorLerp(init, target, scrubPos));
                    }
                    else
                    {
                        receiver(Lerp(init, target, scrubPos));
                    }
                }
                receiver(Lerp(init, target, 1f));
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, target, init, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, target, init, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    receiver(init);
                    mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, init, target, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation FadeAudio(AudioSource audioSource, float targetVol, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Fade", audioSource.transform);
                mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, targetVol, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator Fade(Manipulation mcp, AudioSource audioSource, float targetVol, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float initVol = audioSource.volume;
                float elapsedTime = 0;
                mcp.iterationCount++;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (audioSource == null)
                    {
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        audioSource.volume = MirrorLerp(initVol, targetVol, scrubPos);
                    }
                    else
                    {
                        audioSource.volume = Lerp(initVol, targetVol, scrubPos);
                    }
                }
                audioSource.volume = Lerp(initVol, targetVol, 1f);
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, initVol, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, initVol, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    audioSource.volume = initVol;
                    mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, targetVol, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation CrossFadeAudio(AudioSource from, AudioSource to, float targetVol, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Fade", from.transform);
                mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, from, to, targetVol, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator CrossFade(Manipulation mcp, AudioSource from, AudioSource to, float targetVol, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                to.volume = 0;
                float initB = from.volume;
                float elapsedTime = 0;
                mcp.iterationCount++;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (to == null || from == null)
                    {
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        to.volume = MirrorLerp(0, targetVol, scrubPos);
                        from.volume = MirrorLerp(initB, 0, scrubPos);
                    }
                    else
                    {
                        to.volume = Lerp(0, targetVol, scrubPos);
                        from.volume = Lerp(initB, 0, scrubPos);
                    }
                }
                to.volume = targetVol;
                from.volume = 0;
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, to, from, targetVol, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, to, from, targetVol, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    from.volume = initB;
                    to.volume = 0;
                    mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, from, to, targetVol, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation FadeAlpha(Transform tform, float to, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Alpha", tform);
                if (tform.GetComponent<UnityEngine.UI.Image>())
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, tform.GetComponent<UnityEngine.UI.Image>(), to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                }
                else
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                }
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }

            private IEnumerator LerpAlpha(Manipulation mcp, Transform tform, float to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Material mat = tform.GetComponent<Renderer>().material;

                float from = mat.GetColor("_Color").a;
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.renderQueue = 3000;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    Color color = mat.GetColor("_Color");
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        color.a = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        color.a = Lerp(from, to, scrubPos);
                    }
                    mat.SetColor("_Color", color);
                }
                Color finalColor = mat.GetColor("_Color");
                finalColor.a = Lerp(from, to, 1f);
                mat.SetColor("_Color", finalColor);
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    finalColor.a = from;
                    mat.SetColor("_Color", finalColor);
                    mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            private IEnumerator LerpImageAlpha(Manipulation mcp, UnityEngine.UI.Image image, float to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Transform tform = image.transform;
                float from = image.color.a;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    Color color = image.color;
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        color.a = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        color.a = Lerp(from, to, scrubPos);
                    }
                    image.color = color;
                }
                Color finalColor = image.color;
                finalColor.a = Lerp(from, to, 1f);
                image.color = finalColor;
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    finalColor.a = from;
                    image.color = finalColor;
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation ChangeColor(Transform tform, Color to, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation("Color", tform);
                if (tform.GetComponent<UnityEngine.UI.Image>())
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, tform.GetComponent<UnityEngine.UI.Image>(), to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                }
                else
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, tform, to, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone));
                }
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }

            // Being careful not to impact alpha, so this can be used simultaneously with ChangAlpha()
            private IEnumerator LerpColor(Manipulation mcp, Transform tform, Color to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Material mat = tform.GetComponent<Renderer>().material;
                Color from = mat.GetColor("_Color");
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    Color color = mat.GetColor("_Color");
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        color = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        color = Lerp(from, to, scrubPos);
                    }
                    mat.SetColor("_Color", new Color(color.r, color.g, color.b, mat.GetColor("_Color").a));
                }
                Color finalColor = Lerp(from, to, 1f);
                finalColor.a = mat.GetColor("_Color").a;
                mat.SetColor("_Color", finalColor);
                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, tform, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, tform, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    mat.SetColor("_Color", from);
                    mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, tform, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            private IEnumerator LerpImageColor(Manipulation mcp, UnityEngine.UI.Image image, Color to, float duration, bool mirrorCurve, int loop, int pingPong, int mirrorPingPong, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                mcp.iterationCount++;
                Transform tform = image.transform;
                Color from = image.color;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    Color color = image.color;
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        color = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        color = Lerp(from, to, scrubPos);
                    }
                    image.color = color;
                }
                Color finalColor = image.color;
                finalColor = Lerp(from, to, 1f);
                image.color = finalColor;

                if (pingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, from, duration, mirrorCurve, 0, --pingPong, 0, useTimeScale, onDone));
                }
                else if (mirrorPingPong != 0)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, from, duration, !mirrorCurve, 0, 0, --mirrorPingPong, useTimeScale, onDone));
                }
                else if (loop != 0)
                {
                    image.color = from;
                    mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, to, duration, mirrorCurve, --loop, 0, 0, useTimeScale, onDone));
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            private float GetDesiredDelta(bool useTimeScale)
            {
                return useTimeScale ? Time.deltaTime : Time.unscaledDeltaTime;
            }

            // Matches the position scale and rotation of a sibling transform.
            public Manipulation[] MatchSibling(Transform tform, Transform toTform, float duration, bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                if (tform.parent != toTform.parent)
                {
                    Debug.LogWarning("Attempting to match a target that is not a sibling. No guarantee that scale, position, rotation will match.");
                }
                Manipulation[] mcpList = new Manipulation[3];
                mcpList[0] = Scale(tform, toTform.localScale, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, onDone);
                mcpList[1] = Move(tform, toTform.localPosition, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, false, null);
                mcpList[2] = Rotate(tform, toTform.localEulerAngles, duration, mirrorCurve, loop, pingPong, mirrorPingPong, useTimeScale, true, false, null);
                return mcpList;
            }

            // Called when a coroutine is completed. Executes the OnDone method if necessary, and resets default values.
            private void CoroutineComplete(Manipulation mcp, OnDone OnDoneMethod)
            {
                mcp.coroutine = null;
                wrjInstance.CleanCoroList();
                if (OnDoneMethod != null)
                {
                    OnDoneMethod();
                    OnDoneMethod = null;
                }
                OnDoneMethod = null;
            }

            public void Delay(float delay, System.Action methodWithParameters)
            {
                UtilObject().StartCoroutine(DelayCoro(delay, methodWithParameters));
            }
            private IEnumerator DelayCoro(float delay, System.Action methodWithParameters)
            {
                yield return new WaitForSecondsRealtime(delay);
                methodWithParameters();
            }

            // Make sure there's a game object with a WrjUtils component, to run coroutines.
            private Utils UtilObject()
            {
                if (wrjInstance == null)
                {
                    GameObject go = new GameObject("_WrjUtilObject");
                    DontDestroyOnLoad(go);
                    wrjInstance = go.AddComponent<Utils>();
                    wrjInstance.InitializeCoroList();
                }
                return wrjInstance;
            }
        }
    }
}
