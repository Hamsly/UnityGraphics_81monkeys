using System.Collections;
using System.Collections.Generic;
using PlasticGui;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{

    [CustomEditor(typeof(ShadowCustomCaster2D))]
    [CanEditMultipleObjects]
    internal class ShadowCustomCaster2DEditor : Editor
    {
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool
        {
        };

        class ShadowCaster2DShadowCasterRectangleTool : ShadowCaster2DShapeTool
        {
        };

        class ShadowCaster2DShadowCasterEllipseTool : ShadowCaster2DShapeTool
        {
        };


        private static class Styles
        {

            public static GUIContent useSilhouettedRenderer = EditorGUIUtility.TrTextContent("Use Silhouetted Renderers", "Enable or disable the Silhouetted Renderers");

            public static GUIContent silhouettedRenderer = EditorGUIUtility.TrTextContent("Silhouetted Renderers", "The Renderers to use for the Silhouette");

            public static GUIContent renderer = EditorGUIUtility.TrTextContent("Renderer", "The Renderer to call upon when drawing the shadow");

            public static GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");

            public static GUIContent useTransformZ = EditorGUIUtility.TrTextContent("Use TransformZ", "Use the transform's Z as the simulated Z Position of the shadow");

            public static GUIContent zPosition = EditorGUIUtility.TrTextContent("Shadow ZPosition", "The simulated Z Position of the shadow");

            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");

            public static GUIContent isPersistent = EditorGUIUtility.TrTextContent("Shadow Is Persistent", "Shadow will always draw, not optimized");

            public static GUIContent filterType = EditorGUIUtility.TrTextContent("Filter Type", "Determines how the shadow will discriminate light sources");

            public static GUIContent filterLights = EditorGUIUtility.TrTextContent("Filter Lights", "The List of lights the shadow will use when discriminating light sources");

        }

        SerializedProperty m_UseRendererSilhouette;
        SerializedProperty m_SilhouettedRenderers;
        SerializedProperty m_Renderer;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_UseTransformZ;
        SerializedProperty m_ZPosition;
        SerializedProperty m_ShadowIsPersistent;
        SerializedProperty m_FilterType;
        SerializedProperty m_FilterLights;


        SortingLayerDropDown m_SortingLayerDropDown;


        public void OnEnable()
        {
            m_UseRendererSilhouette = serializedObject.FindProperty("m_UseRendererSilhouette");
            m_SilhouettedRenderers = serializedObject.FindProperty("m_SilhouettedRendererRefs");
            m_Renderer = serializedObject.FindProperty("m_RenderRef");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_UseTransformZ = serializedObject.FindProperty("m_UseTransformZ");
            m_ZPosition = serializedObject.FindProperty("m_ZPosition");
            m_ShadowIsPersistent = serializedObject.FindProperty("m_ShadowIsPersistent");

            m_SortingLayerDropDown = new SortingLayerDropDown();
            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");

            m_FilterType = serializedObject.FindProperty("m_FilterMode");
            m_FilterLights = serializedObject.FindProperty("m_FilterLights");
        }


        public void OnSceneGUI()
        {
        }

        public bool HasRenderer()
        {
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ShadowSpriteCaster2D shadowSpriteCaster = (ShadowSpriteCaster2D)targets[i];
                    if (shadowSpriteCaster.HasSilhouettedRenderer)
                        return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Renderer, Styles.renderer);

            EditorGUILayout.PropertyField(m_UseRendererSilhouette, Styles.useSilhouettedRenderer);
            EditorGUILayout.PropertyField(m_SilhouettedRenderers, Styles.silhouettedRenderer);


            EditorGUILayout.PropertyField(m_CastsShadows, Styles.castsShadows);
            EditorGUILayout.PropertyField(m_UseTransformZ, Styles.useTransformZ);

            if (!m_UseTransformZ.boolValue)
            {
                EditorGUILayout.PropertyField(m_ZPosition, Styles.zPosition);
            }

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(m_ShadowIsPersistent, Styles.isPersistent);
            if (m_ShadowIsPersistent.boolValue)
            {
                EditorGUILayout.HelpBox("PERSISTENT SHADOWS ARE UNOPTIMIZED! Only use this option if you are 100% sure it is required!", MessageType.Warning);
            }

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
