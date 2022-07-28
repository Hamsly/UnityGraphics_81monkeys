using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowCasterGroup2DManager
    {
        private static List<ShadowCasterGroup2D> shadowCasterGroups = new List<ShadowCasterGroup2D>();
        public static List<ShadowCaster2D> ShadowCastersCulled = new List<ShadowCaster2D>();

        private static List<ShadowCaster2D> dynamicShadows = new List<ShadowCaster2D>();
        private static List<ShadowCaster2D> staticShadows = new List<ShadowCaster2D>();
        private static List<ShadowCaster2D> persistentShadows = new List<ShadowCaster2D>();

        //private static bool hasDoneInit = false;

        public ShadowCasterGroup2DManager()
        {
            ShadowRealm2D.OnInit += OnShadowWorldInit;

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
                ShadowRealm2D.Instance.RegisterStaticShadow(caster);
            }
        }

        static CompositeShadowCaster2D FindTopMostCompositeShadowCaster(ShadowCaster2D shadowMeshCaster)
        {
            CompositeShadowCaster2D retGroup = null;

            var transformToCheck = shadowMeshCaster.transform.parent;
            while (transformToCheck != null)
            {
                if (transformToCheck.TryGetComponent<CompositeShadowCaster2D>(out var currentGroup))
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

            if (ShadowRealm2D.Instance is { HasDoneInit: true })
            {
                ShadowRealm2D.Instance.RegisterStaticShadow(shadowCaster);
            }
        }

        public static void RegisterDynamicShadow(ShadowCaster2D shadowCaster)
        {
            if (!dynamicShadows.Contains(shadowCaster))
            {
                dynamicShadows.Add(shadowCaster);
            }
        }

        public static void RegisterPersistentShadow(ShadowCaster2D shadowCaster)
        {
            if (!persistentShadows.Contains(shadowCaster))
            {
                persistentShadows.Add(shadowCaster);
            }
        }

        public static void UnregisterDynamicShadow(ShadowCaster2D shadowCaster)
        {
            dynamicShadows.Remove(shadowCaster);
        }

        public static void UnregisterPersistentShadow(ShadowCaster2D shadowCaster)
        {
            persistentShadows.Remove(shadowCaster);

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

            ShadowCastersCulled.Clear();
            ShadowCastersCulled.AddRange(persistentShadows);

            if (ShadowRealm2D.Instance != null)
            {
                ShadowRealm2D.Instance.GetShadowCasters(ref ShadowCastersCulled,cameraBounds);
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

            AddShadowCasterGroupToList(group, shadowCasterGroups);
        }

        public static void RemoveGroup(ShadowCasterGroup2D group)
        {
            if (group != null && shadowCasterGroups != null)
                RemoveShadowCasterGroupFromList(group, shadowCasterGroups);
        }

    }
}
