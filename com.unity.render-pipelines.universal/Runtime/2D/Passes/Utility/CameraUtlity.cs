using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public static class CameraUtility
    {
        public static Rect OrthographicBounds(this Camera camera)
        {
            float screenAspect = (float)Screen.width / (float)Screen.height;
            float cameraHeight = camera.orthographicSize * 2;
            Vector2 size = new Vector2(cameraHeight * screenAspect, cameraHeight);
            Rect bounds = new Rect(
                (Vector2)camera.transform.position - (size * 0.5f),
                size);
            return bounds;
        }
    }
}
