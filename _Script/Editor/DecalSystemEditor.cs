using UnityEngine;
using UnityEditor;

namespace scene
{
	[CustomEditor(typeof(DecalSystem))]
	public class DecalSystemEditor : Editor
	{

		const string kRootName = "_decal_system";
		const string kDecalName = "_decal";
		const int kDecalLayer = 30;

		[MenuItem("Scene/Create Decal System", true)]
		static bool HasCreateDecalSystem()
		{
			var go = Selection.activeGameObject;
			if (go == null) return false;

			var mf = go.GetComponent<MeshFilter>();
			if (mf == null) return false;

			return true;
		}


		[MenuItem("Scene/Create Decal System")]
		static void CreateDecalSystem()
		{
			var go = Selection.activeGameObject;
			var mf = go.GetComponent<MeshFilter>();

			var root = new GameObject(kRootName);
			root.transform.SetParent(go.transform, false);

			var mc = root.AddComponent<MeshCollider>();
			mc.sharedMesh = mf.sharedMesh;

			root.AddComponent<DecalSystem>();


			root.layer = kDecalLayer;

			Selection.activeGameObject = root;
		}

		SerializedProperty propSize;
		SerializedProperty propOffset;
		SerializedProperty propSprite;
		SerializedProperty propShaderIdx;
		SerializedProperty propCustomShader;
		SerializedProperty propShowDecalIcon;
		SerializedProperty propTakeUV;
		SerializedProperty propTakeUVChannelAsSecondary;


		void OnEnable()
		{
			propSize = serializedObject.FindProperty("size");
			propOffset = serializedObject.FindProperty("offset");
			propSprite = serializedObject.FindProperty("sprite");
			propShaderIdx = serializedObject.FindProperty("shaderIdx");
			propCustomShader = serializedObject.FindProperty("customShader");
			propShowDecalIcon = serializedObject.FindProperty("showDecalIcon");
			propTakeUV = serializedObject.FindProperty("takeUV");
			propTakeUVChannelAsSecondary = serializedObject.FindProperty("takeUVChannelAsSecondary");
		}

		bool showDetail = false;
		bool canMakeStatic = false;
		bool allowMakeStatic = false;

		bool randomRotAlongYAxis = false;


		string storePath = "./Resources/Decal";

		public override void OnInspectorGUI()
		{
			showDetail = EditorGUILayout.Toggle("ShowDetail", showDetail);
			if (showDetail)
			{
				base.OnInspectorGUI();
			}


			var ds = target as DecalSystem;


			storePath = EditorGUILayout.TextField("Save Path", storePath);
			if (storePath.Length > 0)
			{
				if (GUILayout.Button("Update Decal Meshes"))
				{
					EditorUtils.CheckAndCreateDirectroy(storePath);
					ds.UpdateAndSaveDecalMeshes(storePath);
				}
				if (GUILayout.Button("Save Decal Meshes"))
				{
					EditorUtils.CheckAndCreateDirectroy(storePath);
					ds.SaveDecalMeshes(storePath);
				}
			}

			canMakeStatic = EditorGUILayout.Toggle("Make Static", canMakeStatic);
			if (canMakeStatic)
			{
				EditorGUILayout.HelpBox("<Make Static> will make decals cannot be changed. No UNDO.", MessageType.Warning);
				allowMakeStatic = EditorGUILayout.Toggle("Make Static Anyway", allowMakeStatic);
				if (allowMakeStatic && GUILayout.Button("Make Static"))
				{
					ds.MakeStatic();
					serializedObject.Update();
				}
			}

			EditorGUILayout.PropertyField(propShowDecalIcon);
			EditorGUILayout.PropertyField(propSize);
			EditorGUILayout.PropertyField(propOffset);
			randomRotAlongYAxis = EditorGUILayout.Toggle("Random Rotation Along Up Axis", randomRotAlongYAxis);
			EditorGUILayout.PropertyField(propTakeUV);
			if (propTakeUV.boolValue)
			{
				EditorGUILayout.PropertyField(propTakeUVChannelAsSecondary);
			}

			EditorGUILayout.PropertyField(propSprite);
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.LabelField("Shader");
				propShaderIdx.intValue = EditorGUILayout.Popup(propShaderIdx.intValue, ds.shaders);
			}
			EditorGUILayout.EndHorizontal();
			if (propShaderIdx.intValue == ds.shaders.Length - 1)
			{
				EditorGUILayout.PropertyField(propCustomShader);
			}
			if (ds.willCreateMaterial)
			{
				EditorGUILayout.HelpBox("Will Create New Material!", MessageType.Info);
			}
			serializedObject.ApplyModifiedProperties();


		}


		void OnSceneGUI()
		{

			if (!Event.current.control) return;
			var ds = target as DecalSystem;


			int controlId = GUIUtility.GetControlID(FocusType.Passive);
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 1000f, 1 << kDecalLayer))
			{
				Handles.color = Color.green;
				if (ds.sprite == null)
				{
					Handles.color = Color.red;
					Handles.Label(hit.point, "Please specific decal sprite!");
					Handles.DrawWireDisc(hit.point, hit.normal, ds.size * 0.5f);
					SceneView.RepaintAll();
					return;
				}

				if (ds.usingCustomShader && ds.customShader == null)
				{
					Handles.color = Color.red;
					Handles.Label(hit.point, "Please specific custom shader");
					Handles.DrawWireDisc(hit.point, hit.normal, ds.size * 0.5f);
					SceneView.RepaintAll();
					return;
				}

				Handles.color = Color.green;
				Handles.DrawWireDisc(hit.point, hit.normal, ds.size * 0.5f);

				if (Event.current.type == EventType.mouseDown
					&& Event.current.button == 0)
				{
					GUIUtility.hotControl = controlId;
					Event.current.Use();

					var go = new GameObject(kDecalName);
					Undo.RegisterCreatedObjectUndo(go, "Create Decal");

					go.transform.SetParent(ds.transform);
					go.transform.position = hit.point;
					go.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
					if (randomRotAlongYAxis)
					{
						go.transform.localRotation *= Quaternion.Euler(0f, Random.value * 360f, 0f);
					}
					var decal = go.AddComponent<Decal>();
					decal.CreateDecal(ds, ds.size, ds.offset, ds.GetMaterial(), ds.sprite);
					serializedObject.Update();
				}
			}

			if (Event.current.type == EventType.mouseUp
				&& Event.current.button == 0)
			{
				GUIUtility.hotControl = 0;
				Event.current.Use();
			}

			SceneView.RepaintAll();
		}
	}
}
