using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]

    [ExecuteInEditMode]
    public class TileMapShadowController : CompositeShadowCaster2D
    {
        private Tilemap tileMap;
        private TilemapRenderer tilemapRenderer;

        [SerializeField,HideInInspector] private int[] LayerMask = new int[0];

        [SerializeField] private ShadowTileLayer[] shadowTileLayers = {
            new ShadowTileLayer
            {
                layerID = 0,
                unitSize = 8,
                shadowHeight = 0.5f,
                shadowZPosition = 0,
                silhouetteTileRenderer = true
            }
        };

        [Serializable]
        public struct ShadowTileLayer
        {
            public int layerID;
            public int unitSize;
            public float shadowHeight;
            public float shadowZPosition;
            public bool silhouetteTileRenderer;
        }

        private bool ApplicationIsRunning => Application.isPlaying;

        private new void OnEnable()
        {
            base.OnEnable();

            tileMap = GetComponent<Tilemap>();
            tilemapRenderer = GetComponent<TilemapRenderer>();
        }

        private new void OnDisable()
        {
            base.OnDisable();
        }
        private void Start()
        {
            foreach (var layer in shadowTileLayers)
            {
                UpdateShadowCasters(layer);
            }
        }

        private void UpdateShadowCasters(ShadowTileLayer currentLayer)
        {
            if (!ApplicationIsRunning) return;
            if (tilemapRenderer == null) return;

            Dictionary<Vector2Int, List<CombineInstance>> shadowPools =
                new Dictionary<Vector2Int, List<CombineInstance>>();

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);

                if (child.gameObject.name.StartsWith("_"))
                {
                    if (ApplicationIsRunning)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }

                    continue;
                }

                ShadowTile2D[] shadowTileCasters = child.GetComponents<ShadowTile2D>();

                foreach (var shadowCaster in shadowTileCasters)
                {
                    if (shadowCaster.LayerID != currentLayer.layerID) continue;

                    Vector2Int childPos = new Vector2Int(
                        Mathf.RoundToInt(child.position.x / currentLayer.unitSize),
                        Mathf.RoundToInt(child.position.y / currentLayer.unitSize));

                    var combineInstance = new CombineInstance();
                    combineInstance.mesh = shadowCaster.mesh;
                    var mat = child.localToWorldMatrix;

                    mat.m03 -= childPos.x * currentLayer.unitSize;
                    mat.m13 -= childPos.y * currentLayer.unitSize;

                    combineInstance.transform = mat;

                    if (!shadowPools.ContainsKey(childPos))
                        shadowPools.Add(childPos, new List<CombineInstance>());

                    shadowPools[childPos].Add(combineInstance);


                    if (ApplicationIsRunning)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }


            foreach (var shadowPool in shadowPools)
            {
                var mesh = new Mesh();

                mesh.CombineMeshes(shadowPool.Value.ToArray());

                var obj = new GameObject
                {
                    isStatic = true,
                    name = $"ShadowMesh_{currentLayer.layerID} [{shadowPool.Key.x}_{shadowPool.Key.y}]",
                    transform =
                    {
                        position = new Vector3(
                            shadowPool.Key.x * currentLayer.unitSize,
                            shadowPool.Key.y * currentLayer.unitSize)
                    }
                };

                obj.transform.SetParent(transform);
                var caster = obj.AddComponent<ShadowMeshCaster2D>();

                if (caster == null) continue;

                caster.SetMesh(mesh);
                caster.Bounds = new Rect(
                    -currentLayer.unitSize * 0.5f,
                    -currentLayer.unitSize * 0.5f,
                    currentLayer.unitSize,
                    currentLayer.unitSize);

                if (currentLayer.silhouetteTileRenderer)
                {
                    caster.AddSilhouettedRenderer(tilemapRenderer);
                }

                caster.ShadowHeight = currentLayer.shadowHeight;
                caster.ZPosition = currentLayer.shadowZPosition;
                caster.m_ApplyToSortingLayers = LayerMask;
            }
        }
    }
}
