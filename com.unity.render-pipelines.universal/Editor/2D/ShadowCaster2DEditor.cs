using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal class ShadowCasterPath : ScriptablePath
    {
        internal Bounds GetBounds()
        {
            ShadowCaster2D shadowCaster = (ShadowCaster2D)owner;
            Renderer m_Renderer = shadowCaster.GetComponent<Renderer>();
            if (m_Renderer != null)
            {
                return m_Renderer.bounds;
            }
            else
            {
                Collider2D collider = shadowCaster.GetComponent<Collider2D>();
                if (collider != null)
                    return collider.bounds;
            }

            return new Bounds(shadowCaster.transform.position, shadowCaster.transform.lossyScale);
        }

        public override void SetDefaultShape()
        {
            Clear();
            Bounds bounds = GetBounds();

            AddPoint(new ControlPoint(bounds.min));
            AddPoint(new ControlPoint(new Vector3(bounds.min.x, bounds.max.y)));
            AddPoint(new ControlPoint(bounds.max));
            AddPoint(new ControlPoint(new Vector3(bounds.max.x, bounds.min.y)));

            base.SetDefaultShape();
        }
    }




    [CustomEditor(typeof(ShadowCaster2D))]
    [CanEditMultipleObjects]
    internal class ShadowCaster2DEditor : PathComponentEditor<ShadowCasterPath>
    {
        [EditorTool("Edit Shadow Caster Shape", typeof(ShadowCaster2D))]
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool {};
        class ShadowCaster2DShadowCasterRectangleTool : ShadowCaster2DShapeTool {};
        class ShadowCaster2DShadowCasterEllipseTool : ShadowCaster2DShapeTool {};

        public enum ShadowShape
        {
            Freeform,
            Rectangle,
            Ellipse
        }

        struct Ellipse
        {
            public Ellipse(float radius,float ratio,int points)
            {
                this.radius = radius;
                this.ratio = ratio;
                this.points = points;
            }

            public float radius;
            public float ratio;
            public int points;
        }

        public ShadowShape shadowShape;

        Vector3[] shadowFreeFormShape = new Vector3[0];
        Rect shadowRect = new Rect(-0.5f,-0.5f,1,1);
        Ellipse shadowEllipse = new Ellipse(1,0.5f,8);

        private static class Styles
        {
            public static GUIContent shadowMode = EditorGUIUtility.TrTextContent("Use Renderer Silhouette", "When this and Self Shadows are enabled, the Renderer's silhouette is considered part of the shadow. When this is enabled and Self Shadows disabled, the Renderer's silhouette is excluded from the shadow.");
            public static GUIContent silhouettedRenderer = EditorGUIUtility.TrTextContent("Silhouetted Renderers", "The Renderers to use for the Silhouette");
            public static GUIContent selfShadows = EditorGUIUtility.TrTextContent("Self Shadows", "When enabled, the Renderer casts shadows on itself.");
            public static GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");
            public static GUIContent height = EditorGUIUtility.TrTextContent("Shadow Height", "The simulated height of the shadow");
            public static GUIContent zPosition = EditorGUIUtility.TrTextContent("Shadow ZPosition", "The simulated Z Position of the shadow");
            public static GUIContent falloffRate = EditorGUIUtility.TrTextContent("Shadow Falloff Min Distance", "The minimum shadow length before falloff begins");
            public static GUIContent texture = EditorGUIUtility.TrTextContent("Shadow Texture", "The texture to apply to the shadow");
            public static GUIContent ShapePath = EditorGUIUtility.TrTextContent("Shadow Path", "The shape path of the Shadow");
            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");
        }

        SerializedProperty m_UseRendererSilhouette;
        SerializedProperty m_SilhouettedRenderers;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_SelfShadows;
        SerializedProperty m_ReceivesShadows;
        SerializedProperty m_Height;
        SerializedProperty m_ZPosition;
        SerializedProperty m_ShapePath;
        SerializedProperty m_FalloffRate;
        //SerializedProperty m_ShadowTexture;


        SortingLayerDropDown m_SortingLayerDropDown;


        public void OnEnable()
        {
            m_UseRendererSilhouette = serializedObject.FindProperty("m_UseRendererSilhouette");
            m_SilhouettedRenderers = serializedObject.FindProperty("m_SilhouettedRenderers");
            m_SelfShadows = serializedObject.FindProperty("m_SelfShadows");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_Height = serializedObject.FindProperty("m_Height");
            m_ZPosition = serializedObject.FindProperty("m_ZPosition");
            m_ShapePath = serializedObject.FindProperty("m_ShapePath");
            m_FalloffRate = serializedObject.FindProperty("m_FalloffRate");
            //m_ShadowTexture = serializedObject.FindProperty("m_ShadowTexture");

            m_SortingLayerDropDown = new SortingLayerDropDown();
            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");
        }

        public void ShadowCaster2DSceneGUI()
        {
            ShadowCaster2D shadowCaster = target as ShadowCaster2D;

            Transform t = shadowCaster.transform;
            Vector3[] shape = shadowCaster.shapePath;
            Handles.color = Color.white;

            for (int i = 0; i < shape.Length - 1; ++i)
            {
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[i]), t.TransformPoint(shape[i + 1]) });
            }

            if (shape.Length > 1)
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[shape.Length - 1]), t.TransformPoint(shape[0]) });
        }

        public void ShadowCaster2DInspectorGUI<T>() where T : ShadowCaster2DShapeTool
        {
            DoEditButton<T>(PathEditorToolContents.icon, "Edit Shape");
            DoPathInspector<T>();
            DoSnappingInspector<T>();
        }

        public void OnSceneGUI()
        {
            if (m_CastsShadows.boolValue)
                ShadowCaster2DSceneGUI();
        }

        public bool HasRenderer()
        {
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ShadowCaster2D shadowCaster = (ShadowCaster2D)targets[i];
                    if (shadowCaster.hasRenderer)
                        return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            ShadowCaster2D shadowCaster = serializedObject.targetObject as ShadowCaster2D;
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SilhouettedRenderers, Styles.silhouettedRenderer);


            EditorGUILayout.PropertyField(m_UseRendererSilhouette, Styles.shadowMode);
   

            EditorGUILayout.PropertyField(m_CastsShadows, Styles.castsShadows);
            EditorGUILayout.PropertyField(m_SelfShadows, Styles.selfShadows);
            EditorGUILayout.PropertyField(m_Height, Styles.height);
            EditorGUILayout.PropertyField(m_ZPosition, Styles.zPosition);
            EditorGUILayout.PropertyField(m_FalloffRate, Styles.falloffRate);
            //EditorGUILayout.PropertyField(m_ShadowTexture, Styles.texture);

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            EditorGUILayout.Space(10);

            var shadowShapePrev = shadowShape;
            shadowShape = (ShadowShape)EditorGUILayout.EnumPopup(shadowShape);

            if (shadowShape != shadowShapePrev)
            {
                if (shadowShapePrev == ShadowShape.Freeform)
                {
                    if (ToolManager.activeToolType == typeof(ShadowCaster2DShadowCasterShapeTool))
                    {
                        ToolManager.RestorePreviousTool();
                    }
                }
            }

            if (m_CastsShadows.boolValue)
            {

                switch (shadowShape)
                {
                    case ShadowShape.Freeform:
                        ShadowCaster2DInspectorGUI<ShadowCaster2DShadowCasterShapeTool>();
                        break;

                    case ShadowShape.Rectangle:
                        SquareShadowTool();
                        break;

                    case ShadowShape.Ellipse:
                        EllipseShadowTool();
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }


        private void SquareShadowTool()
        {
            shadowRect = EditorGUILayout.RectField(shadowRect);

            EditorGUILayout.PropertyField(m_ShapePath, Styles.ShapePath);

            Vector3 p1 = new Vector3(shadowRect.xMin, shadowRect.yMin, 0);
            Vector3 p2 = new Vector3(shadowRect.xMin, shadowRect.yMax, 0);
            Vector3 p3 = new Vector3(shadowRect.xMax, shadowRect.yMax, 0);
            Vector3 p4 = new Vector3(shadowRect.xMax, shadowRect.yMin, 0);

            UpdateShapePathArray(m_ShapePath, new Vector3[] { p1, p2, p3, p4 });
        }

        private void EllipseShadowTool()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Number Of Points");
            shadowEllipse.points = EditorGUILayout.IntField(shadowEllipse.points);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Radius");
            shadowEllipse.radius = EditorGUILayout.FloatField(shadowEllipse.radius);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ratio");
            shadowEllipse.ratio = EditorGUILayout.FloatField(shadowEllipse.ratio);
            EditorGUILayout.EndHorizontal();


            Vector3[] points = new Vector3[shadowEllipse.points];

            for(int i = 0; i < points.Length; i++)
            {
                float r = i * -((Mathf.PI * 2) / shadowEllipse.points);

                float x = Mathf.Cos(r) * shadowEllipse.radius;
                float y = Mathf.Sin(r) * shadowEllipse.radius * shadowEllipse.ratio;

                points[i] = new Vector3(x, y, 0);
            }

            UpdateShapePathArray(m_ShapePath, points);
        }


        private void UpdateShapePathArray(SerializedProperty shapePathProperty, Vector3[] newPath)
        {
            if (!shapePathProperty.isArray) return;

            while(shapePathProperty.arraySize > newPath.Length)
            {
                shapePathProperty.DeleteArrayElementAtIndex(0);
            }

            while (shapePathProperty.arraySize < newPath.Length)
            {
                shapePathProperty.InsertArrayElementAtIndex(0);
            }

            for( int i = 0; i < newPath.Length; i++)
            {
                Vector3 point = newPath[i];
                var prop = shapePathProperty.GetArrayElementAtIndex(i);

                prop.vector3Value = point;
            }

            shapePathProperty.serializedObject.ApplyModifiedProperties();

            ShadowCaster2D shadowCaster = target as ShadowCaster2D;
            if (shadowCaster != null)
            {
                int hash = LightUtility.GetShapePathHash(shadowCaster.shapePath);
                shadowCaster.shapePathHash = hash;
            }
        }
    }
}
