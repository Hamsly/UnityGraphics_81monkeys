using System;
using System.Collections;
using System.Collections.Generic;
using PlasticGui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Mesh Caster 2D")]
    public class ShadowMeshCaster2D : ShadowCaster2D
    {
        [SerializeField] float m_Height = 1f;
        [SerializeField] float m_FalloffRate = 1f;
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] Mesh m_Mesh;


        internal Mesh mesh => m_Mesh;
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }


        int m_PreviousPathHash = 0;

        public float shadowHeight
        {
            set { m_Height = value; }
            get { return m_Height; }
        }
        public float falloffRate
        {
            set { m_FalloffRate = value; }
            get { return m_FalloffRate; }
        }

        private void Awake()
        {
            base.Awake();

            m_SilhouettedRenderers ??= Array.Empty<Renderer>();

            Bounds bounds = new Bounds(transform.position, Vector3.one);

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                if (m_SilhouettedRenderers.Length < 1)
                {
                    m_SilhouettedRenderers = new Renderer[1];
                    m_SilhouettedRenderers[0] = renderer;
                }
            }
#if USING_PHYSICS2D_MODULE
            else
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                    bounds = collider.bounds;
            }
#endif

            Vector3 inverseScale = Vector3.zero;
            Vector3 relOffset = transform.position;

            if (transform.lossyScale.x != 0 && transform.lossyScale.y != 0)
            {
                inverseScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y);
                relOffset = new Vector3(inverseScale.x * -transform.position.x, inverseScale.y * -transform.position.y);
            }

            if (m_ShapePath == null || m_ShapePath.Length == 0)
            {
                m_ShapePath = new Vector3[]
                {
                    relOffset + new Vector3(inverseScale.x * bounds.min.x, inverseScale.y * bounds.min.y),
                    relOffset + new Vector3(inverseScale.x * bounds.min.x, inverseScale.y * bounds.max.y),
                    relOffset + new Vector3(inverseScale.x * bounds.max.x, inverseScale.y * bounds.max.y),
                    relOffset + new Vector3(inverseScale.x * bounds.max.x, inverseScale.y * bounds.min.y),
                };
            }

            RecalculateBounds();
        }

        protected void OnEnable()
        {
            if (m_Mesh == null || m_InstanceId != GetInstanceID())
            {
                m_Mesh = new Mesh();
                ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);
                m_InstanceId = GetInstanceID();
            }

            m_ShadowCasterGroup = null;
        }

        public void Update()
        {
            bool rebuildMesh = LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
            if (rebuildMesh)
            {
                ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);

                RecalculateBounds();
            }

            base.Update();
        }

        private void RecalculateBounds()
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            foreach (var point in shapePath)
            {
                Vector3 pp = point;
                var transformRef = transform;
                var lossyScale = transformRef.lossyScale;

                pp.x *= lossyScale.x;
                pp.y *= lossyScale.y;

                minX = Mathf.Min(pp.x, minX);
                maxX = Mathf.Max(pp.x, maxX);

                minY = Mathf.Min(pp.y, minY);
                maxY = Mathf.Max(pp.y, maxY);
            }

            var center = new Vector3(Mathf.Lerp(minX, maxX, 0.5f), Mathf.Lerp(minY, maxY, 0.5f), 0);

            m_MeshBounds = new Bounds(center, new Vector3(maxX - minX, maxY - minY, 1));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            foreach (var p in shapePath)
            {
                Vector3 pp = p;
                var transformRef = transform;
                var lossyScale = transformRef.lossyScale;

                pp.x *= lossyScale.x;
                pp.y *= lossyScale.y;
                pp.y += m_ZPosition;

                var point = pp + transformRef.position;
                Gizmos.DrawLine(point, point + new Vector3(0, shadowHeight, 0));

            }

            Gizmos.DrawWireCube(m_MeshBounds.center + transform.position,m_MeshBounds.size);
        }

        public override void CastShadows(CommandBuffer cmdBuffer, int layerToRender,Light2D light, Material material)
        {
            if (!castsShadows || material == null || !IsShadowedLayer(layerToRender)) return;
            cmdBuffer.SetGlobalFloat(k_ShadowHeightID, shadowHeight);
            cmdBuffer.SetGlobalVector(k_ShadowCenterID, shadowPosition);
            cmdBuffer.SetGlobalFloat(k_FalloffRate, falloffRate);
            cmdBuffer.DrawMesh(mesh, transform.localToWorldMatrix, material);
        }

#if UNITY_EDITOR
        void Reset()
        {
            Awake();
            OnEnable();
        }

#endif
    }
}
