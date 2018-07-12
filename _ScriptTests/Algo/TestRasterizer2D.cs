using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace x600d1dea.scene.test
{
	public class TestRasterizer2D : MonoBehaviour
	{
		SpriteRenderer spr;	
		Rasterizer2D.Target target;
		void OnDrawGizmos()
		{
			if (spr == null)
			{
				spr = GetComponent<SpriteRenderer>();
			}
			if (target == null)
			{
				target = Rasterizer2D.CreateTarget(0.2f);
			}
			spr.Rasterize(target, Plot);
		}

		void Plot(Vector2 point, Vector2 uv)
		{
			Gizmos.DrawWireCube(point, 0.05f * Vector3.one);
		}
	}
}
