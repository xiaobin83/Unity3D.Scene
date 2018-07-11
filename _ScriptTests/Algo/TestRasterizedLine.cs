using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene.test
{
	public class TestRasterizedLine : MonoBehaviour
	{
		public GameObject start;
		public GameObject end;

		algo.RasterizedLine rLine;

		static Vector2 MapXZ(Vector3 p)
		{
			return new Vector2(p.x, p.z);
		}

		static Vector3 MapXZ3(Vector3 p)
		{
			return new Vector3(p.x, 0, p.z);
		}

		void OnDrawGizmos()
		{
			if (start == null || end == null)
				return;
			if (rLine == null)
			{
				rLine = new algo.RasterizedLine(
					MapXZ(start.transform.position),
					MapXZ(end.transform.position));
			}
			else
			{
				rLine.Update(
					MapXZ(start.transform.position),
					MapXZ(end.transform.position));
			}

			var bounds = rLine.bounds;
			Gizmos.color = Color.black;
			for (int x = bounds.x0; x <= bounds.x1; ++x)
			{
				Gizmos.DrawLine(new Vector3(x - 0.5f, 0, bounds.y0 - 0.5f), new Vector3(x - 0.5f, 0, bounds.y1 - 0.5f));
			}

			for (int y = bounds.y0; y <= bounds.y1; ++y)
			{
				Gizmos.DrawLine(new Vector3(bounds.x0 - 0.5f, 0, y - 0.5f), new Vector3(bounds.x1 - 0.5f, 0, y - 0.5f));
			}

			var iter = rLine.CreatePixelIterator();
			algo.RasterizedLine.Pixel p;
			Gizmos.color = Color.white;
			while (iter.Next(out p))
			{
				Gizmos.DrawWireCube(new Vector3(p.x, 0, p.y), Vector3.one);
			}

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(MapXZ3(start.transform.position), MapXZ3(end.transform.position));
		}
	}



}
