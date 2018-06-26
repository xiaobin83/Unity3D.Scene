using UnityEngine;
using System.Collections.Generic;
namespace scene
{
	public class TiledBackground : MonoBehaviour
	{
		public Material material;
		public bool showGizmos;

		Camera cam;
		int ID_MainTex;

		public interface TileProvider
		{
			bool showAllTiles { get; }
			Texture GetTile(int tileX, int tileY);
			void GetAllTiles(out int tileX0, out int tileY0, out int tileX1, out int tileY1);
		}

		public void SetCamera(Camera camera)
		{
			cam = camera;
		}

		public TileProvider tileProvider;


		Mesh CreateMesh(int xcount, int ycount, string name)
		{
			var mesh = new Mesh();
			mesh.name = name;
			mesh.hideFlags = HideFlags.DontSave;

			var vertices = new List<Vector3>();
			var uvs = new List<Vector2>();
			var triangles = new List<int[]>();
			int baseIndex = 0;
			for (int j = 0; j < ycount; ++j)
			{
				for (int i = 0; i < xcount; ++i)
				{
					// quad
					float x0 = i, x1 = i + 1;
					float z0 = j, z1 = j + 1;

					vertices.Add(new Vector3(x0, 0, z0));
					vertices.Add(new Vector3(x1, 0, z0));
					vertices.Add(new Vector3(x1, 0, z1));
					vertices.Add(new Vector3(x0, 0, z1));

					uvs.Add(new Vector2(0, 0));
					uvs.Add(new Vector2(1, 0));
					uvs.Add(new Vector2(1, 1));
					uvs.Add(new Vector2(0, 1));

					triangles.Add(new int[] {
						baseIndex + 0,
						baseIndex + 2,
						baseIndex + 1,
						baseIndex + 0,
						baseIndex + 3,
						baseIndex + 2});

					baseIndex += 4;
				}
			}

			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.subMeshCount = triangles.Count;
			for (int i = 0; i < triangles.Count; ++i)
			{
				mesh.SetTriangles(triangles[i], i);
			}
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			return mesh;
		}

		[HideInInspector][System.NonSerialized]
		public GameObject allTilesMeshObj;
		void CreateAllTiles()
		{
			if (tileProvider != null)
			{
				if (allTilesMeshObj != null)
				{
					Destroy(allTilesMeshObj);
				}

				int tileX0, tileY0, tileX1, tileY1;
				tileProvider.GetAllTiles(out tileX0, out tileY0, out tileX1, out tileY1);

				allTilesMeshObj = new GameObject("All Tiles Mesh Object");
				allTilesMeshObj.transform.SetParent(transform, false);

				var xcount = tileX1 - tileX0;
				var ycount = tileY1 - tileY0;
				var mesh = CreateMesh(xcount, ycount, "All Tiles Mesh");
				mesh.hideFlags = HideFlags.None;
				var materials = new Material[xcount * ycount];
				for (int j = 0; j < ycount; ++j)
				{
					for (int i = 0; i < xcount; ++i)
					{
						var index = i + j * xcount;
						materials[index] = new Material(material);
						materials[index].name = material.name + "(Clone)";
						materials[index].SetTexture(ID_MainTex, tileProvider.GetTile(i, j));
					}
				}

				var meshRenderer = allTilesMeshObj.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterials = materials;

				var meshFilter = allTilesMeshObj.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = mesh;
			}
		}


		Material[] materials;
		GameObject meshObj;
		int meshTileXCount;
		int meshTileYCount;
		void CreateMeshAndMaterial(int visibleTileXCount, int visibleTileYCount)
		{
			Debug.Assert(visibleTileXCount > 0 && visibleTileXCount > 0);
			Debug.Assert(material != null);
			meshTileXCount = visibleTileXCount;
			meshTileYCount = visibleTileYCount;

			if (meshObj != null)
			{
				Destroy(meshObj);
				materials = null;
				meshObj = null;
			}

			meshObj = new GameObject("Mesh Object");
			meshObj.hideFlags = HideFlags.DontSave;
			meshObj.transform.SetParent(transform, false);

			var mesh = CreateMesh(meshTileXCount, meshTileYCount, "Tiles Mesh");

			materials = new Material[meshTileXCount * meshTileYCount];
			for (int i = 0; i < materials.Length; ++i)
			{
				materials[i] = new Material(material);
				materials[i].name = materials[i].name + "(Clone)";
				materials[i].hideFlags = HideFlags.DontSave;
			}

			var meshRenderer = meshObj.AddComponent<MeshRenderer>();
			meshRenderer.hideFlags = HideFlags.DontSave;
			meshRenderer.sharedMaterials = materials;

			var meshFilter = meshObj.AddComponent<MeshFilter>();
			meshFilter.hideFlags = HideFlags.DontSave;
			meshFilter.sharedMesh = mesh;

		}
		void Awake()
		{
			ID_MainTex = Shader.PropertyToID("_MainTex");
		}


		int tileXCount, tileYCount;

		bool CheckCreateMeshAndMaterial()
		{
			int tileX0, tileY0, tileX1, tileY1;
			GetTileParams(out tileX0, out tileY0, out tileX1, out tileY1);
			var xcount = tileX1 - tileX0;
			var ycount = tileY1 - tileY0;
			if (tileXCount < xcount || tileYCount < ycount)
			{
				tileXCount = xcount;
				tileYCount = ycount;
				CreateMeshAndMaterial(tileXCount, tileYCount);
				return true;
			}
			return false;
		}

		void GetProjParams(Camera c, out Vector3 wCamMinOnPlane, out Vector3 wCamMaxOnPlane)
		{
			if (c.orthographic)
			{
				var camPos = c.transform.position;
				var size = (c.transform.up + c.transform.right * c.aspect) * c.orthographicSize;

				var camMin = camPos - size;
				var camMax = camPos + size;
				var plane = new Plane(transform.up, transform.position);
				float enter;
				var r = new Ray(camMin, c.transform.forward);
				plane.Raycast(r, out enter);
				camMin = r.GetPoint(enter);
				r = new Ray(camMax, c.transform.forward);
				plane.Raycast(r, out enter);
				camMax = r.GetPoint(enter);


				wCamMinOnPlane = new Vector3(
					Mathf.Min(camMin.x,	camMax.x),
					Mathf.Min(camMin.y,	camMax.y),
					Mathf.Min(camMin.z,	camMax.z));
				wCamMaxOnPlane = new Vector3(
					Mathf.Max(camMin.x,	camMax.x),
					Mathf.Max(camMin.y,	camMax.y),
					Mathf.Max(camMin.z,	camMax.z));
			}
			else
			{
				var corners = c.ProjectFrustumOnXZPlane();
				float minX, maxX, minY, maxY, minZ, maxZ;
				MUtils.MinMax(out minX, out maxX, corners[0].x, corners[1].x, corners[2].x, corners[3].x);
				MUtils.MinMax(out minY, out maxY, corners[0].y, corners[1].y, corners[2].y, corners[3].y);
				MUtils.MinMax(out minZ, out maxZ, corners[0].z, corners[1].z, corners[2].z, corners[3].z);
				wCamMinOnPlane = new Vector3(minX, minY, minZ);
				wCamMaxOnPlane = new Vector3(maxX, maxY, maxZ);
			}
		}

		void GetTileParams(out int tileX0, out int tileY0, out int tileX1, out int tileY1)
		{
			var c = cam;
			if (c == null)
			{
				c = Camera.main;
			}
			Vector3 camMinOnPlane, camMaxOnPlane;
			GetProjParams(c, out camMinOnPlane, out camMaxOnPlane);

			// to local
			var camMin = transform.InverseTransformPoint(camMinOnPlane);
			var camMax = transform.InverseTransformPoint(camMaxOnPlane);
			var min = new Vector3(
				Mathf.Min(camMin.x, camMax.x),
				Mathf.Min(camMin.y, camMax.y),
				Mathf.Min(camMin.z, camMax.z));
			var max = new Vector3(
				Mathf.Max(camMin.x, camMax.x),
				Mathf.Max(camMin.y, camMax.y),
				Mathf.Max(camMin.z, camMax.z));
			camMin = min;
			camMax = max;

			// to grid, using unit quad, so scale is the grid size
			tileX0 = Mathf.FloorToInt(camMin.x);
			tileY0 = Mathf.FloorToInt(camMin.z);
			tileX1 = Mathf.CeilToInt(camMax.x);
			tileY1 = Mathf.CeilToInt(camMax.z);
		}


		void RefreshTiles(int tileX0, int tileY0)
		{
			Debug.Assert(tileProvider != null);

			// TODO: opt
			for (int j = 0; j < meshTileYCount; ++j)
			{
				for (int i = 0; i < meshTileXCount; ++i)
				{
					var tile = tileProvider.GetTile(i + tileX0, j + tileY0);
					var index = i + j * meshTileXCount;
					materials[index].SetTexture(ID_MainTex, tile);
				}
			}
		}

		// update tile
		int observingTileX0 = int.MinValue, observingTileY0 = int.MinValue;
		void LateUpdate()
		{
			var c = cam;
			if (c == null)
			{
				c = Camera.main;
			}
			if (c == null) return;

			if (tileProvider != null)
			{
				var d = CheckCreateMeshAndMaterial();

				if (tileProvider.showAllTiles)
				{
					if (allTilesMeshObj == null)
					{
						CreateAllTiles();
					}
					if (allTilesMeshObj != null && !allTilesMeshObj.activeSelf)
					{
						allTilesMeshObj.SetActive(true);
					}
					if (meshObj.activeSelf)
					{
						meshObj.SetActive(false);
					}
				}
				else
				{
					if (!meshObj.activeSelf)
					{
						meshObj.SetActive(true);
					}

					int tileX0, tileY0, tileX1, tileY1;
					GetTileParams(out tileX0, out tileY0, out tileX1, out tileY1);
					if (d || (tileX0 != observingTileX0 || tileY0 != observingTileY0))
					{
						observingTileX0 = tileX0;
						observingTileY0 = tileY0;
						var p = new Vector3(observingTileX0, 0, observingTileY0);
						meshObj.transform.localPosition = p;
						RefreshTiles(tileX0, tileY0);
					}
				}
			}

		}

		void OnDrawGizmos()
		{
			if (showGizmos)
			{
				if (Application.isPlaying)
				{
					var c = cam;
					if (c == null)
					{
						c = Camera.main;
					}
					Vector3 camMinOnPlane, camMaxOnPlane;
					GetProjParams(c, out camMinOnPlane, out camMaxOnPlane);
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(camMinOnPlane, 0.2f);
					Gizmos.color = Color.blue;
					Gizmos.DrawSphere(camMaxOnPlane, 0.2f);

					int tileX0, tileY0, tileX1, tileY1;
					GetTileParams(out tileX0, out tileY0, out tileX1, out tileY1);
					Gizmos.color = Color.yellow;
					for (int j = tileY0; j < tileY1; j++)
					{
						for (int i = tileX0; i < tileX1; i++)
						{
							var p = transform.TransformPoint(new Vector3(i + 0.5f, 0, j + 0.5f));
							var s = transform.TransformVector(Vector3.one);
							Gizmos.DrawWireCube(p, s);
						}
					}
				}
			}
		}
	}
}