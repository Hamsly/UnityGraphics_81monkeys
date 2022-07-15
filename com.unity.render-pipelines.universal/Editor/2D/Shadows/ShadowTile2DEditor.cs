using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{

    [CustomEditor(typeof(ShadowTile2D))]
    [CanEditMultipleObjects]
    internal class ShadowTile2DEditor : PathComponentEditor<ShadowCasterPath>
    {
        [EditorTool("Edit Shadow Caster Shape", typeof(ShadowTile2D))]
        class ShadowCaster2DShadowCasterShapeTool : ShadowTile2DShapeTool {};


        public void ShadowCaster2DSceneGUI()
        {
            ShadowTile2D shadowTile = target as ShadowTile2D;

            Transform t = shadowTile.transform;
            Vector3[] shape = shadowTile.shapePath;
            Handles.color = Color.white;

            for (int i = 0; i < shape.Length - 1; ++i)
            {
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[i]), t.TransformPoint(shape[i + 1]) });
            }

            if (shape.Length > 1)
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[shape.Length - 1]), t.TransformPoint(shape[0]) });
        }

        public void ShadowCaster2DInspectorGUI<T>() where T : ShadowTile2DShapeTool
        {
            DoEditButton<T>(PathEditorToolContents.icon, "Edit Shape");
            DoPathInspector<T>();
            DoSnappingInspector<T>();
        }

        public void OnSceneGUI()
        {
            ShadowCaster2DSceneGUI();
        }


        public override void OnInspectorGUI()
        {
           ShadowCaster2DInspectorGUI<ShadowCaster2DShadowCasterShapeTool>();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
