using UnityEngine;
using System.Collections.Generic;

namespace scene
{

	public static class TransformExtention
	{
		public static void DestoryChildren(this Transform target)
		{
			var chs = new List<Transform>();
			for (int i = 0; i < target.childCount; ++i)
			{
				GameObject.Destroy(target.GetChild(i).gameObject);
			}
			target.DetachChildren();
		}
	}

}
