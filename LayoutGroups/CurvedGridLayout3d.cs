using UnityEngine;

namespace Wrj
{
	[ExecuteInEditMode]
	public class CurvedGridLayout3d : MonoBehaviour 
	{
		public enum CurveAxis {Columns, Rows, Both}
		public CurveAxis curveAxis;
		private CurveAxis _cachedCurveAxis;
		public enum FaceDirection {Inward, Outward, Toward, Away}
		public FaceDirection faceDirection;
		private FaceDirection _cachedFaceDirection;
		public int columns = 5;
		public float radius = 5f;
		private float _cachedRadius;
		private int _cachedColumns;
		public float columnSpacing = 1f;
		private float _cachedColumnSpacing;
		public float rowSpacing = 1f;
		private float _cachedRowSpacing;
		private Transform[] _children;
		private int _cachedChildrenHash;

		void Update () 
		{
			if (columns < 1 || transform.childCount <= 0)
				return;

			int currentHash = transform.childCount;
			unchecked
			{
				foreach (Transform child in transform)
				{
					currentHash = (currentHash * 397) ^ (child ? child.GetInstanceID() : 0);
				}
			}
			
			if (   columnSpacing != _cachedColumnSpacing
				|| rowSpacing != _cachedRowSpacing
				|| columns != _cachedColumns
				|| radius != _cachedRadius
				|| curveAxis != _cachedCurveAxis
				|| faceDirection != _cachedFaceDirection
				|| _cachedChildrenHash != currentHash)
			{
				_cachedColumnSpacing = columnSpacing;
				_cachedRowSpacing = rowSpacing;
				_cachedColumns = columns;
				_cachedRadius = radius;
				_cachedCurveAxis = curveAxis;
				_cachedFaceDirection = faceDirection;
				_cachedChildrenHash = currentHash;
				_children = GetComponentsInChildren<Transform>();

				// If there are fewer transforms than columns, pretend there are just that many columns.
				int appliedColumns = Mathf.Min(columns, _children.Length - 1);
				int rowCount = transform.childCount / appliedColumns;
				if (transform.childCount % appliedColumns != 0)
				{
					rowCount++;
				}

				// Offsets for centering
				float leftmostAngle = (curveAxis == CurveAxis.Rows) ? ((columnSpacing * (appliedColumns + 1)) * .5f) : -(columnSpacing * (appliedColumns - 1)) * .5f;
				float topmostAngle = (curveAxis != CurveAxis.Columns) ?  -((rowSpacing * (rowCount - 1)) * .5f): (rowSpacing * (rowCount - 1)) * .5f;

				float appliedHorizontalSpacing = 0f;
				float appliedVerticalSpacing = 0f;
				int columnCount = 0;
				foreach (Transform element in transform)
				{
					columnCount++;
					
					// Project to a point on a sphere
					Quaternion rotation;
					if (curveAxis == CurveAxis.Columns)
					{
						rotation = Quaternion.AngleAxis(leftmostAngle + appliedHorizontalSpacing, transform.up);  
					}
					else if (curveAxis == CurveAxis.Rows)
					{
						rotation = Quaternion.AngleAxis(topmostAngle + appliedVerticalSpacing, transform.right);
					}
					else
					{
						rotation = Quaternion.AngleAxis(leftmostAngle + appliedHorizontalSpacing, transform.up);  
						rotation *= Quaternion.AngleAxis(topmostAngle + appliedVerticalSpacing, transform.right);
					}
					Vector3 pos = rotation * transform.forward * radius;

					element.localPosition = pos;
					appliedHorizontalSpacing += columnSpacing;

					if (faceDirection == FaceDirection.Away)
					{
						element.LookAt(element.position - transform.forward);
					}
					else if (faceDirection == FaceDirection.Toward)
					{
						element.LookAt(element.position + transform.forward);
					}
					else
					{	
						element.LookAt(transform.position);
						if (faceDirection == FaceDirection.Inward)
						{
							element.LookAt(element.position - element.forward);
						}
					}
					if (curveAxis == CurveAxis.Columns) 
					{
						element.localPosition = element.localPosition.With(y: topmostAngle - appliedVerticalSpacing);
					}
					if (curveAxis == CurveAxis.Rows) 
					{
						element.localPosition = element.localPosition.With(x: leftmostAngle - appliedHorizontalSpacing);
					}

					if (columnCount == appliedColumns)
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
