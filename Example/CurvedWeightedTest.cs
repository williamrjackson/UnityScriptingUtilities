using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurvedWeightedTest : MonoBehaviour
{
    public List<Transform> weightedTransforms;
    public AnimationCurve weightCurve;

    private Wrj.WeightedElements<Transform> weightedElements;
    // Start is called before the first frame update
    void Start()
    {
        weightedElements = new Wrj.WeightedElements<Transform>(weightedTransforms, weightCurve);
        StartCoroutine(testRandomLoop());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator testRandomLoop()
    {
        while (true)
        {
            Transform affected = weightedElements.GetRandom(true);
            float initY = affected.localScale.y;
            Vector3 targetScale = affected.localScale.With(y: initY + .1f);
            Wrj.Utils.MapToCurve.Ease.Scale(affected, targetScale, .1f);
            Wrj.Utils.MapToCurve.Ease.Move(affected, affected.LocalPosInDir(up: .05f), .1f);
            Wrj.Utils.MapToCurve.Ease.ChangeColor(affected, Color.Lerp(Color.white, Color.black, (initY - 1) / 10), .1f);
            yield return new WaitForSecondsRealtime(.1f);
        }
    }
}
