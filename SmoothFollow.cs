using UnityEngine;

namespace Wrj
{
	public class SmoothFollow : MonoBehaviour
	{
		[SerializeField]
		private Transform followTarget = null;
		[SerializeField]
		private float smoothTime = 0.3F;
		[SerializeField]
		[Range(0f, 1f)]
		private float distanceThreshold = .1f;

		private Vector3 offset;
		private Vector3 velocity = Vector3.zero;

		void Start()
		{
			offset = transform.position - followTarget.position;
		}

		void LateUpdate()
		{
			Vector3 targetPosition = followTarget.position + offset;
			if (Vector3.Distance(transform.position, targetPosition) > distanceThreshold)
			{
				transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
			}
		}
	}
}