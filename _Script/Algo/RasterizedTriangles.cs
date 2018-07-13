using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace x600d1dea.scene.algo
{
	using stubs.utils;

	public class RasterizedTriangles : IDisposable
	{
		public enum AdditionalStream 
		{
			UV		= 1 << 0,
			Color	= 1 << 1,
		}
		NativeArray<Vector3> verts;
		NativeArray<Vector2> uv;
		NativeArray<Color> color;

		AdditionalStream additionalStream;

		public RasterizedTriangles(AdditionalStream additionalStream)
		{
			this.additionalStream = additionalStream;
		}
		
		public void AddMesh(Mesh mesh)
		{
			var triangles = mesh.triangles;


		}

		public void AddTriangles(Vector3[] verts, Vector2[] uv, Color[] color, ushort[] triangles)
		{

		}

		public void Dispose()
		{
	
		}
	}

}
