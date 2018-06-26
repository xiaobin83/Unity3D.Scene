#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;


namespace scene
{
	[RequireComponent(typeof(MeshCollider)), ExecuteInEditMode]
	public class DecalSystem : MonoBehaviour
	{


		[System.NonSerialized]
		public string[] shaders = new string[] {
			"Decal/Unlit/Default", "Decal/Unlit/Default Lightmap UV2", "Custom" };

		// creation	parameter
		public float size = 1f;
		public float offset = 0.05f;

		public Sprite sprite;

		public bool takeUV = false;
		[Range(1, 4)]
		public int takeUVChannelAsSecondary = 2;

		public int shaderIdx;
		public Shader predefinedShader
		{
			get
			{
				return Shader.Find(shaders[shaderIdx]);
			}
		}
		public Shader customShader;


		public bool usingCustomShader
		{
			get
			{
				return shaderIdx == shaders.Length - 1;
			}
		}

		public List<Material> materials;

		public Shader shader
		{
			get
			{
				if (usingCustomShader)
				{
					return customShader;
				}
				else
				{
					return predefinedShader;
				}
			}
		}


		public Material FindMaterial(Shader inShader, Sprite s)
		{
			if (materials == null) return null;

			Shader usedShader = inShader;
			int texId = Shader.PropertyToID("_BaseTexture");
			foreach (var m in materials)
			{
				if (m.shader == usedShader)
				{
					var tex = m.GetTexture(texId);
					if (tex == s.texture)
					{
						return m;
					}
				}
			}
			return null;
		}

		Material FindMaterial(int idx, Sprite s)
		{
			Shader usedShader = null;
			if (idx < shaders.Length - 1)
			{
				usedShader = Shader.Find(shaders[idx]);
			}
			return FindMaterial(usedShader, s);
		}


		Material FindMaterial()
		{
			return FindMaterial(shader, sprite);
		}

		public bool willCreateMaterial
		{
			get
			{
				return FindMaterial() == null;
			}
		}

		public Material GetMaterial(Shader inShader, Sprite inSprite)
		{
			if (materials == null) return null;

			var mtl = FindMaterial(inShader, inSprite);
			if (mtl == null)
			{
				mtl = new Material(inShader);
				mtl.SetTexture("_BaseTexture", inSprite.texture);
				materials.Add(mtl);
			}
			return mtl;
		}

		public Material GetMaterial()
		{
			return GetMaterial(shader, sprite);
		}

		public MeshCollider meshCollider
		{
			get
			{
				return GetComponent<MeshCollider>();
			}
		}

		public TriangleQuadTree qt { get; private set; }

		void Awake()
		{
			CreateQuadTree();
		}




		public void CheckQuadTree()
		{
			if (qt == null)
			{
				CreateQuadTree();
			}
		}

		public void CreateQuadTree()
		{
			Bounds bounds;
			if (gameObject.GetBoundsOfCollider<MeshCollider>(out bounds))
			{
				qt = new TriangleQuadTree(bounds, TriangleQuadTree.kDefaultCellSize);
				var mcs = GetComponentsInChildren<MeshCollider>();
				foreach (var mc in mcs)
				{
					qt.AddMesh(mc.sharedMesh, transform);
				}
			}
		}

		public void MakeStatic()
		{
			var newCloned = Instantiate(gameObject) as GameObject;
			newCloned.name = "*static*" + gameObject.name;
			var decals = newCloned.GetComponentsInChildren<Decal>();
			foreach (var d in decals)
			{
				DestroyImmediate(d);
			}
			var ds = newCloned.GetComponent<DecalSystem>();
			DestroyImmediate(ds);
			var m = newCloned.GetComponent<MeshCollider>();
			DestroyImmediate(m);
			newCloned.transform.SetParent(transform.parent, false);
		}

		public void CheckInvalidDecals()
		{
			var mfs = GetComponentsInChildren<MeshFilter>();
			foreach (var mf in mfs)
			{
				if (mf.sharedMesh == null)
				{
					var d = mf.GetComponent<Decal>();
					d.CreateDecalMesh(this);
				}
			}
		}

		public void UpdateAndSaveDecalMeshes(string savePath)
		{
			SaveDecalMeshes(savePath);

			var mfs = GetComponentsInChildren<MeshFilter>();
			foreach (var mf in mfs)
			{
				string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
				AssetDatabase.DeleteAsset(path);  // delete old one

				// update to new one
				var d = mf.GetComponent<Decal>();
				d.CreateDecalMesh(this);
				AssetDatabase.CreateAsset(mf.sharedMesh, path);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public void SaveDecalMeshes(string savePath)
		{
			CheckInvalidDecals();

			for (int i = 0; i < materials.Count; ++i)
			{
				var m = materials[i];
				if (m == null)
				{
					Debug.LogError("Found empty material, check detail!");
					continue;
				}
				string path = AssetDatabase.GetAssetPath(m);
				if (path.Length == 0)
				{
					path = AssetDatabase.GenerateUniqueAssetPath(savePath + "/mtl_decal.prefab");
					AssetDatabase.CreateAsset(m, path);
					materials[i] = m;
				}
			}

			var mfs = GetComponentsInChildren<MeshFilter>();
			foreach (var mf in mfs)
			{
				Mesh mesh;
				if (mf.sharedMesh == null) continue;
				string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
				if (path.Length == 0)
				{
					path = AssetDatabase.GenerateUniqueAssetPath(savePath + "/decal.prefab");
					mesh = mf.sharedMesh;
				}
				else
				{
					mesh = Instantiate(mf.sharedMesh) as Mesh;
					AssetDatabase.DeleteAsset(path);
					mf.sharedMesh = mesh;
				}
				AssetDatabase.CreateAsset(mesh, path);
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public bool debugDrawBounds = false;
		public bool showDecalIcon = true;
		void OnDrawGizmos()
		{
			if (showDecalIcon)
			{
				for (int i = 0; i < transform.childCount; ++i)
				{
					var dc = transform.GetChild(i);
					Gizmos.DrawIcon(dc.transform.position, "icon_decal.tga", true);
				}
			}
			if (debugDrawBounds)
			{
				if (qt != null)
				{
					qt.DrawGizmos();
				}
			}
		}

	}

}

#endif // UNITY_EDITOR
