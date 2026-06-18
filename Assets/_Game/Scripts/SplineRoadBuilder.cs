using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace SlingshotShopping
{
    /// <summary>
    /// Bakes a flat ribbon road mesh + MeshCollider from a SplineContainer.
    /// Editor-time authoring: the generated mesh is assigned to the shared MeshFilter,
    /// so it serializes into the scene (baked, static) and costs nothing at runtime.
    /// Keep the SplineContainer transform at identity so spline-local == mesh-local.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SplineRoadBuilder : MonoBehaviour
    {
        [Header("Road shape")]
        [Tooltip("Total width of the road in meters.")]
        public float width = 8f;

        [Tooltip("Number of cross-sections sampled along the spline. Higher = smoother but heavier.")]
        [Range(2, 2000)]
        public int samples = 400;

        [Tooltip("Tiles the road texture every N meters along its length.")]
        public float uvTilingMeters = 4f;

        [Header("Output")]
        public bool generateCollider = true;

        SplineContainer _container;
        MeshFilter _filter;
        MeshCollider _collider;

        void Awake()
        {
            // Runtime safety: if the baked mesh is missing, regenerate once.
            EnsureRefs();
            if (Application.isPlaying && (_filter.sharedMesh == null || _filter.sharedMesh.vertexCount == 0))
                BuildRoad();
        }

        void OnEnable() => EnsureRefs();

        void EnsureRefs()
        {
            _container = GetComponent<SplineContainer>();
            _filter = GetComponent<MeshFilter>();
            _collider = GetComponent<MeshCollider>();
        }

        [ContextMenu("Build Road")]
        public void BuildRoad()
        {
            EnsureRefs();

            if (_container == null || _container.Spline == null || _container.Spline.Count < 2)
            {
                Debug.LogWarning("[SplineRoadBuilder] Need a SplineContainer with at least 2 knots.", this);
                return;
            }

            var spline = _container.Spline;
            float half = width * 0.5f;

            int sectionCount = Mathf.Max(2, samples);
            var verts = new List<Vector3>(sectionCount * 2);
            var normals = new List<Vector3>(sectionCount * 2);
            var uvs = new List<Vector2>(sectionCount * 2);
            var tris = new List<int>((sectionCount - 1) * 6);

            float totalLength = SplineUtility.CalculateLength(spline, float4x4.identity);
            float vScale = uvTilingMeters > 0.001f ? totalLength / uvTilingMeters : 1f;

            for (int i = 0; i < sectionCount; i++)
            {
                float t = (float)i / (sectionCount - 1);
                spline.Evaluate(t, out float3 pos, out float3 tan, out float3 up);

                float3 fwd = math.normalizesafe(tan, new float3(0, 0, 1));
                float3 upN = math.normalizesafe(up, new float3(0, 1, 0));
                float3 right = math.normalizesafe(math.cross(upN, fwd), new float3(1, 0, 0));

                float3 left = pos - right * half;
                float3 rgt = pos + right * half;

                verts.Add((Vector3)left);
                verts.Add((Vector3)rgt);
                normals.Add((Vector3)upN);
                normals.Add((Vector3)upN);

                float v = t * vScale;
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }

            for (int i = 0; i < sectionCount - 1; i++)
            {
                int l0 = i * 2;
                int r0 = i * 2 + 1;
                int l1 = (i + 1) * 2;
                int r1 = (i + 1) * 2 + 1;

                // Wound so the top face (along +up) is front-facing.
                tris.Add(l0); tris.Add(l1); tris.Add(r0);
                tris.Add(r0); tris.Add(l1); tris.Add(r1);
            }

            var mesh = _filter.sharedMesh;
            if (mesh == null || mesh.name != "RoadMesh")
            {
                mesh = new Mesh { name = "RoadMesh" };
                _filter.sharedMesh = mesh;
            }
            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            if (generateCollider)
            {
                if (_collider == null) _collider = GetComponent<MeshCollider>();
                if (_collider == null) _collider = gameObject.AddComponent<MeshCollider>();
                _collider.sharedMesh = null;
                _collider.sharedMesh = mesh;
            }

            Debug.Log($"[SplineRoadBuilder] Built road: {sectionCount} sections, {verts.Count} verts, length {totalLength:0.0}m.", this);
        }
    }
}
