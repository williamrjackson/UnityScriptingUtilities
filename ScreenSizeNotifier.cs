using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScreenSizeNotifier : MonoBehaviour
{
    public UnityAction OnScreenChange;
    private Vector2 initialVectorTopRight;
    private Vector2 initialVectorBottomLeft;
    private Vector2 updatedVectorTopRight;
    private Vector2 updatedVectorBottomLeft;
    private Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        updatedVectorBottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 30));
        updatedVectorTopRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 30));
    }

    // Update is called once per frame
    void Update()
    {
        updatedVectorBottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 30));
        updatedVectorTopRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 30));

        if ((initialVectorBottomLeft != updatedVectorBottomLeft) || (initialVectorTopRight != updatedVectorTopRight))
        {
            initialVectorBottomLeft = updatedVectorBottomLeft;
            initialVectorTopRight = updatedVectorTopRight;
            if (OnScreenChange != null)
            {
                OnScreenChange();
            }
        }
    }
}
