using UnityEngine;
using System.Collections.Generic;

namespace scene
{
	public struct Triangle
	{
		public Vertex p0, p1, p2;
		public Plane plane
		{
			get
			{
				return new Plane(p0.vertex, p1.vertex, p2.vertex);
			}
		}

		public Triangle(Vector3 inP0, Vector3 inP1, Vector3 inP2)
		{
			p0 = new Vertex();
			p0.vertex = inP0;
			p1 = new Vertex();
			p1.vertex = inP1;
			p2 = new Vertex();
			p2.vertex = inP2;
		}
		public Triangle(Vertex inP0, Vertex inP1, Vertex inP2)
		{
			p0 = inP0;
			p1 = inP1;
			p2 = inP2;
		}

		public Triangle GetWinding(Vector3 normal)
		{
			var d0 = (p1.vertex - p0.vertex).normalized;
			var d1 = (p2.vertex - p0.vertex).normalized;
			var cross = Vector3.Cross(d0, d1);
			if (Vector3.Dot(normal, cross) < 0f)
			{
				var pp1 = p1;
				var pp2 = p2;
				MUtils.Swap(ref pp1, ref pp2);
				return new Triangle(p0, pp1, pp2);
			}
			return new Triangle(p0, p1, p2);
		}

		public bool IsFacing(Vector3 direction, float tolerance = 0f)
		{
			var d0 = (p1.vertex - p0.vertex).normalized;
			var d1 = (p2.vertex - p0.vertex).normalized;
			var cross = Vector3.Cross(d0, d1);
			return Vector3.Dot(direction, cross) < tolerance;
		}

		// Clip out part at positive side
		// return the rest triangles at negative side (including one on the plane)
		delegate TResult Func<T0, T1, T2, T3, T4, T5, TResult>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t6);
		public Triangle[] ClipBy(Plane inPlane)
		{
			Vector3 ptOnPlane = -inPlane.normal * inPlane.distance;
			float dot0 = Vector3.Dot(p0.vertex - ptOnPlane, inPlane.normal);
			float dot1 = Vector3.Dot(p1.vertex - ptOnPlane, inPlane.normal);
			float dot2 = Vector3.Dot(p2.vertex - ptOnPlane, inPlane.normal);

			int dr0 = MUtils.Approximately(dot0, 0f) ? 0 : (dot0 < 0f ? -1 : 1);
			int dr1 = MUtils.Approximately(dot1, 0f) ? 0 : (dot1 < 0f ? -1 : 1);
			int dr2 = MUtils.Approximately(dot2, 0f) ? 0 : (dot2 < 0f ? -1 : 1);

			if (dr0 > 0 && dr1 > 0 && dr2 > 0)
				return null;
			if (dr0 < 0 && dr1 < 0 && dr2 < 0)
				return new Triangle[] { this };


			// one edge lay on the plane
			System.Func<int, int, int, bool> check = delegate (int kr0, int kr1, int kr2)
			{
				if (kr0 == 0 && kr1 == 0)
				{
					return kr2 <= 0;
				}
				return false;
			};

			if (check(dr0, dr1, dr2) || check(dr0, dr2, dr1)
			|| check(dr1, dr0, dr2) || check(dr1, dr2, dr0)
			|| check(dr2, dr0, dr1) || check(dr2, dr1, dr0))
			{
				return new Triangle[] { this };
			}

			List<Triangle> tris = new List<Triangle>();

			Func<int, int, int, Vertex, Vertex, Vertex, bool> clip = delegate (int kr0, int kr1, int kr2, Vertex pt0, Vertex pt1, Vertex pt2)
			{
				if (kr0 > 0) // p0 at positive side
			{
					float enter;
					if (kr1 < 0 && kr2 < 0) // other two on nagetive plane
				{
						var r = new Ray(pt0.vertex, pt1.vertex - pt0.vertex);
						inPlane.Raycast(r, out enter);
						var t0 = r.GetPoint(enter);

						Vertex tt0 = new Vertex();
						tt0.vertex = t0;
						tt0.uv = pt0.uv + (pt1.uv - pt0.uv) * enter;
						tt0.uv2 = pt0.uv2 + (pt1.uv2 - pt0.uv2) * enter;
						tt0.uv3 = pt0.uv3 + (pt1.uv3 - pt0.uv3) * enter;
						tt0.uv4 = pt0.uv4 + (pt1.uv4 - pt0.uv4) * enter;

						r = new Ray(pt0.vertex, pt2.vertex - pt0.vertex);
						inPlane.Raycast(r, out enter);
						var t1 = r.GetPoint(enter);
						Vertex tt1 = new Vertex();
						tt1.vertex = t1;
						tt1.uv = pt0.uv + (pt2.uv - pt0.uv) * enter;
						tt1.uv2 = pt0.uv2 + (pt2.uv2 - pt0.uv2) * enter;
						tt1.uv3 = pt0.uv3 + (pt2.uv3 - pt0.uv3) * enter;
						tt1.uv4 = pt0.uv4 + (pt2.uv4 - pt0.uv4) * enter;

						tris.Add(new Triangle(tt0, pt1, pt2));
						tris.Add(new Triangle(tt0, pt2, tt1));
					}
					else if (kr1 > 0 && kr2 < 0) // p1 at positive side. p2 at nagetive side
					{
						var r = new Ray(pt0.vertex, pt2.vertex - pt0.vertex);
						inPlane.Raycast(r, out enter);
						var t0 = r.GetPoint(enter);
						Vertex tt0 = new Vertex();
						tt0.vertex = t0;
						tt0.uv = pt0.uv + (pt2.uv - pt0.uv) * enter;
						tt0.uv2 = pt0.uv2 + (pt2.uv2 - pt0.uv2) * enter;
						tt0.uv3 = pt0.uv3 + (pt2.uv3 - pt0.uv3) * enter;
						tt0.uv4 = pt0.uv4 + (pt2.uv4 - pt0.uv4) * enter;

						r = new Ray(pt1.vertex, pt2.vertex - pt1.vertex);
						inPlane.Raycast(r, out enter);
						var t1 = r.GetPoint(enter);
						Vertex tt1 = new Vertex();
						tt1.vertex = t1;
						tt1.uv = pt1.uv + (pt2.uv - pt1.uv) * enter;
						tt1.uv2 = pt1.uv2 + (pt2.uv2 - pt1.uv2) * enter;
						tt1.uv3 = pt1.uv3 + (pt2.uv3 - pt1.uv3) * enter;
						tt1.uv4 = pt1.uv4 + (pt2.uv4 - pt1.uv4) * enter;

						tris.Add(new Triangle(tt0, pt2, tt1));
					}
					else if (kr1 < 0 && kr2 > 0) // p2 at positive side, p1 at nagetive side,
					{
						var r = new Ray(pt0.vertex, pt1.vertex - pt0.vertex);
						inPlane.Raycast(r, out enter);
						var t0 = r.GetPoint(enter);
						Vertex tt0 = new Vertex();
						tt0.vertex = t0;
						tt0.uv = pt0.uv + (pt1.uv - pt0.uv) * enter;
						tt0.uv2 = pt0.uv2 + (pt1.uv2 - pt0.uv2) * enter;
						tt0.uv3 = pt0.uv3 + (pt1.uv3 - pt0.uv3) * enter;
						tt0.uv4 = pt0.uv4 + (pt1.uv4 - pt0.uv4) * enter;

						r = new Ray(pt2.vertex, pt1.vertex - pt2.vertex);
						inPlane.Raycast(r, out enter);
						var t1 = r.GetPoint(enter);
						Vertex tt1 = new Vertex();
						tt1.vertex = t1;
						tt1.uv = pt2.uv + (pt2.uv - pt1.uv) * enter;
						tt1.uv2 = pt2.uv2 + (pt2.uv2 - pt1.uv2) * enter;
						tt1.uv3 = pt2.uv3 + (pt2.uv3 - pt1.uv3) * enter;
						tt1.uv4 = pt2.uv4 + (pt2.uv4 - pt1.uv4) * enter;

						tris.Add(new Triangle(tt0, pt1, tt1));
					}
					return true;
				}
				return false;
			};
			bool clipped = clip(dr0, dr1, dr2, p0, p1, p2);
			clipped = clipped || clip(dr1, dr2, dr0, p1, p2, p0);
			clipped = clipped || clip(dr2, dr0, dr1, p2, p0, p1);
			return tris.ToArray();
		}
	}


}