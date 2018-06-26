using UnityEngine;
using System.Collections;
using UnityEditor;

namespace scene
{
	[CustomEditor(typeof(TiledBackground))]
	public class TiledBackgroundEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Save all tiles"))
			{
				var tiled = target as TiledBackground;

				if (tiled.allTilesMeshObj != null)
				{
					var mf = tiled.allTilesMeshObj.GetComponent<MeshFilter>();
					var meshFileName = AssetDatabase.GenerateUniqueAssetPath("Assets/AllTilesMesh.prefab");
					AssetDatabase.CreateAsset(mf.sharedMesh, meshFileName);
					var loadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshFileName);
					mf.sharedMesh = loadedMesh;
					var mr = tiled.allTilesMeshObj.GetComponent<MeshRenderer>();
					for (int i = 0; i < mr.sharedMaterials.Length; ++i)
					{
						var mtlFileName = AssetDatabase.GenerateUniqueAssetPath("Assets/TileMaterial.prefab");
						AssetDatabase.CreateAsset(mr.sharedMaterials[i], mtlFileName);
						var loadedMtl = AssetDatabase.LoadAssetAtPath<Material>(mtlFileName);
						mr.sharedMaterials[i] = loadedMtl;
					}
				}
			}
		}
	}
}
