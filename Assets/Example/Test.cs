﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Test : MonoBehaviour {
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
    public WrjUtils.WeightedGameObjects randomBumpObjects;
    void Start ()
    {
            if (randomBumpObjects.objectList.Length > 0)
                StartCoroutine(testRandomLoop());

            WrjUtils.MapToCurve.Linear.Move(linTransform, linTransform.localPosition + Vector3.up * 5 + Vector3.right * -1.5f, duration, pingPong: 10);
            WrjUtils.MapToCurve.EaseIn.Move(easeInTransform, easeInTransform.localPosition + Vector3.up * 5 + Vector3.right * .5f, duration, mirrorCurve: false, pingPong: 10);
            WrjUtils.MapToCurve.EaseIn.Move(easeOutTransform, easeOutTransform.localPosition + Vector3.up * 5 + Vector3.right * -.5f, duration, mirrorCurve: true, pingPong: 10);

            WrjUtils.MapToCurve.Ease.MatchSibling(easeTransform, targetTransform, duration, pingPong: 10);

            WrjUtils.MapToCurve.Linear.Rotate(linTransform, Vector3.up * 135, duration, shortestPath: false, pingPong: 10);
            WrjUtils.MapToCurve.EaseIn.Rotate(easeInTransform, Vector3.up * -360, duration, shortestPath: false, pingPong: 10);
            WrjUtils.MapToCurve.EaseIn.Rotate(easeOutTransform, Vector3.forward * -720, duration, shortestPath: false, mirrorPingPong: 10);

            WrjUtils.MapToCurve.Ease.ChangeColor(linTransform, Color.black, duration, pingPong: 10);
            WrjUtils.MapToCurve.Ease.ChangeColor(easeInTransform, Color.red, duration, pingPong: 10);
            WrjUtils.MapToCurve.Ease.ChangeColor(easeOutTransform, Color.blue, duration, pingPong: 10);
            WrjUtils.MapToCurve.Ease.ChangeColor(easeTransform, Color.magenta, duration, pingPong: 10);

            WrjUtils.MapToCurve.Ease.FadeAlpha(targetTransform, 0, duration, pingPong: 10);

            WrjUtils.MapToCurve.Ease.CrossFadeAudio(audioA, audioB, 1, duration);

            WrjUtils.MapToCurve myCurve = new WrjUtils.MapToCurve(scaleCurve);
            myCurve.Scale(curveScaleTransform, curveScaleTransform.localScale * .5f, duration * .5f, pingPong: 9, onDone: FadeOut);

            if (testStop)
                StartCoroutine(StopTest());

    }

    private void FadeOut()
    {
        print("OnDone called. Fading out audio");
        WrjUtils.MapToCurve.Linear.FadeAudio(audioB, 0, duration);
    }
    IEnumerator testRandomLoop()
    {
        while (true)
        {
            Transform affected = randomBumpObjects.GetRandom().transform;
            float initY = affected.localScale.y;
            Vector3 targetScale = new Vector3(1, initY + .1f, 1);
            WrjUtils.MapToCurve.Ease.Scale(affected, targetScale, .1f);
            WrjUtils.MapToCurve.Ease.Move(affected, affected.localPosition + affected.up * .05f, .1f);
            WrjUtils.MapToCurve.Ease.ChangeColor(affected, Color.Lerp(Color.white, Color.black, (initY - 1) / 10), .1f);
            yield return new WaitForSecondsRealtime(.2f);
        }
    }
    IEnumerator StopTest()
    {
        yield return new WaitForSecondsRealtime(1);
        WrjUtils.MapToCurve.StopAllOnTransform(curveScaleTransform);
        yield return new WaitForSecondsRealtime(2);
        WrjUtils.MapToCurve.StopAll();
    }
}

