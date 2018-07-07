using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene
{
	public static class Rasterizer2D 
	{
		public enum PixelShape
		{
			Triangle,
			Rectangle,
			Lozenge,
		}

		public struct Pixel
		{
			public int index;
			public Vector2 pos;
			public Color color;
		}

		public class Buffer
		{
			public int pixelSize = 1;
			public Pixel[] pixels; // todo: pooled

			void BlitUpperTriangle(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, int minX, int minY) 
			{

			}
			void BlitLowerTriangle(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, int minX, int minY) 
			{

			}

			void BlitLine(int i0, int i1, Vector2[] verts, Vector2[] uv)
			{

			}

			void SortIndex(int i0, int i1, int i2, Vector2[] verts, out int outI0, out int outI1, out int outI2)
			{
				var v0 = verts[i0];
				var v1 = verts[i0];
				var v2 = verts[i0];

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

			void BlitInternal(int i0, int i1, int i2, Vector2[] verts, Vector2[] uv, int minX, int minY)
			{
				SortIndex(i0, i1, i2, verts, out i0, out i1, out i2);
				var v0 = verts[i0];
				var v1 = verts[i1];
				var v2 = verts[i2];
				var uv0 = uv[i0];
				var uv1 = uv[i0];
				var uv2 = uv[i0];
				if (v0.y == v1.y)
				{
					BlitLowerTriangle(v1, v2, v0, uv1, uv2, uv0, minX,minY);
				}
				else if (v1.y == v2.y)
				{
					BlitUpperTriangle(v0, v1, v2, uv0, uv1, uv2, minX, minY);
				}
				else
				{
					Vector2 v3 = Vector2.zero;
					Vector2 uv3 = Vector2.zero;
					BlitUpperTriangle(v0, v1, v3, uv0, uv1, uv3, minX, minY);
					BlitLowerTriangle(v1, v3, v2, uv0, uv3, uv2, minX, minY);
				}

			}

			void ToBlitSpace(Vector2[] verts)
			{
				for (int i = 0; i < verts.Length; ++i)
				{
					var v = verts[i];
					v.x = Mathf.Floor(v.x / pixelSize);
					v.y = Mathf.Floor(v.y / pixelSize);
					verts[i] = v;
				}
			}

			void ToBlitSpace(Bounds bounds, out int minX, out int minY, out int maxX, out int maxY)
			{
				var m = bounds.min;
				var M = bounds.max;
				var invPixelSize = 1f / pixelSize;
				minX = Mathf.FloorToInt(m.x * invPixelSize);
				minY = Mathf.FloorToInt(m.y * invPixelSize);
				maxX = Mathf.FloorToInt(M.x * invPixelSize);
				maxY = Mathf.FloorToInt(M.y * invPixelSize);
			}

			public void Blit(SpriteRenderer spr)
			{
				var sp = spr.sprite;

				var verts = sp.vertices;
				ToBlitSpace(verts);


				// bounds
				var bounds = MUtils.GetBounds(verts);
				int minX, minY, maxX, maxY;
				ToBlitSpace(bounds, out minX, out minY, out maxX, out maxY);

				// blit buffer
				int w = maxX - minX + 1;
				int h = maxY - minY + 1;
				pixels = new Pixel[w * h];

				var uv = sp.uv;

				// blit each triangle
				var tris = sp.triangles;
				for (int i = 0; i < tris.Length; i += 3)
				{
					var i0 = tris[i+0];
					var i1 = tris[i+1];
					var i2 = tris[i+2];
					BlitInternal(i0, i1, i2, verts, uv, minX, minY);
				}
			}
		}

		public static Buffer CreateBuffer(Rect rect, int pixelSize = 1)
		{
			Debug.Assert(pixelSize >= 1);
			return new Buffer()
			{
				pixelSize = pixelSize,
			};
		}



		public static void Rasterize(this SpriteRenderer spr, Buffer buffer)
		{
			buffer.Blit(spr);
		}

	}
}
