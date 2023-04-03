using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public abstract class ShadowCasterGroup2D : MonoBehaviour
    {
        public static int nextGroupID = 0;
        internal int m_ShadowGroup;
        List<ShadowCaster2D> m_ShadowCasters;

        private bool hasDoneInit = false;

        protected void Awake()
        {
            m_ShadowGroup = nextGroupID++;
        }

        public int GetShadowGroup(){ return m_ShadowGroup; }

        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            AssertLists();

            m_ShadowCasters.Add(shadowCaster2D);

            shadowCaster2D.m_ShadowGroup = m_ShadowGroup;
            shadowCaster2D.ForceUpdate();
        }

        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters == null) return;

            m_ShadowCasters.Remove(shadowCaster2D);

            shadowCaster2D.m_ShadowGroup = nextGroupID++;
            shadowCaster2D.ForceUpdate();
        }

        private void AssertLists()
        {
            if (hasDoneInit) return;

            m_ShadowCasters ??= new List<ShadowCaster2D>();

            hasDoneInit = true;
        }
    }
}
