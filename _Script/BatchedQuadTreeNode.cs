using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif



namespace scene
{

	public class BatchedQuadTreeNode : MonoBehaviour
	{
		public static QualityLevel quality = QualityLevel.Best;


		public int depth;
		public Bounds bounds;
		// arraries below sorted by priority (StaticSceneHelper) when Batch()
		public string[] prefabs;
		public string[] prefabTags;
		public QualityLevel[] prefabAvailableQuality;

		[System.NonSerialized]
		public System.Action<GameObject, string/*tag*/> onGameObjectCreated = null;
		[System.NonSerialized]
		public System.Action<GameObject, string/*tag*/> onGameObjectWillDestroy = null;
		[System.NonSerialized]
		public System.Func<string/*tag*/, string/*path*/, bool /*create if true*/> onGameObjectWillCreate = null;


		GameObject[] loadedObjects;
		bool[] objectLoadingArranged;

		enum State
		{
			Unrealized,
			Loading,
			Realized,
			PartialRealized
		}
		State state = State.Unrealized;

		bool arrangedUnloading_ = false;
		float arrangedUnloadingTime = float.MaxValue;
		bool arrangedUnloading
		{
			get
			{
				return arrangedUnloading_;
			}
			set
			{
				if (!arrangedUnloading_)
				{
					if (value)
					{
						arrangedUnloadingTime = Time.realtimeSinceStartup + StaticScene.kUnloadingToleranceDuration;
					}
				}
				if (!value)
				{
					arrangedUnloadingTime = float.MaxValue;
				}
				arrangedUnloading_ = value;
			}
		}

		int combinedMeshRenderCount
		{
			get
			{
				return GetComponentsInChildren<MeshRenderer>().Length;
			}
		}

		int allMeshRendererCount
		{
			get
			{
				var mrs = transform.GetComponentsInChildren<MeshRenderer>();
				return mrs.Length;
			}
		}

		void Awake()
		{
			loadedObjects = new GameObject[prefabs.Length];
			objectLoadingArranged = new bool[prefabs.Length];
		}

		void OnDestroy()
		{
			StopAllCoroutines();
#if UNITY_EDITOR
			UnloadImmediate();
#endif
		}


		void LoadFromDisk(int index, string thisTag, string assetPath)
		{
			StartCoroutine(LoadFromDisk2_(index, thisTag, assetPath));
		}

		IEnumerator LoadFromDisk2_(int index, string thisTag, string assetPath)
		{
			var r = StaticSceneStreamingConfig.current.LoadFromDiskAsync(assetPath, reportError: true);
			while (!r.isDone) yield return null;
			if (r.asset != null)
			{
				var go = Instantiate(r.asset) as GameObject;
				go.transform.SetParent(transform, false);
				loadedObjects[index] = go;
				objectLoadingArranged[index] = false;
				if (onGameObjectCreated != null)
				{
					onGameObjectCreated(go, thisTag);
				}
			}
		}

		IEnumerator LoadFromDisk_(bool isPreview)
		{
			state = State.Loading;
			bool halfLoaded = false; // is true if some of batched node wasn't created due to onGameObjectWillCreate returning false
			for (int i = 0; i < prefabs.Length; ++i)
			{
				if (prefabAvailableQuality[i] > quality)
					continue;

				if (loadedObjects[i] != null)
					continue;

				var path = prefabs[i];
				var thisTag = prefabTags[i];
				if (onGameObjectWillCreate != null)
				{
					if (!onGameObjectWillCreate(thisTag, path))
					{
						halfLoaded = true;
						continue;
					}
				}

				var r = StaticSceneStreamingConfig.current.LoadFromDiskAsync(path, reportError: true);
				while (!r.isDone) yield return null;

				if (r.asset != null)
				{
					var go = Instantiate(r.asset) as GameObject;
					if (isPreview)
					{
						go.hideFlags = HideFlags.DontSaveInEditor;
					}
					go.transform.SetParent(transform, false);
					loadedObjects[i] = go;

					if (onGameObjectCreated != null)
					{
						onGameObjectCreated(go, thisTag);
					}
				}
			}
			if (halfLoaded)
				state = State.PartialRealized;
			else
				state = State.Realized;
		}

		public void Load(StaticScene scene)
		{
			arrangedUnloading = false;
			if (state == State.Unrealized
				|| state == State.PartialRealized)
			{
				state = State.Loading;

				bool halfLoaded = false;

				// collect and put to queue in static scene
				for (int i = 0; i < prefabs.Length; ++i)
				{
					if (prefabAvailableQuality[i] > quality)
						continue;

					if (loadedObjects[i] != null || objectLoadingArranged[i])
						continue;

					var path = prefabs[i];
					var thisTag = prefabTags[i];
					if (onGameObjectWillCreate != null)
					{
						if (!onGameObjectWillCreate(thisTag, path))
						{
							halfLoaded = true;
							continue;
						}
					}

					objectLoadingArranged[i] = true;

					scene.AddBatchedNodeToLoad(LoadFromDisk, i, thisTag, path);
				}

				if (halfLoaded)
					state = State.PartialRealized;
				else
					state = State.Realized;
			}
#if UNITY_EDITOR
			MarkToLoad();
#endif
		}

		public void LoadImmediate()
		{
#if UNITY_EDITOR
			UnloadImmediate();
#endif
			if (loadedObjects == null)
			{
				loadedObjects = new GameObject[prefabs.Length];
			}
			var e = LoadFromDisk_(isPreview: true);
			while (e.MoveNext()) ;
		}

#if UNITY_EDITOR
		public void UnloadImmediate()
		{
			if (loadedObjects != null)
			{
				for (int i = 0; i < loadedObjects.Length; ++i)
				{
					var go = loadedObjects[i];
					if (go != null)
						DestroyImmediate(go);
					loadedObjects[i] = null;
				}
			}
		}
#endif

		public void Unload()
		{
			if (state != State.Unrealized)
			{
				arrangedUnloading = true;
			}
#if UNITY_EDITOR
			MarkToUnload();
#endif
		}


		void Update()
		{
			if (arrangedUnloading)
			{
				if (state == State.Realized
					|| state == State.PartialRealized)
				{
					if (Time.realtimeSinceStartup >= arrangedUnloadingTime)
					{
						for (int i = 0; i < loadedObjects.Length; ++i)
						{
							var toUnload = loadedObjects[i];
							if (toUnload != null) // object may not be created since quality not fits
							{
								if (onGameObjectWillDestroy != null)
								{
									onGameObjectWillDestroy(toUnload, prefabTags[i]);
								}
								Destroy(toUnload);
							}
							loadedObjects[i] = null;
						}
						arrangedUnloading = false;
						state = State.Unrealized;
					}
				}
			}
		}







#if UNITY_EDITOR
		// for test
		enum Mark
		{
			Unknown,
			ToLoad,
			ToUnload,
		}
		Mark mark = Mark.Unknown;

		void MarkToLoad()
		{
			mark = Mark.ToLoad;
		}

		void MarkToUnload()
		{
			mark = Mark.ToUnload;
		}

		void OnDrawGizmosSelected()
		{
			if (StaticScene.drawBatchedNodeBounds)
			{
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireCube(bounds.center, bounds.size);
			}

			if (StaticScene.drawStatGizmos)
			{

				UnityEditor.Handles.color = Color.magenta;
				UnityEditor.Handles.DrawSolidDisc(bounds.center, Vector3.up, 1f);
				string str = "MR: " + combinedMeshRenderCount;
				str += "\n";
				str += "All MR: " + allMeshRendererCount;
				UnityEditor.Handles.Label(transform.position, str);
			}

			if (StaticScene.drawLoadingStatGizmos)
			{
				if (mark == Mark.ToLoad)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawWireCube(bounds.center, bounds.size);
				}
			}

		}
#endif

	}

}