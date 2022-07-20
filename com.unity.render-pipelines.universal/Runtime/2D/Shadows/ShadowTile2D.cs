using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Tile 2D")]
    public class ShadowTile2D : MonoBehaviour
    {
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] Mesh m_Mesh;

        [SerializeField] private int layerID = 0;

        public int LayerID => layerID;

        internal Mesh mesh => m_Mesh;
        internal Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        int m_PreviousPathHash = 0;

        private  void Awake()
        {
            if (m_ShapePath == null || m_ShapePath.Length == 0)
            {
                m_ShapePath = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f),
                };
            }
        }

        protected  void OnEnable()
        {
            if (m_Mesh != null) return;

            m_Mesh = new Mesh();
            ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);
        }

        protected void Update()
        {
            bool rebuildMesh = LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
            if (rebuildMesh)
            {
                ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);
            }
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
