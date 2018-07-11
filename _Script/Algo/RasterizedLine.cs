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
			float x0 = start.x; 
			float y0 = start.y;
			float x1 = end.x;
			float y1 = end.y;

			bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
			if (steep)
			{
				MUtils.Swap(ref x0, ref y0);
				MUtils.Swap(ref x1, ref y1);
			}
			if (x0 > x1)
			{
				MUtils.Swap(ref x0, ref x1);
				MUtils.Swap(ref y0, ref y1);
			}

			float dx = x1 - x0;
			float dy = y1 - y0;
			float errDelta;
			if (dx == 0)
			{
				if (steep)
				{
					yield return new Pixel()
					{
						x = Mathf.FloorToInt(y0),
						y = Mathf.FloorToInt(x0),
					};
				}
				else
				{
					yield return new Pixel()
					{
						x = Mathf.FloorToInt(x0),
						y = Mathf.FloorToInt(y0),
					};
				}
				yield break;
			}
			else
			{
				errDelta = Mathf.Abs(dy / dx);
				int yDir = (int)Mathf.Sign(dy);

				float err = 0f;
				int y = Mathf.RoundToInt(y0);
				int xstart = Mathf.RoundToInt(x0);
				int xend = Mathf.RoundToInt(x1);

				if (steep)
				{
					for (int x = xstart; x <= xend; ++x) 
					{
						yield return new Pixel()
						{
							x = y,
							y = x,
						};
						err += errDelta;
						if (err >= 0.5f)
						{
							y += yDir;
							err -= 1f;
						}
					}
				}
				else
				{
					for (int x = xstart; x <= xend; ++x) 
					{
						yield return new Pixel()
						{
							x = x,
							y = y,
						};
						err += errDelta;
						if (err >= 0.5f)
						{
							y += yDir;
							err -= 1f;
						}
					}
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


		// Xiaolin Wu's line algorithm
		IEnumerator<Pixel> RasterizeAA()
		{
			float x0 = start.x;
			float y0 = start.y;
			float x1 = end.x;
			float y1 = end.y;

			bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
			if (steep)
			{
				MUtils.Swap(ref x0, ref y0);
				MUtils.Swap(ref x1, ref y1);
			}
			if (x0 > x1)
			{
				MUtils.Swap(ref x0, ref x1);
				MUtils.Swap(ref y0, ref y1);
			}

			float dx = x1 - x0;
			float dy = y1 - y0;
			float gradient;
			if (dx == 0)
			{
				gradient = 1;
			}
			else
			{
				gradient = dy / dx;
			}

			var xend = Mathf.Round(x0);
			var yend = y0 + gradient * (xend - x0);
			var xgap = RFPart(x0 + 0.5f);
			int xpxl1 = (int)xend; // this will be used in the main loop
			int ypxl1 = (int)IPart(yend);
			if (steep)
			{
				yield return new Pixel()
				{
					x = ypxl1,
					y = xpxl1,
					c = RFPart(yend) * xgap,
				};

				yield return new Pixel()
				{
					x = ypxl1 + 1,
					y = xpxl1,
					c = FPart(yend) * xgap,
				};
			}
			else
			{
				yield return new Pixel()
				{
					x = xpxl1,
					y = ypxl1,
					c = RFPart(yend)*xgap,
				};

				yield return new Pixel()
				{
					x = xpxl1,
					y = ypxl1 + 1,
					c = FPart(yend) * xgap,

				};
			}
			var intery = yend + gradient; // first y-intersection for the main loop

			// handle second endpoint
			xend = Round(x1);
		    yend = y1 + gradient * (xend - x1);
		    xgap = FPart(x1 + 0.5f);
		    int xpxl2 = (int)xend; //this will be used in the main loop
			int ypxl2 = (int)IPart(yend);
			if (steep)
			{
				yield return new Pixel()
				{
					x = ypxl2,
					y = xpxl2,
					c = RFPart(yend) * xgap,
				};

				yield return new Pixel()
				{
					x = ypxl2 + 1,
					y = xpxl2,
					c = RFPart(yend) * xgap,
				};
			}
			else
			{
				yield return new Pixel()
				{
					x = xpxl2,
					y = xpxl2 + 1,
					c = FPart(yend) * xgap,
				};

				yield return new Pixel()
				{
					x = xpxl2,
					y = ypxl2,
					c = RFPart(yend) * xgap,
				};
			}


			// main loop
			if (steep)
			{
				for (int x = xpxl1 + 1; x <= xpxl2 - 1; ++x)
				{
					yield return new Pixel()
					{
						x = (int)IPart(intery),
						y = x,
						c = RFPart(intery),
					};

					yield return new Pixel()
					{
						x = (int)IPart(intery)+1,
						y = x,
						c = FPart(intery),
					};
					intery += gradient;
				}
			}
			else
			{
				for (int x = xpxl1 + 1; x <= xpxl2 - 1; ++x)
				{
					yield return new Pixel()
					{
						x = x,
						y = (int)IPart(intery),
						c = RFPart(intery),
					};
					yield return new Pixel()
					{
						x = x,
						y = (int)IPart(intery) + 1,
						c = FPart(intery),
					};
					intery += gradient;
				}
			}
		}

		public PixelIterator CreatePixelIterator()
		{
			return new PixelIterator(Rasterize());
		}

		public PixelIterator CreatePixelIteratorAA()
		{
			return new PixelIterator(RasterizeAA());
		}


	}
}
