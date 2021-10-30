using UnityEngine;
using UnityEngine.Events;

namespace Wrj
{
    public class ScreenSizeNotifier : MonoBehaviour
    {
        public delegate void ScreenChangeDelegate(Vector3 dimensions);
        public ScreenChangeDelegate OnScreenChange;

        private Vector2 initialVectorTopRight;
        private Vector2 initialVectorBottomLeft;
        private Vector2 updatedVectorTopRight;
        private Vector2 updatedVectorBottomLeft;
	    private Camera mainCamera;
	    public static Vector3 Dimensions 
        {
            get 
            {
	            if (!Application.isPlaying)
                {
                    return Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height,0));
                }
                else
                {
                    return Instance.mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height,0));
                }
            }
        }

        /// Static Singleton behavior
        protected static ScreenSizeNotifier _instance;
        public static ScreenSizeNotifier Instance
        {
            get
            {
                return _instance;
            }
        }
        void Awake ()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(this);
            }
        }

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
                    OnScreenChange(Dimensions);
                }
            }
        }
    }
}