#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;

namespace scene
{

	public class TriangleQuadTree : GenericQuadTree<int>
	{
		public const float kDefaultCellSize = 5f;

		public List<Vertex> verts { get; private set; }

		public bool hasUV1
		{
			get; private set;
		}
		public bool hasUV2
		{
			get; private set;
		}
		public bool hasUV3
		{
			get; private set;
		}
		public bool hasUV4
		{
			get; private set;
		}

		public TriangleQuadTree(Bounds inBounds, float inMinCellSize)
			: base(inBounds, inMinCellSize)
		{
			verts = new List<Vertex>();
		}

		public void AddMesh(Mesh sharedMesh, Transform transform)
		{
			AddTriangles(sharedMesh.triangles, sharedMesh.vertices, sharedMesh.uv, sharedMesh.uv2, sharedMesh.uv3, sharedMesh.uv4, transform);
		}

		void AddTriangles(
			int[] triangles,
			Vector3[] inVertices,
			Vector2[] uv, Vector2[] uv2, Vector2[] uv3, Vector2[] uv4,
			Transform transform)
		{
			int baseIdx = verts.Count;
			Vertex[] vertices = new Vertex[inVertices.Length];

			hasUV1 = (uv.Length == inVertices.Length);
			hasUV2 = (uv2.Length == inVertices.Length);
			hasUV3 = (uv3.Length == inVertices.Length);
			hasUV4 = (uv4.Length == inVertices.Length);

			for (int i = 0; i < inVertices.Length; ++i)
			{
				Vertex v = new Vertex();
				v.vertex = transform.TransformPoint(inVertices[i]);
				if (hasUV1)
				{
					v.uv = uv[i];
				}
				if (hasUV2)
				{
					v.uv2 = uv2[i];
				}
				if (hasUV3)
				{
					v.uv3 = uv3[i];
				}
				if (hasUV4)
				{
					v.uv4 = uv4[i];
				}
				vertices[i] = v;
			}

			for (int i = 0; i < triangles.Length; i += 3)
			{
				int a = triangles[i];
				int b = triangles[i + 1];
				int c = triangles[i + 2];
				Vector3 p0 = vertices[a].vertex;
				Vector3 p1 = vertices[b].vertex;
				Vector3 p2 = vertices[c].vertex;

				Bounds bn = new Bounds(p0, Vector3.zero);
				bn.Encapsulate(p1);
				bn.Encapsulate(p2);

				Node leaf = FindLeaf(root, bn, loose: 0f, depth: 0);
				leaf.objs.Add(baseIdx + a);
				leaf.objs.Add(baseIdx + b);
				leaf.objs.Add(baseIdx + c);
			}
			verts.AddRange(vertices);
		}


		public List<int> GetIntersectedTriangles(Bounds boundsToTest)
		{
			List<int> triangles = new List<int>();
			Traverse((Node n) => boundsToTest.Intersects(n.bounds),
					 (Node n) => triangles.AddRange(n.objs));
			return triangles;
		}

		public List<Bounds> GetIntersectedBounds(Bounds boundsToTest)
		{
			List<Bounds> bounds = new List<Bounds>();
			Traverse(
				(n) => boundsToTest.Intersects(n.bounds),
				(n) =>
				{
					if (n.objs.Count > 0)
						bounds.Add(n.bounds);
				});
			return bounds;
		}

		public void DrawGizmos()
		{
			Traverse(
				(n) => true,
				(n) =>
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireCube(n.bounds.center, n.bounds.size);
				});
		}


	}

}

#endif // UNITY_EDITOR
