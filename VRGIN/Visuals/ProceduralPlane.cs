using System;
using UnityEngine;

namespace VRGIN.Visuals
{
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	public class ProceduralPlane : MonoBehaviour
	{
		private const int DEFAULT_X_SEGMENTS = 10;

		private const int DEFAULT_Y_SEGMENTS = 10;

		private const int MIN_X_SEGMENTS = 1;

		private const int MIN_Y_SEGMENTS = 1;

		private const float DEFAULT_WIDTH = 1f;

		private const float DEFAULT_HEIGHT = 1f;

		public int xSegments = 10;

		public int ySegments = 10;

		public Vector2 topLeftOffset = Vector2.zero;

		public Vector2 topRightOffset = Vector2.zero;

		public Vector2 bottomLeftOffset = Vector2.zero;

		public Vector2 bottomRightOffset = Vector2.zero;

		public float distance = 1f;

		private Mesh modelMesh;

		private MeshFilter meshFilter;

		public float width = 1f;

		public float height = 1f;

		private int numVertexColumns;

		private int numVertexRows;

		public float angleSpan = 160f;

		public float curviness;

		public void AssignDefaultShader()
		{
			MeshRenderer component = base.gameObject.GetComponent<MeshRenderer>();
			component.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
			component.sharedMaterial.color = Color.white;
		}

		public void Rebuild()
		{
			modelMesh = new Mesh();
			modelMesh.name = "ProceduralPlaneMesh";
			meshFilter = base.gameObject.GetComponent<MeshFilter>();
			meshFilter.mesh = modelMesh;
			if (xSegments < 1)
			{
				xSegments = 1;
			}
			if (ySegments < 1)
			{
				ySegments = 1;
			}
			numVertexColumns = xSegments + 1;
			numVertexRows = ySegments + 1;
			int num = numVertexColumns * numVertexRows;
			int num2 = num;
			int num3 = xSegments * ySegments * 2 * 3;
			Vector3[] array = new Vector3[num];
			Vector2[] array2 = new Vector2[num2];
			int[] array3 = new int[num3];
			float num4 = width / (float)xSegments;
			float num5 = height / (float)ySegments;
			float num6 = 1f / (float)xSegments;
			float num7 = 1f / (float)ySegments;
			float num8 = (0f - width) / 2f;
			float num9 = (0f - height) / 2f;
			float num10 = angleSpan * (float)Math.PI / 180f;
			float num11 = 1f;
			float num12 = (float)Screen.width / (float)Screen.height;
			float num13 = num10 / num11;
			for (int i = 0; i < numVertexRows; i++)
			{
				for (int j = 0; j < numVertexColumns; j++)
				{
					Vector3 vector = new Vector3((float)j * num4 + num8 + bottomLeftOffset.x * (float)(numVertexColumns - 1 - j) / (float)(numVertexColumns - 1) * (float)(numVertexRows - 1 - i) / (float)(numVertexRows - 1) + bottomRightOffset.x * (float)j / (float)(numVertexColumns - 1) * (float)(numVertexRows - 1 - i) / (float)(numVertexRows - 1) + topLeftOffset.x * (float)(numVertexColumns - 1 - j) / (float)(numVertexColumns - 1) * (float)i / (float)(numVertexRows - 1) + topRightOffset.x * (float)j / (float)(numVertexColumns - 1) * (float)i / (float)(numVertexRows - 1), (float)i * num5 + num9 + bottomLeftOffset.y * (float)(numVertexColumns - 1 - j) / (float)(numVertexColumns - 1) * (float)(numVertexRows - 1 - i) / (float)(numVertexRows - 1) + bottomRightOffset.y * (float)j / (float)(numVertexColumns - 1) * (float)(numVertexRows - 1 - i) / (float)(numVertexRows - 1) + topLeftOffset.y * (float)(numVertexColumns - 1 - j) / (float)(numVertexColumns - 1) * (float)i / (float)(numVertexRows - 1) + topRightOffset.y * (float)j / (float)(numVertexColumns - 1) * (float)i / (float)(numVertexRows - 1) - (height - 1f) / 2f, distance);
					float x = Mathf.Lerp(num12 * height * vector.x, Mathf.Cos((float)Math.PI / 2f - vector.x * num13) * distance, Mathf.Clamp01(curviness));
					float z = Mathf.Sin((float)Math.PI / 2f - vector.x * num13 * Mathf.Clamp01(curviness));
					int num14 = i * numVertexColumns + j;
					array[num14] = new Vector3(x, vector.y, z);
					if (curviness > 1f)
					{
						float value = curviness - 1f;
						array[num14] = Vector3.Lerp(array[num14], array[num14].normalized * distance, Mathf.Clamp01(value));
					}
					array2[num14] = new Vector2((float)j * num6, (float)i * num7);
					if (i != 0 && j < numVertexColumns - 1)
					{
						int num15 = (i - 1) * xSegments * 6 + j * 6;
						array3[num15] = i * numVertexColumns + j;
						array3[num15 + 1] = i * numVertexColumns + j + 1;
						array3[num15 + 2] = (i - 1) * numVertexColumns + j;
						array3[num15 + 3] = (i - 1) * numVertexColumns + j;
						array3[num15 + 4] = i * numVertexColumns + j + 1;
						array3[num15 + 5] = (i - 1) * numVertexColumns + j + 1;
					}
				}
			}
			modelMesh.Clear();
			modelMesh.vertices = array;
			modelMesh.uv = array2;
			modelMesh.triangles = array3;
			modelMesh.RecalculateNormals();
			modelMesh.RecalculateBounds();
		}

		public float TransformX(float x)
		{
			float num = (float)Screen.width / (float)Screen.height;
			float num2 = angleSpan * (float)Math.PI / 180f;
			float num3 = 1f;
			return Mathf.Lerp(num * height * x, Mathf.Cos((float)Math.PI / 2f - x * (num2 / num3)) * distance, curviness);
		}

		public float TransformZ(float x)
		{
			_ = (float)Screen.width / (float)Screen.height;
			float num = angleSpan * (float)Math.PI / 180f;
			float num2 = 1f;
			float num3 = num / num2;
			return Mathf.Sin((float)Math.PI / 2f - x * num3 * curviness);
		}
	}
}
