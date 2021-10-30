using UnityEngine;

namespace Wrj
{
    [ExecuteInEditMode]
    public class ScreenBorders : MonoBehaviour
    {
        [SerializeField] private ScreenBorder leftBorder;
        [SerializeField] private ScreenBorder rightBorder;
        [SerializeField] private ScreenBorder ground;
        [SerializeField] private ScreenBorder ceiling;

        void Start()
	    {
		    if (Application.isPlaying)
		    {
			    Wrj.ScreenSizeNotifier.Instance.OnScreenChange += SetBorders;
		    }
        }
#if UNITY_EDITOR
        void Update()
        {
            SetBorders(ScreenSizeNotifier.Dimensions);
        }
#endif
        void SetBorders(Vector3 worldDimensions)
        {
            if (leftBorder.transform != null)
            {
                leftBorder.transform.localScale = transform.localScale.With(y: worldDimensions.y * 2f);
                leftBorder.transform.position = Vector3.zero.With(x: (-worldDimensions.x - (leftBorder.transform.lossyScale.x * .5f)) + leftBorder.offset);
            }

            if (rightBorder.transform != null)
            {
                rightBorder.transform.localScale = transform.localScale.With(y: worldDimensions.y * 2f);
                rightBorder.transform.position = Vector3.zero.With(x: (worldDimensions.x + (rightBorder.transform.lossyScale.x * .5f)) + rightBorder.offset);
            }

            if (ground.transform != null)
            {
                ground.transform.localScale = transform.localScale.With(x: worldDimensions.x * 2f);
                ground.transform.position = Vector3.zero.With(y: (-worldDimensions.y - (ground.transform.lossyScale.y * .5f)) + ground.offset);
            }

            if (ceiling.transform != null)
            {
                ceiling.transform.localScale = transform.localScale.With(x: worldDimensions.x * 2f);
                ceiling.transform.position = Vector3.zero.With(y: (worldDimensions.y + (ceiling.transform.lossyScale.y * .5f)) + ceiling.offset);
            }
        }

        [System.Serializable]
        protected class ScreenBorder 
        {
            public Transform transform;
            public float offset;
        }
    }
}
