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

        public Vector3? Target { get; private set; }

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
            var direction = transform.forward;
            var vertices = new List<Vector3>();

            var pos = transform.position;
            var v = -(Velocity * transform.forward).y * Scale;
            var g = Physics.gravity.y * Scale;
            var h = pos.y - Offset;

            if (v * v >= 2 * g * h)
            {
                var totT1 = (Mathf.Sqrt(v * v - 2 * g * h) + v) / g;
                var totT2 = (v - Mathf.Sqrt(v * v - 2 * g * h)) / g;
                var totT = Mathf.Max(totT1, totT2);
                totT = Mathf.Abs(totT);

                var timeStep = totT / VertexCount;

                for (var i = 0; i <= VertexCount; i++)
                {
                    var t = Mathf.Clamp(((i / (VertexCount - 1f)) * totT) + ((Time.time * UvSpeed) % 2) * timeStep - timeStep, 0, totT);
                    vertices.Add(transform.InverseTransformPoint(pos + ((direction * Velocity) * t + 0.5f * Physics.gravity * t * t) * Scale));
                }


                var target = transform.position + ((direction * Velocity) * totT + 0.5f * Physics.gravity * totT * totT) * Scale;
                Target = new Vector3(target.x, 0, target.z);
            }
            else
            {
                Target = null;
            }

            GetComponent<Renderer>().material.mainTextureOffset += new Vector2(UvSpeed * Time.deltaTime, 0);

            _mesh.vertices = vertices.ToArray();
            _mesh.SetIndices(vertices.Take(vertices.Count - 1).Select((ve, i) => i).Where(i => i % 2 == 0).SelectMany(i => new int[] { i, i + 1 }).ToArray(), MeshTopology.Lines, 0);

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
