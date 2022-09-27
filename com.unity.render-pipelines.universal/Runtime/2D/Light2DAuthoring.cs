using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
    {
#if UNITY_EDITOR
        private const string s_IconsPath = "Packages/com.unity.render-pipelines.universal/Editor/2D/Resources/SceneViewIcons/";
        private static readonly string[] s_LightIconFileNames = new[]
        {
            "ParametricLight.png",
            "FreeformLight.png",
            "SpriteLight.png",
            "PointLight.png",
            "PointLight.png"
        };

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            var p = transform.position;
            p.z = -p.z;

            Vector3 iconPos = p + new Vector3(0, p.z, 0);

            Gizmos.color = m_Color;

            Vector3 cubeSize = new Vector3(0.1f, Mathf.Abs(p.z), 0);
            Gizmos.DrawWireCube(Vector3.Lerp(p, iconPos, 0.5f), cubeSize);

            Gizmos.DrawIcon(iconPos, s_IconsPath + s_LightIconFileNames[(int)m_LightType], true, Gizmos.color);
        }

        void Reset()
        {
            m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
        }

        internal List<Vector2> GetFalloffShape()
        {
            return LightUtility.GetOutlinePath(m_ShapePath, m_ShapeLightFalloffSize);
        }

#endif
    }
}
