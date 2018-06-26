using UnityEngine;
using System.Collections;

namespace scene
{
	public static class EditorUtils
	{
		public static void CheckAndCreateDirectroy(string path)
		{
			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}
		}
	}
}
