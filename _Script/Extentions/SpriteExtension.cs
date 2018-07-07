using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene
{

	static public class SpriteExtension
	{
		public class Extension 
		{
			Sprite sprite;

			public static readonly Bounds emptyBounds = new Bounds();

			public Extension(Sprite spr)
			{
				sprite = spr;
			}

			public Bounds localBounds
			{
				get
				{
					var verts = sprite.vertices;
					if (verts.Length > 0)
					{
						var b = new Bounds(verts[0], Vector2.zero);
						for (int i = 1; i < verts.Length; ++i)
						{
							b.Encapsulate(verts[i]);
						}
						return b;
					}
					return emptyBounds;
				}
			}
		}
		public static Extension GetSpriteExtension(this Sprite spr)
		{
			return new Extension(spr);
		}
	}
}
