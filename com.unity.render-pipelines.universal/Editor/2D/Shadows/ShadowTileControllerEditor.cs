using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(TileMapShadowController))]
    [CanEditMultipleObjects]
    public class ShadowTileControllerEditor : Editor
    {

        private static class Styles
        {
            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");
        }


        SortingLayerDropDown SortingLayerDropDown;

        // Start is called before the first frame update
        public void OnEnable()
        {
            SortingLayerDropDown = new SortingLayerDropDown();
            SortingLayerDropDown.OnEnable(serializedObject, "LayerMask");
        }

        public override void OnInspectorGUI()
        {
            ShadowSpriteCaster2D shadowSpriteCaster = serializedObject.targetObject as ShadowSpriteCaster2D;
            serializedObject.Update();

            DrawDefaultInspector();

            SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            serializedObject.ApplyModifiedProperties();
        }

    }
}
