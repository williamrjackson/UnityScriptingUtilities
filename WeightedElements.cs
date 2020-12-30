using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
    [System.Serializable]
    public class WeightedElements<T>
    {
        private List<WeightedElement> objectList = new List<WeightedElement>();
        private List<int> availableIndices = new List<int>();

        private int m_LastSelectedIndex = -1;

        /// <summary>
        /// Returns a random element from the list where objects with higher weights are more likely
        /// </summary>
        public T GetRandom(bool preventImmediateRepeat = false)
        {
            if (objectList == null || objectList.Count == 0) return default(T);
            if (objectList.Count < 2) return objectList[0].element;

            int weightedRandomIndex = m_LastSelectedIndex;
            int iterationCount = 0;
            while (weightedRandomIndex == m_LastSelectedIndex && iterationCount < (availableIndices.Count * 2))
            {
                weightedRandomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
                if (!preventImmediateRepeat) break;
            }

            m_LastSelectedIndex = weightedRandomIndex;
            
            return objectList[weightedRandomIndex].element;
        }
        public void Add(T element, int weight)
        {
            WeightedElement newElement = new WeightedElement(element, weight);
            objectList.Add(newElement);
            int index = objectList.IndexOf(newElement);
            for (int i = 0; i < weight; i++)
            {
                availableIndices.Add(index);
            }
        }
        public void Remove(T element)
        {
            foreach (var item in objectList)
            {
                if (EqualityComparer<T>.Default.Equals(item.element, element))
                {
                    objectList.Remove(item);
                    return;
                }
            }
            availableIndices.Clear();
            for (int i = 0; i < objectList.Count; i++)
            {
                for (int j = 0; j < objectList[i].weight; j++)
                {
                    availableIndices.Add(i);
                }
            }
        }
        public void Clear()
        {
            objectList.Clear();
            availableIndices.Clear();
        }
        public int Count
        {
            get
            {
                return objectList.Count;
            }
        }

        public void ApplyLinearWeights(List<T> source, bool invert = false)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (invert)
                {
                    Add(source[i], source.Count - i);
                }
                else
                {
                    Add(source[i], i + 1);
                }
            }
        }
        public void ApplyCurveWeights(List<T> source, AnimationCurve curve, bool invert = false)
        {
            for (int i = 0; i < source.Count; i++)
            {
                float curveVal = 0f;
                if (invert)
                {
                    curveVal = curve.Evaluate(Utils.Remap((float)i, 0f, (float)source.Count, 1f, 0f));
                }
                else
                {
                    curveVal = curve.Evaluate(Utils.Remap((float)i, 0f, (float)source.Count, 0f, 1f));
                }

                int weight = Mathf.RoundToInt(curveVal * 100f);

                Add(source[i], Mathf.Max(1, weight));
            }
        }

        public WeightedElements() { }
        public WeightedElements (List<T> source, AnimationCurve curve, bool invert=false)
        {
            Clear();
            ApplyCurveWeights(source, curve, invert);
        }
        public WeightedElements (List<T> source, bool invert=false)
        {
            Clear();
            ApplyLinearWeights(source, invert);
        }

        [System.Serializable]
		private class WeightedElement
		{
			public T element;
			public int weight;

            public WeightedElement(T element, int weight)
            {
                this.element = element;
                this.weight = weight;
            } 
		}
	}
}
