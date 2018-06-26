using UnityEngine;

namespace scene
{
	public static class CameraExtensions
	{
		public static Bounds GetFrustumBoundsBasedOnXZProjection(this Camera camera, float verticalSize)
		{
			var corners = camera.ProjectFrustumOnXZPlane();
			Vector3 halfHeight = new Vector3(0f, verticalSize * 0.5f, 0f);
			Bounds b = new Bounds(corners[0] + halfHeight, Vector3.zero);
			b.Encapsulate(corners[1] + halfHeight);
			b.Encapsulate(corners[2] + halfHeight);
			b.Encapsulate(corners[3] + halfHeight);

			b.Encapsulate(corners[0] - halfHeight);
			b.Encapsulate(corners[1] - halfHeight);
			b.Encapsulate(corners[2] - halfHeight);
			b.Encapsulate(corners[3] - halfHeight);

			return b;
		}
		public static Vector3[] ProjectFrustumOnXZPlane(this Camera camera)
		{
			Plane plane = new Plane(Vector3.up, Vector3.zero);
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
			Line L0;
			MUtils.Intersects(planes[0], plane, out L0);
			Line L1;
			MUtils.Intersects(planes[1], plane, out L1);
			Line L2;
			MUtils.Intersects(planes[2], plane, out L2);
			Line L3;
			MUtils.Intersects(planes[3], plane, out L3);

			Line2d l2d0 = MUtils.MappingAxis.Map(L0);
			Line2d l2d1 = MUtils.MappingAxis.Map(L1);
			Line2d l2d2 = MUtils.MappingAxis.Map(L2);
			Line2d l2d3 = MUtils.MappingAxis.Map(L3);

			float s, t;
			l2d0.Intersects(l2d2, out t, out s);
			Vector3 rightTop = MUtils.MappingAxis.Map(l2d0.GetPoint(t));

			l2d0.Intersects(l2d3, out t, out s);
			Vector3 rightBottom = MUtils.MappingAxis.Map(l2d0.GetPoint(t));

			l2d1.Intersects(l2d2, out t, out s);
			Vector3 leftTop = MUtils.MappingAxis.Map(l2d1.GetPoint(t));

			l2d1.Intersects(l2d3, out t, out s);
			Vector3 leftBottom = MUtils.MappingAxis.Map(l2d1.GetPoint(t));

			return new Vector3[] { leftTop, rightTop, rightBottom, leftBottom };
		}

		public static void DrawFrustumXZProjectionGizmos(this Camera camera)
		{
			Vector3[] p = camera.ProjectFrustumOnXZPlane();
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(p[0], p[1]);
			Gizmos.DrawLine(p[1], p[2]);
			Gizmos.DrawLine(p[2], p[3]);
			Gizmos.DrawLine(p[3], p[0]);

			Gizmos.color = Color.red;
			Vector3 center = Vector3.zero;
			foreach (var pp in p)
			{
				center += pp;
			}
			center *= 1f / p.Length;
			Gizmos.DrawWireSphere(center, 1f);
		}

		public static Plane[] GetPlanesBasedOnFrustumXZProjection(this Camera camera)
		{
			Vector3[] bounds = camera.ProjectFrustumOnXZPlane();

			Vector3 topLine = bounds[0] - bounds[1];
			Vector3 topNormal = new Vector3(-topLine.z, topLine.y, topLine.x);
			Plane topPlane = new Plane(topNormal.normalized, bounds[0]);

			Vector3 bottomLine = bounds[2] - bounds[3];
			Vector3 bottomNormal = new Vector3(-bottomLine.z, bottomLine.y, bottomLine.x);
			Plane bottomPlane = new Plane(bottomNormal.normalized, bounds[2]);

			Vector3 leftLine = bounds[3] - bounds[0];
			Vector3 leftNormal = new Vector3(-leftLine.z, leftLine.y, leftLine.x);
			Plane leftPlane = new Plane(leftNormal.normalized, bounds[0]);

			Vector3 rightLine = bounds[1] - bounds[2];
			Vector3 rightNormal = new Vector3(-rightLine.z, rightLine.y, rightLine.x);
			Plane rightPlane = new Plane(rightNormal.normalized, bounds[1]);

			return new Plane[] { topPlane, bottomPlane, leftPlane, rightPlane };
		}

		public static void CollapseViewport(this Camera camera, float normalizedPos)
		{
			var r = new Rect(0f, normalizedPos, Screen.width, Screen.height);
			camera.rect = r;
		}

	}
}