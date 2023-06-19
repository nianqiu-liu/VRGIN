using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.Visuals
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class ArcRenderer : MonoBehaviour
    {
        public int VertexCount = 50;

        public float UvSpeed = 5f;

        public float Velocity = 6f;

        private MeshFilter _MeshFilter;

        private Renderer _Renderer;

        public Vector3 target;

        public float Offset;

        public float Scale = 1f;

        private Mesh _mesh;

        private void Awake()
        {
            _MeshFilter = GetComponent<MeshFilter>();
            _Renderer = GetComponent<Renderer>();
            _mesh = new Mesh();
            _Renderer.material = new Material(VRManager.Instance.Context.Materials.Sprite);
            _Renderer.shadowCastingMode = ShadowCastingMode.Off;
            _Renderer.receiveShadows = false;
            _Renderer.useLightProbes = false;
            _Renderer.material.color = VRManager.Instance.Context.PrimaryColor;
        }

        public void Update()
        {
            var forward = transform.forward;
            var list = new List<Vector3>();
            var position = transform.position;
            var num = (0f - (Velocity * transform.forward).y) * Scale;
            var num2 = Physics.gravity.y * Scale;
            var num3 = position.y - Offset;
            var a = (Mathf.Sqrt(num * num - 2f * num2 * num3) + num) / num2;
            var b = (num - Mathf.Sqrt(num * num - 2f * num2 * num3)) / num2;
            var f = Mathf.Max(a, b);
            f = Mathf.Abs(f);
            var num4 = f / (float)VertexCount;
            for (var j = 0; j <= VertexCount; j++)
            {
                var num5 = Mathf.Clamp((float)j / ((float)VertexCount - 1f) * f + Time.time * UvSpeed % 2f * num4 - num4, 0f, f);
                list.Add(transform.InverseTransformPoint(position + (forward * Velocity * num5 + 0.5f * Physics.gravity * num5 * num5) * Scale));
            }

            target = transform.position + (forward * Velocity * f + 0.5f * Physics.gravity * f * f) * Scale;
            target.y = 0f;
            GetComponent<Renderer>().material.mainTextureOffset += new Vector2(UvSpeed * Time.deltaTime, 0f);
            _mesh.vertices = list.ToArray();
            _mesh.SetIndices((from i in list.Take(list.Count - 1).Select((Vector3 ve, int i) => i)
                              where i % 2 == 0
                              select i).SelectMany((int i) => new int[2]
            {
                i,
                i + 1
            }).ToArray(), MeshTopology.Lines, 0);
            _MeshFilter.mesh = _mesh;
        }

        private void OnEnable()
        {
            GetComponent<Renderer>().enabled = true;
        }

        private void OnDisable()
        {
            GetComponent<Renderer>().enabled = false;
        }
    }
}
