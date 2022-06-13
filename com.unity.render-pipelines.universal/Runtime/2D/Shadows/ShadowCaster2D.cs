using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public class ShadowCaster2D : ShadowCasterGroup2D
    {
        [SerializeField] protected Renderer[] m_SilhouettedRenderers = new Renderer[0];
        [SerializeField] protected bool m_HasRenderer = false;
        [SerializeField] protected bool m_UseRendererSilhouette = true;
        [SerializeField] protected float m_ZPosition = 0f;
        [SerializeField] protected int m_InstanceId;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] int[] m_ApplyToSortingLayers = null;
        [SerializeField] Renderer2DData.ShadowMaterialTypes m_materialType = Renderer2DData.ShadowMaterialTypes.MeshShadows;

        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;

        internal ShadowCasterGroup2D m_ShadowCasterGroup = null;
        internal ShadowCasterGroup2D m_PreviousShadowCasterGroup = null;

        internal static readonly int k_ShadowHeightID = Shader.PropertyToID("_ShadowHeight");
        internal static readonly int k_ShadowCenterID = Shader.PropertyToID("_ShadowCenter");
        internal static readonly int k_FalloffRate = Shader.PropertyToID("_FalloffRate");

        /// <summary>
        /// If true, the shadow casting shape is included as part of the shadow. If false, the shadow casting shape is excluded from the shadow.
        /// </summary>
        public bool selfShadows
        {
            set { m_SelfShadows = value; }
            get { return m_SelfShadows; }
        }

        /// <summary>
        /// If selfShadows is true, useRendererSilhoutte specifies that the renderer's sihouette should be considered part of the shadow. If selfShadows is false, useRendererSilhoutte specifies that the renderer's sihouette should be excluded from the shadow
        /// </summary>
        public bool useRendererSilhouette
        {
            set { m_UseRendererSilhouette = value; }
            get { return m_UseRendererSilhouette && m_HasRenderer; }
        }

        /// <summary>
        /// Specifies if shadows will be cast.
        /// </summary>
        public bool castsShadows
        {
            set { m_CastsShadows = value; }
            get { return m_CastsShadows; }
        }

        public bool hasRenderer
        {
            get => m_HasRenderer;
        }

        public Renderer[] silhouettedRenderer
        {
            get => m_SilhouettedRenderers;
        }

        protected Vector3 shadowPosition
        {
            get
            {
                var pos = transform.position;
                return new Vector3(pos.x, pos.y, m_ZPosition);
            }
        }

        public Renderer2DData.ShadowMaterialTypes materialType
        {
            get => m_materialType;
            protected set => m_materialType = value;
        }

        internal bool IsShadowedLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
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

        protected void Awake()
        {
            if (m_ApplyToSortingLayers == null)
                m_ApplyToSortingLayers = SetDefaultSortingLayers();
        }

        protected void OnEnable()
        {
            if (m_InstanceId != GetInstanceID())
            {
                m_InstanceId = GetInstanceID();
            }

            m_ShadowCasterGroup = null;
        }

        public void Update()
        {
            if (m_SilhouettedRenderers != null)
            {
                m_HasRenderer = m_SilhouettedRenderers.Length > 0;
            }
            else
            {
                m_HasRenderer = false;
            }


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

        public virtual void CastShadows(CommandBuffer cmdBuffer,int layerToRender,Light2D light,Material material)
        {
        }

        public virtual void ExcludeSilhouettes(CommandBuffer cmdBuffer,int layerToRender,Material material)
        {
            if (useRendererSilhouette && IsShadowedLayer(layerToRender))
            {
                var renderers = silhouettedRenderer;
                if (renderers != null)
                {
                    foreach (var currentRenderer in renderers)
                    {
                        if (currentRenderer != null)
                        {
                            cmdBuffer.DrawRenderer(currentRenderer, material);
                        }
                    }
                }
            }
        }
    }
}
