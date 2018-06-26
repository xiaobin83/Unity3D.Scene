using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
namespace scene
{
public class StaticSceneHelper : MonoBehaviour
{

	public string objectTag = "";

	// loading priority, the small the highest
	public int priority = 0;

	// including and above
	public QualityLevel availableSetting = QualityLevel.Fastest;

	public string storePath = string.Empty;

}
}

#endif
