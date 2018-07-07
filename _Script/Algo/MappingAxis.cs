using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene
{
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
		public static Line2D Map(Line L)
		{
			return new Line2D(Map(L.P), Map(L.D));
		}

		public static void SetPosition(ref Vector3 p0, Vector3 p1)
		{
			Vector2 pp1 = Map(p1);
			p0[mappingX[(int)mappingPlane]] = pp1.x;
			p0[mappingY[(int)mappingPlane]] = pp1.y;
		}
	}



}
