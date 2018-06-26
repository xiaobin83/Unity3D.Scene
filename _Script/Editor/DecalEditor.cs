using UnityEngine;
using UnityEditor;

namespace scene
{
	[CustomEditor(typeof(Decal))]
	public class DecalEditor : Editor
	{
		SerializedProperty propOffset;
		SerializedProperty propSprite;
		SerializedProperty propCustomShader;

		void OnEnable()
		{
			propOffset = serializedObject.FindProperty("offset");
			propSprite = serializedObject.FindProperty("decalSprite");
		}


		bool showDetail = false;
		public override void OnInspectorGUI()
		{
			var decal = target as Decal;
			var ds = decal.GetComponentInParent<DecalSystem>();
			if (ds == null)
			{
				EditorGUILayout.HelpBox("Decal should be only place to the child of DecalSystem!", MessageType.Error);
				return;
			}
			EditorGUILayout.PropertyField(propOffset);
			EditorGUILayout.PropertyField(propSprite);
			serializedObject.ApplyModifiedProperties();



			var mtl = ds.FindMaterial(decal.decalMaterial.shader, decal.decalSprite);
			if (mtl == null)
			{
				EditorGUILayout.HelpBox("Sprite selected used different texture. It will create new material for rendering", MessageType.Warning);
				if (GUILayout.Button("Create New Material"))
				{
					mtl = ds.GetMaterial(decal.decalMaterial.shader, decal.decalSprite);
					decal.UpdateMaterial(mtl);
				}
			}
			if (GUI.changed)
			{
				decal.UpdateUV();
			}

			showDetail = EditorGUILayout.Toggle("Show Detail", showDetail);
			if (showDetail)
			{

				if (GUILayout.Button("Update Decal Mesh"))
				{
					decal.CreateDecalMesh(ds);
				}
				base.OnInspectorGUI();
			}


		}

		bool dragging = false;
		void OnSceneGUI()
		{
			var decal = target as Decal;
			var ds = decal.GetComponentInParent<DecalSystem>();

			if (Event.current.type == EventType.mouseDrag
				&& Event.current.button == 0)
			{
				dragging = true;
			}
			if (dragging)
			{
				decal.showDecalMesh = false;
				if (Event.current.type == EventType.mouseUp
					&& Event.current.button == 0)
				{
					dragging = false;
					if (ds != null)
					{
						decal.showDecalMesh = true;
						decal.CreateDecalMesh(ds);
					}
				}
			}
			if (Event.current.type == EventType.ValidateCommand
				&& Event.current.commandName == "UndoRedoPerformed")
			{
				decal.showDecalMesh = true;
				if (ds != null)
				{
					decal.CreateDecalMesh(ds);
				}
			}
		}

	}

}
