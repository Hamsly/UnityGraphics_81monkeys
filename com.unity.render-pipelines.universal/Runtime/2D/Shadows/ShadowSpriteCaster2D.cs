using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Sprite Caster 2D")]
    public class ShadowSpriteCaster2D : ShadowCaster2D
    {
        enum SpriteCasterType
        {
            Standing,
            Flat,
        }

        [SerializeField] float m_Direction = 0f;
        [SerializeField] Vector2 m_Size = Vector2.one;
        [SerializeField] Mesh m_Mesh;
        [SerializeField] Texture2D m_Texture;
        [SerializeField] SpriteCasterType m_SpriteCasterType;
        [SerializeField] private bool m_ReorientPerLight;
        [SerializeField,Range(0f,1f)] private float m_basePoint;

        internal static readonly int k_ShadowTexture = Shader.PropertyToID("_ShadowTexture");
        internal static readonly int k_ShadowBasePos = Shader.PropertyToID("_ShadowBasePos");
        internal static readonly int k_ShadowInfo = Shader.PropertyToID("_ShadowInfo");

        private VertexAttributeDescriptor[] vertexLayout;

        private Vector2 basePos1;
        private Vector2 basePos2;

        private float directionPrev = 0;
        private Vector2 sizePrev = Vector2.zero;
        private SpriteCasterType spriteCasterTypePrev = SpriteCasterType.Standing;
        private bool reorientPerLightPrev = false;
        public Texture2D texture
        {
            set
            {
                if (value != m_Texture)
                {
                    m_Texture = value;
                    m_Size = m_Texture.texelSize;
                }
            }
            get { return m_Texture; }
        }
        private new void Awake()
        {
            m_Mesh = null;
            materialType = m_SpriteCasterType == SpriteCasterType.Standing ?
                Renderer2DData.ShadowMaterialTypes.SpriteShadow :
                Renderer2DData.ShadowMaterialTypes.SpriteShadowSimple;

            base.Awake();
        }

        private void GenerateMesh(Vector2 size,float direction, ref Mesh mesh)
        {
            switch (m_SpriteCasterType)
            {
                case SpriteCasterType.Standing:
                    GenerateStandingSpriteMesh(ref mesh);
                    RecalculateBounds();
                    break;

                case SpriteCasterType.Flat:
                    GenerateFlatSpriteMesh(size, direction, ref mesh);
                    RecalculateBounds();
                    break;
            }
        }

        private void RecalculateBounds()
        {
            float rr = 0;
            switch (m_SpriteCasterType)
            {
                case SpriteCasterType.Standing:
                    rr = m_Size.x;
                    break;

                case SpriteCasterType.Flat:
                    rr = m_Size.magnitude;
                    break;
            }

            m_Bounds = new Rect(-rr, -rr, rr * 2, rr * 2);
        }

        private void GenerateStandingSpriteMesh( ref Mesh mesh)
        {
            mesh = null;
        }

        const int NUMBER_OF_VERTS = 5;
        Vector3[] vertices = new Vector3[NUMBER_OF_VERTS];
        Vector2[] UVs = new Vector2[NUMBER_OF_VERTS];
        int[] triangles = new int[4 * 3];

        private void GenerateFlatSpriteMesh(Vector2 size,float direction, ref Mesh mesh)
        {



            //direction -= (Mathf.PI / 2);
            Vector3 wDirectionVector = new Vector3(Mathf.Cos(direction), Mathf.Sin(direction),0);
            Vector3 hDirectionVector = new Vector3(-wDirectionVector.y, wDirectionVector.x, 0);

            float ww = size.x * 0.5f;
            float hh = size.y * 0.5f;
            vertices[0] = (wDirectionVector * -ww) + (hDirectionVector * -hh);
            vertices[1] = (wDirectionVector * -ww) + (hDirectionVector * hh);
            vertices[2] = (wDirectionVector * ww) + (hDirectionVector * -hh);
            vertices[3] = (wDirectionVector * ww) + (hDirectionVector * hh);
            vertices[4] = Vector3.zero;

            UVs[0] = new Vector2(0, 0);
            UVs[1] = new Vector2(0, 1);
            UVs[2] = new Vector2(1, 0);
            UVs[3] = new Vector2(1, 1);
            UVs[4] = new Vector2(0.5f, 0.5f);

            //float dd = UnityEngine.Mathf.Sqrt((size.x * size.x) + (size.y * size.y));

            int i = 0;
            triangles[i++] = 0;
            triangles[i++] = 1;
            triangles[i++] = 4;

            triangles[i++] = 1;
            triangles[i++] = 3;
            triangles[i++] = 4;

            triangles[i++] = 3;
            triangles[i++] = 2;
            triangles[i++] = 4;

            triangles[i++] = 2;
            triangles[i++] = 0;
            triangles[i++] = 4;

            mesh.Clear();

            mesh.name = gameObject.name;

            mesh.vertices = vertices;
            mesh.uv2 = UVs;
            mesh.triangles = triangles;
        }


        public new void Update()
        {
            bool rebuildMesh = LightUtility.CheckForChange(m_Direction, ref directionPrev);
            if (m_Size != sizePrev)
            {
                sizePrev = m_Size;
                rebuildMesh = true;
            }

            if (m_SpriteCasterType != spriteCasterTypePrev)
            {
                spriteCasterTypePrev = m_SpriteCasterType;
                rebuildMesh = true;

                materialType = m_SpriteCasterType == SpriteCasterType.Standing ?
                    Renderer2DData.ShadowMaterialTypes.SpriteShadow :
                    Renderer2DData.ShadowMaterialTypes.SpriteShadowSimple;
            }

            if (m_ReorientPerLight != reorientPerLightPrev)
            {
                reorientPerLightPrev = m_ReorientPerLight;
                rebuildMesh = true;
            }

            if (rebuildMesh)
            {
                if (m_Mesh == null)
                {
                    m_Mesh = new Mesh();
                }

                GenerateMesh(m_Size, m_Direction * Mathf.Deg2Rad, ref m_Mesh);
            }

            base.Update();
        }

        public override void CastShadows(CommandBuffer cmdBuffer, int layerToRender,Light2D light, Material material,int groupIndex)
        {
            if (!CastsShadows || material == null || !IsShadowedLayer(layerToRender)) return;
            cmdBuffer.SetGlobalVector(k_ShadowCenterID, shadowPosition);
            cmdBuffer.SetGlobalTexture(k_ShadowTexture, texture);

            Vector2 lightPos = (Vector2)light.transform.position - new Vector2(shadowPosition.x, shadowPosition.y);
            float dir;

            if (m_ReorientPerLight)
            {
                dir = ((m_Direction + 90) * Mathf.Deg2Rad) + Mathf.Atan2(lightPos.y, lightPos.x);
            }
            else
            {
                dir = m_Direction * Mathf.Deg2Rad;
            }

            cmdBuffer.SetGlobalVector(k_ShadowInfo,new Vector4(m_Size.x,m_Size.y * (1f - m_basePoint),dir,m_basePoint));

            if (m_SpriteCasterType == SpriteCasterType.Standing)
            {
                cmdBuffer.DrawProcedural(transform.localToWorldMatrix,material,-1,MeshTopology.Points,1);
            }
            else
            {
                if (m_ReorientPerLight)
                {
                    GenerateMesh(m_Size, dir, ref m_Mesh);
                }

                cmdBuffer.DrawMesh(m_Mesh, transform.localToWorldMatrix, material,0,-1);
            }
        }

        private void OnDrawGizmosSelected()
        {
            switch (m_SpriteCasterType)
            {
                case SpriteCasterType.Standing:
                    var basePos = transform.position;

                    var dd = (m_Direction) * Mathf.Deg2Rad;
                    Vector3 wDirectionVector = new Vector3(Mathf.Cos(dd), Mathf.Sin(dd), 0);
                    var p1 = basePos + (wDirectionVector * (m_Size.x * 0.5f));
                    var p2 = basePos + (wDirectionVector * (m_Size.x * -0.5f));

                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(p1, p2);
                    break;
            }
        }

        private void OnValidate()
        {
            m_Direction = Mathf.Repeat(m_Direction, 360);
        }

#if UNITY_EDITOR
        void Reset()
        {
            Awake();
            OnEnable();
        }

#endif
    }
}
