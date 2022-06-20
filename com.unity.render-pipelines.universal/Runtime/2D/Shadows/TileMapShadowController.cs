using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public class TileMapShadowController : CompositeShadowCaster2D
    {
        private Tilemap tileMap;
        private TilemapRenderer tilemapRenderer;

        [SerializeField] private bool SilhouetteTileRenderer  = true;

        private void OnEnable()
        {
            base.OnEnable();

            tileMap = GetComponent<Tilemap>();
            tilemapRenderer = GetComponent<TilemapRenderer>();
            UpdateShadowCasters();

#if UNITY_EDITOR
            Tilemap.tilemapTileChanged += OnTileChange;
#endif
        }

        private void OnDisable()
        {
            base.OnDisable();
#if UNITY_EDITOR
            Tilemap.tilemapTileChanged -= OnTileChange;
#endif
        }

#if UNITY_EDITOR
        private static void OnTileChange(Tilemap tileMap, Tilemap.SyncTile[] syncTiles)
        {
            if (tileMap.gameObject.TryGetComponent(out TileMapShadowController tileMapShadowController))
            {
                tileMapShadowController.UpdateShadowCasters();
            }
        }
#endif

        private void OnValidate()
        {
            UpdateShadowCasters();
        }

        private void UpdateShadowCasters()
        {
            if (tilemapRenderer == null) return;


            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);

                if (child.TryGetComponent(out ShadowCaster2D shadowCaster2D))
                {
                    shadowCaster2D.useRendererSilhouette = SilhouetteTileRenderer;
                    shadowCaster2D.AddSilhouettedRenderer(tilemapRenderer);
                }
            }
        }
    }
}
