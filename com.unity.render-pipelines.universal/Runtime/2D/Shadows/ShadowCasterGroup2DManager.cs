using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowCasterGroup2DManager
    {
        static List<ShadowCasterGroup2D> s_ShadowCasterGroups = null;
        static List<ShadowCasterGroup2D> s_ShadowCasterGroupsCulled = null;

        public static List<ShadowCasterGroup2D> shadowCasterGroups { get { return s_ShadowCasterGroups; } }
        public static List<ShadowCasterGroup2D> shadowCasterGroupsCulled { get { return s_ShadowCasterGroups; } }


        public static void AddShadowCasterGroupToList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            int positionToInsert = 0;
            for (positionToInsert = 0; positionToInsert < list.Count; positionToInsert++)
            {
                if (shadowCaster.GetShadowGroup() == list[positionToInsert].GetShadowGroup())
                    break;
            }

            list.Insert(positionToInsert, shadowCaster);
        }

        public static void RemoveShadowCasterGroupFromList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            list.Remove(shadowCaster);
        }

        static CompositeShadowCaster2D FindTopMostCompositeShadowCaster(ShadowCaster2D shadowMeshCaster)
        {
            CompositeShadowCaster2D retGroup = null;

            Transform transformToCheck = shadowMeshCaster.transform.parent;
            while (transformToCheck != null)
            {
                CompositeShadowCaster2D currentGroup;
                if (transformToCheck.TryGetComponent<CompositeShadowCaster2D>(out currentGroup))
                    retGroup = currentGroup;

                transformToCheck = transformToCheck.parent;
            }

            return retGroup;
        }

        public static bool AddToShadowCasterGroup(ShadowCaster2D shadowMeshCaster, ref ShadowCasterGroup2D shadowCasterGroup)
        {
            ShadowCasterGroup2D newShadowCasterGroup = FindTopMostCompositeShadowCaster(shadowMeshCaster) as ShadowCasterGroup2D;

            if (newShadowCasterGroup == null)
                newShadowCasterGroup = shadowMeshCaster.GetComponent<ShadowCaster2D>();

            if (newShadowCasterGroup != null && shadowCasterGroup != newShadowCasterGroup)
            {
                newShadowCasterGroup.RegisterShadowCaster2D(shadowMeshCaster);
                shadowCasterGroup = newShadowCasterGroup;
                return true;
            }

            return false;
        }

        public static void OptimizeShadows(Bounds cameraBounds)
        {
            s_ShadowCasterGroupsCulled ??= new List<ShadowCasterGroup2D>();

            s_ShadowCasterGroupsCulled.Clear();

            foreach (var shadowCasterGroup in s_ShadowCasterGroups)
            {
                if(shadowCasterGroup.OptimizeShadows(cameraBounds))
                    s_ShadowCasterGroupsCulled.Add(shadowCasterGroup);
            }
        }

        public static void RemoveFromShadowCasterGroup(ShadowCaster2D shadowMeshCaster, ShadowCasterGroup2D shadowCasterGroup)
        {
            if (shadowCasterGroup != null)
                shadowCasterGroup.UnregisterShadowCaster2D(shadowMeshCaster);
        }

        public static void AddGroup(ShadowCasterGroup2D group)
        {
            if (group == null)
                return;

            if (s_ShadowCasterGroups == null)
                s_ShadowCasterGroups = new List<ShadowCasterGroup2D>();

            AddShadowCasterGroupToList(group, s_ShadowCasterGroups);
        }

        public static void RemoveGroup(ShadowCasterGroup2D group)
        {
            if (group != null && s_ShadowCasterGroups != null)
                RemoveShadowCasterGroupFromList(group, s_ShadowCasterGroups);
        }
    }
}
