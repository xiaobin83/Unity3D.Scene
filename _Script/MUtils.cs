using UnityEngine;
using System.Collections;

namespace scene
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

		/* 
		 * +---+---+
		 * | 3 | 2 |
		 * +---+---+
		 * | 0 | 1 |
		 * +---+---+
		 */

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


		public class MappingAxis
		{
			public enum Plane
			{
				XZ = 0,
				XY,
				YZ
			}

			public static Plane mappingPlane = Plane.XZ;

			static int[] mappingX = new int[] { 0, 0, 1 };
			static int[] mappingY = new int[] { 2, 1, 2 };

			public static Vector2 Map(Vector3 p)
			{
				return new Vector2(p[mappingX[(int)mappingPlane]], p[mappingY[(int)mappingPlane]]);
			}

			public static Vector3 Map(Vector2 p)
			{
				Vector3 ret = new Vector3(0f, 0f, 0f);
				ret[mappingX[(int)mappingPlane]] = p.x;
				ret[mappingY[(int)mappingPlane]] = p.y;
				return ret;
			}
			public static Line2d Map(Line L)
			{
				return new Line2d(Map(L.P), Map(L.D));
			}

			public static void SetPosition(ref Vector3 p0, Vector3 p1)
			{
				Vector2 pp1 = Map(p1);
				p0[mappingX[(int)mappingPlane]] = pp1.x;
				p0[mappingY[(int)mappingPlane]] = pp1.y;
			}
		}

	}

}