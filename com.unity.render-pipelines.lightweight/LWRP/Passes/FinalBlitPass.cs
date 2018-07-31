using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class FinalBlitPass : ScriptableRenderPass
    {
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private Material blitMaterial { get; set; }

        public FinalBlitPass(Material blitMaterial)
        {
            this.blitMaterial = blitMaterial;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.descriptor = baseDescriptor;
        }

        public override void Execute(ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            Material material = renderingData.cameraData.isStereoEnabled ? null : blitMaterial;
            RenderTargetIdentifier sourceRT = colorAttachmentHandle.Identifier();

            CommandBuffer cmd = CommandBufferPool.Get("Final Blit Pass");
            cmd.SetGlobalTexture("_BlitTex", sourceRT);

            // We need to handle viewport on a RT. We do it by rendering a fullscreen quad + viewport
            if (!renderingData.cameraData.isDefaultViewport)
            {
                SetRenderTarget(
                    cmd,
                    BuiltinRenderTextureType.CameraTarget,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    ClearFlag.None,
                    Color.black,
                    descriptor.dimension);

                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(renderingData.cameraData.camera.pixelRect);
                LightweightPipeline.DrawFullScreen(cmd, material);
            }
            else
            {
                cmd.Blit(colorAttachmentHandle.Identifier(), BuiltinRenderTextureType.CameraTarget, material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
