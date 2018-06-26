using UnityEngine;
using System.Collections;

namespace scene
{
	[RequireComponent(typeof(Collider))]
	public class PaintGeometry : MonoBehaviour
	{

		public GameObject objectToPaint;
		[Range(0.01f, 4f)]
		public float radius = 1f;
		[Range(0.01f, 4f)]
		public float distance = 1f;

		public Vector3 initialRotation;

		public bool randomRot = false;
		public Vector3 rotation;


		public bool randomScale = false;
		[Range(0.01f, 4f)]
		public float scaleMin = 1f;
		[Range(0.01f, 4f)]
		public float scaleMax = 1f;

		public bool lockUp = true;

	}
}