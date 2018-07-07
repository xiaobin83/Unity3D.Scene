using UnityEngine;
using System.Collections.Generic;

namespace x600d1dea.scene
{
	public class LineSegment2D
	{
		public Line2D line;
		public float d0, d1;

		public LineSegment2D(Vector2 p0, Vector2 p1)
		{
			line = new Line2D(p0, p1-p0);
			d0 = 0f;
			d1 = 1f;
		}

		public LineSegment2D(Line2D line, float d0, float d1)
		{
			this.line = line;
			this.d0 = Mathf.Min(d0, d1);
			this.d1 = Mathf.Max(d0, d1);
		}

		public Vector2 startPoint {
			get {
				return GetPoint(d0);
			}
		}

		public Vector2 endPoint {
			get {
				return GetPoint(d1);
			}
		}

		public Vector2 GetPoint(float t)
		{
			return line.GetPoint(t);
		}
		
		public bool Intersects(LineSegment2D lineSeg, out float s, out float t)
		{
			if (line.Intersects(lineSeg.line, out s, out t))
			{
				return PointOnSegment(s) && lineSeg.PointOnSegment(t);
			}
			return false;
		}

		public bool Intersects(Line2D line, out float s, out float t)
		{
			if (this.line.Intersects(line, out s, out t))
			{
				return PointOnSegment(s);
			}
			return false;
		}

		public bool PointOnSegment(float t) 
		{
			return MUtils.LessOrEqual(d0, t) && MUtils.LessOrEqual(t, d1);
		}

		public static List<LineSegment2D> FromBounds(Bounds2D bounds)
		{
			List<LineSegment2D> segs = new List<LineSegment2D>();

			// from bottom to top, and from left to right

			// horizontal
			segs.Add(new LineSegment2D(bounds.min, new Vector2(bounds.max.x, bounds.min.y)));
			segs.Add(new LineSegment2D(new Vector2(bounds.min.x, bounds.max.y), bounds.max));
			
			// vertical
			segs.Add(new LineSegment2D(bounds.min, new Vector2(bounds.min.x, bounds.max.y)));
			segs.Add(new LineSegment2D(new Vector2(bounds.max.x, bounds.min.y), bounds.max));

			return segs;
		}

		public static List<LineSegment2D> FromBounds(Bounds bounds)
		{
			return FromBounds(Bounds2D.FromBounds(bounds));
		}

	}
}
