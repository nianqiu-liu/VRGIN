using System.Collections.Generic;
using UnityEngine;

namespace VRGIN.Helpers
{
	public static class MeshExtension
	{
		public static Vector3 GetBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p)
		{
			Vector3 result = default(Vector3);
			result.x = ((v2.y - v3.y) * (p.x - v3.x) + (v3.x - v2.x) * (p.y - v3.y)) / ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
			result.y = ((v3.y - v1.y) * (p.x - v3.x) + (v1.x - v3.x) * (p.y - v3.y)) / ((v3.y - v1.y) * (v2.x - v3.x) + (v1.x - v3.x) * (v2.y - v3.y));
			result.z = 1f - result.x - result.y;
			return result;
		}

		public static bool InTriangle(Vector3 barycentric)
		{
			if (barycentric.x >= 0f && barycentric.x <= 1f && barycentric.y >= 0f && barycentric.y <= 1f)
			{
				return barycentric.z >= 0f;
			}
			return false;
		}

		public static Vector3[] GetMappedPoints(this Mesh aMesh, Vector2 aUVPos)
		{
			List<Vector3> list = new List<Vector3>();
			Vector3[] vertices = aMesh.vertices;
			Vector2[] uv = aMesh.uv;
			int[] triangles = aMesh.triangles;
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int num = triangles[i];
				int num2 = triangles[i + 1];
				int num3 = triangles[i + 2];
				Vector3 barycentric = GetBarycentric(uv[num], uv[num2], uv[num3], aUVPos);
				if (InTriangle(barycentric))
				{
					Vector3 item = barycentric.x * vertices[num] + barycentric.y * vertices[num2] + barycentric.z * vertices[num3];
					list.Add(item);
				}
			}
			return list.ToArray();
		}
	}
}
