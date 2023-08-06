using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Wrj
{
    public class PoolManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Prototype disabled child object to duplicate in the pool")]
        private Component sourceObject = null;
        [Tooltip("If the object is not finished within this amount of time it will auto-disable. Set to 0 to for permanence.")]
        [SerializeField]
        private float defaultLifeSpan = 10f;

        public UnityAction<GameObject> OnObjectStashing;

        private List<GameObject> _GameObjects = new List<GameObject>();
        private Dictionary<GameObject, Coroutine> _RunningTimeouts = new Dictionary<GameObject, Coroutine>();

        private void Start()
        {
            sourceObject.gameObject.SetActive(false);
        }

        public GameObject Next(float lifeSpan)
        {
            if (sourceObject == null)
            {
                Debug.LogWarning($"No source object provided to PoolManager ({name}).", this);
                return null;
            }

            // Get the first available game object from the pool. 
            // This will add a new one if necessary.
            GameObject go = FirstAvailable();

            // Start the auto-disable timer, if needed.
            if (defaultLifeSpan > 0f)
            {
                Coroutine thisTimeout = StartCoroutine(LifeSpanRoutine(go, lifeSpan));
                _RunningTimeouts.Add(go, thisTimeout);
            }

            // Position the object to match the source.
            go.transform.localPosition = sourceObject.transform.localPosition;
            go.transform.localRotation = sourceObject.transform.localRotation;
           
            // Enable in the hierarchy
            go.SetActive(true);

            return go;
        }
        public GameObject Next()
        {
            return Next(defaultLifeSpan);
        }
        public T Next<T>(float lifeSpan)
        {
            GameObject go = Next(lifeSpan);
            T target = go.GetComponent<T>();
            if (target == null) Debug.LogWarning($"Pool element has no {typeof(T).Name}");
            return target;
        }
        public T Next<T>()
        {
            return Next<T>(defaultLifeSpan);
        }
        public dynamic NextComponent(float lifeSpan)
        {
            return Next(lifeSpan).GetComponent(sourceObject.GetType().Name);
        }
        public dynamic NextComponent()
        {
            return Next(defaultLifeSpan).GetComponent(sourceObject.GetType().Name);
        }


        public void Finish(GameObject element)
        {
            // Find the object in the list
            foreach (GameObject go in _GameObjects)
            {
                if (go == element.gameObject)
                {
                    OnObjectStashing(go);
                    // Disable it in the hierarchy
                    go.SetActive(false);
                    // Kill the auto-disable timer
                    if (_RunningTimeouts.ContainsKey(go))
                    {
                        StopCoroutine(_RunningTimeouts[go]);
                        _RunningTimeouts.Remove(go);
                    }
                    return;
                }
            }
        }
        public void Finish(Component element)
        {
            Finish(element.gameObject);
        }

        private GameObject FirstAvailable()
        {
            // Find the first object available
            foreach (GameObject go in _GameObjects)
            {
                if (!go.activeSelf)
                {
                    return go;
                }
            }
            // If none available add a new one
            return InstantiateNewObject();
        }

        private GameObject InstantiateNewObject()
        {
            // Creat object
            GameObject newGO = Instantiate(sourceObject.gameObject);
            // Name it
            newGO.name = sourceObject.name + "(PoolObject)";
            // Child it
            newGO.transform.parent = transform;
            // Add to list
            _GameObjects.Add(newGO);
            return newGO;
        }

        // Auto-disable timer
        private IEnumerator LifeSpanRoutine(GameObject go, float lifeSpan)
        {
            yield return new WaitForSeconds(lifeSpan);
            Finish(go);
        }
    }
}