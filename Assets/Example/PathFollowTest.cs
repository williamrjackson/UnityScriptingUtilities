using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollowTest : MonoBehaviour {
    public float speed = 5;
    public Wrj.BezierPath[] paths;

    private int pathIndex = 0;
	// Use this for initialization
	void Start () {
        RunPath();
	}
    void RunPath()
    {
        if (pathIndex < paths.Length)
        {
            Wrj.Utils.MapToCurve.Ease.MoveAlongPath(transform, paths[pathIndex], speed * paths[pathIndex].GetCurveLength(), onDone: RunPath);
            pathIndex++;
        }
    }
}
