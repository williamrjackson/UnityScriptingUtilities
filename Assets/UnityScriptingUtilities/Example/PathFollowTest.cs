using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollowTest : MonoBehaviour {
    public float speed = 5;
    public Wrj.BezierPath[] paths;
    public TrailRenderer trail;
    private int pathIndex = 0;
	// Use this for initialization
	void Start () {
        StartCoroutine(RunPathDelayed(1f));
	}
    void RunPath()
    {
        if (pathIndex < paths.Length)
        {
            trail.transform.parent = null;
            Vector3 look = Vector3.zero;
            transform.position = paths[pathIndex].GetPointOnCurve(0, ref look);
            trail = Instantiate(trail.gameObject, transform).GetComponent<TrailRenderer>();
            trail.transform.localPosition = Vector3.zero;
            Wrj.Utils.MapToCurve.Ease.MoveAlongPath(transform, paths[pathIndex], speed * paths[pathIndex].GetCurveLength(), onDone: RunPath);
            pathIndex++;
        }
    }

    IEnumerator RunPathDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        RunPath();
    }
}
