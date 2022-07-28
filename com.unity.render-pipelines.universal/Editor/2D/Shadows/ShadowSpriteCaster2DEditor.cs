using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{

    [CustomEditor(typeof(ShadowSpriteCaster2D))]
    [CanEditMultipleObjects]
    internal class ShadowSpriteCaster2DEditor : Editor
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
            public static GUIContent shadowMode = EditorGUIUtility.TrTextContent("Use Renderer Silhouette",
                "When this and Self Shadows are enabled, the Renderer's silhouette is considered part of the shadow. When this is enabled and Self Shadows disabled, the Renderer's silhouette is excluded from the shadow.");

            public static GUIContent shadowTexture = EditorGUIUtility.TrTextContent("Shadow Texture","The reference texture the cast.");
            public static GUIContent size = EditorGUIUtility.TrTextContent("Size","The simulated size of the caster");

            public static GUIContent silhouettedRenderer = EditorGUIUtility.TrTextContent("Silhouetted Renderers", "The Renderers to use for the Silhouette");

            public static GUIContent spriteCasterType = EditorGUIUtility.TrTextContent("Sprite Caster Type", "General rules for generating the shadow mesh");

            public static GUIContent reorientPerLight = EditorGUIUtility.TrTextContent("Reorient Per Light", "When enabled, the mesh will rotate towards the currently rendering light");

            public static GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");

            public static GUIContent direction = EditorGUIUtility.TrTextContent("Shadow Direction", "The simulated direction of the shadow");

            public static GUIContent useTransformZ = EditorGUIUtility.TrTextContent("Use TransformZ", "Use the transform's Z as the simulated Z Position of the shadow");

            public static GUIContent zPosition = EditorGUIUtility.TrTextContent("Shadow ZPosition", "The simulated Z Position of the shadow");

            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");

            public static GUIContent isPersistent = EditorGUIUtility.TrTextContent("Shadow Is Persistent", "Shadow will always draw, not optimized");

            public static GUIContent basePoint = EditorGUIUtility.TrTextContent("Base Point", "The normalized y position on the texture we begin drawing from.");
        }

        SerializedProperty m_UseRendererSilhouette;
        SerializedProperty m_Size;
        SerializedProperty m_Direction;
        SerializedProperty m_SilhouettedRenderers;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_ReceivesShadows;
        SerializedProperty m_UseTransformZ;
        SerializedProperty m_ZPosition;
        SerializedProperty m_Texture;
        SerializedProperty m_SpriteCasterType;
        SerializedProperty m_ReorientPerLight;
        SerializedProperty m_basePoint;
        SerializedProperty m_ShadowIsPersistent;


        SortingLayerDropDown m_SortingLayerDropDown;


        public void OnEnable()
        {
            m_UseRendererSilhouette = serializedObject.FindProperty("m_UseRendererSilhouette");
            m_Size = serializedObject.FindProperty("m_Size");
            m_Direction = serializedObject.FindProperty("m_Direction");
            m_SilhouettedRenderers = serializedObject.FindProperty("m_SilhouettedRenderers");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_UseTransformZ = serializedObject.FindProperty("m_UseTransformZ");
            m_ZPosition = serializedObject.FindProperty("m_ZPosition");
            m_Texture = serializedObject.FindProperty("m_Texture");
            m_SpriteCasterType = serializedObject.FindProperty("m_SpriteCasterType");
            m_ReorientPerLight = serializedObject.FindProperty("m_ReorientPerLight");
            m_basePoint = serializedObject.FindProperty("m_basePoint");
            m_ShadowIsPersistent = serializedObject.FindProperty("m_ShadowIsPersistent");

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
                    if (shadowSpriteCaster.HasSilhouettedRenderer)
                        return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            ShadowSpriteCaster2D shadowSpriteCaster = serializedObject.targetObject as ShadowSpriteCaster2D;
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Texture, Styles.shadowTexture);
            EditorGUILayout.PropertyField(m_basePoint, Styles.basePoint);
            EditorGUILayout.PropertyField(m_SpriteCasterType, Styles.spriteCasterType);
            EditorGUILayout.PropertyField(m_ReorientPerLight, Styles.reorientPerLight);
            EditorGUILayout.PropertyField(m_Size, Styles.size);
            EditorGUILayout.PropertyField(m_Direction, Styles.direction);
            EditorGUILayout.PropertyField(m_SilhouettedRenderers, Styles.silhouettedRenderer);


            EditorGUILayout.PropertyField(m_UseRendererSilhouette, Styles.shadowMode);


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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
