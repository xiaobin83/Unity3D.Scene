using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace x600d1dea.scene.test
{
	[CustomEditor(typeof(TestRasterizer2D))]
	public class TestBreakSprite : Editor 
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Break to particles"))
			{
				var t = target as TestRasterizer2D;
				if (t != null)
				{
					t.GetComponent<SpriteRenderer>().BreakToParticles(1f, t.particleMaterial);
				}
			}
		}
	}
}
