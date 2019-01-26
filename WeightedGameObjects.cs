using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wrj
{
	[System.Serializable]
	public class WeightedGameObjects
	{
		public WeightedElement[] objectList;
		private int m_LastSelectedIndex = -1;
		
		/// Returns a random element from the list where objects with higher weights are more likely
		public GameObject GetRandom(bool preventImmediateRepeat = false)
		{
			List<int> allOptions = new List<int>();

			for (int i = 0; i < objectList.Length; i++)
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
		[System.Serializable]
		public struct WeightedElement
		{
			public GameObject element;
			public int weight;
		}
	}
}
