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
        public AudioSource audioA;
        public AudioSource audioB;
        public Transform linTransform;
        public Transform easeOutTransform;
        public Transform easeInTransform;
        public Transform easeTransform;
        public Transform targetTransform;
        public AnimationCurve scaleCurve;
        public Transform curveScaleTransform;
        public GameObject recursiveTestGO;
        public WeightedGameObjects randomBumpObjects;
        void Start()
        {
            if (randomBumpObjects.objectList.Length > 0)
                StartCoroutine(testRandomLoop());

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

            Utils.MapToCurve.Ease.CrossFadeAudio(audioA, audioB, 1, duration);

            Utils.MapToCurve myCurve = new Utils.MapToCurve(scaleCurve);
            myCurve.Scale(curveScaleTransform, curveScaleTransform.localScale * .5f, duration * .5f, pingPong: 9, onDone: FadeOut);

            recursiveTestGO.PerChild(SetLayer);

            if (testStop)
                StartCoroutine(StopTest());

        }

        private void SetLayer(GameObject go)
        {
            go.layer = LayerMask.NameToLayer("TransparentFX");
        }

        private void FadeOut()
        {
            print("OnDone called. Fading out audio");
            Utils.MapToCurve.Linear.FadeAudio(audioB, 0, duration);
        }
        IEnumerator testRandomLoop()
        {
            while (true)
            {
                Transform affected = randomBumpObjects.GetRandom().transform;
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

