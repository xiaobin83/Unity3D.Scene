using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene.algo
{

	public class RasterizedLine
	{
		Vector2 start;
		Vector2 end;

		public struct Pixel
		{
			public int x, y;
			public float c;
		}
		public struct Bounds
		{
			public int x0, y0;
			public int x1, y1;
		}

		public class PixelIterator
		{
			IEnumerator<Pixel> iter;
			public PixelIterator(IEnumerator<Pixel> iter)
			{
				this.iter = iter;
			}

			public bool Next(out Pixel p)
			{
				var r = iter.MoveNext();
				p = iter.Current;
				return r;
			}
			
		}

		public Bounds bounds
		{
			get
			{
				int x0 = Mathf.RoundToInt(Mathf.Min(start.x, end.x));
				int y0 = Mathf.RoundToInt(Mathf.Min(start.y, end.y));
				int x1 = Mathf.RoundToInt(Mathf.Max(start.x, end.x));
				int y1 = Mathf.RoundToInt(Mathf.Max(start.y, end.y));
				return new Bounds()
				{
					x0 = x0, y0 = y0, x1 = x1+1, y1 = y1+1,
				};
			}
		}

		public RasterizedLine(Vector2 start, Vector2 end)
		{
			this.start = start;
			this.end = end;
		}

		public void Update(Vector2 start, Vector2 end)
		{
			this.start = start;
			this.end = end;
		}

		// Bresenham's line algorithm
		IEnumerator<Pixel> Rasterize()
		{
			int x0 = Mathf.RoundToInt(start.x);
			int y0 = Mathf.RoundToInt(start.y);
			int x1 = Mathf.RoundToInt(end.x);
			int y1 = Mathf.RoundToInt(end.y);

			bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
			if (steep)
			{
				MUtils.Swap(ref x0, ref y0);
				MUtils.Swap(ref x1, ref y1);
			}
			if (x1 < x0)
			{
				MUtils.Swap(ref x0, ref x1);
				MUtils.Swap(ref y0, ref y1);
			}

			int dy = y1 - y0;
			int dx = x1 - x0;
			int yi = 1;
			if (dy < 0)
			{
				yi = -1;
				dy = -dy;
			}
			int D = 2*dy - dx;
			int y = y0;

			if (steep)
			{
				for (int x = x0; x <= x1; ++x)
				{
					yield return new Pixel()
					{
						x = y, y = x, c = 1f,
					};
					if (D > 0)
					{
						y = y + yi;
						D = D - 2*dx;
					}
					D = D + 2*dy;
				}
			}
			else
			{
				for (int x = x0; x <= x1; ++x)
				{
					yield return new Pixel()
					{
						x = x, y = y, c = 1f,
					};
					if (D > 0)
					{
						y = y + yi;
						D = D - 2*dx;
					}
					D = D + 2*dy;
				}
			}
		}



		// integer part of x
		static float IPart(float x)
		{
			return Mathf.Floor(x);
		}

		static float Round(float x)
		{
			return IPart(x + 0.5f);
		}

		// fractional part of x
		static float FPart(float x)
		{
			return x - Mathf.Floor(x);
		}

		static float RFPart(float x)
		{
			return 1f - FPart(x);
		}

		public PixelIterator CreatePixelIterator()
		{
			return new PixelIterator(Rasterize());
		}

	}
}
