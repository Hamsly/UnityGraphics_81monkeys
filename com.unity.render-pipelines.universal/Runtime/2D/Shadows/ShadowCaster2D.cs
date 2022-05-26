using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Caster 2D")]
    public class ShadowCaster2D : ShadowCasterGroup2D
    {
        
        [SerializeField] float m_Height = 1f;
        [SerializeField] float m_ZPosition = 0f;
        [SerializeField] float m_FalloffRate = 1f;
        [SerializeField] bool m_HasRenderer = false;
        [SerializeField] bool m_UseRendererSilhouette = true;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] int[] m_ApplyToSortingLayers = null;
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] Mesh m_Mesh;
        [SerializeField] int m_InstanceId;
        [SerializeField] Texture2D m_ShadowTexture;
        [SerializeField] Renderer[] m_SilhouettedRenderers = new Renderer[0];

        internal ShadowCasterGroup2D m_ShadowCasterGroup = null;
        internal ShadowCasterGroup2D m_PreviousShadowCasterGroup = null;

        internal Mesh mesh => m_Mesh;
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;
        int m_PreviousPathHash = 0;


        /// <summary>
        /// If selfShadows is true, useRendererSilhoutte specifies that the renderer's sihouette should be considered part of the shadow. If selfShadows is false, useRendererSilhoutte specifies that the renderer's sihouette should be excluded from the shadow
        /// </summary>
        public bool useRendererSilhouette
        {
            set { m_UseRendererSilhouette = value; }
            get { return m_UseRendererSilhouette && m_HasRenderer;  }
        }

        public bool hasRenderer
        {
            get => m_HasRenderer;
        }

        public Renderer[] silhouettedRenderer
        {
            get => m_SilhouettedRenderers;
        }

        public float shadowHeight
        {
            set { m_Height = value; }
            get { return m_Height; }
        }
        public float fallOffrate
        {
            set { m_FalloffRate = value; }
            get { return m_FalloffRate; }
        }

        public Texture2D shadowTexture
        {
            set { m_ShadowTexture = value; }
            get { return m_ShadowTexture; }
        }

        /// <summary>
        /// If true, the shadow casting shape is included as part of the shadow. If false, the shadow casting shape is excluded from the shadow.
        /// </summary>
        public bool selfShadows
        {
            set { m_SelfShadows = value; }
            get { return m_SelfShadows; }
        }

        public Vector3 shadowPosition
        {
            get
            {
                Vector3 basePos = transform.position;
                basePos.z = m_ZPosition;
                return basePos;
            }
        }

        /// <summary>
        /// Specifies if shadows will be cast.
        /// </summary>
        public bool castsShadows
        {
            set { m_CastsShadows = value; }
            get { return m_CastsShadows; }
        }

        static int[] SetDefaultSortingLayers()
        {
            int layerCount = SortingLayer.layers.Length;
            int[] allLayers = new int[layerCount];

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                allLayers[layerIndex] = SortingLayer.layers[layerIndex].id;
            }

            return allLayers;
        }

        internal bool IsShadowedLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
        }

        private void Awake()
        {
            if (m_ApplyToSortingLayers == null)
                m_ApplyToSortingLayers = SetDefaultSortingLayers();

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

        protected void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_ShadowCasterGroup);
        }

        public void Update()
        {
            m_HasRenderer = m_SilhouettedRenderers != null;

            bool rebuildMesh = LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
            if (rebuildMesh)
                ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);

            m_PreviousShadowCasterGroup = m_ShadowCasterGroup;
            bool addedToNewGroup = ShadowCasterGroup2DManager.AddToShadowCasterGroup(this, ref m_ShadowCasterGroup);
            if (addedToNewGroup && m_ShadowCasterGroup != null)
            {
                if (m_PreviousShadowCasterGroup == this)
                    ShadowCasterGroup2DManager.RemoveGroup(this);

                ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_PreviousShadowCasterGroup);
                if (m_ShadowCasterGroup == this)
                    ShadowCasterGroup2DManager.AddGroup(this);
            }

            if (LightUtility.CheckForChange(m_ShadowGroup, ref m_PreviousShadowGroup))
            {
                ShadowCasterGroup2DManager.RemoveGroup(this);
                ShadowCasterGroup2DManager.AddGroup(this);
            }

            if (LightUtility.CheckForChange(m_CastsShadows, ref m_PreviousCastsShadows))
            {
                if (m_CastsShadows)
                    ShadowCasterGroup2DManager.AddGroup(this);
                else
                    ShadowCasterGroup2DManager.RemoveGroup(this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            foreach (Vector3 p in shapePath)
            {
                Vector3 pp = p;
                pp.x *= transform.lossyScale.x;
                pp.y *= transform.lossyScale.y;
                pp.y += m_ZPosition;

                var point = pp + transform.position;
                Gizmos.DrawLine(point, point + new Vector3(0, shadowHeight, 0));
            }
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
