using System;
using UnityEngine;

namespace CapturePanorama
{
	public static class Icosphere
	{
		public static Mesh BuildIcosphere(float radius, int iterations)
		{
			Mesh mesh = BuildIcosahedron(radius);
			for (int i = 0; i < iterations; i++)
			{
				Refine(mesh);
			}
			return mesh;
		}

		public static Mesh BuildIcosahedron(float radius)
		{
			Mesh mesh = new Mesh();
			float num = (float)((1.0 + Math.Sqrt(5.0)) / 2.0);
			Vector3[] array = new Vector3[12]
			{
				new Vector3(-1f, num, 0f),
				new Vector3(1f, num, 0f),
				new Vector3(-1f, 0f - num, 0f),
				new Vector3(1f, 0f - num, 0f),
				new Vector3(0f, -1f, num),
				new Vector3(0f, 1f, num),
				new Vector3(0f, -1f, 0f - num),
				new Vector3(0f, 1f, 0f - num),
				new Vector3(num, 0f, -1f),
				new Vector3(num, 0f, 1f),
				new Vector3(0f - num, 0f, -1f),
				new Vector3(0f - num, 0f, 1f)
			};
			float num2 = radius / new Vector3(1f, num, 0f).magnitude;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] *= num2;
			}
			mesh.vertices = array;
			mesh.triangles = new int[60]
			{
				0, 11, 5, 0, 5, 1, 0, 1, 7, 0,
				7, 10, 0, 10, 11, 1, 5, 9, 5, 11,
				4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
				3, 9, 4, 3, 4, 2, 3, 2, 6, 3,
				6, 8, 3, 8, 9, 4, 9, 5, 2, 4,
				11, 6, 2, 10, 8, 6, 7, 9, 8, 1
			};
			return mesh;
		}

		private static void Refine(Mesh m)
		{
			throw new Exception("TODO");
		}
	}
}
