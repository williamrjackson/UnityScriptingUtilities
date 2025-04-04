﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Wrj
{
    //bool mirrorCurve = false, int loop = 0, int pingPong = 0, int mirrorPingPong = 0
    public enum RepeatStyle { Loop, PingPong, MirrorPingPong }
    public class Utils : MonoBehaviour
    {
        /// Returns a component of Type T by finding the existing one, or by instantiating one if not found.
        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out T component))
            {
                return component;
            }
            return go.AddComponent<T>();
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
        public static Material InstanceMaterial(GameObject go)
        {
            if (go.TryGetComponent<Renderer>(out var renderer)) 
            {
                renderer.material = new Material(renderer.material);
                return renderer.material;
            }
            if (go.TryGetComponent<UnityEngine.UI.Image>(out var image)) 
            {
                image.material = new Material(image.material);
                return image.material;
            }
            if (go.TryGetComponent<UnityEngine.UI.RawImage>(out var rawImage)) 
            {
                rawImage.material = new Material(rawImage.material);
                return rawImage.material;
            }
            return null;
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
        
        public static float LoopedNoise(float radius, float time, float offset = 0f)
        {
            time = Mathf.Repeat(time, 1f);
            float noiseX = radius * Mathf.Cos(time * 2f * Mathf.PI);
            float noiseY = radius * Mathf.Sin(time * 2f * Mathf.PI);
            return Mathf.PerlinNoise(noiseX + offset, noiseY + offset);
        }
        public static Vector2 LoopedNoiseV2(float radius, float time)
        {
            time = Mathf.Repeat(time, 1f);
            float x = LoopedNoise(radius, time, 0f);
            float y = LoopedNoise(radius, time, radius * 2f);
            return new Vector2(x, y);
        }
        public static Vector3 LoopedNoiseV3(float radius, float time)
        {
            time = Mathf.Repeat(time, 1f);
            float x = LoopedNoise(radius, time, 0f);
            float y = LoopedNoise(radius, time, radius * 2f);
            float z = LoopedNoise(radius, time, radius * 4f);
            return new Vector3(x, y, z);
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
                float t = 1f / pointCount * i;
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
                Debug.Log("Exeption: " + ex.Message);
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
            get { return (Random.value > .5f); }
        }

        public static string SplitCamelCase(string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        /// <summary>
        /// Determine if a command line argument is used by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool GetArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Get a command line argument's value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetArgVal(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
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
        //      map.ScaleTransform(transform, transform.localScale * .25f, 5, onDone: SomeMethodThatReturnsVoid);

        [System.Serializable]
        public class MapToCurve
        {
            [SerializeField]
            private AnimationCurve _curve;
            [SerializeField]
            private ParticleSystem.MinMaxCurve _minMaxCurve;
            [SerializeField]
            private bool _isMinMaxCurveMode = false;
            [SerializeField]
            private float _minMaxLerpFactor = 0f;

            public delegate void OnDone();

            public static MapToCurve Linear = new MapToCurve(LinearCurve);
            public static MapToCurve Ease = new MapToCurve(EaseCurve);
            public static MapToCurve EaseIn = new MapToCurve(EaseInCurve);
            public static MapToCurve EaseOut = new MapToCurve(EaseOutCurve);
            public static MapToCurve MinMax = new MapToCurve(DefaultMinMaxCurve);

            public static ParticleSystem.MinMaxCurve DefaultMinMaxCurve
            {
                get => new ParticleSystem.MinMaxCurve(1f, EaseInCurve, EaseOutCurve);
            }
            public static AnimationCurve LinearCurve
            {
                get => AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
            public static AnimationCurve EaseCurve
            {
                get => AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
            public static AnimationCurve EaseInCurve
            {
                get
                {
                    var easeIn = EaseCurve;
                    Keyframe startKey = LinearCurve.keys[0];
                    easeIn.MoveKey(0, startKey);
                    return easeIn;
                }
            }
            public static AnimationCurve EaseOutCurve
            {
                get
                {
                    var easeOut = EaseCurve;
                    Keyframe endKey = LinearCurve.keys[1];
                    easeOut.MoveKey(1, endKey);
                    return easeOut;
                }
            }

            private float Evaluate(float time)
            {
                if (_isMinMaxCurveMode)
                {
                    EnsureValidCurveMode();
                    return _minMaxCurve.Evaluate(time, _minMaxLerpFactor);
                }
                return _curve.Evaluate(time);
            }
            public MapToCurve()
            {
                _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
            public MapToCurve(AnimationCurve c)
            {
                _curve = c;
            }
            public MapToCurve(ParticleSystem.MinMaxCurve c)
            {
                _minMaxCurve = c;
                _isMinMaxCurveMode = true;
            }
            private void EnsureValidCurveMode()
            {
                _minMaxCurve.curveMultiplier = 1f;
                if (_minMaxCurve.mode != ParticleSystemCurveMode.TwoCurves)
                {
                    _minMaxCurve.mode = ParticleSystemCurveMode.TwoCurves;
                }
                if (_minMaxCurve.curveMin.keys.Length == 0 ||
                    _minMaxCurve.curveMax.keys.Length == 0)
                {
                    _minMaxCurve = DefaultMinMaxCurve;
                }

            }
            private void RefreshMinMaxLerpFactor()
            {
                _minMaxLerpFactor = Random.value;
            }
            private void RefreshMinMaxLerpForIteration(RepeatStyle repeatStyle, int iteration)
            {
                if (!_isMinMaxCurveMode) return;
                if (repeatStyle != RepeatStyle.MirrorPingPong)
                {
                    RefreshMinMaxLerpFactor();
                    return;
                }
                if (repeatStyle == RepeatStyle.MirrorPingPong && iteration % 2 != 0)
                {
                    RefreshMinMaxLerpFactor();
                    return;
                }
            }

            public class Manipulation : CustomYieldInstruction
            {
                public override bool keepWaiting
                {
                    get
                    {
                        return coroutine != null;
                    }
                }
                public Coroutine coroutine;
                public Transform transform;
                public enum ManipulationType { Scale, Move, Rotate, Audio, Color, Alpha, Camera, NotApplicable }
                private ManipulationType type;
                private int _iterationCount = 0;
                public int iterations { get => _iterationCount; }
                public int IncrementIterations(int iterations)
                {
                    _iterationCount++;
                    if (iterations > 0)
                    {
                        iterations--;
                    }
                    else if (iterations < 0)
                    {
                        iterations = -1;
                    }
                    return iterations;
                }
                public bool IsRunning => coroutine != null;

                public Manipulation(ManipulationType _type, Transform _transform)
                {
                    type = _type;
                    transform = _transform;
                    if (wrjInstance == null)
                        return;
                    foreach (Manipulation mcp in wrjInstance.m_ManipulationList)
                    {
                        if (mcp.transform == transform && mcp.type == type && type != ManipulationType.NotApplicable)
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
                return Mathf.LerpUnclamped(a, b, Evaluate(time));
            }
            public Vector3 Lerp(Vector3 a, Vector3 b, float time)
            {
                return Vector3.LerpUnclamped(a, b, Evaluate(time));
            }
            public Quaternion Lerp(Quaternion a, Quaternion b, float time)
            {
                return Quaternion.LerpUnclamped(a, b, Evaluate(time));
            }
            public Color Lerp(Color a, Color b, float time)
            {
                return Color.Lerp(a, b, Evaluate(time));
            }
            public float MirrorLerp(float a, float b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Mathf.LerpUnclamped(a, b, Remap(Evaluate(t), -1, 2, 2, -1));
            }
            public Vector3 MirrorLerp(Vector3 a, Vector3 b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Vector3.LerpUnclamped(a, b, Remap(Evaluate(t), -1, 2, 2, -1));
            }
            public Quaternion MirrorLerp(Quaternion a, Quaternion b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Quaternion.LerpUnclamped(a, b, Remap(Evaluate(t), -1, 2, 2, -1));
            }
            public Color MirrorLerp(Color a, Color b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                return Color.LerpUnclamped(a, b, Remap(Evaluate(t), -1, 2, 2, -1));
            }
            public static Matrix4x4 Lerp(Matrix4x4 a, Matrix4x4 b, float time)
            {
                Matrix4x4 ret = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                    ret[i] = Mathf.LerpUnclamped(a[i], b[i], time);
                return ret;
            }
            public static Matrix4x4 MirrorLerp(Matrix4x4 a, Matrix4x4 b, float time)
            {
                float t = Remap(time, -1, 2, 2, -1);
                Matrix4x4 ret = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                    ret[i] = Remap(Mathf.LerpUnclamped(a[i], b[i], t), -1, 2, 2, -1);
                return ret;
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
            public Manipulation Scale(Transform tform, Vector3 to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Scale, tform);
                mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator ScaleLocal(Manipulation mcp, Transform tform, Vector3 to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);
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

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        tform.localScale = from;
                        mcp.coroutine = UtilObject().StartCoroutine(ScaleLocal(mcp, tform, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation Move(Transform tform, Vector3 to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, bool pendulum = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Move, tform);
                mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, pendulum, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator MoveLocal(Manipulation mcp, Transform tform, Vector3 to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);
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

                if (pendulum && mcp.iterations % 2 == 0)
                {
                    from = to - from;
                }

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        tform.localPosition = from;
                        mcp.coroutine = UtilObject().StartCoroutine(MoveLocal(mcp, tform, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation MoveWorld(Transform tform, Vector3 to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, bool pendulum = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Move, tform);
                mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, pendulum, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator MoveWorldspace(Manipulation mcp, Transform tform, Vector3 to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);
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

                if (pendulum && mcp.iterations % 2 == 0)
                {
                    from = to - from;
                }


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        tform.position = from;
                        mcp.coroutine = UtilObject().StartCoroutine(MoveWorldspace(mcp, tform, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation MoveAlongPath(Transform tform, BezierPath path, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, bool inverse = false, bool align = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Move, tform);
                mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path.Curve, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, inverse, align, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }

            public Manipulation MoveAlongPath(Transform tform, Vector3[] path, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, bool inverse = false, bool align = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Move, tform);
                mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, inverse, align, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator MovePath(Manipulation mcp, Transform tform, Vector3[] path, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, bool inverse, bool align, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);
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

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, !inverse, align, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, !inverse, align, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(MovePath(mcp, tform, path, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, inverse, align, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            public Manipulation Rotate(Transform tform, Vector3 eulerTo, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, bool shortestPath = true, bool pendulum = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Rotate, tform);
                if (shortestPath)
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, tform.rotation, Quaternion.Euler(eulerTo.x, eulerTo.y, eulerTo.z), duration, mirrorCurve, repeatStyle, iterations, useTimeScale, pendulum, onDone));
                }
                else
                {
                    mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, tform.localEulerAngles, eulerTo, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, pendulum, onDone));
                }
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator RotateLocalQuaternionLerp(Manipulation mcp, Transform tform, Quaternion from, Quaternion to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);
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

                if (pendulum && mcp.iterations % 2 == 0)
                {
                    from = Quaternion.Euler(to.eulerAngles - from.eulerAngles);
                }


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, to, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, to, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        tform.localRotation = from;
                        mcp.coroutine = UtilObject().StartCoroutine(RotateLocalQuaternionLerp(mcp, tform, from, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            private IEnumerator RotateLocal(Manipulation mcp, Transform tform, Vector3 from, Vector3 to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, bool pendulum, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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

                if (pendulum && mcp.iterations % 2 == 0)
                {
                    from = to - from;
                }


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, to, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, to, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        tform.localEulerAngles = from;
                        mcp.coroutine = UtilObject().StartCoroutine(RotateLocal(mcp, tform, from, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, pendulum, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            /// <summary>
            /// Modify a referenced float.
            /// Example: ManipulateFloat(receiver: x => floatToChange = x)
            /// </summary>
            public Manipulation ManipulateFloat(System.Action<float> receiver, float init, float target, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.NotApplicable, UtilObject().transform);
                mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, init, target, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator FloatManip(Manipulation mcp, System.Action<float> receiver, float init, float target, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, target, init, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, target, init, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        receiver(init);
                        mcp.coroutine = UtilObject().StartCoroutine(FloatManip(mcp, receiver, init, target, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation CamProjectionMorph(Camera cam, Matrix4x4 targetMatrix, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Camera, cam.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpCameraProjection(mcp, cam, targetMatrix, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator LerpCameraProjection(Manipulation mcp, Camera cam, Matrix4x4 targetMatrix, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                Matrix4x4 initMatrix = cam.projectionMatrix;
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (cam == null)
                    {
                        yield break;
                    }
                    elapsedTime += GetDesiredDelta(useTimeScale);
                    float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
                    if (mirrorCurve)
                    {
                        cam.projectionMatrix = MirrorLerp(initMatrix, targetMatrix, scrubPos);
                    }
                    else
                    {
                        cam.projectionMatrix = Lerp(initMatrix, targetMatrix, scrubPos);
                    }
                }
                cam.projectionMatrix = Lerp(initMatrix, targetMatrix, 1f);

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpCameraProjection(mcp, cam, initMatrix, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpCameraProjection(mcp, cam, initMatrix, duration, !mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        cam.projectionMatrix = initMatrix;
                        mcp.coroutine = UtilObject().StartCoroutine(LerpCameraProjection(mcp, cam, targetMatrix, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            public Manipulation FadeAudio(AudioSource audioSource, float targetVol, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Audio, audioSource.transform);
                mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, targetVol, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator Fade(Manipulation mcp, AudioSource audioSource, float targetVol, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float initVol = audioSource.volume;
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, initVol, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, initVol, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        audioSource.volume = initVol;
                        mcp.coroutine = UtilObject().StartCoroutine(Fade(mcp, audioSource, targetVol, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation CrossFadeAudio(AudioSource from, AudioSource to, float targetVol, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Audio, from.transform);
                mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, from, to, targetVol, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            private IEnumerator CrossFade(Manipulation mcp, AudioSource from, AudioSource to, float targetVol, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                to.volume = 0;
                float initB = from.volume;
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, to, from, targetVol, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, to, from, targetVol, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        from.volume = initB;
                        to.volume = 0;
                        mcp.coroutine = UtilObject().StartCoroutine(CrossFade(mcp, from, to, targetVol, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation FadeAlpha(Transform tform, float to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, string matColorReference = "_Color", OnDone onDone = null)
            {
                if (tform.TryGetComponent<CanvasGroup>(out var canvasGroup))
                {
                    return FadeAlpha(canvasGroup, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
                    // mcp.coroutine = UtilObject().StartCoroutine(LerpCanvasAlpha(mcp, canvasGroup, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                }
                else if (tform.TryGetComponent<UnityEngine.UI.Image>(out var image))
                {
                    return FadeAlpha(image, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
                    // mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                }
                else if (tform.TryGetComponent<UnityEngine.UI.RawImage>(out var rawImage))
                {
                    return FadeAlpha(rawImage, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
                    // mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageAlpha(mcp, rawImage, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                }

                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Alpha, tform);    
                mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, matColorReference, onDone));
                return mcp;
            }
            public Manipulation FadeAlpha(CanvasGroup canvasGroup, float to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Alpha, canvasGroup.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpCanvasAlpha(mcp, canvasGroup, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            public Manipulation FadeAlpha(UnityEngine.UI.Image image, float to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Alpha, image.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            public Manipulation FadeAlpha(UnityEngine.UI.RawImage rawImage, float to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Alpha, rawImage.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageAlpha(mcp, rawImage, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }

            private IEnumerator LerpAlpha(Manipulation mcp, Transform tform, float to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, string matColorReference, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

                Material mat = tform.GetComponent<Renderer>().material;

                float from = mat.GetColor(matColorReference).a;
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
                    Color color = mat.GetColor(matColorReference);
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
                    mat.SetColor(matColorReference, color);
                }
                Color finalColor = mat.GetColor(matColorReference);
                finalColor.a = Lerp(from, to, 1f);
                mat.SetColor(matColorReference, finalColor);


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, matColorReference, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, matColorReference, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        finalColor.a = from;
                        mat.SetColor(matColorReference, finalColor);
                        mcp.coroutine = UtilObject().StartCoroutine(LerpAlpha(mcp, tform, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, matColorReference, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            private IEnumerator LerpImageAlpha(Manipulation mcp, UnityEngine.UI.Image image, float to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        finalColor.a = from;
                        image.color = finalColor;
                        mcp.coroutine = UtilObject().StartCoroutine(LerpImageAlpha(mcp, image, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            private IEnumerator LerpRawImageAlpha(Manipulation mcp, UnityEngine.UI.RawImage image, float to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageAlpha(mcp, image, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageAlpha(mcp, image, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        finalColor.a = from;
                        image.color = finalColor;
                        mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageAlpha(mcp, image, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            private IEnumerator LerpCanvasAlpha(Manipulation mcp, UnityEngine.CanvasGroup canvasGroup, float to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

                Transform tform = canvasGroup.transform;
                float from = canvasGroup.alpha;
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
                        canvasGroup.alpha = MirrorLerp(from, to, scrubPos);
                    }
                    else
                    {
                        canvasGroup.alpha = Lerp(from, to, scrubPos);
                    }
                }
                canvasGroup.alpha = Lerp(from, to, 1f);

                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpCanvasAlpha(mcp, canvasGroup, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpCanvasAlpha(mcp, canvasGroup, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        canvasGroup.alpha = from;
                        mcp.coroutine = UtilObject().StartCoroutine(LerpCanvasAlpha(mcp, canvasGroup, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            public Manipulation ChangeColor(Transform tform, UnityEngine.Color to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, string matColorReference = "_Color", OnDone onDone = null)
            {
                if (tform.TryGetComponent<UnityEngine.UI.Image>(out var image))
                {
                    return ChangeColor(image, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
                }
                if (tform.TryGetComponent<UnityEngine.UI.RawImage>(out var rawImage))
                {
                    return ChangeColor(rawImage, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
                }
                else if (tform.TryGetComponent<Renderer>(out var renderer))
                {
                    return ChangeColor(renderer, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, matColorReference, onDone);
                }
                return null;
            }
            public Manipulation ChangeColor(Renderer renderer, Color to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, string matColorReference = "_Color", OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Color, renderer.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, renderer, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, matColorReference, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            public Manipulation ChangeColor(UnityEngine.UI.Image image, Color to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Color, image.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }
            public Manipulation ChangeColor(UnityEngine.UI.RawImage rawImage, Color to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Color, rawImage.transform);
                mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageColor(mcp, rawImage, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));
                UtilObject().AddToCoroList(mcp);
                return mcp;
            }

            // Being careful not to impact alpha, so this can be used simultaneously with ChangAlpha()
            private IEnumerator LerpColor(Manipulation mcp, Renderer renderer, UnityEngine.Color to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, string matColorReference, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

                Material mat = renderer.material;
                Color from = mat.GetColor(matColorReference);
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (renderer.transform == null)
                    {
                        StopAllOnTransform(renderer.transform);
                        yield break;
                    }
                    Color color = mat.GetColor(matColorReference);
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
                    mat.SetColor(matColorReference, new UnityEngine.Color(color.r, color.g, color.b, mat.GetColor(matColorReference).a));
                }
                UnityEngine.Color finalColor = Lerp(from, to, 1f);
                finalColor.a = mat.GetColor(matColorReference).a;
                mat.SetColor(matColorReference, finalColor);


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, renderer, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, matColorReference, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, renderer, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, matColorReference, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        mat.SetColor(matColorReference, from);
                        mcp.coroutine = UtilObject().StartCoroutine(LerpColor(mcp, renderer, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, matColorReference, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }

            private IEnumerator LerpImageColor(Manipulation mcp, UnityEngine.UI.Image image, Color to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

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


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        image.color = from;
                        mcp.coroutine = UtilObject().StartCoroutine(LerpImageColor(mcp, image, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
            private IEnumerator LerpRawImageColor(Manipulation mcp, UnityEngine.UI.RawImage rawImage, Color to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
            {
                float elapsedTime = 0;
                iterations = mcp.IncrementIterations(iterations);
                RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

                Transform tform = rawImage.transform;
                Color from = rawImage.color;
                while (elapsedTime < duration)
                {
                    yield return new WaitForEndOfFrame();
                    if (tform == null)
                    {
                        StopAllOnTransform(tform);
                        yield break;
                    }
                    Color color = rawImage.color;
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
                    rawImage.color = color;
                }
                Color finalColor = rawImage.color;
                finalColor = Lerp(from, to, 1f);
                rawImage.color = finalColor;


                if (iterations != 0)
                {
                    if (repeatStyle == RepeatStyle.PingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageColor(mcp, rawImage, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.MirrorPingPong)
                    {
                        mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageColor(mcp, rawImage, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                    else if (repeatStyle == RepeatStyle.Loop)
                    {
                        rawImage.color = from;
                        mcp.coroutine = UtilObject().StartCoroutine(LerpRawImageColor(mcp, rawImage, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
                    }
                }
                else
                {
                    CoroutineComplete(mcp, onDone);
                }
            }
	        public Manipulation ChangeFill(Transform tform, float to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
	        {
		        Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Color, tform);
		        if (tform.TryGetComponent<UnityEngine.UI.Image>(out var image))
		        {
			        return ChangeFill(image, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
		        }
		        else
		        {
		        	return null;
		        }
	        }
	        public Manipulation ChangeFill(UnityEngine.UI.Image img, float to, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
	        {
		        Manipulation mcp = new Manipulation(Manipulation.ManipulationType.Color, img.transform);

		        mcp.coroutine = UtilObject().StartCoroutine(LerpImageFill(mcp, img, to, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone));

		        UtilObject().AddToCoroList(mcp);
		        return mcp;
	        }
	        
	        private IEnumerator LerpImageFill(Manipulation mcp, UnityEngine.UI.Image image, float to, float duration, bool mirrorCurve, RepeatStyle repeatStyle, int iterations, bool useTimeScale, OnDone onDone)
	        {
		        float elapsedTime = 0;
		        iterations = mcp.IncrementIterations(iterations);
		        RefreshMinMaxLerpForIteration(repeatStyle, mcp.iterations);

		        Transform tform = image.transform;
		        float from = image.fillAmount;
		        while (elapsedTime < duration)
		        {
			        yield return new WaitForEndOfFrame();
			        if (tform == null)
			        {
				        StopAllOnTransform(tform);
				        yield break;
			        }
			        float fill = image.fillAmount;
			        elapsedTime += GetDesiredDelta(useTimeScale);
			        float scrubPos = Remap(elapsedTime, 0, duration, 0, 1);
			        if (mirrorCurve)
			        {
				        fill = MirrorLerp(from, to, scrubPos);
			        }
			        else
			        {
				        fill = Lerp(from, to, scrubPos);
			        }
			        image.fillAmount = fill;
		        }
		        float finalFill = Lerp(from, to, 1f);
		        image.fillAmount = finalFill;


		        if (iterations != 0)
		        {
			        if (repeatStyle == RepeatStyle.PingPong)
			        {
				        mcp.coroutine = UtilObject().StartCoroutine(LerpImageFill(mcp, image, from, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
			        }
			        else if (repeatStyle == RepeatStyle.MirrorPingPong)
			        {
				        mcp.coroutine = UtilObject().StartCoroutine(LerpImageFill(mcp, image, from, duration, !mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
			        }
			        else if (repeatStyle == RepeatStyle.Loop)
			        {
				        image.fillAmount = from;
				        mcp.coroutine = UtilObject().StartCoroutine(LerpImageFill(mcp, image, to, duration, mirrorCurve, repeatStyle, --iterations, useTimeScale, onDone));
			        }
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
            public Manipulation[] MatchSibling(Transform tform, Transform toTform, float duration, bool mirrorCurve = false, RepeatStyle repeatStyle = RepeatStyle.Loop, int iterations = 0, bool useTimeScale = false, OnDone onDone = null)
            {
                if (tform.parent != toTform.parent)
                {
                    Debug.LogWarning("Attempting to match a target that is not a sibling. No guarantee that scale, position, rotation will match.");
                }
                Manipulation[] mcpList = new Manipulation[3];
                mcpList[0] = Scale(tform, toTform.localScale, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, onDone);
                mcpList[1] = Move(tform, toTform.localPosition, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, false, null);
                mcpList[2] = Rotate(tform, toTform.localEulerAngles, duration, mirrorCurve, repeatStyle, iterations, useTimeScale, true, false, null);
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
