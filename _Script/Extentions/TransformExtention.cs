using UnityEngine;
using System.Collections.Generic;

namespace scene
{

	public static class TransformExtention
	{
		public static void DestoryChildren(this Transform target)
		{
			var chs = new List<GameObject>();
			for (int i = 0; i < target.childCount; ++i)
			{
				chs.Add(target.GetChild(i).gameObject);
			}
			target.DetachChildren();
			foreach (var ch in chs)
			{
				GameObject.Destroy(ch);
			}
		}
	}

}
