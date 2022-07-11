using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowCasterGroup2DManager
    {
        public static List<ShadowCasterGroup2D> shadowCasterGroups { get; private set; } = null;
        public static List<ShadowCaster2D> shadowCastersCulled = new List<ShadowCaster2D>();

        private static List<ShadowCaster2D> dynamicShadows = new List<ShadowCaster2D>();
        private static List<ShadowCaster2D> staticShadows = new List<ShadowCaster2D>();

        private static bool hasDoneInit = false;

        public ShadowCasterGroup2DManager()
        {
            Shadow2DWorldManager.OnInit += OnShadowWorldInit;

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

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

        private void OnSceneUnloaded(Scene scene)
        {
            ClearAllStaticShadowReferences();
        }

        private static void OnShadowWorldInit()
        {
            foreach (var caster in staticShadows)
            {
                Shadow2DWorldManager.Instance.RegisterStaticShadow(caster);
            }
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

        public static void RegisterStaticShadow(ShadowCaster2D shadowCaster)
        {
            if (!staticShadows.Contains(shadowCaster))
            {
                staticShadows.Add(shadowCaster);
            }

            if (Shadow2DWorldManager.Instance?.HasDoneInit ?? false)
            {
                Shadow2DWorldManager.Instance.RegisterStaticShadow(shadowCaster);
            }
        }

        public static void RegisterDynamicShadow(ShadowCaster2D shadowCaster)
        {
            if (!dynamicShadows.Contains(shadowCaster))
            {
                dynamicShadows.Add(shadowCaster);
            }
        }

        public static void UnregisterDynamicShadow(ShadowCaster2D shadowCaster)
        {
            dynamicShadows.Remove(shadowCaster);
        }

        public static List<ShadowCaster2D> GetDynamicShadows()
        {
            return dynamicShadows;
        }

        public static void ClearAllStaticShadowReferences()
        {
            staticShadows?.Clear();
        }

        public static void OptimizeShadows(Rect cameraBounds)
        {
            if(shadowCasterGroups == null) return;

            AssertLists();

            shadowCastersCulled.Clear();

            if (Shadow2DWorldManager.Instance != null)
            {
                Shadow2DWorldManager.Instance.GetShadowCasters(ref shadowCastersCulled,cameraBounds);
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

            AssertLists();

            AddShadowCasterGroupToList(group, shadowCasterGroups);
        }

        public static void RemoveGroup(ShadowCasterGroup2D group)
        {
            if (group != null && shadowCasterGroups != null)
                RemoveShadowCasterGroupFromList(group, shadowCasterGroups);
        }

        private static void AssertLists()
        {
            if (hasDoneInit) return;

            shadowCasterGroups ??= new List<ShadowCasterGroup2D>();
            shadowCastersCulled ??= new List<ShadowCaster2D>();
            hasDoneInit = true;
        }
    }
}
