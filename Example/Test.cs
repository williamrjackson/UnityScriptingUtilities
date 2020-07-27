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

            Utils.MapToCurve.Linear.MoveWorld(linTransform, linTransform.PosInWorldDir(up: 5f, right: -1.5f), duration, pingPong: 10);
            Utils.MapToCurve.EaseIn.MoveWorld(easeInTransform, easeInTransform.PosInWorldDir(up: 5f, right: .5f), duration, mirrorCurve: false, pingPong: 10);
            Utils.MapToCurve.EaseIn.MoveWorld(easeOutTransform, easeOutTransform.PosInWorldDir(up: 5f, right: -.5f), duration, mirrorCurve: true, pingPong: 10);
            Utils.MapToCurve.Ease.MatchSibling(easeTransform, targetTransform, duration, pingPong: 10);

            Utils.MapToCurve.Linear.Rotate(linTransform, Vector3.up * 135, duration, shortestPath: false, pingPong: 10);
            Utils.MapToCurve.EaseIn.Rotate(easeInTransform, Vector3.up * -360, duration, shortestPath: false, pingPong: 10);
            Utils.MapToCurve.EaseIn.Rotate(easeOutTransform, Vector3.forward * -720, duration, shortestPath: false, mirrorPingPong: 10);

            Utils.MapToCurve.Ease.ChangeColor(linTransform, Color.black, duration, pingPong: 10);
            Utils.MapToCurve.Ease.ChangeColor(easeInTransform, Color.red, duration, pingPong: 10);
            Utils.MapToCurve.Ease.ChangeColor(easeOutTransform, Color.blue, duration, pingPong: 10);
            Utils.MapToCurve.Ease.ChangeColor(easeTransform, Color.magenta, duration, pingPong: 10);

            Utils.MapToCurve.Ease.FadeAlpha(targetTransform, 0, duration, pingPong: 10);

            Utils.MapToCurve myCurve = new Utils.MapToCurve(scaleCurve);
            myCurve.Scale(curveScaleTransform, curveScaleTransform.localScale * .5f, duration * .5f, pingPong: 9);

            StartCoroutine(testRandomLoop());
            recursiveTestGO.PerChild(SetLayer);

            if (testStop)
                StartCoroutine(StopTest());

        }

        private void SetLayer(GameObject go)
        {
            go.layer = LayerMask.NameToLayer("TransparentFX");
        }

        IEnumerator testRandomLoop()
        {
            while (true)
            {
                Transform affected = randomBumpObjects.GetRandom(true);
                float initY = affected.localScale.y;
                Vector3 targetScale = new Vector3(1, initY + .1f, 1);
                Utils.MapToCurve.Ease.Scale(affected, targetScale, .1f);
                Utils.MapToCurve.Ease.Move(affected, affected.LocalPosInDir(up: .05f), .1f);
                Utils.MapToCurve.Ease.ChangeColor(affected, Color.Lerp(Color.white, Color.black, (initY - 1) / 10), .1f);
                yield return new WaitForSecondsRealtime(.25f);
            }
        }
        IEnumerator StopTest()
        {
            yield return new WaitForSecondsRealtime(1);
            Utils.MapToCurve.StopAllOnTransform(curveScaleTransform);
            yield return new WaitForSecondsRealtime(2);
            Utils.MapToCurve.StopAll();
        }
    }
}

