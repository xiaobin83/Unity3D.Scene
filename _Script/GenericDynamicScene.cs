using UnityEngine;
using System.Collections.Generic;


namespace scene
{
	public class GenericDynamicScene : MonoBehaviour
	{

		public enum NodeVisibility
		{
			Visible,
			Invisible
		}
		public class NodeAction
		{
			public System.Action<NodeVisibility> action;
			public Bounds bounds;
			public System.Action destroy_;
			public void Destroy()
			{
				if (destroy_ != null)
				{
					destroy_();
				}
			}
		}

		public Bounds bounds;
		public float minCellSize = 10f;
		public float loose = 0.2f;

		GenericQuadTree<NodeAction> tree;

		void CheckCreate()
		{
			if (tree == null)
			{
				tree = new GenericQuadTree<NodeAction>(bounds, minCellSize, loose);
			}
		}

		protected virtual void Awake()
		{
			CheckCreate();
		}


		float nextCheckTime = 0;
		public float nextCheckDur = 1f;
		Vector3 cachedCamPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

		HashSet<GenericQuadTree<NodeAction>.Node> prev = new HashSet<GenericQuadTree<NodeAction>.Node>();

		[System.NonSerialized]
		public bool forceStop = false;

		void LateUpdate()
		{
			if (forceStop)
			{
				return;
			}
			CheckAdd();
			if (Camera.main != null && Time.realtimeSinceStartup > nextCheckTime)
			{
				nextCheckTime = Time.realtimeSinceStartup + nextCheckDur;
				if (MUtils.Approximately(Camera.main.transform.position, cachedCamPosition)) return;
				cachedCamPosition = Camera.main.transform.position;
				var camBounds = Camera.main.GetFrustumBoundsBasedOnXZProjection(20);
				var intersected = tree.GetIntersectedNodes(camBounds);
				var cur = new HashSet<GenericQuadTree<NodeAction>.Node>();
				foreach (var i in intersected)
				{
					cur.Add(i);
				}
				int exceptNumBefore = prev.Count;
				prev.ExceptWith(cur);

				foreach (var b in prev)
				{
					foreach (var go in b.objs)
					{
						if (go != null)
						{
							go.action(NodeVisibility.Invisible);
						}
					}
				}
				foreach (var b in cur)
				{
					foreach (var go in b.objs)
					{
						if (go != null)
						{
							go.action(NodeVisibility.Visible);
						}
					}
				}

				prev = cur;
			}
		}

		List<NodeAction> toAdd = new List<NodeAction>();

		public NodeAction Add(System.Action<NodeVisibility> action, Bounds bounds)
		{
			var na = new NodeAction();
			na.action = action;
			na.bounds = bounds;
			toAdd.Add(na);
			return na;
		}

		void CheckAdd()
		{
			if (toAdd.Count > 0)
			{
				foreach (var na in toAdd)
				{
					var node = tree.Add(na, na.bounds);
					na.destroy_ = delegate
					{
						node.Detach(na);
					};
					na.action(NodeVisibility.Invisible);
				}
				toAdd.Clear();
			}
		}



#if UNITY_EDITOR
		public bool drawBounds = false;

		[Range(0, 4)]
		public int maxDrawingDepth = 1;

		void DrawBounds(Bounds b, int depth, Color color)
		{
			if (depth > maxDrawingDepth) return;
			if (Mathf.Max(b.size.x, b.size.z) < minCellSize) return;
			Gizmos.color = color;
			Gizmos.DrawWireCube(b.center, b.size);
			for (int i = 0; i < 4; ++i)
			{
				var cb = MUtils.QuadTree_GetSubBounds(b, i, loose);
				DrawBounds(cb, depth + 1, color * 0.9f);
			}
		}

		void OnDrawGizmosSelected()
		{
			if (drawBounds)
				DrawBounds(bounds, 0, Color.cyan);
			if (tree != null)
			{
				Gizmos.color = Color.cyan;
				tree.ForEachManagedObject(
					(na) =>
					{
						Gizmos.DrawWireCube(na.bounds.center, na.bounds.size);
					});
			}
		}

#endif

	}

}