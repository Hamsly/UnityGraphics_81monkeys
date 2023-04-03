// Copyright 2020 Connor Andrew Ngo
// Licensed under the MIT License

using UnityEngine;

namespace Auios.QuadTree
{
    public interface IQuadTreeObjectBounds<in T>
    {
        Rect GetRect(T obj);
        float GetTop(T obj);
        float GetBottom(T obj);
        float GetLeft(T obj);
        float GetRight(T obj);
    }
}
