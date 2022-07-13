using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public class ShadowCaster2D : ShadowCasterGroup2D , IQuadTreeNode
    {
        [SerializeField] protected Renderer[] m_SilhouettedRenderers;
        [SerializeField] protected bool m_HasRenderer = false;
        [SerializeField] protected bool m_UseRendererSilhouette = true;
        [SerializeField] protected float m_ZPosition = 0f;
        [SerializeField] protected int m_InstanceId;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] int[] m_ApplyToSortingLayers = null;
        [SerializeField] Renderer2DData.ShadowMaterialTypes m_materialType = Renderer2DData.ShadowMaterialTypes.MeshShadows;

        private int tick;
        private static int startTick = 0;
        private const int TICK_COUNT = 0x0F;

        private bool isStatic = false;

        private bool forceUpdate = false;

        protected Rect MBounds;
        public Rect Bounds
        {
            get
            {
                var p = transform.position;
                //return new Rect(MBounds.x + p.x,MBounds.y + p.y,MBounds.width,MBounds.height);
                return new Rect(p.x, p.y,0,0);
            }
        }

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

        public void AddSilhouettedRenderer(Renderer renderer)
        {
            if (!m_SilhouettedRenderers.Contains(renderer))
            {
                Array.Resize(ref m_SilhouettedRenderers, m_SilhouettedRenderers.Length + 1);
                m_SilhouettedRenderers[m_SilhouettedRenderers.Length - 1] = renderer;
            }
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

        protected new void Awake()
        {
            base.Awake();

            ForceUpdate();

            tick = startTick;

            startTick += 1;
            startTick &= TICK_COUNT;


            if (m_ApplyToSortingLayers == null)
                m_ApplyToSortingLayers = SetDefaultSortingLayers();
        }

        protected void OnEnable()
        {
            m_SilhouettedRenderers = new Renderer[0];

            if (m_InstanceId != GetInstanceID())
            {
                m_InstanceId = GetInstanceID();
            }

            m_ShadowCasterGroup = null;

            isStatic = gameObject.isStatic;
        }
        protected void Start()
        {
            if (isStatic)
            {
                ShadowCasterGroup2DManager.RegisterDynamicShadow(this);
            }
            else
            {
                ShadowCasterGroup2DManager.RegisterStaticShadow(this);
            }
        }

        protected void OnDisable()
        {
            if (!gameObject.isStatic)
            {
                ShadowCasterGroup2DManager.UnregisterDynamicShadow(this);
            }
        }

        protected virtual void OnUpdate()
        {

        }

        public void ForceUpdate()
        {
            forceUpdate = true;
        }

        public void Update()
        {
            if (!forceUpdate)
            {
                if (isStatic) return;

                if (tick != 0)
                {
                    tick -= 1;

                    return;
                }

                tick = TICK_COUNT;
            }

            forceUpdate = false;


            OnUpdate();

            m_HasRenderer = (m_SilhouettedRenderers?.Length ?? 0) > 0;

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
