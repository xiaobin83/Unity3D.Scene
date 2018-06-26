using UnityEngine;
using System.Collections;
using UnityEditor;

namespace scene
{

	[CustomEditor(typeof(scene.StaticScene))]
	public class StaticSceneEditor : Editor
	{

		bool stepByStep = false;
		string savePath = "./Resources/BatchedScene/";

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var staticScene = serializedObject.targetObject as StaticScene;


			EditorGUILayout.Separator();

			GUILayout.Label("Batched Node Count: " + staticScene.batchedNodeCount);
			GUILayout.Label("Batched Mesh Count: " + staticScene.meshesInTotal);
			GUILayout.Label("Triangles Culled: " + staticScene.trianglesCulled);
			GUILayout.Label("Vertex Removed: " + staticScene.vertexRemoved);

			EditorGUILayout.Separator();


			StaticScene.drawBatchedNodeBounds = GUILayout.Toggle(StaticScene.drawBatchedNodeBounds, "Draw Batched Node Bounds");
			StaticScene.drawStatGizmos = GUILayout.Toggle(StaticScene.drawStatGizmos, "Draw Stat Gizmos");
			StaticScene.drawLoadingStatGizmos = GUILayout.Toggle(StaticScene.drawLoadingStatGizmos, "Draw Loading Gizmos");

			StaticScene.doNotSave = GUILayout.Toggle(StaticScene.doNotSave, "Do Not Save");


			GUILayout.Label("DrawingDepth");
			int prevDrawingDepth = StaticScene.drawingDepth;
			StaticScene.drawingDepth = EditorGUILayout.IntSlider(StaticScene.drawingDepth, 0, 10);
			if (prevDrawingDepth != StaticScene.drawingDepth)
				SceneView.RepaintAll();

			if (Application.isPlaying)
			{
				if (GUILayout.Button("GC"))
				{
					System.GC.Collect();
					Resources.UnloadUnusedAssets();
				}
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("Preview"))
			{
				staticScene.Preview();
			}

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("Save To: " + savePath);
			stepByStep = GUILayout.Toggle(stepByStep, "Step By Step");
			if (!stepByStep)
			{
				if (GUILayout.Button("Compile"))
				{
					EditorUtils.CheckAndCreateDirectroy(savePath);
					staticScene.Compile(savePath);
					serializedObject.Update();
				}
			}
			else
			{
				if (GUILayout.Button("Prepare"))
				{
					EditorUtils.CheckAndCreateDirectroy(savePath);
					staticScene.PrepareAssetFolder(savePath);
				}
				if (staticScene.hasTarget && GUILayout.Button("Collect"))
				{
					staticScene.Collect(full: true);
					SceneView.RepaintAll();

				}
				if (staticScene.built && GUILayout.Button("Merge"))
				{
					staticScene.Merge();
					SceneView.RepaintAll();
				}
				if (staticScene.built && GUILayout.Button("Batch"))
				{
					staticScene.Batch();
					serializedObject.Update();
					SceneView.RepaintAll();
				}
				if (staticScene.built && GUILayout.Button("Save"))
				{
					staticScene.Save();
					serializedObject.Update();
				}

			}
		}
	}

}