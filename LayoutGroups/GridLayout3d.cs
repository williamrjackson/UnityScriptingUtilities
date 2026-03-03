using UnityEngine;

namespace Wrj
{
	[ExecuteInEditMode]
	public class GridLayout3d : MonoBehaviour 
	{
		public int columns = 5;
		private int _cachedColumns;
		public bool columnCentering = false;
		private bool _cachedColumnCentering = false;
		public float columnSpacing = 1f;
		private float _cachedColumnSpacing;
		public bool rowCentering = false;
		private bool _cachedRowCentering = false;
		public float rowSpacing = 1f;
		private float _cachedRowSpacing;
		private Transform[] _children;
		private int _cachedChildrenHash;

		void Update () 
		{
			if (columns < 1)
				return;

			int currentHash = transform.childCount;
			unchecked
			{
				foreach (Transform child in transform)
				{
					currentHash = (currentHash * 397) ^ (child ? child.GetInstanceID() : 0);
				}
			}
			
			if (columnSpacing != _cachedColumnSpacing || columnCentering != _cachedColumnCentering
				|| rowSpacing != _cachedRowSpacing || rowCentering != _cachedRowCentering
				|| columns != _cachedColumns || _cachedChildrenHash != currentHash)
			{
				_cachedColumnSpacing = columnSpacing;
				_cachedColumnCentering = columnCentering;
				_cachedRowSpacing = rowSpacing;
				_cachedRowCentering = rowCentering;
				_cachedColumns = columns;
				_cachedChildrenHash = currentHash;
				_children = GetComponentsInChildren<Transform>();
				int rowCount = transform.childCount / columns;
				if (transform.childCount % columns != 0)
				{
					rowCount++;
				}
				Vector3 leftmostPos = (columnCentering) ? transform.localPosition.With(x: -(columnSpacing * (columns - 1)) * .5f) : Vector3.zero;
				Vector3 topmostPos = (rowCentering) ? transform.localPosition.With(y: (rowSpacing * (rowCount - 1)) * .5f) : Vector3.zero;

				float appliedHorizontalSpacing = 0f;
				float appliedVerticalSpacing = 0f;
				int columnCount = 0;
				foreach (Transform element in transform)
				{
					columnCount++;
					element.localPosition = transform.localPosition.With(x: leftmostPos.x + appliedHorizontalSpacing, y: topmostPos.y - appliedVerticalSpacing);
					appliedHorizontalSpacing += columnSpacing;
					if (columnCount == columns)
					{
						columnCount = 0;
						appliedHorizontalSpacing = 0f;
						appliedVerticalSpacing += rowSpacing;
					}
				}
			}
		}
	}
}
