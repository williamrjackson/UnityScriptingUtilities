using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{

    public class Test : MonoBehaviour
    {
        public float duration = 3;
        public bool testStop;
        public Transform linTransform;
        public Transform easeOutTransform;
        public Transform easeInTransform;
        public Transform easeTransform;
        public Transform targetTransform;
        public AnimationCurve scaleCurve;
        public Transform curveScaleTransform;
        public GameObject recursiveTestGO;
        public Transform weight10;
        public Transform weight5;
        public Transform weight3;
        public Transform weight1;
        private WeightedElements<Transform> randomBumpObjects = new WeightedElements<Transform>();

        void Start()
        {
            randomBumpObjects.Add(weight10, 10);
            randomBumpObjects.Add(weight5, 5);
            randomBumpObjects.Add(weight3, 3);
            randomBumpObjects.Add(weight1, 1);

            Utils.MapToCurve.Linear.MoveWorld(linTransform, linTransform.PosInWorldDir(up: 5f, right: -1.5f), duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.EaseIn.MoveWorld(easeInTransform, easeInTransform.PosInWorldDir(up: 5f, right: .5f), duration, mirrorCurve: false, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.EaseIn.MoveWorld(easeOutTransform, easeOutTransform.PosInWorldDir(up: 5f, right: -.5f), duration, mirrorCurve: true, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.Ease.MatchSibling(easeTransform, targetTransform, duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);

            Utils.MapToCurve.Linear.Rotate(linTransform, Vector3.up * 135, duration, shortestPath: false, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.EaseIn.Rotate(easeInTransform, Vector3.up * -360, duration, shortestPath: false, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.EaseIn.Rotate(easeOutTransform, Vector3.forward * -720, duration, shortestPath: false, repeatStyle: RepeatStyle.MirrorPong, iterations: Utils.Infinity);

            Utils.MapToCurve.Ease.ChangeColor(linTransform, Color.black, duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.Ease.ChangeColor(easeInTransform, Color.red, duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.Ease.ChangeColor(easeOutTransform, Color.blue, duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);
            Utils.MapToCurve.Ease.ChangeColor(easeTransform, Color.magenta, duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);

            Utils.MapToCurve.Ease.FadeAlpha(targetTransform, 0, duration, repeatStyle: RepeatStyle.PingPong, iterations: Utils.Infinity);

            Utils.MapToCurve myCurve = new Utils.MapToCurve(scaleCurve);
            myCurve.Scale(curveScaleTransform, curveScaleTransform.localScale * .5f, duration * .5f, repeatStyle: RepeatStyle.PingPong, iterations: 10);

            recursiveTestGO.PerChild(SetLayer);
            
            TestRandomLoop();

            if (testStop)
                StartCoroutine(StopTest());
        }
        private void TestRandomLoop()
        {
            float dur = 1f;
            Transform affected = randomBumpObjects.GetRandom();
            float initY = affected.localScale.y;
            Vector3 targetScale = new Vector3(1, initY + .1f, 1);
            Utils.MapToCurve.Ease.Scale(affected, targetScale, dur);
            Utils.MapToCurve.Ease.Scale(affected, targetScale, dur);
            Utils.MapToCurve.Ease.Move(affected, affected.LocalPosInDir(up: .05f), dur);
            Utils.MapToCurve.Ease.ChangeColor(affected, Color.Lerp(Color.white, Color.black, (initY - 1) / 10), dur, onDone: () => TestRandomLoop());
        }

        private void SetLayer(GameObject go)
        {
            go.layer = LayerMask.NameToLayer("TransparentFX");
        }

        IEnumerator StopTest()
        {
            CustomLog.Log("Testing custom log...", Color.magenta);
            yield return new WaitForSecondsRealtime(1);
            Utils.MapToCurve.StopAllOnTransform(curveScaleTransform);
            yield return new WaitForSecondsRealtime(2);
            Utils.MapToCurve.StopAll();
        }
    }
}

