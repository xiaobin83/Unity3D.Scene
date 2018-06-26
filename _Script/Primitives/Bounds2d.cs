using UnityEngine;
using System.Collections.Generic;

namespace scene
{

	public struct Bounds2d
	{
		public Vector2 min;
		public Vector2 max;
		public Rect rect
		{
			get
			{
				return new Rect(min, max - min);
			}
		}

		public Bounds2d(Vector2 inMin, Vector2 inMax)
		{
			min = new Vector2(Mathf.Min(inMin.x, inMax.x), Mathf.Min(inMin.y, inMax.y));
			max = new Vector2(Mathf.Max(inMin.x, inMax.x), Mathf.Max(inMin.y, inMax.y));
		}

		public static Bounds2d FromBounds(Bounds bounds)
		{
			return new Bounds2d(
				MUtils.MappingAxis.Map(bounds.min),
				MUtils.MappingAxis.Map(bounds.max));
		}

		public bool Intersects(LineSegment2d seg, out float t0, out float t1, out float t2, out float t3)
		{
			if (Intersects(seg.line, out t0, out t1, out t2, out t3))
			{
				return seg.PointOnSegment(t0) || seg.PointOnSegment(t1)
					|| seg.PointOnSegment(t2) || seg.PointOnSegment(t3);
			}
			return false;
		}

		public bool Intersects(Line2d line, out float t0, out float t1, out float t2, out float t3)
		{
			List<LineSegment2d> segs = LineSegment2d.FromBounds(this);

			t0 = t1 = t2 = t3 = 0f;


			float unused;
			// horizontal
			float h0, h1;
			bool intersects = segs[0].line.Intersects(line, out unused, out h0);
			if (!intersects) return false;
			intersects = segs[1].line.Intersects(line, out unused, out h1);
			if (!intersects) return false;

			// vertical
			float v0, v1;
			intersects = segs[2].line.Intersects(line, out unused, out v0);
			if (!intersects) return false;
			intersects = segs[3].line.Intersects(line, out unused, out v1);
			if (!intersects) return false;

			float hh0 = Mathf.Min(h0, h1);
			float hh1 = Mathf.Max(h0, h1);
			float vv0 = Mathf.Min(v0, v1);
			float vv1 = Mathf.Max(v0, v1);

			if (MUtils.LessOrEqual(hh0, vv0))
			{
				// hit h0 first
				t0 = hh0;
				t1 = vv0;
				t2 = Mathf.Min(hh1, vv1);
				t3 = Mathf.Max(hh1, vv1);
				return MUtils.LessOrEqual(vv0, hh1);
			}
			else
			{
				// hit v0 first
				t0 = vv0;
				t1 = hh0;
				t2 = Mathf.Min(vv1, hh1);
				t3 = Mathf.Max(vv1, hh1);
				return MUtils.LessOrEqual(hh0, vv1);
			}
		}
	}
}

