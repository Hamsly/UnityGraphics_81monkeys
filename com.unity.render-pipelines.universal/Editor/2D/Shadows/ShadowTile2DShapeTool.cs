using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    class ShadowTile2DShapeTool : ShadowCaster2DShapeTool
    {
        protected override IShape GetShape(Object target)
        {
            return (target as ShadowTile2D).shapePath.ToPolygon(false);
        }
    }
}
