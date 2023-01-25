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
        public ShadowPipelineRendererReference m_RenderRef;

        public ShadowPipelineRendererReference[] m_SilhouettedRendererRefs;
        public override bool HasSilhouettedRenderer => (m_SilhouettedRendererRefs?.Length ?? 0) > 0;

        private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");
        private new void Awake()
        {
            materialType = Renderer2DData.ShadowMaterialTypes.Custom;

            base.Awake();
        }

        protected new void OnEnable()
        {
            base.OnEnable();

            m_RenderRef ??= new ShadowPipelineRendererReference();

            if (m_InstanceId != GetInstanceID())
            {
                m_InstanceId = GetInstanceID();
            }
        }

        public override void CastShadows(CommandBuffer cmdBuffer, int layerToRender,Light2D light, Material material,int groupIndex)
        {
            if (!CastsShadows || m_RenderRef == null || m_RenderRef.Material == null || !IsShadowedLayer(layerToRender)) return;

            m_RenderRef.Material.SetFloat(k_ShadowStencilGroupID,groupIndex);

            if (m_RenderRef.Passes.Length > 0)
            {
                foreach (var passIndex in m_RenderRef.Passes)
                {
                    cmdBuffer.DrawRenderer(m_RenderRef.TargetRenderer, m_RenderRef.Material, 0, passIndex);
                }
            }
            else
            {
                cmdBuffer.DrawRenderer(m_RenderRef.TargetRenderer, m_RenderRef.Material, 0, -1);
            }
        }

        public override void ExcludeSilhouettes(CommandBuffer cmdBuffer,int layerToRender,Material material,int groupIndex)
        {

            if (!useRendererSilhouette || !IsShadowedLayer(layerToRender)) return;

            if (m_SilhouettedRendererRefs == null) return;

            foreach (var currentRenderer in m_SilhouettedRendererRefs)
            {
                if (currentRenderer == null) continue;
                if (currentRenderer.TargetRenderer == null || currentRenderer.Material == null) continue;

                var targetMaterial = currentRenderer.Material;

                targetMaterial.SetFloat(k_ShadowStencilGroupID,groupIndex);

                if (currentRenderer.Passes.Length > 0)
                {
                    foreach (var passIndex in currentRenderer.Passes)
                    {
                        cmdBuffer.DrawRenderer(currentRenderer.TargetRenderer, targetMaterial, 0, passIndex);
                    }
                }
                else
                {
                    cmdBuffer.DrawRenderer(currentRenderer.TargetRenderer, targetMaterial, 0, -1);
                }
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
