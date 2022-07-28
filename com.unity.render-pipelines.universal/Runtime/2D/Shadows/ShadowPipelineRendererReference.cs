using System;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [Serializable]
    public class ShadowPipelineRendererReference
    {
        [Tooltip("The renderer to use in the shadow pipeline")]
        public Renderer TargetRenderer;
        [Tooltip("The material to use then rendering. If null the renderer's material will be used")]
        [SerializeField] private Material MaterialOverride;
        [Tooltip("A list of the shader passes to be used when rendering. If null all shader passes will be run")]
        public int[] Passes;

        public Material Material
        {
            get
            {
                if (MaterialOverride != null) return MaterialOverride;
                if (TargetRenderer == null) return null;

#if UNITY_EDITOR
                return TargetRenderer.sharedMaterial;
#else
                return TargetRenderer.material;
#endif
            }

            set
            {
                MaterialOverride = value;
            }
        }
    }
}
