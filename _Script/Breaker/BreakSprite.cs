using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace x600d1dea.scene
{
	using algo;
	using stubs.utils;

	public static class BreakSprite
	{
		const uint particleBufferSize = 256;

		public static void BreakToParticles(this SpriteRenderer spr, float pixelSize, Material material)
		{
			var points = ArrayPool<Vector3>.Get(particleBufferSize);
			spr.Rasterize(Rasterizer2D.CreateTarget(pixelSize), (point, uv) => {
				points.Push(point);
			});

			var go = new GameObject();
			var ps = go.AddComponent<ParticleSystem>();
			ps.Stop();

			var psmain = ps.main;
			psmain.loop = false;
			psmain.maxParticles = (int)points.length;
			psmain.duration = 20;
			psmain.gravityModifier = 1;

			var psemit = ps.emission;
			psemit.enabled = false;

			var pscol = ps.collision;
			pscol.enabled = true;
			pscol.mode = ParticleSystemCollisionMode.Collision2D;
			pscol.type = ParticleSystemCollisionType.World;
			pscol.bounce = 0.3f;
			pscol.lifetimeLoss = 0.1f;
			pscol.dampen = 0.1f;


			var psr = ps.GetComponent<ParticleSystemRenderer>();
			psr.material = material;



			ps.Emit((int)points.length);

			var particles = ArrayPool<ParticleSystem.Particle>.Get(points.length);
			particles.Resize(points.length);
			ps.GetParticles(particles.array);
			for (int i = 0; i < particles.length; ++i)
			{
				particles.array[i].position = points[i];
				particles.array[i].startSize = pixelSize; 
				particles.array[i].velocity = new Vector3(1, 1, 0) * Random.insideUnitCircle * 0.2f;
			}
			ps.SetParticles(particles.array, (int)particles.length);

			ps.Play();
			ArrayPool<Vector3>.Release(points);
			ArrayPool<ParticleSystem.Particle>.Release(particles);
		}
	}
}

