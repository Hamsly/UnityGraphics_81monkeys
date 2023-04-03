// Copyright 2020 Connor Andrew Ngo
// Licensed under the MIT License
using System.Numerics;

namespace Auios.QuadTree
{

    /// <summary>Stores the position and size of the quadrant.</summary>
    public class QuadTreeRect
    {
        // Assume X and Y are the top-left of the rectangle
        public float X;
        public float Y;

        // Assume Width and Height extrude towards the bottom-right
        public float Width;
        public float Height;

        // Properties
        public float Top => Y;
        public float Left => X;
        public float Bottom => Y + Height;
        public float Right => X + Width;
        public float HalfWidth => Width * 0.5f;
        public float HalfHeight => Height * 0.5f;
        public float CenterX => X + HalfWidth;
        public float CenterY => Y + HalfHeight;
        public Vector2 Position => new Vector2(X, Y);
        public Vector2 Size => new Vector2(Width, Height);
        public Vector2 Half => new Vector2(HalfWidth, HalfHeight);
        public Vector2 Center => new Vector2(CenterX, CenterY);

        public QuadTreeRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            isOverlapped = false;
        }

        public bool Overlaps(float x, float y)
        {
            if (x < Left || x > Right) return false;
            if (y < Top || y > Bottom) return false;
            return true;
        }

        public bool Overlaps(QuadTreeRect r)
        {
            return Left < r.Right &&
                   Right > r.Left &&
                   Top > r.Bottom &&
                   Bottom < r.Top;
        }

        /// <summary>If an overlap check from the QuadTree returns true on this quadrant this flag will be set to true. Set to false on QuadTree.Clear().</summary>
        public bool isOverlapped;

    }
}
