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

        [SerializeField] private List<GameObject> shadowObjects;
        [SerializeField] private float shadowHeight = 1;
        [SerializeField] private bool silhouetteTileRenderer  = true;
        [SerializeField] private int unitSize = 4;

        private bool ApplicationIsRunning => Application.isPlaying;

        private new void OnEnable()
        {
            base.OnEnable();

            tileMap = GetComponent<Tilemap>();
            tilemapRenderer = GetComponent<TilemapRenderer>();
/*
#if UNITY_EDITOR
            Tilemap.tilemapTileChanged += OnTileChange;
#endif
*/
        }

        private new void OnDisable()
        {
            base.OnDisable();
            /*
#if UNITY_EDITOR
            Tilemap.tilemapTileChanged -= OnTileChange;
#endif
*/
        }
/*
#if UNITY_EDITOR
        private static void OnTileChange(Tilemap tileMap, Tilemap.SyncTile[] syncTiles)
        {
            if (tileMap.gameObject.TryGetComponent(out TileMapShadowController tileMapShadowController))
            {
                tileMapShadowController.UpdateShadowCasters();
            }
        }
#endif
*/

        private void Start()
        {
            UpdateShadowCasters();
        }

        private void UpdateShadowCasters()
        {
            if (!ApplicationIsRunning) return;
            if (tilemapRenderer == null) return;

            Dictionary<Vector2Int, List<CombineInstance>> shadowPools = new Dictionary<Vector2Int,  List<CombineInstance>>();

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

                if (child.TryGetComponent(out ShadowTile2D shadowCaster))
                {
                    Vector2Int childPos = new Vector2Int(
                        Mathf.RoundToInt(child.position.x / unitSize),
                        Mathf.RoundToInt(child.position.y / unitSize));

                    var combineInstance = new CombineInstance();
                    combineInstance.mesh = shadowCaster.mesh;
                    var mat = child.localToWorldMatrix;

                    mat.m03 -= childPos.x * unitSize;
                    mat.m13 -= childPos.y * unitSize;

                    combineInstance.transform = mat;

                    if (!shadowPools.ContainsKey(childPos))
                        shadowPools.Add(childPos,new List<CombineInstance>());

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

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);

                if (ApplicationIsRunning)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            foreach (var shadowPool in shadowPools)
            {
                var mesh = new Mesh();

                mesh.CombineMeshes(shadowPool.Value.ToArray());

                var obj = new GameObject
                {
                    isStatic = true,
                    name = $"ShadowMesh[{shadowPool.Key.x}_{shadowPool.Key.y}]",
                    transform =
                    {
                        position = new Vector3(shadowPool.Key.x * unitSize, shadowPool.Key.y * unitSize)
                    }
                };

                obj.transform.SetParent(transform);
                var caster = obj.AddComponent<ShadowMeshCaster2D>();

                if (caster != null)
                {
                    caster.SetMesh(mesh);
                    caster.Bounds = new Rect(-unitSize * 0.5f, -unitSize * 0.5f, unitSize, unitSize);

                    if (silhouetteTileRenderer)
                    {
                        caster.AddSilhouettedRenderer(tilemapRenderer);
                    }

                    caster.shadowHeight = shadowHeight;
                }
            }
        }
    }
}
