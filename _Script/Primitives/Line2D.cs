using UnityEngine;

namespace x600d1dea.scene
{
	public class Line2D
	{
		public Vector2 P;
		public Vector2 D;

		public Vector2 perpendicular {
			get {
				return new Vector2(-D.y, D.x);
			}
		}

		public Line2D(Vector2 p, Vector2 direction)
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
		public bool Intersects(Line2D line, out float t, out float s)
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
}

