using UnityEngine;
using System.Collections;
using scene;

namespace x600d1dea.scene
{
	public static class MUtils
	{

		const float kEpsilon = 0.001f;

		public static bool Approximately(float a, float b, float epsilon = kEpsilon)
		{
			return Mathf.Abs(a - b) < epsilon;
		}

		public static bool LessOrEqual(float a, float b)
		{
			return a < b || Approximately(a, b);
		}

		public static bool GreaterOrEqual(float a, float b)
		{
			return a > b || Approximately(a, b);
		}

		public static bool Approximately(Vector3 a, Vector3 b)
		{
			for (int i = 0; i < 3; ++i)
			{
				if (!Approximately(a[i], b[i]))
					return false;
			}
			return true;
		}

		public static void Swap<T>(ref T a, ref T b)
		{
			T t = a;
			a = b;
			b = t;
		}

		public static void MinMax(out float min, out float max, float a, float b, params float[] others)
		{
			min = Mathf.Min(a, b);
			max = Mathf.Max(a, b);
			if (others != null)
			{
				for (int i = 0; i < others.Length; ++i)
				{
					var f = others[i];
					if (f < min)
						min = f;
					else if (f > max)
						max = f;
				}
			}
		}


		public static bool Intersects(Plane a, Plane b, out Line line)
		{
			Vector3 dir = Vector3.Cross(a.normal, b.normal);
			Vector3 p = Vector3.zero;
			if (Approximately(dir.sqrMagnitude, 0f))
			{
				line = new Line();
				return false;
			}
			dir = dir.normalized;
			float dot = Vector3.Dot(a.normal, b.normal);
			float kd = 1f - dot * dot;
			float k0 = (-a.distance + b.distance * dot) / kd;
			float k1 = (-b.distance + a.distance * dot) / kd;
			p = k0 * a.normal + k1 * b.normal;
			line = new Line(p, dir);
			return true;
		}

		public static bool Intersects(Plane plane, Vector3 p0, Vector3 p1, out float enter)
		{
			var d = p1 - p0;
			if (Approximately(Vector3.Dot(plane.normal, d.normalized), 0f))
			{
				if (Approximately(Vector3.Dot(p0 - plane.normal * plane.distance, plane.normal), 0f))
				{
					enter = Mathf.Infinity;
					return true;
				}
				enter = 0f;
				return false;
			}
			float m = plane.distance - Vector3.Dot(p0, plane.normal);
			float deno = Vector3.Dot(d, plane.normal);
			enter = m / deno;
			return GreaterOrEqual(enter, 0f) && LessOrEqual(enter, 1f);
		}

		public static int MinAxis(Vector3 value)
		{
			float minValue = float.PositiveInfinity;
			int minAxis = 0;
			for (int i = 0; i < 3; ++i)
			{
				if (value[i] < minValue)
				{
					minValue = value[i];
					minAxis = i;
				}
			}
			return minAxis;
		}

		public static int MaxAxis(Vector3 value)
		{
			float maxValue = float.NegativeInfinity;
			int minAxis = 0;
			for (int i = 0; i < 3; ++i)
			{
				if (value[i] > maxValue)
				{
					maxValue = value[i];
					minAxis = i;
				}
			}
			return minAxis;
		}

		/* parameter s:
		 * 
		 *                   6
		 *         +-----+-----+
		 *        /|  7 /|  6 /|
		 *       +-----+-----+ +
		 *      /|  4 /|/ 5 /|/|           | /                      /
		 *     +-----+-----+ + +          3|/ 2                  7 / 6
		 *     |/    |/    |/|/ 2     -----+----- bottom     -----+----- top
		 *     +-----+-----+ +          0 / 1                  4 /| 5
		 *     |     |/    |/ 1          /                      / |
		 *     +-----+-----+
		 *        0
		 *     
		 */

		public static Bounds OctTree_GetSubBounds(Bounds bounds, int s, float loose = 0f)
		{
			return bounds;
		}


		/* parameter s:
		 * 		+---+---+
		 * 		| 3 | 2 |
		 * 		+---+---+
		 * 		| 0 | 1 |
		 * 		+---+---+
		 */

		public static Bounds QuadTree_GetSubBounds(Bounds bounds, int s, float loose = 0f)
		{
			Vector3 bm = bounds.min;
			Vector3 bM = bounds.max;
			Vector3 topC = new Vector3((bm.x + bM.x) * 0.5f, bM.y, (bm.z + bM.z) * 0.5f);
			Bounds nb = new Bounds();
			if (s == 0)
			{
				nb.SetMinMax(bm, topC);
				nb = nb.Loose(loose);
			}
			else if (s == 1)
			{
				nb.SetMinMax(new Vector3(topC.x, bm.y, bm.z),
							 new Vector3(bM.x, topC.y, topC.z));
				nb = nb.Loose(loose);
			}
			else if (s == 2)
			{
				nb.SetMinMax(new Vector3(topC.x, bm.y, topC.z),
							 bM);
				nb = nb.Loose(loose);
			}
			else if (s == 3)
			{
				nb.SetMinMax(new Vector3(bm.x, bm.y, topC.z),
							 new Vector3(topC.x, topC.y, bM.z));
				nb = nb.Loose(loose);
			}
			return nb;
		}



		public static bool QuadTree_IsBoundsInSub(Bounds bounds, int s, Bounds boundsToTest, float loose = 0f)
		{
			var subBounds = QuadTree_GetSubBounds(bounds, s, loose);
			var boundary = boundsToTest.GetBoundary();
			foreach (var b in boundary)
			{
				if (!subBounds.Contains(b))
					return false;
			}
			return true;
		}
		public static readonly Bounds emptyBounds = new Bounds();

		public static Bounds GetBounds(Vector2[] verts)
		{
			if (verts.Length > 0)
			{
				var b = new Bounds(verts[0], Vector3.zero);
				for (int i = 1; i < verts.Length; ++i)
				{
					b.Encapsulate(verts[i]);
				}
				return b;
			}
			return emptyBounds;
		}
	}
}