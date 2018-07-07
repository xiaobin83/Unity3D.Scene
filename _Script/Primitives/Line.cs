using UnityEngine;

namespace x600d1dea.scene
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
}
