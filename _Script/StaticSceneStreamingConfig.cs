using UnityEngine;

namespace scene
{
	public interface IAssetRequest
	{
		Object asset { get; }
		bool isDone { get; }
	}

	public abstract class StaticSceneStreamingConfig : MonoBehaviour
	{
		public static int maxNumLoadBlockOnce = 2;
		public static float loadTimeDuration = 0.1f;

		public string[] tagPriority;

		public string[] excludingTags;

		public static StaticSceneStreamingConfig current;
		void Awake()
		{
			current = this;
		}

		void OnDestroy()
		{
			current = null;
		}

		public int GetTagPriority(string assetTag)
		{
			if (tagPriority == null) return 0;
			for (int i = 0; i < tagPriority.Length; ++i)
			{
				if (tagPriority[i] == assetTag)
					return i;
			}
			return Mathf.Max(tagPriority.Length - 1, 0);
		}

		public int GetTagPrioritySize()
		{
			if (tagPriority == null) return 1;
			return Mathf.Max(tagPriority.Length, 1);
		}

        public abstract bool IsTagExcluded(string tag);
        public abstract bool IsForceUpdateOnce();
        public abstract IAssetRequest LoadFromDiskAsync(string path, bool reportError);
		public abstract Object LoadFromDisk(string path, bool reportError);

	}
}
