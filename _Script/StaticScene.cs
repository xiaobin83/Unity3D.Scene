#define UNLOAD_AT_LOW_MEMORY

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace scene
{

	public class StaticScene : MonoBehaviour
	{
		public const float kUnloadingToleranceDuration = 1f;
		const float kQuadTreeCheckDuration = 0.5f;
		const float kMemoryFreeDuration = 5f;



		public const string kBatchedRoot = "BatchedRoot";
		public const string kBatched = "Batched";
		public const string kObjects = "Objects";
		public const string kCombined = "Combined";


		public float minCellSize = 10f;
		public float loose = 0.2f; // percentage of bounds of node



		// serialized in after target processed
		[System.Serializable]
		public class SNode
		{
			public Bounds bounds;
			public int index;
			public int parentIndexInList = -1;
			public int indexInList = -1;
			public GameObject associated;

			BatchedQuadTreeNode batchedNode_;
			public BatchedQuadTreeNode batchedNode
			{
				get
				{
					if (associated != null)
					{
						if (batchedNode_ == null)
						{
							batchedNode_ = associated.GetComponent<BatchedQuadTreeNode>();
						}
						return batchedNode_;
					}
					return null;
				}
			}
		}
		public List<SNode> nodes;

		GenericQuadTree<int> tree;

		Dictionary<string/*tag*/, GameObject> tagIndexedGameObjectsRoot;

		public static StaticScene current;

		void Awake()
		{
#if UNITY_EDITOR
			StopPreview();
#endif
			current = this;


			tagIndexedGameObjectsRoot = new Dictionary<string, GameObject>();


			Load();
		}

		void OnDestroy()
		{
			current = null;
		}

		void Load()
		{
			Debug.Assert(nodes.Count > 0);
			tree = new GenericQuadTree<int>(nodes[0].bounds, minCellSize);
			var createdNodes = new List<GenericQuadTree<int>.Node>();
			createdNodes.Add(tree.root);

			// setup root
			var sn = nodes[0];
			tree.root.associated = sn;
			if (sn.batchedNode != null)
			{
				sn.batchedNode.onGameObjectWillCreate = HandleGameObjectWillCreate;
				sn.batchedNode.onGameObjectCreated = HandleGameObjectCreated;
				sn.batchedNode.onGameObjectWillDestroy = HandleGameObjectWillDestroy;
			}

			for (int i = 1; i < nodes.Count; ++i)
			{
				sn = nodes[i];
				var n = tree.CreateNode(sn.bounds, null, sn.index);
				n.associated = sn;
				if (sn.batchedNode != null)
				{
					sn.batchedNode.onGameObjectWillCreate = HandleGameObjectWillCreate;
					sn.batchedNode.onGameObjectCreated = HandleGameObjectCreated;
					sn.batchedNode.onGameObjectWillDestroy = HandleGameObjectWillDestroy;
				}
				createdNodes.Add(n);
			}

			// setup structure
			for (int i = 0; i < createdNodes.Count; ++i)
			{
				sn = nodes[i];
				var n = createdNodes[i];
				if (sn.parentIndexInList != -1)
				{
					Debug.Assert(sn.parentIndexInList < createdNodes.Count);
					var parentN = createdNodes[sn.parentIndexInList];
					parentN.SetNode(n, loose);
				}
			}
		}

		static bool HandleGameObjectWillCreate(string tag, string path)
		{
			if (StaticSceneStreamingConfig.current != null)
			{
				if (StaticSceneStreamingConfig.current.IsTagExcluded(tag))
					return false;
			}
			if (current != null)
				return current.IsVisible(tag);
			return false;
		}

		static void HandleGameObjectCreated(GameObject go, string tag)
		{
			if (current != null)
			{
				GameObject objRoot = current.GetTaggedRoot(tag);
				go.transform.SetParent(objRoot.transform, true);
			}
		}

		static void HandleGameObjectWillDestroy(GameObject go, string tag)
		{
			// nothing here
		}

		struct LoadStub
		{
			public System.Action<int, string, string> loadFunc;
			public int assetIndex;
			public string assetTag;
			public string assetPath;
		}
		Stack<LoadStub>[] tagPriorityQueue;

		int GetTagPriority(string assetTag)
		{
			if (StaticSceneStreamingConfig.current != null)
			{
				return StaticSceneStreamingConfig.current.GetTagPriority(assetTag);
			}
			return 0;
		}

		int GetTagPrioritySize()
		{
			if (StaticSceneStreamingConfig.current != null)
			{
				return StaticSceneStreamingConfig.current.GetTagPrioritySize();
			}
			return 1;
		}

		void InitLoadPriorityQueue()
		{
			if (tagPriorityQueue == null)
			{
				var size = GetTagPrioritySize();
				tagPriorityQueue = new Stack<LoadStub>[size];
				for (int i = 0; i < tagPriorityQueue.Length; ++i)
				{
					tagPriorityQueue[i] = new Stack<LoadStub>();
				}
			}
		}

		public void AddBatchedNodeToLoad(System.Action<int, string, string> loadFunc, int assetIndex, string assetTag, string assetPath)
		{
			InitLoadPriorityQueue();
			var prio = GetTagPriority(assetTag);
			LoadStub L;
			L.loadFunc = loadFunc;
			L.assetIndex = assetIndex;
			L.assetTag = assetTag;
			L.assetPath = assetPath;
			tagPriorityQueue[prio].Push(L);
		}

		float nextLoadTime = 0;
		void Update()
		{
			if (tagPriorityQueue == null) return;
			if (Time.realtimeSinceStartup > nextLoadTime)
			{
				nextLoadTime = Time.realtimeSinceStartup + StaticSceneStreamingConfig.loadTimeDuration;
				for (int i = 0; i < tagPriorityQueue.Length; ++i)
				{
					var q = tagPriorityQueue[i];
					int loadCount = 0;
					while (q.Count > 0)
					{
						var stub = q.Pop();
						stub.loadFunc(stub.assetIndex, stub.assetTag, stub.assetPath);
						loadCount++;
						if (loadCount >= StaticSceneStreamingConfig.maxNumLoadBlockOnce)
						{
							return;
						}
					}
				}
			}
		}


		HashSet<BatchedQuadTreeNode> prev = new HashSet<BatchedQuadTreeNode>();

#if UNLOAD_AT_LOW_MEMORY
		HashSet<BatchedQuadTreeNode> outSights = new HashSet<BatchedQuadTreeNode>();
		bool shouldReleaseAllOutSights = false;
#endif

		float nextCheckTime = 0;
		float nextMemoryFreeTime = float.MaxValue;
		Vector3 cachedCamPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		public bool IsForceStop = false;

		void LateUpdate()
		{
			if (IsForceStop)
			{
				return;
			}

			if ((Camera.main != null && Time.realtimeSinceStartup > nextCheckTime))
			{
				nextCheckTime = Time.realtimeSinceStartup + kQuadTreeCheckDuration;

#if UNLOAD_AT_LOW_MEMORY
				if (!shouldReleaseAllOutSights)
#endif
				{
					if (MUtils.Approximately(Camera.main.transform.position, cachedCamPosition) && !StaticSceneStreamingConfig.current.IsForceUpdateOnce())
						return;
				}


				cachedCamPosition = Camera.main.transform.position;

				var bounds = Camera.main.GetFrustumBoundsBasedOnXZProjection(20f);
				var intersected = tree.GetIntersectedNodes(bounds);
				var cur = new HashSet<BatchedQuadTreeNode>();
				foreach (var i in intersected)
				{
					var sn = (SNode)i.associated;
					if (sn.batchedNode != null)
						cur.Add(sn.batchedNode);
				}
				bool hasUnloading = false;

#if UNLOAD_AT_LOW_MEMORY
				prev.ExceptWith(cur); // now prev contains nodes out sight
				outSights.ExceptWith(cur);
				outSights.UnionWith(prev);
				prev = cur;

				if (shouldReleaseAllOutSights)
				{
					shouldReleaseAllOutSights = false;
					foreach (var b in outSights)
					{
						hasUnloading = true;
						b.Unload();
					}
					outSights.Clear();
				}

				foreach (var b in cur)
				{
					b.Load(this);
				}
#else
			prev.ExceptWith(cur);
			foreach (var b in prev)
			{
				hasUnloading = true;
				b.Unload();
			}

			foreach (var b in cur)
			{
				b.Load();
			}
			prev = cur;
#endif



				if (hasUnloading && nextMemoryFreeTime == float.MaxValue)
				{
					nextMemoryFreeTime = Time.realtimeSinceStartup + kMemoryFreeDuration;
				}
			}

			if (Time.realtimeSinceStartup > nextMemoryFreeTime)
			{
				nextMemoryFreeTime = float.MaxValue;
			}
		}

		public void ReleaseAllNodesOutSight()
		{
			shouldReleaseAllOutSights = true;
		}

		Dictionary<string, bool> visibility = new Dictionary<string, bool>();

		bool IsVisible(string tag)
		{
			bool visib;
			if (visibility.TryGetValue(tag, out visib))
			{
				return visib;
			}
			return true;
		}

		GameObject GetTaggedRoot(string objTag)
		{
			GameObject objRoot;
			if (!tagIndexedGameObjectsRoot.TryGetValue(objTag, out objRoot))
			{
				objRoot = new GameObject(objTag);
				tagIndexedGameObjectsRoot.Add(objTag, objRoot);
				objRoot.transform.SetParent(transform, false);
			}
			return objRoot;
		}

		public void SetVisibility(string objTag, bool visib)
		{
			visibility[objTag] = visib;
			GameObject objRoot = GetTaggedRoot(objTag);
			objRoot.SetActive(visib);
		}


#if UNITY_EDITOR

		public static bool drawBatchedNodeBounds;
		public static bool drawStatGizmos;
		public static bool drawLoadingStatGizmos;
		public static bool doNotSave = false;
		public static int drawingDepth = 10;

		string savePath;
		const string saveSubPath = "Batched";

		public bool cullFaceNotTorwardsMainCamera;
		public float facingTolerance;
		public bool removeAllTangent = false;
		public bool removeUv3Uv4 = false;

		public GameObject[] targets;

		GameObject target
		{
			get
			{
				if (targets != null && targets.Length > 0)
					return targets[0];
				return null;
			}
			set
			{
				if (targets != null && targets.Length > 0)
					targets[0] = value;
			}
		}

		public int minObjectCount = 5;
		public string[] pathOfMeshAssetToBatch;

		GenericQuadTree<MeshRenderer> quadTree = null;

		public bool hasTarget
		{
			get
			{
				return target != null;
			}
		}

		public bool built
		{
			get
			{
				return hasTarget && quadTree != null;
			}
		}

		bool merged_ = false;
		public bool merged
		{
			get
			{
				return built && merged_;
			}
		}

		public void Collect(bool full = false)
		{
			quadTree = null;
			merged_ = false;

			MeshRenderer[] mrs = null;
			if (!full)
			{
				mrs = target.GetComponentsInChildren<MeshRenderer>();
			}
			else
			{
				var fullMrs = new List<MeshRenderer>();
				foreach (var t in targets)
				{
					var m = t.GetComponentsInChildren<MeshRenderer>();
					fullMrs.AddRange(m);
				}
				mrs = fullMrs.ToArray();
			}

			Bounds b = new Bounds();
			bool first = false;
			if (mrs != null)
			{
				foreach (var mr in mrs)
				{
					if (!first)
					{
						first = true;
						b = mr.bounds;
					}
					else
					{
						b.Encapsulate(mr.bounds);
					}
				}


				quadTree = new GenericQuadTree<MeshRenderer>(b, minCellSize, loose);
				foreach (var mr in mrs)
				{
					quadTree.Add(mr, mr.bounds);
				}
			}

		}


		public void Merge()
		{
			// merge leaf which's num of objs attached less than minObjectCount into parent
			do
			{
				// merge leaf which objs count < minObjectCount to its parent
				quadTree.Traverse(
					(n) => true,
					action: delegate { },
					postAction: (n) =>
					 {
						 if (n.objs.Count < minObjectCount)
						 {
							 if (n.parent != null)
							 {
								 n.parent.objs.AddRange(n.objs);
								 n.objs.Clear();
							 }
						 }
					 });

				var nodesToRemove = new List<GenericQuadTree<MeshRenderer>.Node>();

				// cleanup leaf	hasn't any obj or child node
				quadTree.Traverse(
					(n) => true,
					action: delegate { },
					postAction: (n) =>
					{
						if (n.objs.Count == 0 && !n.hasAnyChild)
						{
							nodesToRemove.Add(n);
						}
					});

				if (nodesToRemove.Count == 0) // nothing to remove -> objects not moved
					break;

				foreach (var n in nodesToRemove)
				{
					if (n.parent != null)
						n.parent.RemoveChild(n);
				}


			} while (true);

			merged_ = true;
		}


		static bool PathMatchesBatchList(string path, List<Regex> regex)
		{
			path = path.ToLower();
			foreach (var r in regex)
			{
				if (r.IsMatch(path))
					return true;
			}
			return false;
		}



		public void PrepareAssetFolder(string savePath)
		{
			this.savePath = savePath + "/" + saveSubPath;
			CleanUpAssetFolder();
		}

		void CleanUpAssetFolder()
		{
			if (doNotSave) return;

			var batchNameRegex = new Regex("/r_.*");

			var assets = AssetDatabase.FindAssets("", new string[] { savePath });
			foreach (var a in assets)
			{
				var path = AssetDatabase.GUIDToAssetPath(a);
				if (batchNameRegex.IsMatch(path)) // patch, delete combined batch only. (There are other things put together)
				{
					Debug.Log("Deleting " + path);
					AssetDatabase.DeleteAsset(path);
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		int saveIndex = 0;

		void SaveMesh(GameObject go, string name, string storePath)
		{
			if (doNotSave) return;
			if (string.IsNullOrEmpty(storePath))
			{
				storePath = savePath;
			}
			var mf = go.GetComponent<MeshFilter>();
			Debug.Assert(mf != null);
			var p = AssetDatabase.GetAssetPath(mf.sharedMesh);
			if (!string.IsNullOrEmpty(p))
			{
				mf.sharedMesh = Instantiate(mf.sharedMesh) as Mesh;
			}
			var path = AssetDatabase.GenerateUniqueAssetPath(storePath + "/" + name + "_" + saveIndex + "_.prefab");
			++saveIndex;
			AssetDatabase.CreateAsset(mf.sharedMesh, path);
		}

		string SavePrefab(GameObject obj, string name, string saveDir)
		{
			if (doNotSave) return name;
			if (string.IsNullOrEmpty(saveDir))
			{
				saveDir = savePath;
			}
			var path = "";
			path = AssetDatabase.GenerateUniqueAssetPath(saveDir + "/" + name + "_" + saveIndex + "_.prefab");
			++saveIndex;
			PrefabUtility.CreatePrefab(path, obj, ReplacePrefabOptions.ConnectToPrefab);
			path = path.Replace(saveDir + "/", "");
			path = saveSubPath + "/" + path.Replace(".prefab", "");
			return path;
		}


		// remove duplicated index
		static void Shrink(
			List<int> triangles,
			Vector3[] vertices, Vector3[] normals, Vector4[] tangents,
			Vector2[] uv, Vector2[] uv2, Vector2[] uv3, Vector2[] uv4,
			Color[] colors,
			out Vector3[] newVertices, out Vector3[] newNormals, out Vector4[] newTangents,
			out Vector2[] newUv, out Vector2[] newUv2, out Vector2[] newUv3, out Vector2[] newUv4,
			out Color32[] newColors32,
			out int[] newTriangles)
		{
			var LnewTriangles = new List<int>();
			var LnewVertices = new List<Vector3>();
			var LnewNormals = new List<Vector3>();
			var LnewTangents = new List<Vector4>();
			var LnewUv = new List<Vector2>();
			var LnewUv2 = new List<Vector2>();
			var LnewUv3 = new List<Vector2>();
			var LnewUv4 = new List<Vector2>();
			var LnewColors32 = new List<Color32>();

			var indSet = new Dictionary<int/*old ind*/, int/*new ind*/>();
			for (int i = 0; i < triangles.Count; ++i)
			{
				var oldInd = triangles[i];
				int newInd = -1;
				if (!indSet.TryGetValue(oldInd, out newInd))
				{
					// fetch a vertex and add to array
					newInd = LnewVertices.Count;

					LnewVertices.Add(vertices[oldInd]);

					if (normals != null && normals.Length > 0)
					{
						Debug.Assert(oldInd < normals.Length);
						LnewNormals.Add(normals[oldInd]);
					}

					if (tangents != null && tangents.Length > 0)
					{
						Debug.Assert(oldInd < tangents.Length);
						LnewTangents.Add(tangents[oldInd]);
					}

					if (uv != null && uv.Length > 0)
					{
						Debug.Assert(oldInd < uv.Length);
						LnewUv.Add(uv[oldInd]);
					}

					if (uv2 != null && uv2.Length > 0)
					{
						Debug.Assert(oldInd < uv2.Length);
						LnewUv2.Add(uv2[oldInd]);
					}

					if (uv3 != null && uv3.Length > 0)
					{
						Debug.Assert(oldInd < uv3.Length);
						LnewUv3.Add(uv3[oldInd]);
					}

					if (uv4 != null && uv4.Length > 0)
					{
						Debug.Assert(oldInd < uv4.Length);
						LnewUv4.Add(uv4[oldInd]);
					}

					if (colors != null && colors.Length > 0)
					{
						Debug.Assert(oldInd < colors.Length);
						var clr = colors[oldInd];
						var clr32 = new Color32((byte)(clr.r * 255), (byte)(clr.g * 255), (byte)(clr.b * 255), (byte)(clr.a * 255));
						LnewColors32.Add(clr32);
					}
					indSet.Add(oldInd, newInd);
				}
				LnewTriangles.Add(newInd);
			}

			newVertices = LnewVertices.ToArray();
			if (LnewNormals.Count > 0)
				newNormals = LnewNormals.ToArray();
			else
				newNormals = null;

			if (LnewTangents.Count > 0)
				newTangents = LnewTangents.ToArray();
			else
				newTangents = null;

			if (LnewUv.Count > 0)
				newUv = LnewUv.ToArray();
			else
				newUv = null;

			if (LnewUv2.Count > 0)
				newUv2 = LnewUv2.ToArray();
			else
				newUv2 = null;

			if (LnewUv3.Count > 0)
				newUv3 = LnewUv3.ToArray();
			else
				newUv3 = null;

			if (LnewUv4.Count > 0)
				newUv4 = LnewUv4.ToArray();
			else
				newUv4 = null;

			if (LnewColors32.Count > 0)
				newColors32 = LnewColors32.ToArray();
			else
				newColors32 = null;

			newTriangles = LnewTriangles.ToArray();
		}

		[System.NonSerialized]
		public int batchedNodeCount;
		[System.NonSerialized]
		public int meshesInTotal;
		[System.NonSerialized]
		public int trianglesCulled;
		[System.NonSerialized]
		public int vertexRemoved;
		void CullBackFace(GameObject g)
		{
			var mf = g.GetComponent<MeshFilter>();
			var triangles = mf.sharedMesh.triangles;
			var vertices = mf.sharedMesh.vertices;
			var normals = mf.sharedMesh.normals;
			var tangents = mf.sharedMesh.tangents;
			var uv = mf.sharedMesh.uv;
			var uv2 = mf.sharedMesh.uv2;
			var uv3 = mf.sharedMesh.uv3;
			var uv4 = mf.sharedMesh.uv4;
			var colors = mf.sharedMesh.colors;

			var toClip = new List<int>();
			var toKeep = new List<int>();
			for (int i = 0; i < triangles.Length; i += 3)
			{
				var ind0 = triangles[i];
				var ind1 = triangles[i + 1];
				var ind2 = triangles[i + 2];

				var p0 = vertices[ind0];
				var p1 = vertices[ind1];
				var p2 = vertices[ind2];

				var tr = new Triangle(p0, p1, p2);
				if (tr.IsFacing(Camera.main.transform.forward, facingTolerance)
					|| tr.IsFacing(Vector3.down))
				{
					toKeep.Add(ind0);
					toKeep.Add(ind1);
					toKeep.Add(ind2);
				}
				else
				{
					toClip.Add(ind0);
					toClip.Add(ind1);
					toClip.Add(ind2);
					++trianglesCulled;
				}
			}
			//  Remove unused (cliped) vertices.
			Vector3[] newVertices, newNormals;
			Vector4[] newTangents;
			Vector2[] newUv, newUv2, newUv3, newUv4;
			Color32[] newColors32;
			int[] newTriangles;

			// discard toClip

			Shrink(toKeep, vertices, normals, tangents, uv, uv2, uv3, uv4, colors,
				out newVertices, out newNormals, out newTangents,
				out newUv, out newUv2, out newUv3, out newUv4, out newColors32, out newTriangles);

			vertexRemoved += (vertices.Length - newVertices.Length);

			bool constructed = false;
			var mesh = new Mesh();
			try
			{
				mesh.vertices = newVertices;
				mesh.normals = newNormals;
				if (removeAllTangent)
				{
					mesh.tangents = null;
				}
				else
				{
					mesh.tangents = newTangents;
				}
				mesh.uv = newUv;
				mesh.uv2 = newUv2;
				if (removeUv3Uv4)
				{
					mesh.uv3 = null;
					mesh.uv4 = null;
				}
				else
				{
					mesh.uv3 = newUv3;
					mesh.uv4 = newUv4;
				}
				mesh.colors = null;
				mesh.colors32 = newColors32;  // convert to color32
				mesh.triangles = newTriangles;
				mesh.RecalculateBounds();
				mesh.Optimize();

				constructed = true;
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.ToString());
			}

			if (constructed)
				mf.sharedMesh = mesh;
		}


		List<GameObject> SplitSubMeshes(GameObject go)
		{
			var gos = new List<GameObject>();
			var mf = go.GetComponent<MeshFilter>();
			var mr = go.GetComponent<MeshRenderer>();
			var sh = go.GetComponent<StaticSceneHelper>();
			if (mr.sharedMaterials.Length == 1)
			{
				gos.Add(go);
			}
			else
			{
				Debug.Assert(mf.sharedMesh.subMeshCount == mr.sharedMaterials.Length);
				var vertices = mf.sharedMesh.vertices;
				var normals = mf.sharedMesh.normals;
				var tangents = mf.sharedMesh.tangents;
				var uv = mf.sharedMesh.uv;
				var uv2 = mf.sharedMesh.uv2;
				var uv3 = mf.sharedMesh.uv3;
				var uv4 = mf.sharedMesh.uv4;
				var colors = mf.sharedMesh.colors;

				for (int s = 0; s < mf.sharedMesh.subMeshCount; ++s)
				{
					var indices = mf.sharedMesh.GetIndices(s);
					var topo = mf.sharedMesh.GetTopology(s);


					var newIndices = new List<int>();
					var newVertices = new List<Vector3>();
					var newNormals = new List<Vector3>();
					var newTangents = new List<Vector4>();
					var newUv = new List<Vector2>();
					var newUv2 = new List<Vector2>();
					var newUv3 = new List<Vector2>();
					var newUv4 = new List<Vector2>();
					var newColors = new List<Color>();

					var bakedIndices = new Dictionary<int/*original ind*/, int/*new ind*/>();
					for (int i = 0; i < indices.Length; ++i)
					{
						var ind = indices[i];

						int bakedInd;
						if (!bakedIndices.TryGetValue(ind, out bakedInd))
						{
							// bake
							newVertices.Add(vertices[ind]);
							if (normals.Length > 0)
							{
								newNormals.Add(normals[ind]);
							}
							if (tangents.Length > 0)
							{
								newTangents.Add(tangents[ind]);
							}
							if (uv.Length > 0)
							{
								newUv.Add(uv[ind]);
							}
							if (uv2.Length > 0)
							{
								newUv2.Add(uv2[ind]);
							}
							if (uv3.Length > 0)
							{
								newUv3.Add(uv3[ind]);
							}
							if (uv4.Length > 0)
							{
								newUv4.Add(uv4[ind]);
							}
							if (colors.Length > 0)
							{
								newColors.Add(colors[ind]);
							}
							bakedInd = newVertices.Count - 1;
							newIndices.Add(bakedInd);
							bakedIndices.Add(ind, bakedInd);
						}
						else
						{
							// add the bakedInd
							newIndices.Add(bakedInd);
						}
					}
					var bakedMesh = new Mesh();
					bakedMesh.Clear();
					bakedMesh.vertices = newVertices.ToArray();
					if (newNormals.Count > 0)
					{
						bakedMesh.normals = newNormals.ToArray();
					}
					if (newTangents.Count > 0)
					{
						bakedMesh.tangents = newTangents.ToArray();
					}
					if (newUv.Count > 0)
					{
						bakedMesh.uv = newUv.ToArray();
					}
					if (newUv2.Count > 0)
					{
						bakedMesh.uv2 = newUv2.ToArray();
					}
					if (newUv3.Count > 0)
					{
						bakedMesh.uv3 = newUv3.ToArray();
					}
					if (newUv4.Count > 0)
					{
						bakedMesh.uv4 = newUv4.ToArray();
					}
					if (newColors.Count > 0)
					{
						bakedMesh.colors = newColors.ToArray();
					}
					bakedMesh.SetIndices(newIndices.ToArray(), topo, 0);

					bakedMesh.Optimize();

					// create new object
					var newGo = new GameObject(go.name + "_sub_" + s);
					newGo.transform.position = go.transform.position;
					newGo.transform.rotation = go.transform.rotation;
					newGo.transform.localScale = go.transform.lossyScale;
					newGo.transform.SetParent(go.transform.parent);

					var newMf = newGo.AddComponent<MeshFilter>();
					newMf.sharedMesh = bakedMesh;

					var newMr = newGo.AddComponent<MeshRenderer>();
					newMr.sharedMaterial = mr.sharedMaterials[s];
					newMr.shadowCastingMode = mr.shadowCastingMode;
					newMr.receiveShadows = mr.receiveShadows;

					newGo.tag = go.tag;
					newGo.layer = go.layer;

					if (sh != null)
					{
						var newSh = newGo.AddComponent<StaticSceneHelper>();
						newSh.objectTag = sh.objectTag;
						newSh.availableSetting = sh.availableSetting;
						newSh.priority = sh.priority;
						newSh.storePath = sh.storePath;
					}

					gos.Add(newGo);
				}
				DestroyImmediate(go);
			}
			return gos;
		}

		public void Batch()
		{
			trianglesCulled = 0;
			vertexRemoved = 0;

			saveIndex = 0;

			if (quadTree != null)
			{
				if (pathOfMeshAssetToBatch != null && pathOfMeshAssetToBatch.Length > 0)
				{
					// batch
					var batchedRoot = new GameObject(kBatchedRoot);
					batchedRoot.transform.SetParent(transform, false);
					var regex = new List<Regex>();
					foreach (var p in pathOfMeshAssetToBatch)
					{
						regex.Add(new Regex(p));
					}

					quadTree.Traverse(
						(n) => true,
						(n) =>
						{
							var toBatch = new List<GameObject>();
							n.objs.ForEach(
								(mr) =>
								{
									toBatch.Add(mr.gameObject);
								});
							if (toBatch.Count > 0)
							{
							// GameObject object node
							var go = new GameObject(kBatched);
								go.transform.position = n.bounds.center;
								go.transform.SetParent(batchedRoot.transform);
								n.associated = go;

								var bn = go.AddComponent<BatchedQuadTreeNode>();
								bn.bounds = n.bounds;
								bn.depth = n.depth;

							// material indexed mesh list set
							var toCombine = new Dictionary<Material, List<MeshFilter>>();
								var toCopy = new List<MeshFilter>();

							// Combine meshes with the same material into one (Batch)

							// Split sub meshes
							var splited = new List<GameObject>();
								foreach (var g in toBatch)
								{
									splited.AddRange(SplitSubMeshes(g));
								}
								toBatch = splited;


							// organize by objectTags?
							foreach (var g in toBatch)
								{
									var mf = g.GetComponent<MeshFilter>();
									var mr = mf.GetComponent<MeshRenderer>();
									if (mr.sharedMaterial == null)
									{
										Debug.LogWarningFormat("{0} has empty sharedMatrial", g.GetPathInScene());
										continue;
									}
									List<MeshFilter> mfList;
									toCombine.TryGetValue(mr.sharedMaterial, out mfList);
									if (mfList == null)
									{
										mfList = new List<MeshFilter>();
										toCombine.Add(mr.sharedMaterial, mfList);
									}
									mfList.Add(mf);
								}

								var combinedGos = new List<GameObject>();

								foreach (var kv in toCombine)
								{
									var combined = new GameObject(kCombined);
									combined.transform.SetParent(go.transform, false);

									var combinedMf = combined.AddComponent<MeshFilter>();
									combinedMf.sharedMesh = new Mesh();
									var combinedMeshRenderer = combined.AddComponent<MeshRenderer>();
									var combinedStaticSceneHelper = combined.AddComponent<StaticSceneHelper>();


									var mfsToCombine = kv.Value;

									combined.tag = mfsToCombine[0].gameObject.tag;
									combined.layer = mfsToCombine[0].gameObject.layer;


								// assuming mf has the same mtl attached the same MeshRender & StaticSceneHelper (which should not be correct)
								var mr = mfsToCombine[0].GetComponent<MeshRenderer>();
									combinedMeshRenderer.receiveShadows = mr.receiveShadows;
									combinedMeshRenderer.shadowCastingMode = mr.shadowCastingMode;


									var sh = mfsToCombine[0].GetComponentInParent<StaticSceneHelper>();
									if (sh != null)
									{
										combinedStaticSceneHelper.objectTag = sh.objectTag;
										combinedStaticSceneHelper.priority = sh.priority;
										combinedStaticSceneHelper.availableSetting = sh.availableSetting;
										combinedStaticSceneHelper.storePath = sh.storePath;
									}


									CombineInstance[] combine = new CombineInstance[mfsToCombine.Count];
									int i = 0;
									while (i < mfsToCombine.Count)
									{
										combine[i].mesh = mfsToCombine[i].sharedMesh;
										mfsToCombine[i].gameObject.transform.SetParent(combined.transform);
										combine[i].transform = combined.transform.worldToLocalMatrix * mfsToCombine[i].transform.localToWorldMatrix;
										DestroyImmediate(mfsToCombine[i].gameObject);  // destroy the original
									i++;
									}
									combinedMf.sharedMesh.CombineMeshes(combine);
									combinedMeshRenderer.sharedMaterial = kv.Key;

									combinedGos.Add(combined);
								}


							// cull backface
							if (cullFaceNotTorwardsMainCamera)
								{
									foreach (var g in combinedGos)
									{
										CullBackFace(g);
									}
								}

							// distribute large mesh to available sub nodes



							// Save
							var combinedMrs = go.GetComponentsInChildren<MeshRenderer>();

							// get loading preference
							var objectTags = new List<string>();
								var priority = new List<int>();
								var qualitySettings = new List<QualityLevel>();
								foreach (var m in combinedMrs)
								{
									var sh = m.GetComponent<StaticSceneHelper>();
									objectTags.Add(sh.objectTag);
									priority.Add(sh.priority);
									qualitySettings.Add(sh.availableSetting);
								}

							// save mesh
							foreach (var m in combinedMrs)
								{
									var sh = m.gameObject.GetComponent<StaticSceneHelper>();
									var storePath = string.Empty;
									if (sh != null)
									{
										storePath = sh.storePath;
									}
									SaveMesh(m.gameObject, n.path + "_" + m.gameObject.name + "_mesh", storePath);
								}


							// save gameobject prefab
							var prefabPath = new List<string>();
								foreach (var m in combinedMrs)
								{
									var sh = m.gameObject.GetComponent<StaticSceneHelper>();
								//								bool saveToDlc = false;
								var storePath = string.Empty;
									if (sh != null)
									{
										storePath = sh.storePath;
										DestroyImmediate(sh);
									}
									var path = SavePrefab(m.gameObject, n.path + "_" + m.gameObject.name, storePath);
									prefabPath.Add(path);
								}

							// remove object from scene
							if (!doNotSave)
								{
									foreach (var m in combinedMrs)
									{
										DestroyImmediate(m.gameObject);
									}
								}

							// sort
							var newPrefabPath = new List<string>();
								var newObjectTags = new List<string>();
								var newQualitySettings = new List<QualityLevel>();

								var index = new int[priority.Count];
								for (int i = 0; i < index.Length; ++i)
								{
									index[i] = i;
								}
								System.Array.Sort(index, (a, b) => priority[a] - priority[b]);
								for (int i = 0; i < index.Length; ++i)
								{
									newPrefabPath.Add(prefabPath[index[i]]);
									newObjectTags.Add(objectTags[index[i]]);
									newQualitySettings.Add(qualitySettings[index[i]]);
								}

								bn.prefabs = newPrefabPath.ToArray();
								bn.prefabTags = newObjectTags.ToArray();
								bn.prefabAvailableQuality = newQualitySettings.ToArray();

							} // toBatch.Count > 0
					});
					if (!doNotSave)
					{
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
					batchedNodeCount = batchedRoot.transform.childCount;
					meshesInTotal = batchedRoot.GetComponentsInChildren<MeshFilter>().Length;
				}
			}
		}

		public void Save()
		{
			nodes = new List<SNode>();
			// serialize quad tree
			quadTree.Traverse(
				(n) => true,
				action: (n) =>
				{
					var sn = new SNode();
					sn.bounds = n.bounds;
					sn.index = n.index;
					sn.indexInList = nodes.Count;
					sn.associated = (GameObject)n.associated;
					nodes.Add(sn);
					n.associated = sn;
				},
				postAction: (n) =>
				{
					var sn = (SNode)n.associated;
					if (n.parent != null)
					{
						var parentSn = (SNode)n.parent.associated;
						Debug.Assert(parentSn != null);
						sn.parentIndexInList = parentSn.indexInList;
					}
				});
		}

		public void Compile(string savePath)
		{
			var trans = transform.FindChild(kBatchedRoot);
			if (trans != null)
			{
				DestroyImmediate(trans.gameObject);
			}
			PrepareAssetFolder(savePath);
			Collect(full: true);
			Merge();
			Batch();
			Save();
			DestroyImmediate(target);
			targets = null;
		}

		bool isPreviewing = false;
		public void Preview()
		{
			if (!isPreviewing)
			{
				Load();
				tree.Traverse(
					(n) => true,
					(n) =>
					{
						var sn = (SNode)n.associated;
						if (sn.batchedNode != null)
							sn.batchedNode.LoadImmediate();
					});
				isPreviewing = true;
			}
		}

		void StopPreview()
		{
			if (tree != null)
			{
				tree.Traverse(
					(n) => true,
					(n) =>
					{
						var sn = (SNode)n.associated;
						if (sn.batchedNode != null)
							sn.batchedNode.UnloadImmediate();
					});
				tree = null;
			}
			isPreviewing = false;
		}


		void OnDrawGizmosSelected()
		{
			if (quadTree != null)
			{
				Gizmos.color = Color.cyan;
				quadTree.ForEachManagedObject(
					(mr) =>
					{
						if (mr != null && mr.gameObject.activeInHierarchy)
						{
							Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
						}
					});

				quadTree.Traverse(
					pred: (_) => true,
					action: (n) =>
					{
						if (n.depth > drawingDepth) return;


						if (n.objs.Count > 0)
						{
							if (n.objs.Count < minObjectCount)
							{
								UnityEditor.Handles.color = Color.red;
								UnityEditor.Handles.DrawSolidDisc(n.bounds.center, Vector3.up, 1f);
							}
							else
							{
								UnityEditor.Handles.color = Color.green;
								UnityEditor.Handles.DrawSolidDisc(n.bounds.center, Vector3.up, 0.5f);
							}
							UnityEditor.Handles.Label(n.bounds.center, n.objs.Count.ToString());

							Gizmos.color = Color.red;
							foreach (var mr in n.objs)
							{
								if (mr != null && mr.gameObject.activeInHierarchy)
									Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
							}
						}
						if (n.objs.Count > 0)
						{
							Gizmos.color = Color.green;
						}
						else
						{
							Gizmos.color = Color.yellow;
						}
						Gizmos.DrawWireCube(n.bounds.center, n.bounds.size);
					});
				if (Camera.main != null)
				{
					Camera.main.DrawFrustumXZProjectionGizmos();
				}


			}
		}
#endif



	}

}