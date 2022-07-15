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
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Sprite Caster 2D")]
    public class ShadowSpriteCaster2D : ShadowCaster2D
    {
        enum SpriteCasterType
        {
            Standing,
            flat,
        }

        [SerializeField] float m_Direction = 270f;
        [SerializeField] Vector2 m_Size = Vector2.one;
        [SerializeField] Mesh m_Mesh;
        [SerializeField] Texture2D m_Texture;
        [SerializeField] SpriteCasterType m_SpriteCasterType;
        [SerializeField] private bool m_ReorientPerLight;

        internal static readonly int k_ShadowTexture = Shader.PropertyToID("_ShadowTexture");
        internal Mesh mesh => m_Mesh;

        private VertexAttributeDescriptor[] vertexLayout;

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
            materialType = Renderer2DData.ShadowMaterialTypes.SpriteShadow;

            base.Awake();

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                if (m_SilhouettedRenderers.Length < 1)
                {
                    m_SilhouettedRenderers = new Renderer[1];
                    m_SilhouettedRenderers[0] = renderer;
                }
            }

        }

        private void GenerateMesh(Vector2 size,float direction, ref Mesh mesh)
        {
            switch (m_SpriteCasterType)
            {
                case SpriteCasterType.Standing:
                    GenerateStandingSpriteMesh(size, direction, ref mesh);
                    break;

                case SpriteCasterType.flat:
                    GenerateFlatSpriteMesh(size, direction, ref mesh);
                    break;
            }
        }

        private void GenerateStandingSpriteMesh(Vector2 size,float direction, ref Mesh mesh)
        {
            var numberOfVerts = 5;
            Vector3[] vertices = new Vector3[numberOfVerts];
            Vector2[] UVs = new Vector2[numberOfVerts];
            int[] triangles = new int[4 * 3];


            //direction -= (Mathf.PI / 2);
            Vector3 directionVector = new Vector3(Mathf.Cos(direction), Mathf.Sin(direction),0);

            float ww = size.x * 0.5f;
            vertices[0] = directionVector * ww;
            vertices[1] = (directionVector * ww) + (Vector3.forward * size.y);
            vertices[2] = directionVector * -ww;
            vertices[3] = (directionVector * -ww) + (Vector3.forward * size.y);
            vertices[4] = Vector3.forward * (size.y * 0.5f);

            UVs[0] = new Vector2(0, 0);
            UVs[1] = new Vector2(0, 1);
            UVs[2] = new Vector2(1, 0);
            UVs[3] = new Vector2(1, 1);
            UVs[4] = new Vector2(0.5f, 0.5f);

            float dd = UnityEngine.Mathf.Sqrt((size.x * size.x) + (size.y * size.y));

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

            if (mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                mesh.Clear();
            }

            mesh.vertices = vertices;
            mesh.uv2 = UVs;
            mesh.triangles = triangles;
        }

        private void GenerateFlatSpriteMesh(Vector2 size,float direction, ref Mesh mesh)
        {
            var numberOfVerts = 5;
            Vector3[] vertices = new Vector3[numberOfVerts];
            Vector2[] UVs = new Vector2[numberOfVerts];
            int[] triangles = new int[4 * 3];


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

            float dd = UnityEngine.Mathf.Sqrt((size.x * size.x) + (size.y * size.y));

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

            if (mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                mesh.Clear();
            }

            mesh.vertices = vertices;
            mesh.uv2 = UVs;
            mesh.triangles = triangles;
        }

        protected new void OnDisable()
        {
            base.OnDisable();
            ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_ShadowCasterGroup);
        }

        new public void Update()
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
            }

            if (m_ReorientPerLight != reorientPerLightPrev)
            {
                reorientPerLightPrev = m_ReorientPerLight;
                rebuildMesh = true;
            }

            if (rebuildMesh)
            {
                GenerateMesh(m_Size, m_Direction * Mathf.Deg2Rad, ref m_Mesh);
            }

            base.Update();
        }


        public override void CastShadows(CommandBuffer cmdBuffer, int layerToRender,Light2D light, Material material)
        {
            if (!castsShadows || material == null || !IsShadowedLayer(layerToRender)) return;
            cmdBuffer.SetGlobalVector(k_ShadowCenterID, shadowPosition);
            cmdBuffer.SetGlobalTexture(k_ShadowTexture, texture);

            Mesh meshToRender = m_Mesh;
            if (m_ReorientPerLight)
            {
                Vector2 LightPos = (Vector2)light.transform.position - new Vector2(shadowPosition.x, shadowPosition.y);
                float dir = ((m_Direction + 90) * Mathf.Deg2Rad) + Mathf.Atan2(LightPos.y , LightPos.x);

                m_Mesh = null;
                GenerateMesh(m_Size, dir , ref m_Mesh);

                meshToRender = m_Mesh;
            }

            cmdBuffer.DrawMesh(meshToRender, transform.localToWorldMatrix, material);
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
