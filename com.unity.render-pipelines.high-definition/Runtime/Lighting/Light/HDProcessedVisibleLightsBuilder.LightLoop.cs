using System;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class HDProcessedVisibleLightsBuilder
    {
        private void SortLightKeys()
        {
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.SortVisibleLights)))
            {
                //Tunning against ps4 console,
                //32 items insertion sort has a workst case of 3 micro seconds.
                //200 non recursive merge sort has around 23 micro seconds.
                //From 200 and more, Radix sort beats everything.
                var sortSize = sortedLightCounts;
                if (sortSize <= 32)
                    CoreUnsafeUtils.InsertionSort(m_SortKeys, sortSize);
                else if (m_Size <= 200)
                    CoreUnsafeUtils.MergeSort(m_SortKeys, sortSize, ref m_SortSupportArray);
                else
                    CoreUnsafeUtils.RadixSort(m_SortKeys, sortSize, ref m_SortSupportArray);
            }
        }

        private void BuildVisibleLightEntities(in CullingResults cullResults)
        {
            m_Size = 0;

            if (!m_ProcessVisibleLightCounts.IsCreated)
            {
                int totalCounts = Enum.GetValues(typeof(ProcessLightsCountSlots)).Length;
                m_ProcessVisibleLightCounts.ResizeArray(totalCounts);
            }

            for (int i = 0; i < m_ProcessVisibleLightCounts.Length; ++i)
                m_ProcessVisibleLightCounts[i] = 0;

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.BuildVisibleLightEntities)))
            {
                int totalLightCount = GetTotalLightCount(cullResults);

                if (totalLightCount == 0 || HDLightRenderDatabase.instance == null)
                    return;

                if (totalLightCount > m_Capacity)
                {
                    ResizeArrays(totalLightCount);
                }

                m_Size = totalLightCount;

                //TODO: this should be accelerated by a c++ API
                var defaultEntity = HDLightRenderDatabase.instance.GetDefaultLightEntity();
                for (int i = 0; i < totalLightCount; ++i)
                {
                    Light light = GetLightByIndex(cullResults, i);

                    int dataIndex = HDLightRenderDatabase.instance.FindEntityDataIndex(light);
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                    {
                        //Shuriken lights bullshit: this happens because shuriken lights dont have the HDAdditionalLightData OnEnabled.
                        //Because of this, we have to forcefully create a light render entity on the rendering side. Horrible!!!
                        if (light.TryGetComponent<HDAdditionalLightData>(out var hdAdditionalLightData))
                        {
                            if (!hdAdditionalLightData.lightEntity.valid)
                                hdAdditionalLightData.CreateHDLightRenderEntity(autoDestroy: true);
                        }
                        else
                            dataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(defaultEntity);
                    }
                    // custom-begin:
#if UNITY_EDITOR
                    else if (UnityEditor.SceneVisibilityManager.instance.IsHidden(light.gameObject))
                    {
                        dataIndex = HDLightRenderDatabase.InvalidDataIndex;
                    }
#endif
                    // custom-end

                    m_VisibleLightEntityDataIndices[i] = dataIndex;
                    m_VisibleLightBakingOutput[i] = light.bakingOutput;
                    m_VisibleLightShadowCasterMode[i] = light.lightShadowCasterMode;
                    m_VisibleLightShadows[i] = light.shadows;
                    m_VisibleLightBounceIntensity[i] = light.bounceIntensity;
                }
            }
        }

        private void ProcessShadows(
            HDCamera hdCamera,
            HDShadowManager shadowManager,
            in HDShadowInitParameters inShadowInitParameters,
            in CullingResults cullResults)
        {
            int shadowLights = m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.ShadowLights];
            if (shadowLights == 0)
                return;

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.ProcessShadows)))
            {
                NativeArray<VisibleLight> visibleLights = cullResults.visibleLights;
                var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();

                var defaultEntity = HDLightRenderDatabase.instance.GetDefaultLightEntity();
                int defaultEntityDataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(defaultEntity);

                unsafe
                {
                    HDProcessedVisibleLight* entitiesPtr = (HDProcessedVisibleLight*)m_ProcessedEntities.GetUnsafePtr<HDProcessedVisibleLight>();
                    for (int i = 0; i < shadowLights; ++i)
                    {
                        int lightIndex = m_ShadowLightsDataIndices[i];
                        HDProcessedVisibleLight* entity = entitiesPtr + lightIndex;
                        HDAdditionalLightData additionalLightData = HDLightRenderDatabase.instance.hdAdditionalLightData[entity->dataIndex];

                        if (additionalLightData == null)
                            continue;

                        if ((!cullResults.GetShadowCasterBounds(lightIndex, out var bounds) && additionalLightData.shadowUpdateMode != ShadowUpdateMode.OnDemand) || defaultEntityDataIndex == entity->dataIndex)
                        {
                            entity->shadowMapFlags = ShadowMapFlags.None;
                            continue;
                        }

                        
                        VisibleLight visibleLight = visibleLights[lightIndex];
                        additionalLightData.ReserveShadowMap(hdCamera.camera, shadowManager, hdShadowSettings, inShadowInitParameters, visibleLight, entity->lightType);
                    }
                }
            }
        }

        private void FilterVisibleLightsByAOV(AOVRequestData aovRequest)
        {
            if (!aovRequest.hasLightFilter)
                return;

            for (int i = 0; i < m_Size; ++i)
            {
                var dataIndex = m_VisibleLightEntityDataIndices[i];
                if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                    continue;

                var go = HDLightRenderDatabase.instance.aovGameObjects[dataIndex];
                if (go == null)
                    continue;

                if (!aovRequest.IsLightEnabled(go))
                    m_VisibleLightEntityDataIndices[i] = HDLightRenderDatabase.InvalidDataIndex;
            }
        }

        protected abstract int GetTotalLightCount(in CullingResults cullResults);
        protected abstract Light GetLightByIndex(in CullingResults cullResults, int index);
    }

    internal partial class HDProcessedVisibleLightsRegularBuilder : HDProcessedVisibleLightsBuilder
    {
        protected override int GetTotalLightCount(in CullingResults cullResults)
        {
            return cullResults.visibleLights.Length;
        }

        protected override Light GetLightByIndex(in CullingResults cullResults, int index)
        {
            return cullResults.visibleLights[index].light;
        }
    }

    internal partial class HDProcessedVisibleLightsDynamicBuilder : HDProcessedVisibleLightsBuilder
    {
        protected override int GetTotalLightCount(in CullingResults cullResults)
        {
            return cullResults.visibleLights.Length + cullResults.visibleOffscreenVertexLights.Length;
        }

        protected override Light GetLightByIndex(in CullingResults cullResults, int index)
        {
            if (index < cullResults.visibleLights.Length)
            {
                return cullResults.visibleLights[index].light;
            }
            else
            {
                int offScreenLightIndex = index - cullResults.visibleLights.Length;
                return cullResults.visibleOffscreenVertexLights[offScreenLightIndex].light;
            }
        }
    }

}
