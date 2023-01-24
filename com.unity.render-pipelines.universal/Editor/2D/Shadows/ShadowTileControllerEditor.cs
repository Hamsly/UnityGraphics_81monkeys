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
            public static GUIContent filterType = EditorGUIUtility.TrTextContent("Filter Type", "Determines how the shadow will discriminate light sources");
            public static GUIContent filterLights = EditorGUIUtility.TrTextContent("Filter Lights", "The List of lights the shadow will use when discriminating light sources");
        }


        SortingLayerDropDown SortingLayerDropDown;

        SerializedProperty m_FilterType;
        SerializedProperty m_FilterLights;

        // Start is called before the first frame update
        public void OnEnable()
        {
            SortingLayerDropDown = new SortingLayerDropDown();
            SortingLayerDropDown.OnEnable(serializedObject, "LayerMask");

            m_FilterType = serializedObject.FindProperty("filterMode");
            m_FilterLights = serializedObject.FindProperty("filterLights");
        }

        public override void OnInspectorGUI()
        {
            //TileMapShadowController tileMapShadowController = serializedObject.targetObject as TileMapShadowController;
            serializedObject.Update();

            DrawDefaultInspector();

            SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            EditorGUILayout.PropertyField(m_FilterType, Styles.filterType);
            if (m_FilterType.enumValueIndex != 0)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(m_FilterLights, Styles.filterLights);
                EditorGUI.indentLevel -= 1;
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
}
