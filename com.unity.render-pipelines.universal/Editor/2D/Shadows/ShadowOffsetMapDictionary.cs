using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ShadowOffsetMapDictionary", menuName = "Shadow2D/Spawn Shadow Offset Map Dictionary", order = 1)]
public class ShadowOffsetMapDictionary : ScriptableObject
{
    [System.Serializable]
    public struct ShadowTexturePair
    {
        public Texture2D SourceTexture;
        public Texture2D OffsetTexture;

        public ShadowTexturePair(Texture2D sourceTexture, Texture2D offsetTexture)
        {
            SourceTexture = sourceTexture;
            OffsetTexture = offsetTexture;
        }
    }

    public List<ShadowTexturePair> ShadowTexturePairs = new List<ShadowTexturePair>();

    public void OnEnable()
    {
        ValidateAllPairs();
    }

    private void ValidateAllPairs()
    {
        foreach (var pair in ShadowTexturePairs)
        {
            if (pair.SourceTexture == null || pair.OffsetTexture == null)
            {
#if UNITY_EDITOR
                if (pair.SourceTexture == null && pair.OffsetTexture != null)
                {
                    var path = AssetDatabase.GetAssetPath(pair.OffsetTexture);
                    AssetDatabase.DeleteAsset(path);
                }
#endif
                ShadowTexturePairs.Remove(pair);
            }
            else
            {
                if (pair.SourceTexture.texelSize != pair.OffsetTexture.texelSize)
                {
                    pair.OffsetTexture.Resize(pair.SourceTexture.width, pair.SourceTexture.height);
                }
            }
        }
    }

    public void AddShadowTexPair(Texture2D sourceTexture, Texture2D offsetTexture)
    {
        if (offsetTexture != null && sourceTexture != null)
        {
            ShadowTexturePairs.Add(new ShadowTexturePair(sourceTexture, offsetTexture));
        }

        ValidateAllPairs();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    public Texture2D GetShadowTexture(Texture2D sourceTexture)
    {
        // TODO: Check if the current texture pair is valid (Size) and handle it before returning

        for (int i = 0; i < ShadowTexturePairs.Count; i++)
        {
            if (ShadowTexturePairs[i].SourceTexture == sourceTexture)
            {
                return ShadowTexturePairs[i].OffsetTexture;
            }
        }

        return null;
    }

    /// <summary>
    /// Generate a "blank" offset texture with the dimensions of the given source texture
    /// </summary>
    /// <param name="sourceTexture"></param>
    /// <returns></returns>
    public static Texture2D GenerateNewWorkingTexture(Texture2D sourceTexture)
    {
        Texture2D newTex = new Texture2D(sourceTexture.width, sourceTexture.height);

        var fillColor = new Color(0.5f, 0, 0.5f, 1);
        var fillColorArray = newTex.GetPixels();

        for(var i = 0; i < fillColorArray.Length; ++i)
        {
            fillColorArray[i] = fillColor;
        }

        newTex.filterMode = FilterMode.Point;
        newTex.wrapMode = TextureWrapMode.Clamp;
        newTex.SetPixels( fillColorArray );
        newTex.Apply();

#if UNITY_EDITOR

        var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Textures/ShadowOffsetMaps/" + sourceTexture.name + "_ShadowOffset.renderTexture");
        AssetDatabase.CreateAsset(newTex,path);

#endif

        return newTex;
    }
}
