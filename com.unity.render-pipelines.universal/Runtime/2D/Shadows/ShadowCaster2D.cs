using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UnityEngine.Experimental.Rendering.Universal
{
    public class ShadowCaster2D : ShadowCasterGroup2D
    {
        [SerializeField] protected Renderer[] m_SilhouettedRenderers;
        [SerializeField] protected bool m_UseRendererSilhouette = true;
        [SerializeField] private bool m_UseTransformZ = true;
        [SerializeField] private float m_ZPosition = 0f;
        [SerializeField] protected int m_InstanceId;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] public int[] m_ApplyToSortingLayers = null;
        [SerializeField] Renderer2DData.ShadowMaterialTypes m_materialType = Renderer2DData.ShadowMaterialTypes.MeshShadows;

        protected Transform Transform => transform;

        public enum ShadowFilterMode
        {
            None,
            WhiteList,
            BlackList,
        };

        [SerializeField] public ShadowFilterMode m_FilterMode = ShadowFilterMode.None;
        [SerializeField] public List<Light2D> m_FilterLights = new List<Light2D>();

        public bool m_ShadowIsPersistent = false;

        private int tick;
        private static int startTick = 0;
        private const int TICK_COUNT = 0x0F;

        private bool isStatic = false;

        private bool forceUpdate = false;

        protected Rect m_Bounds;

        protected float radius = 0;
        public Rect Bounds
        {
            get
            {
                if (Transform == null) return default;
                var p = Transform.position;
                return new Rect(m_Bounds.x + p.x,m_Bounds.y + p.y,m_Bounds.width,m_Bounds.height);
            }

            set
            {
                m_Bounds = value;
            }
        }

        public float ZPosition
        {
            set => m_ZPosition = value;
            get => m_UseTransformZ ? -Transform.position.z : m_ZPosition;
        }

        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;

        internal ShadowCasterGroup2D m_ShadowCasterGroup = null;
        internal ShadowCasterGroup2D m_PreviousShadowCasterGroup = null;

        internal static readonly int k_ShadowHeightID = Shader.PropertyToID("_ShadowHeight");
        internal static readonly int k_ShadowCenterID = Shader.PropertyToID("_ShadowCenter");

        /// <summary>
        /// If selfShadows is true, useRendererSilhoutte specifies that the renderer's sihouette should be considered part of the shadow. If selfShadows is false, useRendererSilhoutte specifies that the renderer's sihouette should be excluded from the shadow
        /// </summary>
        public bool useRendererSilhouette
        {
            set { m_UseRendererSilhouette = value; }
            get { return m_UseRendererSilhouette && HasSilhouettedRenderer; }
        }

        /// <summary>
        /// Specifies if shadows will be cast.
        /// </summary>
        public bool CastsShadows
        {
            set { m_CastsShadows = value; }
            get { return m_CastsShadows; }
        }

        public virtual bool HasSilhouettedRenderer => (m_SilhouettedRenderers?.Length ?? 0) > 0;

        public Renderer[] SilhouettedRenderer => m_SilhouettedRenderers;

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
                var pos = Transform.position;
                return new Vector3(pos.x, pos.y, ZPosition);
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
            return new int[0];
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
            m_SilhouettedRenderers ??= new Renderer[0];

            if (m_InstanceId != GetInstanceID())
            {
                m_InstanceId = GetInstanceID();
            }

            m_ShadowCasterGroup = null;

            isStatic = gameObject.isStatic;


        }

        protected void Start()
        {
            if (!m_ShadowIsPersistent)
            {
                if (!isStatic)
                {
                    ShadowCasterGroup2DManager.RegisterDynamicShadow(this);
                }
                else
                {
                    ShadowCasterGroup2DManager.RegisterStaticShadow(this);
                }
            }
            else
            {
                ShadowCasterGroup2DManager.RegisterPersistentShadow(this);
            }
        }


        protected void OnDestroy()
        {
            if (!gameObject.isStatic)
            {
                ShadowCasterGroup2DManager.UnregisterDynamicShadow(this);
            }

            if (m_ShadowIsPersistent)
            {
                ShadowCasterGroup2DManager.UnregisterPersistentShadow(this);
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

        public virtual void CastShadows(CommandBuffer cmdBuffer,int layerToRender,Light2D light,Material material, int groupIndex)
        {
        }

        public virtual void ExcludeSilhouettes(CommandBuffer cmdBuffer,int layerToRender,Material material,int groupIndex)
        {
            if (!useRendererSilhouette || !IsShadowedLayer(layerToRender)) return;

            var renderers = SilhouettedRenderer;
            if (renderers == null) return;

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
