using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>

    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Mesh Caster 2D")]
    public class ShadowMeshCaster2D : ShadowCaster2D
    {
        [SerializeField] float m_Height = 1f;
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] Mesh m_Mesh;

        //private MaterialPropertyBlock materialPropertyBlock;

        internal Mesh mesh => m_Mesh;
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }


        int m_PreviousPathHash = 0;

        public float ShadowHeight
        {
            set
            {
                m_Height = value;
                //materialPropertyBlock.SetFloat(k_ShadowHeightID,m_Height);
            }
            get => m_Height;
        }

        private new void Awake()
        {

            base.Awake();

            //materialPropertyBlock = new MaterialPropertyBlock();
            //materialPropertyBlock.SetFloat(k_ShadowHeightID,ShadowHeight);
            //materialPropertyBlock.SetVector(k_ShadowCenterID,shadowPosition);

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
            Vector3 relOffset = Transform.position;

            if (Transform.lossyScale.x != 0 && Transform.lossyScale.y != 0)
            {
                inverseScale = new Vector3(1 / Transform.lossyScale.x, 1 / Transform.lossyScale.y);
                relOffset = new Vector3(inverseScale.x * -Transform.position.x, inverseScale.y * -Transform.position.y);
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

        protected new void OnEnable()
        {
            base.OnEnable();

            if (m_Mesh == null || m_InstanceId != GetInstanceID())
            {
                m_Mesh = new Mesh();
                ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);
                m_InstanceId = GetInstanceID();
            }
        }

        protected override void OnUpdate()
        {
            bool rebuildMesh = LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
            if (rebuildMesh)
            {
                ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);

                RecalculateBounds();
            }
        }

        public void SetMesh(Mesh mesh)
        {
            m_Mesh = mesh;
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

                var lossyScale = Transform.lossyScale;

                pp.x *= lossyScale.x;
                pp.y *= lossyScale.y;

                minX = Mathf.Min(pp.x, minX);
                maxX = Mathf.Max(pp.x, maxX);

                minY = Mathf.Min(pp.y, minY);
                maxY = Mathf.Max(pp.y, maxY);
            }

            m_Bounds = new Rect(new Vector2(minX,minY), new Vector2(maxX - minX, maxY - minY));
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

                var point = pp + transformRef.position;
                Gizmos.DrawLine(point, point + new Vector3(0, ShadowHeight, 0));
            }

            Gizmos.DrawWireCube(m_Bounds.center + (Vector2)transform.position,m_Bounds.size);
        }

        public override void CastShadows(CommandBuffer cmdBuffer, int layerToRender,Light2D light, Material material,int groupIndex)
        {
            if (!CastsShadows || material == null || !IsShadowedLayer(layerToRender)) return;

            //materialPropertyBlock.SetVector(k_ShadowCenterID,shadowPosition);

            cmdBuffer.SetGlobalFloat(k_ShadowHeightID, ShadowHeight);
            cmdBuffer.SetGlobalVector(k_ShadowCenterID, shadowPosition);
            cmdBuffer.DrawMesh(mesh, Transform.localToWorldMatrix, material,0,-1,null);
            //cmdBuffer.DrawMesh(mesh, Transform.localToWorldMatrix, material,0,-1, materialPropertyBlock);
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
