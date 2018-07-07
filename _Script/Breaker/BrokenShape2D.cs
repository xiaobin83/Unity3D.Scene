using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene
{
	[RequireComponent(typeof(ParticleSystem))]
	public class BrokenShape2D : MonoBehaviour
	{
		public void Init(Rasterizer2D.Buffer buffer)
		{
			ParticleSystem.Particle p;
			ParticleSystem ps;
			for (int i = 0; i < buffer.pixels.Length; ++i)
			{
			}
		}
		public void Emmit()
		{

		}
	}
}

