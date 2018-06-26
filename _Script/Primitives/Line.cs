using UnityEngine;
using System.Collections.Generic;

namespace scene
{
	public struct Line
	{
		public Vector3 P;
		public Vector3 D_;
		public Vector3 D
		{
			get
			{
				return D_;
			}
			set
			{
				D_ = value.normalized;
			}
		}

		public Line(Vector3 p, Vector3 d)
		{
			P = p;
			D_ = d.normalized;
		}
	}


	public struct Line2d
	{
		public Vector2 P;
		public Vector2 D;

		public Vector2 perpendicular {
			get {
				return new Vector2(-D.y, D.x);
			}
		}

		public Line2d(Vector2 p, Vector2 direction)
		{
			this.P = p;
			this.D = direction;
		}

		public Vector2 GetPoint(float t)
		{
			return P + D*t;
		}

		public float GetDistanceTo(Vector2 point)
		{
			Vector2 perp = perpendicular.normalized;
			return Mathf.Abs(Vector2.Dot((point - P), perp));
		}

		float Kross(Vector2 x, Vector2 y)
		{
			// z comp of a full cross
			return x[1]*y[0] - x[0]*y[1];
		}


		// this -> P + D*t
		// other -> line.P + line.D*s
		public bool Intersects(Line2d line, out float t, out float s)
		{
			// P0 + D0t = P1 + D1s => D0t - D1s = P1 - P0
			// P1 - P0 -> K
			// Kross D0 at both side -> -Kross(D0, D1)s = Kross(D0, K) => s = Kross(D0, K)/Kross(D1, D0)
			// Kross D1 at both side -> -Kross(D0, D1)t = Kross(D1, K) => t = Kross(D1, K)/Kross(D1, D0)
			// condition -> Kross(D1, D0) != 0

			float kross = Kross(line.D, D);
			if (!MUtils.Approximately(kross, 0f))
			{
				Vector2 K = line.P - P;
				s = Kross(D, K)/kross;
				t = Kross(line.D, K)/kross;
				return true;
			}
			s = t = 0;
			return false;
		}
	}

	public struct LineSegment2d
	{
		public Line2d line;
		public float d0, d1;

		public LineSegment2d(Vector2 p0, Vector2 p1)
		{
			line = new Line2d(p0, p1-p0);
			d0 = 0f;
			d1 = 1f;
		}

		public LineSegment2d(Line2d line, float d0, float d1)
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
		
		public bool Intersects(LineSegment2d lineSeg, out float s, out float t)
		{
			if (line.Intersects(lineSeg.line, out s, out t))
			{
				return PointOnSegment(s) && lineSeg.PointOnSegment(t);
			}
			return false;
		}

		public bool Intersects(Line2d line, out float s, out float t)
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

		public static List<LineSegment2d> FromBounds(Bounds2d bounds)
		{
			List<LineSegment2d> segs = new List<LineSegment2d>();

			// from bottom to top, and from left to right

			// horizontal
			segs.Add(new LineSegment2d(bounds.min, new Vector2(bounds.max.x, bounds.min.y)));
			segs.Add(new LineSegment2d(new Vector2(bounds.min.x, bounds.max.y), bounds.max));
			
			// vertical
			segs.Add(new LineSegment2d(bounds.min, new Vector2(bounds.min.x, bounds.max.y)));
			segs.Add(new LineSegment2d(new Vector2(bounds.max.x, bounds.min.y), bounds.max));

			return segs;
		}

		public static List<LineSegment2d> FromBounds(Bounds bounds)
		{
			return FromBounds(Bounds2d.FromBounds(bounds));
		}

	}




}