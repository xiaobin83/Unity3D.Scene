using UnityEngine;

namespace scene
{
	public static class GameObjectExtention
	{
		public static bool GetBoundsOfRenderer(this GameObject target, out Bounds bounds)
		{
			var sizeFound = false;
			var renderers = target.GetComponentsInChildren<Renderer>();
			var b = new Bounds();
			foreach (var r in renderers)
			{
				if (!sizeFound)
				{
					b = r.bounds;
				}
				else
				{
					b.Encapsulate(r.bounds);
				}
				sizeFound = true;
			}
			bounds = b;
			return sizeFound;

		}

		public static bool GetBoundsOfCollider<T>(this GameObject target, out Bounds bounds) where T : Collider
		{
			var sizeFound = false;
			var colliders = target.GetComponentsInChildren<Collider>();
			var b = new Bounds();
			foreach (var c in colliders)
			{
				if (!sizeFound)
				{
					b = c.bounds;
				}
				else
				{
					b.Encapsulate(c.bounds);
				}
				sizeFound = true;
			}
			bounds = b;
			return sizeFound;
		}

		public static string GetPathInScene(this GameObject go)
		{
			string name = "";
			var g = go.transform;
			while (g != null)
			{
				name = g.name + "/" + name;
				g = g.transform.parent;
			}
			if (name.Length > 0)
			{
				name = name.Remove(name.Length - 1);
			}
			return name;
		}

		public static void ForEachGameObjectInHierarchy(this GameObject go, System.Action<GameObject> action, bool exceptThisObject = false)
		{
			if (!exceptThisObject)
				action(go);
			for (int i = 0; i < go.transform.childCount; ++i)
			{
				var t = go.transform.GetChild(i);
				t.gameObject.ForEachGameObjectInHierarchy(action, exceptThisObject: false);
			}
		}
	}
}