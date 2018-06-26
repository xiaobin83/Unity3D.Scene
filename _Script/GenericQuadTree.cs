using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// loose quad tree
// on XZ-plane

namespace scene
{

	public class GenericQuadTree<T>
	{
		public const int kMaxDepth = 4;

		public class Node
		{
			public List<T> objs = new List<T>();
			public Node[] children;
			public Node parent;
			public Bounds bounds;
			public object associated = null;
			public int index = 0;
			public string path
			{
				get
				{
					if (parent != null)
					{
						return parent.path + "_" + index;
					}
					return "r";
				}
			}

			int cachedDepth_ = -1;
			public int depth
			{
				get
				{
					if (cachedDepth_ == -1)
					{
						if (parent != null)
							cachedDepth_ = parent.depth + 1;
						else
							cachedDepth_ = 0;
					}
					return cachedDepth_;
				}
			}


			public bool hasAnyChild
			{
				get
				{
					if (children != null)
					{
						foreach (var c in children)
						{
							if (c != null)
								return true;
						}
					}
					return false;
				}
			}

			public float maxEdge
			{
				get
				{
					return Mathf.Max(bounds.size.x, bounds.size.z);
				}
			}


			public Node(Bounds inBounds, Node inParent, int inIndex)
			{
				bounds = inBounds;
				parent = inParent;
				index = inIndex;
			}

			public Node GetSubNode(int s, float loose)
			{
				if (children == null)
				{
					children = new Node[4];
				}
				if (children[s] == null)
				{
					children[s] = new Node(GetSubBounds(s, loose), this, s);
				}
				return children[s];
			}

			public Bounds GetSubBounds(int s, float loose)
			{
				return MUtils.QuadTree_GetSubBounds(bounds, s, loose);
			}


			/* 
			 * +---+---+
			 * | 3 | 2 |
			 * +---+---+
			 * | 0 | 1 |
			 * +---+---+
			 */
			public bool BoundsInSub(int s, Bounds b, float loose)
			{
				return MUtils.QuadTree_IsBoundsInSub(bounds, s, b, loose);
			}

			public void RemoveChild(Node n)
			{
				var index = System.Array.IndexOf(children, n);
				Debug.Assert(index != -1);
				children[index] = null;
			}

			public void SetNode(Node n, float loose)
			{
				n.parent = this;
				if (children == null)
				{
					children = new Node[4];
				}
				children[n.index] = n;
#if UNITY_EDITOR
				var b = GetSubBounds(n.index, loose);
				Debug.Assert(MUtils.Approximately(n.bounds.min, b.min));
				Debug.Assert(MUtils.Approximately(n.bounds.max, b.max));
#endif
			}

			public void Detach(T obj)
			{
				objs.Remove(obj);
			}
		}

		float loose;
		float minCellSize;
		Bounds bounds;
		public Node root
		{
			get; private set;
		}

		public GenericQuadTree(Bounds inBounds, float inMinCellSize, float inLoose = 0f)
		{
			loose = inLoose;
			minCellSize = inMinCellSize;
			bounds = inBounds;
			bounds = bounds.Loose(inLoose);
			root = new Node(inBounds, null, 0);
		}

		public Node Add(T obj, Bounds b)
		{
			var leaf = FindLeaf(root, b, loose, 0);
			leaf.objs.Add(obj);
			return leaf;
		}


		public void ForEachManagedObject(System.Action<T> action)
		{
			Traverse(
				(n) => true,
				(n) => n.objs.ForEach(action));
		}

		public List<Node> GetIntersectedNodes(Bounds boundsToTest)
		{
			var nodes = new List<Node>();
			Traverse(
				(n) => boundsToTest.Intersects(n.bounds),
				(n) =>
				{
					nodes.Add(n);
				});
			return nodes;
		}

		protected Node FindLeaf(Node node, Bounds b, float loose, int depth)
		{
			if (node.maxEdge < minCellSize || depth >= kMaxDepth)
				return node;
			for (int i = 0; i < 4; ++i)
			{
				if (node.BoundsInSub(i, b, loose))
				{
					return FindLeaf(node.GetSubNode(i, loose), b, loose, depth + 1);
				}
			}
			return node;
		}


		public Node CreateNode(Bounds b, Node parent, int index)
		{
			return new Node(b, null, index);
		}

		public void Traverse(System.Predicate<Node> pred, System.Action<Node> action, System.Action<Node> postAction = null)
		{
			Traverse(root, pred, action, postAction);
		}

		void Traverse(Node node, System.Predicate<Node> pred, System.Action<Node> action, System.Action<Node> postAction = null)
		{
			if (node == null || !pred(node)) return;
			action(node);
			if (node.hasAnyChild)
			{
				for (int i = 0; i < node.children.Length; ++i)
				{
					Traverse(node.children[i], pred, action, postAction);
				}
			}
			if (postAction != null)
			{
				postAction(node);
			}
		}

	}
} // namespace common
