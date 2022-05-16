using UnityEngine;
using System.Collections.Generic;

namespace Wrj
{
	[ExecuteInEditMode]
	public class LayoutGroup3d : MonoBehaviour {

		public bool horizontalCentering = true;
		private bool _cachedHorizontalCentering = true;
		public float horizontalSpacing = 1f;
		private float _cachedHorizontalSpacing;
		public bool verticalCentering = true;
		private bool _cachedVerticalCentering = true;
		public float verticalSpacing;
		private float _cachedVerticalSpacing;
		public bool depthCentering = true;
		private bool _cachedDepthCentering = true;
		public float depthSpacing;
		private float _cachedDepthSpacing;
		private List<Transform> _children;

		void Update () 
		{
			if (Application.isPlaying) return;
			Refresh();
		}
		
		public void Refresh()
		{
			List<Transform> children = new  List<Transform>();
			foreach (Transform child in transform)
			{
				if (child.gameObject.activeInHierarchy)
				{
					children.Add(child);
				}
			}
			if (_children != children)
			{
				_children = children;
				_cachedHorizontalSpacing += 1f;
				_cachedVerticalSpacing += 1f;
				_cachedDepthSpacing += 1f;
			}

			if (horizontalSpacing != _cachedHorizontalSpacing || horizontalCentering != _cachedHorizontalCentering)
			{
				_cachedHorizontalSpacing = horizontalSpacing;
				_cachedHorizontalCentering = horizontalCentering;
				Vector3 leftmostPos = (horizontalCentering) ? transform.localPosition.With(x: -(horizontalSpacing * (children.Count - 1)) * .5f) : Vector3.zero;
				float appliedSpacing = 0f;
				foreach (Transform element in children)
				{
					element.localPosition = element.localPosition.With(x: leftmostPos.x + appliedSpacing);
					appliedSpacing += horizontalSpacing;
				}
			}

			if (verticalSpacing != _cachedVerticalSpacing || verticalCentering != _cachedVerticalCentering)
			{
				_cachedVerticalSpacing = verticalSpacing;
				_cachedVerticalCentering = verticalCentering;
				Vector3 topmostPos = (verticalCentering) ? transform.localPosition.With(y: -(verticalSpacing * (children.Count - 1)) * .5f) : Vector3.zero;
				float appliedSpacing = 0f;
				foreach (Transform element in children)
				{
					element.localPosition = element.localPosition.With(y: topmostPos.y + appliedSpacing);
					appliedSpacing += verticalSpacing;
				}
			}

			if (depthSpacing != _cachedDepthSpacing || depthCentering != _cachedDepthCentering)
			{
				_cachedDepthSpacing = depthSpacing;
				_cachedDepthCentering = depthCentering;
				Vector3 farmostPos = (depthCentering) ? transform.localPosition.With(z: -(depthSpacing * (children.Count - 1)) * .5f) : Vector3.zero;
				float appliedSpacing = 0f;
				foreach (Transform element in children)
				{
					element.localPosition = element.localPosition.With(z: farmostPos.z + appliedSpacing);
					appliedSpacing += depthSpacing;
				}
			}
		}
	}
}
