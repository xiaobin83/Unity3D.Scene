using UnityEngine;
using UnityEditor;

namespace scene
{

	[CustomEditor(typeof(PaintGeometry))]
	public class PaintGeometryInspector : Editor
	{

		const string kRootName = "_painted_geometry";
		const int kPaintLayer = 31;

		[MenuItem("Scene/Paint Geometry", true)]
		static bool HasPaintGeometry()
		{
			var go = Selection.activeGameObject;
			if (go == null) return false;

			var mf = go.GetComponent<MeshFilter>();
			if (mf == null) return false;

			return true;
		}

		[MenuItem("Scene/Paint Geometry")]
		static void PaintGeometry()
		{
			var go = Selection.activeGameObject;
			var mf = go.GetComponent<MeshFilter>();

			var root = new GameObject(kRootName);
			root.transform.SetParent(go.transform, false);
			var mc = root.AddComponent<MeshCollider>();
			mc.sharedMesh = mf.sharedMesh;

			root.AddComponent<PaintGeometry>();

			root.layer = kPaintLayer;

			Selection.activeGameObject = root;
		}

		float rescale = 1f;
		public override void OnInspectorGUI()
		{
			bool stopped = false;

			if (GUILayout.Button("Fix Height"))
			{
				var paint = target as PaintGeometry;
				for (int i = 0; i < paint.transform.childCount; ++i)
				{
					var c = paint.transform.GetChild(i);
					RaycastHit hit;
					if (Physics.Raycast(new Ray(c.position + Vector3.up * 100, -Vector3.up), out hit, 1000f, 1 << kPaintLayer))
					{
						c.position = hit.point;
					}
				}
			}

			rescale = EditorGUILayout.FloatField("Rescale", rescale);
			if (GUILayout.Button("Rescale Child"))
			{
				var paint = target as PaintGeometry;
				for (int i = 0; i < paint.transform.childCount; ++i)
				{
					var c = paint.transform.GetChild(i);
					if (rescale < 0.1) rescale = 0.1f;
					c.transform.localScale *= rescale;
				}
			}


			if (GUILayout.Button("Make Static"))
			{
				var paint = target as PaintGeometry;
				var newCloned = Instantiate(paint.gameObject) as GameObject;
				newCloned.name = "*static*" + paint.gameObject.name;

				var clonedPaint = newCloned.GetComponent<PaintGeometry>();
				DestroyImmediate(clonedPaint);
				var clonedMeshCollider = newCloned.GetComponent<MeshCollider>();
				DestroyImmediate(clonedMeshCollider);

				stopped = true;
			}

			if (!stopped)
			{
				dynamicObject = GUILayout.Toggle(dynamicObject, "Paint Dynamic Object");
				base.OnInspectorGUI();
			}

		}

		bool painting;
		Vector3 lastPaintPosition;
		bool dynamicObject = false;

		void StartPainting()
		{
			painting = true;
			lastPaintPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
		}

		void StopPainting()
		{
			painting = false;
		}

		void OnSceneGUI()
		{
			var paint = target as PaintGeometry;


			int controlId = GUIUtility.GetControlID(FocusType.Passive);


			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, 1000f, 1 << kPaintLayer))
			{
				if (paint.objectToPaint != null)
				{
					Handles.color = Color.green;
					Handles.DrawWireDisc(hit.point, hit.normal, paint.radius);
				}
				else
				{
					Handles.color = Color.red;
					Handles.Label(hit.point, "Please assign object to paint");
					Handles.DrawWireDisc(hit.point, hit.normal, paint.radius);
				}
				Handles.DrawLine(hit.point, hit.point + hit.normal);


				if (painting && paint.objectToPaint != null)
				{
					if ((lastPaintPosition - hit.point).magnitude > paint.distance)
					{
						lastPaintPosition = hit.point;
						Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
						if (paint.lockUp)
							rot = Quaternion.identity;
						GameObject go = GameObject.Instantiate(paint.objectToPaint, hit.point, rot) as GameObject;
						Undo.RegisterCreatedObjectUndo(go, "Paint Mesh");
						go.isStatic = !dynamicObject;
						go.transform.SetParent(paint.transform);
						go.transform.localRotation *= Quaternion.Euler(paint.initialRotation);
						if (paint.randomRot)
						{
							go.transform.localRotation *= Quaternion.Euler(Random.value * paint.rotation);
						}


						if (paint.randomScale)
						{
							Vector3 scale = go.transform.localScale;
							scale *= Random.Range(paint.scaleMin, paint.scaleMax);
							go.transform.localScale = scale;
						}
					}
				}
			}

			switch (Event.current.type)
			{
				case EventType.mouseDown:
					if (Event.current.button == 0)
					{
						GUIUtility.hotControl = controlId;
						StartPainting();
						Event.current.Use();
					}
					break;
				case EventType.mouseUp:
					if (Event.current.button == 0)
					{
						GUIUtility.hotControl = 0;
						StopPainting();
						Event.current.Use();
					}
					break;
			}


			SceneView.RepaintAll();
		}
	}


}