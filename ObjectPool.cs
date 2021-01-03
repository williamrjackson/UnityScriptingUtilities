using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Prototype disabled child object to duplicate in the pool")]
        private GameObject sourceObject = null;
        [Tooltip("If the object is not finished within this amount of time it will auto-disable. Set to 0 to disable.")]
        [SerializeField]
        private float maxLifespan = 10f;
        private List<GameObject> m_GameObjects = new List<GameObject>();
        private Dictionary<GameObject, Coroutine> m_RunningTimeouts = new Dictionary<GameObject, Coroutine>();

        public GameObject GetObject()
        {
            if (sourceObject == null)
            {
                Debug.LogWarning("No source provided to ObjectPool.");
                return null;
            }

            // Get the first available game object from the pool. 
            // This will add a new one if necessary.
            GameObject go = FirstAvailable();

            // Start the auto-disable timer, if needed.
            if (maxLifespan > 0f)
            {
                Coroutine thisTimeout = StartCoroutine(LifeSpanRoutine(go));
                m_RunningTimeouts.Add(go, thisTimeout);
            }

            // Position the object to match the source.
            go.transform.localPosition = sourceObject.transform.localPosition;
            go.transform.localRotation = sourceObject.transform.localRotation;
           
            // Enable in the hierarchy
            go.SetActive(true);

            return go;
        }

        public void FinishWithObject(GameObject gameObject)
        {
            // Find the object in the list
            foreach (GameObject go in m_GameObjects)
            {
                if (go == gameObject)
                {
                    // Disable it in the hierarchy
                    go.SetActive(false);
                    // Kill the auto-disable timer
                    if (m_RunningTimeouts.ContainsKey(go))
                    {
                        StopCoroutine(m_RunningTimeouts[go]);
                        m_RunningTimeouts.Remove(go);
                    }
                    return;
                }
            }
        }

        private GameObject FirstAvailable()
        {
            // Find the first object available
            foreach (GameObject go in m_GameObjects)
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
            GameObject newGO = Instantiate(sourceObject);
            // Name it
            newGO.name = sourceObject.name + "(PoolObject)";
            // Child it
            newGO.transform.parent = transform;
            // Add to list
            m_GameObjects.Add(newGO);
            return newGO;
        }

        // Auto-disable timer
        private IEnumerator LifeSpanRoutine(GameObject go)
        {
            yield return new WaitForSeconds(maxLifespan);
            FinishWithObject(go);
        }
    }
}

