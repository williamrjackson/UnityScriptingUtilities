using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
	[System.Serializable]
	public class WeightedElements<T>
	{
		private List<WeightedElement> objectList = new List<WeightedElement>();
		private int m_LastSelectedIndex = -1;

        /// <summary>
        /// Returns a random element from the list where objects with higher weights are more likely
        /// </summary>
        public T GetRandom(bool preventImmediateRepeat = false)
		{
            if (objectList.Count < 2) return objectList[0].element;
            List<int> allOptions = new List<int>();

			for (int i = 0; i < objectList.Count; i++)
			{
				if (!preventImmediateRepeat || i != m_LastSelectedIndex)
				{
					for (int j = 0; j < objectList[i].weight; j++)
					{
						allOptions.Add(i);
					}
				}
			}
			int weightedRandomIndex = allOptions[UnityEngine.Random.Range(0, allOptions.Count)];
			m_LastSelectedIndex = weightedRandomIndex;

			return objectList[weightedRandomIndex].element;
		}
        public void Add(T element, int weight)
        {
            objectList.Add(new WeightedElement(element, weight));
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
        }
        public void Clear()
        {
            objectList.Clear();
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
                
                Add(source[i], weight);
            }
        }

        public WeightedElements (List<T> source, AnimationCurve curve)
        {
            Clear();
            ApplyCurveWeights(source, curve);
        }
        public WeightedElements (List<T> source)
        {
            Clear();
            ApplyLinearWeights(source);
        }

        public WeightedElements()
        {
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
