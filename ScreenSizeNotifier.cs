using UnityEngine;

namespace Wrj
{
    public class ScreenSizeNotifier : MonoBehaviour
    {
        public delegate void ScreenChangeWorldDelegate(Vector3 worldDimensions);
        public delegate void ScreenChangeDelegate(Vector2 screenDimensions);
        public delegate void ChangeDelegate();
        public ScreenChangeWorldDelegate OnScreenChangeWorld;
        public ScreenChangeDelegate OnScreenChange;
        public ChangeDelegate OnChange;

        private int initialWidth;
        private int initialHeight;
        private int updatedWidth;
        private int updatedHeight;
	    private Camera mainCamera;
	    public static Vector3 ScreenDimensions => new Vector2(Screen.width, Screen.height);
	    public static Vector3 WorldDimensions 
        {
            get 
            {
	            if (!Application.isPlaying)
                {
	                if (Camera.main == null) return Vector3.zero;
	                return Camera.main.ScreenToWorldPoint(ScreenDimensions);
                }
                else
                {
	                if (Instance == null || Instance.mainCamera == null) return Vector3.zero;
	                return Instance.mainCamera.ScreenToWorldPoint(ScreenDimensions);
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
                mainCamera = Camera.main;
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
            updatedHeight = Screen.height;
            updatedWidth = Screen.width;
        }

        void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            updatedHeight = Screen.height;
            updatedWidth = Screen.width;

            if ((initialHeight != updatedHeight) || (initialWidth != updatedWidth))
            {
                initialHeight = updatedHeight;
                initialWidth = updatedWidth;
                Notify();
            }
        }
        
        void Notify()
        {
            if (OnScreenChangeWorld != null)
            {
                OnScreenChangeWorld(WorldDimensions);
            }
            if (OnScreenChange != null)
            {
                OnScreenChange(ScreenDimensions);
            }
            if (OnChange != null)
            {
                OnChange();
            }
        }
    }
}