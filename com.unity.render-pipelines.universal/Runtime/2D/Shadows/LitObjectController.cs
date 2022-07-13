using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LitObjectController : MonoBehaviour
{
    public Renderer Renderer;
    public Vector2Int ObjectOffset = Vector2Int.zero;


    private MaterialPropertyBlock propertyBlock;
    private static readonly int Property = Shader.PropertyToID("_ObjectOffset");

    private void Awake()
    {
        Renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        UpdatePropertyBlock();
    }

    private void UpdatePropertyBlock()
    {
        if (Renderer == null) return;

        propertyBlock ??= new MaterialPropertyBlock();

        Renderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetVector(Property,(Vector2)ObjectOffset);

        Renderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        Renderer ??= GetComponent<Renderer>();
        UpdatePropertyBlock();
    }
}
