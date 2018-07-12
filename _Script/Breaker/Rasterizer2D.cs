using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene
{
	public static class Rasterizer2D 
	{
		public delegate void PlotDelegate(Vector2 point, Vector2 uv);

		public class Target
		{
			public struct Pixel
			{
				Vector2 point;
				Vector2 uv;
			}

			
			float pixelSize = 1;

			public Target(float pixelSize)
			{
				this.pixelSize = pixelSize;
			}

			static bool NextY(algo.RasterizedLine.PixelIterator iter, int y, out int x0, out int y0)
			{
				algo.RasterizedLine.Pixel p;
				while (iter.Next(out p))
				{
					if (y != p.y)
					{
						x0 = p.x;
						y0 = p.y;
						return true;
					}
				}
				x0 = 0;
				y0 = 0;
				return false;
			}

			static bool FindY(algo.RasterizedLine.PixelIterator iter, int y, out int x0)
			{
				algo.RasterizedLine.Pixel p;
				while (iter.Next(out p))
				{
					if (p.y == y)
					{
						x0 = p.x;
						return true;
					}

				}
				x0 = 0;
				return false;
			}

			/*
				   . v0
				  / \
			  v1 /___\ v2

			 */
			void BlitUpperTriangle(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, PlotDelegate plotDelegate) 
			{
				if (v1.x > v2.x)
					MUtils.Swap(ref v1, ref v2);

				var L1 = new algo.RasterizedLine(v1, v0);
				var i1 = L1.CreatePixelIterator();
				int y0 = int.MinValue;
				int x0;
				int x1;

				float dx = v2.x - v0.x;
				float dy = v0.y - v2.y;
				
				Vector2 newUv0 = Vector2.zero;
				Vector3 newUv1 = Vector2.zero;

				if (dy != 0)
				{
					while (NextY(i1, y0, out x0, out y0))
					{
						x1 = Mathf.RoundToInt(v2.x - dx*(y0 - v2.y)/dy);
						BlitHorizontalLine(x0, x1, y0, newUv0, newUv1, plotDelegate);
					}
				}
			}

			static void CacheYOnChanged(algo.RasterizedLine.PixelIterator iter, List<algo.RasterizedLine.Pixel> list)
			{
				algo.RasterizedLine.Pixel p;
				int y = int.MaxValue;
				while (iter.Next(out p))
				{
					if (y != p.y)
					{
						list.Add(p);
						y = p.y;
					}
				}
			}

			static bool CheckSequence(List<algo.RasterizedLine.Pixel> list1, List<algo.RasterizedLine.Pixel> list2)
			{
				if (list1.Count > 1)
				{
					return ((list1[0].y > list1[1].y) && (list2[0].y > list2[1].y))
						|| ((list1[0].y < list1[1].y) && (list2[0].y < list2[1].y));
				}
				return true;
			}

			/*
				v0  ----  v1
				    \  /
					 \/
					 v2
			 */
			void BlitTriangle(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, PlotDelegate plotDelegate) 
			{
				if (v0.x > v1.x)
					MUtils.Swap(ref v0, ref v1);

				var L1 = new algo.RasterizedLine(v0, v2);
				var L2 = new algo.RasterizedLine(v1, v2);
				var i1 = L1.CreatePixelIterator();
				var i2 = L2.CreatePixelIterator();

				Vector2 newUv0 = Vector2.zero;
				Vector3 newUv1 = Vector2.zero;

				var pList1 = stubs.utils.ListPool<algo.RasterizedLine.Pixel>.Get();
				var pList2 = stubs.utils.ListPool<algo.RasterizedLine.Pixel>.Get();

				CacheYOnChanged(i1, pList1);
				CacheYOnChanged(i2, pList2);
				
				if (CheckSequence(pList1, pList2))
				{
					for (int i = 0; i < pList1.Count; ++i)
					{
						var a = pList1[i];
						var b = pList2[i];
						BlitHorizontalLine(a.x, b.x, a.y, newUv0, newUv1, plotDelegate);
					}
				}
				else
				{
					for (int i = 0; i < pList1.Count; ++i)
					{
						var a = pList1[i];
						var b = pList2[pList2.Count - 1 - i];
						BlitHorizontalLine(a.x, b.x, a.y, newUv0, newUv1, plotDelegate);
					}
				}
				stubs.utils.ListPool<algo.RasterizedLine.Pixel>.Release(pList1);
				stubs.utils.ListPool<algo.RasterizedLine.Pixel>.Release(pList2);
			}

			void BlitHorizontalLine(int x0, int x1, int y, Vector2 uv0, Vector2 uv1, PlotDelegate plotDelegate)
			{
				if (x0 > x1)
					MUtils.Swap(ref x0, ref x1);
				float xx0, yy0;
				Vector2 newUv = Vector2.zero;
				for (int x = x0; x <= x1; ++x)
				{
					ToWorldSpace(x, y, out xx0, out yy0);
					plotDelegate(new Vector2(xx0, yy0), newUv);
				}
			}

			void SortIndex(int i0, int i1, int i2, Vector2[] verts, out int outI0, out int outI1, out int outI2)
			{
				var v0 = verts[i0];
				var v1 = verts[i1];
				var v2 = verts[i2];

				var minY = v0.y;
				var minIndex = i0;
				if (v1.y < minY)
				{
					minY = v1.y;
					minIndex = i1;
				}
				if (v2.y < minY)
				{
					minIndex = i2;
				}

				var maxY = v0.y;
				var maxIndex = i0;
				if (v1.y > maxY)
				{
					maxY = v1.y;
					maxIndex = i1;
				}
				if (v2.y > maxY)
				{
					maxIndex = i2;
				}

				outI0 = minIndex;
				outI2 = maxIndex;
				if (i0 != minIndex && i0 != maxIndex)
				{
					outI1 = i0;
				}
				else if (i1 != minIndex && i1 != maxIndex)
				{
					outI1 = i1;
				}
				else if (i2 != minIndex && i2 != maxIndex)
				{
					outI1 = i2;
				}
				else
				{
					outI1 = -1;
					Debug.Assert(false, "never get here");
				}
			}

			void BlitInternal(int i0, int i1, int i2, Vector2[] verts, Vector2[] uv, PlotDelegate plotDelegate)
			{
				SortIndex(i0, i1, i2, verts, out i0, out i1, out i2);
				var v0 = verts[i0];
				var v1 = verts[i1];
				var v2 = verts[i2];
				var uv0 = uv[i0];
				var uv1 = uv[i1];
				var uv2 = uv[i2];
				if (v0.y == v1.y)
				{
					Gizmos.color = Color.blue;
					BlitTriangle(v0, v1, v2, uv0, uv1, uv2, plotDelegate);
				}
				else if (v1.y == v2.y)
				{
					Gizmos.color = Color.red;
					BlitTriangle(v1, v2, v0, uv1, uv2, uv0, plotDelegate);
				}
				else
				{
					Gizmos.color = Color.yellow;
					var y = v1.y;
					var x = v0.x - (v0.y - y)/(v0.y - v2.y)*(v0.x - v2.x);
					var v3 = new Vector2(x, y);
					var uv3 = uv2 + (uv1 - uv2) * (v3 - v2).magnitude / (v0 - v2).magnitude;
					BlitTriangle(v1, v3, v0, uv1, uv3, uv0, plotDelegate);
					BlitTriangle(v1, v3, v2, uv1, uv3, uv2, plotDelegate);
				}

			}

			void ToBlitSpace(Vector2[] verts, Transform transform)
			{
				for (int i = 0; i < verts.Length; ++i)
				{
					var v = transform.TransformPoint(verts[i]);
					v.x = v.x / pixelSize;
					v.y = v.y / pixelSize;
					verts[i] = v;
				}
			}
			void ToWorldSpace(int x, int y, out float x0, out float y0)
			{
				x0 = x * pixelSize;
				y0 = y * pixelSize;
			}

			public void Blit(SpriteRenderer spr, PlotDelegate plotDelegate)
			{
				var sp = spr.sprite;

				var verts = sp.vertices;
				ToBlitSpace(verts, spr.transform);


				// bounds
				var uv = sp.uv;

				// blit each triangle
				var tris = sp.triangles;
				for (int i = 0; i < tris.Length; i += 3)
				{
					var i0 = tris[i+0];
					var i1 = tris[i+1];
					var i2 = tris[i+2];
					BlitInternal(i0, i1, i2, verts, uv, plotDelegate);
				}
			}
		}

		public static Target CreateTarget(float pixelSize = 1)
		{
			Debug.Assert(pixelSize >= 1);
			return new Target(pixelSize);
		}

		public static void Rasterize(this SpriteRenderer spr, Target target, PlotDelegate plotDelegate)
		{
			target.Blit(spr, plotDelegate);
		}

	}
}
