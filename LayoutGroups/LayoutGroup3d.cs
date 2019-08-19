using UnityEngine;

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
		private Transform[] _children;

		void Update () 
		{
			// If the children have changed, force an update on everything
			Transform[] children = GetComponentsInChildren<Transform>();
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
				Vector3 leftmostPos = (horizontalCentering) ? transform.localPosition.With(x: -(horizontalSpacing * (transform.childCount - 1)) * .5f) : Vector3.zero;
				float appliedSpacing = 0f;
				foreach (Transform element in transform)
				{
					element.localPosition = element.localPosition.With(x: leftmostPos.x + appliedSpacing);
					appliedSpacing += horizontalSpacing;
				}
			}

			if (verticalSpacing != _cachedVerticalSpacing || verticalCentering != _cachedVerticalCentering)
			{
				_cachedVerticalSpacing = verticalSpacing;
				_cachedVerticalCentering = verticalCentering;
				Vector3 topmostPos = (verticalCentering) ? transform.localPosition.With(y: -(verticalSpacing * (transform.childCount - 1)) * .5f) : Vector3.zero;
				float appliedSpacing = 0f;
				foreach (Transform element in transform)
				{
					element.localPosition = element.localPosition.With(y: topmostPos.y + appliedSpacing);
					appliedSpacing += verticalSpacing;
				}
			}

			if (depthSpacing != _cachedDepthSpacing || depthCentering != _cachedDepthCentering)
			{
				_cachedDepthSpacing = depthSpacing;
				_cachedDepthCentering = depthCentering;
				Vector3 farmostPos = (depthCentering) ? transform.localPosition.With(z: -(depthSpacing * (transform.childCount - 1)) * .5f) : Vector3.zero;
				float appliedSpacing = 0f;
				foreach (Transform element in transform)
				{
					element.localPosition = element.localPosition.With(z: farmostPos.z + appliedSpacing);
					appliedSpacing += depthSpacing;
				}
			}
		}
	}
}
