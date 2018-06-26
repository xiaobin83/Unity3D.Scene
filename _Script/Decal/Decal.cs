#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;

namespace scene
{
	[ExecuteInEditMode]
	public class Decal : MonoBehaviour
	{
		const float kMinSize = 0.01f;

		public float offset = 0f;


		public Bounds localBounds
		{
			get
			{
				return new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
			}
		}
		public Vector3[] localBoundary
		{
			get
			{
				return localBounds.GetBoundary();
			}
		}

		// world space bounds
		public Bounds bounds
		{
			get
			{
				Vector3[] p = localBoundary;
				Bounds b = new Bounds(transform.TransformPoint(p[0]), Vector3.zero);
				for (int i = 1; i < p.Length; ++i)
				{
					b.Encapsulate(transform.TransformPoint(p[i]));
				}
				return b;
			}
		}

		MeshFilter meshFilter;
		MeshRenderer meshRenderer;

		public Mesh decalMesh;
		public Material decalMaterial;
		public Sprite decalSprite;

		public void CreateDecal(DecalSystem ds, float inSize, float inOffset, Material inMaterial, Sprite inSprite)
		{
			float size = Mathf.Max(inSize, kMinSize);
			offset = inOffset;
			decalMaterial = inMaterial;
			decalSprite = inSprite;
			transform.localScale = new Vector3(size, size, size);
			CreateDecalMesh(ds);
		}

		List<Vector3> GetVertexPositions(List<Vertex> vertices)
		{
			var positions = new List<Vector3>();
			for (int i = 0; i < vertices.Count; ++i)
			{
				positions.Add(vertices[i].vertex);
			}
			return positions;
		}

		Vector2[] GetVertexUV(List<Vertex> vertices)
		{
			var uvs = new Vector2[vertices.Count];
			for (int i = 0; i < vertices.Count; ++i)
			{
				uvs[i] = vertices[i].uv;
			}
			return uvs;
		}

		Vector2[] GetVertexUV2(List<Vertex> vertices)
		{
			var uvs = new Vector2[vertices.Count];
			for (int i = 0; i < vertices.Count; ++i)
			{
				uvs[i] = vertices[i].uv2;
			}
			return uvs;
		}

		Vector2[] GetVertexUV3(List<Vertex> vertices)
		{
			var uvs = new Vector2[vertices.Count];
			for (int i = 0; i < vertices.Count; ++i)
			{
				uvs[i] = vertices[i].uv3;
			}
			return uvs;
		}

		Vector2[] GetVertexUV4(List<Vertex> vertices)
		{
			var uvs = new Vector2[vertices.Count];
			for (int i = 0; i < vertices.Count; ++i)
			{
				uvs[i] = vertices[i].uv4;
			}
			return uvs;
		}



		public void CreateDecalMesh(DecalSystem ds)
		{
			ds.CheckQuadTree();


			var triangles = ds.qt.GetIntersectedTriangles(bounds);
			var verts = ds.qt.verts;
			List<Vertex> newVerts = new List<Vertex>();
			List<int> newTriangles = new List<int>();

			Dictionary<int /*old index*/, int /*new index*/> dict = new Dictionary<int, int>();

			for (int i = 0; i < triangles.Count; ++i)
			{
				int idx = triangles[i];
				var vert = verts[idx];
				vert.vertex = transform.InverseTransformPoint(vert.vertex);

				int newIdx = 0;
				if (!dict.TryGetValue(idx, out newIdx))
				{
					newVerts.Add(vert);
					newIdx = newVerts.Count - 1;
					dict.Add(idx, newIdx);
				}
				newTriangles.Add(newIdx);
			}

			List<int> clippedTriangles;
			List<Vertex> clippedVerts;

			Clip(newTriangles, newVerts, out clippedTriangles, out clippedVerts);

			// offset
			for (int i = 0; i < clippedVerts.Count; ++i)
			{
				var p = clippedVerts[i];
				p.vertex.y += offset;
				clippedVerts[i] = p;
			}


			List<Vector3> clippedVertPositions = GetVertexPositions(clippedVerts);
			List<Vector2> uv = MakeUV(clippedVertPositions);

			DestroyDecalMesh(alsoDestroyAsset: true);

			decalMesh = new Mesh();
			decalMesh.SetVertices(clippedVertPositions);
			decalMesh.SetUVs(0, uv);
			if (ds.takeUV)
			{
				switch (ds.takeUVChannelAsSecondary)
				{
					case 0:
					case 1:
						decalMesh.uv2 = GetVertexUV(clippedVerts);
						break;
					case 2:
						decalMesh.uv2 = GetVertexUV2(clippedVerts);
						break;
					case 3:
						decalMesh.uv2 = GetVertexUV3(clippedVerts);
						break;
					case 4:
						decalMesh.uv2 = GetVertexUV4(clippedVerts);
						break;
				}
			}
			decalMesh.SetTriangles(clippedTriangles, 0);
			decalMesh.RecalculateNormals();
			decalMesh.RecalculateBounds();

			CheckMeshRenderer();
			meshFilter.sharedMesh = decalMesh;
			meshRenderer.sharedMaterial = decalMaterial;
		}

		void CheckMeshRenderer()
		{
			meshFilter = gameObject.GetComponent<MeshFilter>();
			if (meshFilter == null)
			{
				meshFilter = gameObject.AddComponent<MeshFilter>();
			}

			meshRenderer = gameObject.GetComponent<MeshRenderer>();
			if (meshRenderer == null)
			{
				meshRenderer = gameObject.AddComponent<MeshRenderer>();
			}
		}

		void DestroyDecalMesh(bool alsoDestroyAsset)
		{
			if (decalMesh != null)
			{
				DestroyImmediate(decalMesh, alsoDestroyAsset);
				decalMesh = null;
			}
		}

		List<Vector2> MakeUV(IEnumerable<Vector3> verts)
		{

			var rect = decalSprite.textureRect;
			var invWidth = 1f / decalSprite.texture.width;
			var invHeight = 1f / decalSprite.texture.height;
			var u0 = rect.xMin * invWidth;
			var u1 = rect.xMax * invWidth;
			var uLength = u1 - u0;
			var v0 = rect.yMin * invHeight;
			var v1 = rect.yMax * invHeight;
			var vLength = v1 - v0;

			var uvs = new List<Vector2>();
			foreach (var p in verts)
			{
				var uv = new Vector2(u0 + (p.x + 0.5f) * uLength, v0 + (p.z + 0.5f) * vLength);
				uvs.Add(uv);
			}
			return uvs;
		}


		public void UpdateUV()
		{
			var uvs = MakeUV(decalMesh.vertices);
			decalMesh.SetUVs(0, uvs);
		}

		public void UpdateMaterial(Material material)
		{
			decalMaterial = material;
			CheckMeshRenderer();
			meshRenderer.sharedMaterial = decalMaterial;
		}


		/*
		 * 
		 *    3 +---------+ 2   +Z
		 *     /|        /|     .
		 *  7 +---------+6|    /|\
		 *    | |0      | |     | _ .  -Y
		 *    | +-------|-+ 1   |  / \
		 *    |/        |/      | /
		 *  4 +---------+ 5     |/
		 *            ----------/----------> +X
		 *                     /
		 *                    /
		 *                   /
		 *                  +Y
		 */

		void AddTriangle(List<int> newTriangles, List<Vertex> newVerts, Vertex p0, Vertex p1, Vertex p2)
		{
			int baseIndex = newVerts.Count;
			newVerts.Add(p0);
			newVerts.Add(p1);
			newVerts.Add(p2);
			newTriangles.Add(baseIndex);
			newTriangles.Add(baseIndex + 1);
			newTriangles.Add(baseIndex + 2);
		}



		void ClipAgainstLine(
			Plane plane,
			List<int> triangles, List<Vertex> verts,
			out List<int> clippedTriangles, out List<Vertex> clippedVerts)
		{
			List<int> newTriangles = new List<int>();
			List<Vertex> newVerts = new List<Vertex>();

			System.Action<Vertex, Vertex, Vertex> addTriangle = delegate (Vertex p0, Vertex p1, Vertex p2)
			{
				AddTriangle(newTriangles, newVerts, p0, p1, p2);
			};

			for (int i = 0; i < triangles.Count; i += 3)
			{
				int a = triangles[i];
				int b = triangles[i + 1];
				int c = triangles[i + 2];

				Vertex p0 = verts[a];
				Vertex p1 = verts[b];
				Vertex p2 = verts[c];

				bool b0 = plane.GetSide(p0.vertex);
				bool b1 = plane.GetSide(p1.vertex);
				bool b2 = plane.GetSide(p2.vertex);
				if (b0 && b1 && b2)
				{
					continue;
				}
				addTriangle(p0, p1, p2);
			}

			clippedTriangles = newTriangles;
			clippedVerts = newVerts;
		}

		void ClipTriangles(Plane plane, List<int> triangles, List<Vertex> verts, out List<int> clippedTriangles, out List<Vertex> clippedVerts)
		{
			var newTriangles = new List<int>();
			var newVerts = new List<Vertex>();
			System.Action<Vertex, Vertex, Vertex> addTriangle = delegate (Vertex p0, Vertex p1, Vertex p2)
			{
				AddTriangle(newTriangles, newVerts, p0, p1, p2);
			};
			for (int i = 0; i < triangles.Count; i += 3)
			{
				int a = triangles[i];
				int b = triangles[i + 1];
				int c = triangles[i + 2];

				Vertex p0 = verts[a];
				Vertex p1 = verts[b];
				Vertex p2 = verts[c];

				var triangle = new Triangle(p0, p1, p2);
				var tris = triangle.ClipBy(plane);
				if (tris != null)
				{
					foreach (var tr in tris)
					{
						var wtr = tr.GetWinding(Vector3.up);
						addTriangle(wtr.p0, wtr.p1, wtr.p2);
						//addTriangle(tr.p0, tr.p1, tr.p2);
					}
				}

			}
			clippedTriangles = newTriangles;
			clippedVerts = newVerts;
		}

		public bool doNotClipTriangle = false;

		void Clip(List<int> triangles, List<Vertex> verts, out List<int> clippedTriangles, out List<Vertex> clippedVerts)
		{
			Plane left = new Plane(Vector3.left, -localBounds.extents.x);
			Plane top = new Plane(Vector3.forward, -localBounds.extents.z);
			Plane right = new Plane(Vector3.right, -localBounds.extents.x);
			Plane bottom = new Plane(Vector3.back, -localBounds.extents.z);

			ClipAgainstLine(left, triangles, verts, out clippedTriangles, out clippedVerts);
			ClipAgainstLine(top, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);
			ClipAgainstLine(right, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);
			ClipAgainstLine(bottom, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);

			if (!doNotClipTriangle)
			{
				ClipTriangles(left, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);
				ClipTriangles(top, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);
				ClipTriangles(right, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);
				ClipTriangles(bottom, clippedTriangles, clippedVerts, out clippedTriangles, out clippedVerts);
			}

		}

		public bool showProjector = true;
		public bool showDecalMesh = false;

		public bool debugShowQuadTreeHit = false;


		void DrawHelpers()
		{
			Vector3[] p = localBoundary;
			if (showProjector)
			{
				for (int i = 0; i < p.Length; ++i)
				{
					p[i] = transform.TransformPoint(p[i]);
				}

				// top
				Gizmos.color = Color.green;
				Gizmos.DrawLine(p[4], p[5]);
				Gizmos.DrawLine(p[5], p[6]);
				Gizmos.DrawLine(p[6], p[7]);
				Gizmos.DrawLine(p[7], p[4]);

				// Positive plane
				Gizmos.color = Color.green * 0.5f;
				Vector3 d0 = p[0] - p[4];
				Vector3 d1 = p[1] - p[5];
				Vector3 d2 = p[2] - p[6];
				Vector3 d3 = p[3] - p[7];
				Gizmos.DrawLine(p[4], p[4] + d0);
				Gizmos.DrawLine(p[5], p[5] + d1);
				Gizmos.DrawLine(p[6], p[6] + d2);
				Gizmos.DrawLine(p[7], p[7] + d3);

				// Negative plane
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(p[4], p[4] - d0 * 0.5f);
				Gizmos.DrawLine(p[5], p[5] - d1 * 0.5f);
				Gizmos.DrawLine(p[6], p[6] - d2 * 0.5f);
				Gizmos.DrawLine(p[7], p[7] - d3 * 0.5f);
			}

			if (showDecalMesh)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireMesh(decalMesh, transform.position, transform.rotation, transform.localScale);
			}
		}

		void OnDrawGizmosSelected()
		{
			DrawHelpers();
			var ds = GetComponentInParent<DecalSystem>();
			if (ds != null)
			{
				ds.CheckQuadTree();
				if (debugShowQuadTreeHit)
				{
					List<Bounds> bs = ds.qt.GetIntersectedBounds(bounds);
					Gizmos.color = Color.red;
					foreach (var b in bs)
					{
						Gizmos.DrawWireCube(b.center, b.size);
					}
				}
			}


		}

	}
}

#endif