using UnityEngine;

namespace scene
{
	public static class BoundsExtension
	{
		public static Vector3[] GetBoundary(this Bounds bounds)
		{
			Vector3[] p = new Vector3[8];
			Vector3 size = bounds.size;
			p[0] = bounds.min;
			p[1] = p[0] + new Vector3(size.x, 0f, 0f);
			p[2] = p[0] + new Vector3(size.x, 0f, size.z);
			p[3] = p[0] + new Vector3(0f, 0f, size.z);
			p[4] = p[0] + new Vector3(0f, size.y, 0f);
			p[5] = p[4] + new Vector3(size.x, 0f, 0f);
			p[6] = p[4] + new Vector3(size.x, 0f, size.z);
			p[7] = p[4] + new Vector3(0f, 0f, size.z);
			return p;
		}

		public static Bounds Loose(this Bounds bounds, float loose)
		{
			var min = bounds.min;
			var max = bounds.max;
			var centerToMin = min - bounds.center;
			var centerToMax = max - bounds.center;
			bounds.SetMinMax(min + centerToMin * loose, max + centerToMax * loose);
			return bounds;
		}

		public static float GetProjectedAreaOnXZPlane(this Bounds bounds)
		{
			var size = bounds.max - bounds.min;
			return size.x * size.z;
		}
	}
}

