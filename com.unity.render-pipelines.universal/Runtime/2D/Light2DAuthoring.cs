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

            Vector3 iconPos = transform.position + new Vector3(0, transform.position.z, 0);

            Gizmos.color = m_Color;

            Vector3 cubeSize = new Vector3(0.1f, transform.position.z, 0);
            Gizmos.DrawWireCube(Vector3.Lerp(transform.position, iconPos, 0.5f), cubeSize);

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
