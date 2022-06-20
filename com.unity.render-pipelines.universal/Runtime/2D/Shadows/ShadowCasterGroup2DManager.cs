using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowCasterGroup2DManager
    {
        public static List<ShadowCasterGroup2D> shadowCasterGroups { get; private set; } = null;
        public static List<ShadowCasterGroup2D> shadowCasterGroupsCulled { get; private set; } = null;


        public static void AddShadowCasterGroupToList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            var positionToInsert = 0;
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

            var transformToCheck = shadowMeshCaster.transform.parent;
            while (transformToCheck != null)
            {
                CompositeShadowCaster2D currentGroup;
                if (transformToCheck.TryGetComponent<CompositeShadowCaster2D>(out currentGroup))
                    retGroup = currentGroup;

                transformToCheck = transformToCheck.parent;
            }

            return retGroup;
        }

        public static bool AddToShadowCasterGroup(ShadowCaster2D shadowCaster2D, ref ShadowCasterGroup2D shadowCasterGroup)
        {
            var newShadowCasterGroup = FindTopMostCompositeShadowCaster(shadowCaster2D) as ShadowCasterGroup2D;

            if (newShadowCasterGroup == null)
                newShadowCasterGroup = shadowCaster2D.GetComponent<ShadowCaster2D>();

            if (newShadowCasterGroup == null || shadowCasterGroup == newShadowCasterGroup) return false;

            newShadowCasterGroup.RegisterShadowCaster2D(shadowCaster2D);
            shadowCasterGroup = newShadowCasterGroup;
            return true;

        }

        public static void OptimizeShadows(Rect cameraBounds)
        {
            shadowCasterGroupsCulled ??= new List<ShadowCasterGroup2D>();

            shadowCasterGroupsCulled.Clear();

            foreach (var shadowCasterGroup in shadowCasterGroups)
            {
                if(shadowCasterGroup.OptimizeShadows(cameraBounds))
                    shadowCasterGroupsCulled.Add(shadowCasterGroup);
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

            if (shadowCasterGroups == null)
                shadowCasterGroups = new List<ShadowCasterGroup2D>();

            AddShadowCasterGroupToList(group, shadowCasterGroups);
        }

        public static void RemoveGroup(ShadowCasterGroup2D group)
        {
            if (group != null && shadowCasterGroups != null)
                RemoveShadowCasterGroupFromList(group, shadowCasterGroups);
        }
    }
}
