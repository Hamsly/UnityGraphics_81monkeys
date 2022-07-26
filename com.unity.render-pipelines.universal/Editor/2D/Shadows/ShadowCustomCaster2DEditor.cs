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

        public enum ShadowType
        {
            Standing,
            Billboard,
            Flat
        }


        public ShadowType shadowType;

        private static class Styles
        {

            public static GUIContent silhouettedRenderer =
                EditorGUIUtility.TrTextContent("Silhouetted Renderers", "The Renderers to use for the Silhouette");

            public static GUIContent renderer =
                EditorGUIUtility.TrTextContent("Renderer", "The Renderer to call upon when drawing the shadow");

            public static GUIContent passes =
                EditorGUIUtility.TrTextContent("Shader Passes", "The passes of the renderer's material that will get used to draw shadows. -1 for all passes");

            public static GUIContent castsShadows =
                EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");

            public static GUIContent useTransformZ =
                EditorGUIUtility.TrTextContent("Use TransformZ", "Use the transform's Z as the simulated Z Position of the shadow");

            public static GUIContent zPosition =
                EditorGUIUtility.TrTextContent("Shadow ZPosition", "The simulated Z Position of the shadow");

            public static GUIContent sortingLayerPrefixLabel =
                EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");
        }

        SerializedProperty m_UseRendererSilhouette;
        SerializedProperty m_SilhouettedRenderers;
        SerializedProperty m_Renderer;
        SerializedProperty m_Passes;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_UseTransformZ;
        SerializedProperty m_ZPosition;


        SortingLayerDropDown m_SortingLayerDropDown;


        public void OnEnable()
        {
            m_UseRendererSilhouette = serializedObject.FindProperty("m_UseRendererSilhouette");
            m_SilhouettedRenderers = serializedObject.FindProperty("m_SilhouettedRenderers");
            m_Renderer = serializedObject.FindProperty("m_targetRenderer");
            m_Passes = serializedObject.FindProperty("m_targetPasses");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_UseTransformZ = serializedObject.FindProperty("m_UseTransformZ");
            m_ZPosition = serializedObject.FindProperty("m_ZPosition");

            m_SortingLayerDropDown = new SortingLayerDropDown();
            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");
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
                    if (shadowSpriteCaster.hasRenderer)
                        return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            ShadowSpriteCaster2D shadowSpriteCaster = serializedObject.targetObject as ShadowSpriteCaster2D;
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Renderer, Styles.renderer);
            EditorGUILayout.PropertyField(m_Passes, Styles.passes);

            EditorGUILayout.PropertyField(m_SilhouettedRenderers, Styles.silhouettedRenderer);


            EditorGUILayout.PropertyField(m_CastsShadows, Styles.castsShadows);
            EditorGUILayout.PropertyField(m_UseTransformZ, Styles.useTransformZ);
            if (!m_UseTransformZ.boolValue)
            {
                EditorGUILayout.PropertyField(m_ZPosition, Styles.zPosition);
            }

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            EditorGUILayout.Space(10);


            serializedObject.ApplyModifiedProperties();
        }
    }
}
