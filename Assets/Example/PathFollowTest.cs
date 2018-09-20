using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollowTest : MonoBehaviour {
    public Wrj.BezierPath path;
	// Use this for initialization
	void Start () {
        Wrj.Utils.MapToCurve.Ease.MoveAlongPath(transform, path, 5, pingPong: 20, align: true);
	}
}
