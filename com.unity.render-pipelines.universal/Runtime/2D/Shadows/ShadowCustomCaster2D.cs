using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Custom Caster 2D")]
    public class ShadowCustomCaster2D : ShadowCaster2D
    {
        [SerializeField] private Renderer m_targetRenderer;
        [SerializeField] private int[] m_targetPasses = {-1};
        private Material rendererMaterial
        {
            get
            {
                #if UNITY_EDITOR
                return m_targetRenderer?.sharedMaterial;
                #else
                return m_targetRenderer?.material;
                #endif
            }
        }

        private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");
        private new void Awake()
        {
            materialType = Renderer2DData.ShadowMaterialTypes.Custom;

            base.Awake();

            m_SilhouettedRenderers ??= Array.Empty<Renderer>();
        }

        protected new void OnEnable()
        {
            base.OnEnable();

            if (m_InstanceId != GetInstanceID())
            {
                m_InstanceId = GetInstanceID();
            }
        }

        public override void CastShadows(CommandBuffer cmdBuffer, int layerToRender,Light2D light, Material material,int groupIndex)
        {
            if (!castsShadows || m_targetRenderer == null || rendererMaterial == null || !IsShadowedLayer(layerToRender)) return;

            rendererMaterial.SetFloat(k_ShadowStencilGroupID,groupIndex);

            foreach (var passIndex in m_targetPasses)
            {
                cmdBuffer.DrawRenderer(m_targetRenderer,rendererMaterial,0,passIndex);
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
