using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public abstract class ShadowCasterGroup2D : MonoBehaviour
    {
        [SerializeField] internal int m_ShadowGroup = 0;
        List<ShadowCaster2D> m_ShadowCasters;
        List<ShadowCaster2D> m_ShadowCastersCulled;

        public List<ShadowCaster2D> GetShadowCasters() { return m_ShadowCasters; }
        public List<ShadowCaster2D> GetShadowCastersCulled() { return m_ShadowCastersCulled; }

        public int GetShadowGroup() { return m_ShadowGroup; }

        public void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            m_ShadowCasters ??= new List<ShadowCaster2D>();

            m_ShadowCasters.Add(shadowCaster2D);
        }

        public void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D)
        {
            if (m_ShadowCasters != null)
                m_ShadowCasters.Remove(shadowCaster2D);
        }

        public bool OptimizeShadows(Bounds cameraBounds)
        {
            m_ShadowCasters ??= new List<ShadowCaster2D>();
            m_ShadowCastersCulled ??= new List<ShadowCaster2D>();

            m_ShadowCastersCulled.Clear();

            foreach (var shadowCaster in m_ShadowCasters)
            {
                if(shadowCaster == null) continue;

                var b = shadowCaster.MeshBounds;
                b.center += shadowCaster.transform.position;
                if(!cameraBounds.Intersects(b)) continue;

                m_ShadowCastersCulled.Add(shadowCaster);
            }

            return m_ShadowCastersCulled.Count > 0;
        }
    }
}
