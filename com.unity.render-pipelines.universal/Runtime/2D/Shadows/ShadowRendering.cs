using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class ShadowRendering
    {
        private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");
        private static readonly int k_ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");
        private static readonly int k_ShadowVolumeIntensityID = Shader.PropertyToID("_ShadowVolumeIntensity");
        private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");

        private static readonly int k_MainTex_TexelSize = Shader.PropertyToID("_MainTex_TexelSize");
        private static readonly int k_BlurStrength = Shader.PropertyToID("_BlurStrength");


        private static RenderTargetHandle m_WorkingTexture;
        private static RenderTargetHandle[] m_RenderTargets = null;
        public static  uint maxTextureCount { get; private set; }

        public static void InitializeBudget(uint maxTextureCount)
        {
            if (m_RenderTargets == null || m_RenderTargets.Length != maxTextureCount)
            {
                m_RenderTargets = new RenderTargetHandle[maxTextureCount];
                ShadowRendering.maxTextureCount = maxTextureCount;

                m_WorkingTexture.id = Shader.PropertyToID($"_WorkingTexture");

                for (int i = 0; i < maxTextureCount; i++)
                {
                    unsafe
                    {
                        m_RenderTargets[i].id = Shader.PropertyToID($"ShadowTex_{i}");
                    }
                }
            }
        }

        public static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int shadowIndex)
        {
            CreateShadowRenderTexture(pass, m_RenderTargets[shadowIndex], renderingData, cmdBuffer);
        }

        public static void PrerenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, int shadowIndex, float shadowIntensity)
        {
            // Render the shadows for this light
            RenderShadows(pass, renderingData, cmdBuffer, layerToRender, light, shadowIntensity, m_RenderTargets[shadowIndex].Identifier());
        }

        public static void SetGlobalShadowTexture(CommandBuffer cmdBuffer, Light2D light, int shadowIndex)
        {
            cmdBuffer.SetGlobalTexture("_ShadowTex", m_RenderTargets[shadowIndex].Identifier());
            cmdBuffer.SetGlobalFloat(k_ShadowIntensityID, 1 - light.shadowIntensity);
            cmdBuffer.SetGlobalFloat(k_ShadowVolumeIntensityID, 1 - light.shadowVolumeIntensity);
        }

        public static void DisableGlobalShadowTexture(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalFloat(k_ShadowIntensityID, 1);
            cmdBuffer.SetGlobalFloat(k_ShadowVolumeIntensityID, 1);
        }

        private static void CreateShadowRenderTexture(IRenderPass2D pass, RenderTargetHandle rtHandle, RenderingData renderingData, CommandBuffer cmdBuffer)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightRenderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 24;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            cmdBuffer.GetTemporaryRT(rtHandle.id, descriptor, FilterMode.Point);
        }

        public static void ReleaseShadowRenderTexture(CommandBuffer cmdBuffer, int shadowIndex)
        {
            cmdBuffer.ReleaseTemporaryRT(m_RenderTargets[shadowIndex].id);
        }

        private static Material GetShadowMaterial(this Renderer2DData rendererData,Renderer2DData.ShadowMaterialTypes shadowMaterialType, int index)
        {
            var shadowMaterialIndex = index % 255;
            if (rendererData.shadowMaterials[(int)shadowMaterialType,shadowMaterialIndex] == null)
            {
                rendererData.shadowMaterials[(int)shadowMaterialType,shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.GetShaderType(shadowMaterialType));
                rendererData.shadowMaterials[(int)shadowMaterialType,shadowMaterialIndex].SetFloat(k_ShadowStencilGroupID, shadowMaterialIndex);
                //Debug.Log("Built " + shadowMaterialType + " material " + shadowMaterialIndex);
            }

            return rendererData.shadowMaterials[(int)shadowMaterialType,shadowMaterialIndex];
        }

        private static Material GetRemoveSelfShadowMaterial(this Renderer2DData rendererData, int index)
        {
            var shadowMaterialIndex = index % 255;


            if (rendererData.removeSelfShadowMaterials[shadowMaterialIndex] == null)
            {
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex] = CoreUtils.CreateEngineMaterial(rendererData.removeSelfShadowShader);
                rendererData.removeSelfShadowMaterials[shadowMaterialIndex].SetFloat(k_ShadowStencilGroupID, shadowMaterialIndex);
                //Debug.Log("Built Remove Self material " + shadowMaterialIndex);
            }

            return rendererData.removeSelfShadowMaterials[shadowMaterialIndex];
        }

        public static void RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture)
        {
            cmdBuffer.SetRenderTarget(renderTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            cmdBuffer.ClearRenderTarget(true, true, Color.black);  // clear stencil

            var lightPosition = light.transform.position;
            cmdBuffer.SetGlobalVector(k_LightPosID, lightPosition);

            var lightRange = Mathf.Max(light.pointLightOuterRadius * 2,4);

            var incrementingGroupIndex = 0;
            var previousShadowGroupIndex = -1;

            var shadowCasters = ShadowCasterGroup2DManager.shadowCastersCulled;
            if (shadowCasters != null && shadowCasters.Count > 0)
            {
                var silhouettes = new List<ShadowCaster2D>();

                // Draw the shadow casting group first, then draw the silhouettes..
                foreach (var shadowCaster in shadowCasters)
                {
                    if (shadowCaster == null) continue;
                    //if(!lightRect.Overlaps(shadowCaster.Bounds))  continue;

                    var shadowGroupIndex = shadowCaster.GetShadowGroup();
                    if (LightUtility.CheckForChange(shadowGroupIndex, ref previousShadowGroupIndex))
                    {
                        if (previousShadowGroupIndex != -1)
                        {
                            var removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(incrementingGroupIndex);
                            foreach (var silhouette in silhouettes)
                            {
                                silhouette.ExcludeSilhouettes(cmdBuffer,layerToRender,removeSelfShadowMaterial);
                            }

                            silhouettes.Clear();
                        }

                        incrementingGroupIndex++;
                    }

                    var xDiff = shadowCaster.Bounds.x - lightPosition.x;
                    var yDiff = shadowCaster.Bounds.y - lightPosition.y;
                    if(xDiff > lightRange || xDiff < -lightRange ||
                       yDiff > lightRange || yDiff < -lightRange)
                        continue;

                    var shadowMaterial = pass.rendererData.GetShadowMaterial(shadowCaster.materialType,incrementingGroupIndex);
                    shadowCaster.CastShadows(cmdBuffer,layerToRender,light,shadowMaterial);

                    if (shadowCaster.useRendererSilhouette)
                    {
                        silhouettes.Add(shadowCaster);
                    }
                }

                /// Run the masking one last time.
                var mat = pass.rendererData.GetRemoveSelfShadowMaterial(incrementingGroupIndex);
                foreach (var silhouette in silhouettes)
                {
                    silhouette.ExcludeSilhouettes(cmdBuffer,layerToRender,mat);
                }
            }
        }
    }
}
