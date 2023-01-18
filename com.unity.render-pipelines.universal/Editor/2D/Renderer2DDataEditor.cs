using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Renderer2DData), true)]
    internal class Renderer2DDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");
            public static readonly GUIContent lightRenderTexturesHeader = EditorGUIUtility.TrTextContent("Light Render Textures");
            public static readonly GUIContent lightBlendStylesHeader = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent postProcessHeader = EditorGUIUtility.TrTextContent("Post-processing");

            public static readonly GUIContent transparencySortMode = EditorGUIUtility.TrTextContent("Transparency Sort Mode", "Default sorting mode used for transparent objects");
            public static readonly GUIContent transparencySortAxis = EditorGUIUtility.TrTextContent("Transparency Sort Axis", "Axis used for custom axis sorting mode");
            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightRTScale = EditorGUIUtility.TrTextContent("Render Scale", "The resolution of intermediate light render textures, in relation to the screen resolution. 1.0 means full-screen size.");
            public static readonly GUIContent maxLightRTCount = EditorGUIUtility.TrTextContent("Max Light Render Textures", "How many intermediate light render textures can be created and utilized concurrently. Higher value usually leads to better performance on mobile hardware at the cost of more memory.");
            public static readonly GUIContent maxShadowRTCount = EditorGUIUtility.TrTextContent("Max Shadow Render Textures", "How many intermediate shadow render textures can be created and utilized concurrently. Higher value usually leads to better performance on mobile hardware at the cost of more memory.");
            public static readonly GUIContent defaultMaterialType = EditorGUIUtility.TrTextContent("Default Material Type", "Material to use when adding new objects to a scene");
            public static readonly GUIContent defaultCustomMaterial = EditorGUIUtility.TrTextContent("Default Custom Material", "Material to use when adding new objects to a scene");

            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent useDepthStencilBuffer = EditorGUIUtility.TrTextContent("Depth/Stencil Buffer", "Uncheck this when you are certain you don't use any feature that requires the depth/stencil buffer (e.g. Sprite Mask). Not using the depth/stencil buffer may improve performance, especially on mobile platforms.");
            public static readonly GUIContent postProcessIncluded = EditorGUIUtility.TrTextContent("Enabled", "Turns post-processing on (check box selected) or off (check box cleared). If you clear this check box, Unity excludes post-processing render Passes, shaders, and textures from the build.");
            public static readonly GUIContent postProcessData = EditorGUIUtility.TrTextContent("Data", "The asset containing references to shaders and Textures that the Renderer uses for post-processing.");

            public static readonly GUIContent cameraSortingLayerTextureHeader = EditorGUIUtility.TrTextContent("Camera Sorting Layer Texture", "Layers from back most to selected bounds will be rendered to _CameraSortingLayerTexture");
            public static readonly GUIContent cameraSortingLayerTextureBound = EditorGUIUtility.TrTextContent("Foremost Sorting Layer", "Layers from back most to selected bounds will be rendered to _CameraSortingLayerTexture");
            public static readonly GUIContent cameraSortingLayerDownsampling = EditorGUIUtility.TrTextContent("Downsampling Method", "Method used to copy _CameraSortingLayerTexture");

            public static readonly GUIContent shadowsHeader = EditorGUIUtility.TrTextContent("Shadows");
            public static readonly GUIContent shadowsRebuildMatsButton = EditorGUIUtility.TrTextContent("Rebuild Materials");

            public static readonly GUIContent RenderFeatures =
                new GUIContent("Renderer Features",
                               "Features to include in this renderer.\nTo add or remove features, use the plus and minus at the bottom of this box.");

            public static readonly GUIContent PassNameField =
                new GUIContent("Name", "Render pass name. This name is the name displayed in Frame Debugger.");

            public static readonly GUIContent MissingFeature = new GUIContent("Missing RendererFeature",
                                                                              "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature.");

            public static GUIStyle BoldLabelSimple;
        }

        struct LightBlendStyleProps
        {
            public SerializedProperty name;
            public SerializedProperty maskTextureChannel;
            public SerializedProperty blendMode;
            public SerializedProperty blendFactorMultiplicative;
            public SerializedProperty blendFactorAdditive;
        }

        private SerializedProperty m_RendererFeatures;
        private SerializedProperty m_RendererFeaturesMap;
        private SerializedProperty m_FalseBool;
        [SerializeField] private bool falseBool = false;

        SerializedProperty m_TransparencySortMode;
        SerializedProperty m_TransparencySortAxis;
        SerializedProperty m_HDREmulationScale;
        SerializedProperty m_LightRenderTextureScale;
        SerializedProperty m_LightBlendStyles;
        LightBlendStyleProps[] m_LightBlendStylePropsArray;
        SerializedProperty m_UseDepthStencilBuffer;
        SerializedProperty m_DefaultMaterialType;
        SerializedProperty m_DefaultCustomMaterial;
        SerializedProperty m_MaxLightRenderTextureCount;
        SerializedProperty m_MaxShadowRenderTextureCount;
        SerializedProperty m_PostProcessData;

        SerializedProperty m_UseCameraSortingLayersTexture;
        SerializedProperty m_CameraSortingLayersTextureBound;
        SerializedProperty m_CameraSortingLayerDownsamplingMethod;

        SavedBool m_GeneralFoldout;
        SavedBool m_LightRenderTexturesFoldout;
        SavedBool m_LightBlendStylesFoldout;
        SavedBool m_CameraSortingLayerTextureFoldout;
        SavedBool m_PostProcessingFoldout;
        SavedBool m_ShadowFoldout;

        Analytics.Renderer2DAnalytics m_Analytics = Analytics.Renderer2DAnalytics.instance;
        Renderer2DData m_Renderer2DData;
        bool m_WasModified;

        List<Editor> m_Editors = new List<Editor>();

        void SendModifiedAnalytics(Analytics.IAnalytics analytics)
        {
            if (m_WasModified)
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.instance_id = m_Renderer2DData.GetInstanceID();
                modifiedData.was_create_event = false;
                modifiedData.blending_layers_count = 0;
                modifiedData.blending_modes_used = 0;
                analytics.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            }
        }

        void OnEnable()
        {
            m_WasModified = false;
            m_Renderer2DData = (Renderer2DData)serializedObject.targetObject;

            m_RendererFeatures = serializedObject.FindProperty(nameof(Renderer2DData.m_RendererFeatures));
            m_RendererFeaturesMap = serializedObject.FindProperty(nameof(Renderer2DData.m_RendererFeatureMap));
            var editorObj = new SerializedObject(this);
            m_FalseBool = editorObj.FindProperty(nameof(falseBool));

            m_TransparencySortMode = serializedObject.FindProperty("m_TransparencySortMode");
            m_TransparencySortAxis = serializedObject.FindProperty("m_TransparencySortAxis");
            m_HDREmulationScale = serializedObject.FindProperty("m_HDREmulationScale");
            m_LightRenderTextureScale = serializedObject.FindProperty("m_LightRenderTextureScale");
            m_LightBlendStyles = serializedObject.FindProperty("m_LightBlendStyles");
            m_MaxLightRenderTextureCount = serializedObject.FindProperty("m_MaxLightRenderTextureCount");
            m_MaxShadowRenderTextureCount = serializedObject.FindProperty("m_MaxShadowRenderTextureCount");
            m_PostProcessData = serializedObject.FindProperty("m_PostProcessData");

            m_CameraSortingLayersTextureBound = serializedObject.FindProperty("m_CameraSortingLayersTextureBound");
            m_UseCameraSortingLayersTexture = serializedObject.FindProperty("m_UseCameraSortingLayersTexture");
            m_CameraSortingLayerDownsamplingMethod = serializedObject.FindProperty("m_CameraSortingLayerDownsamplingMethod");

            int numBlendStyles = m_LightBlendStyles.arraySize;
            m_LightBlendStylePropsArray = new LightBlendStyleProps[numBlendStyles];

            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                props.name = blendStyleProp.FindPropertyRelative("name");
                props.maskTextureChannel = blendStyleProp.FindPropertyRelative("maskTextureChannel");
                props.blendMode = blendStyleProp.FindPropertyRelative("blendMode");
                props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.multiplicative");
                props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additive");

                if (props.blendFactorMultiplicative == null)
                    props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.modulate");
                if (props.blendFactorAdditive == null)
                    props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additve");
            }

            m_UseDepthStencilBuffer = serializedObject.FindProperty("m_UseDepthStencilBuffer");
            m_DefaultMaterialType = serializedObject.FindProperty("m_DefaultMaterialType");
            m_DefaultCustomMaterial = serializedObject.FindProperty("m_DefaultCustomMaterial");

            m_GeneralFoldout = new SavedBool($"{target.GetType()}.GeneralFoldout", true);
            m_LightRenderTexturesFoldout = new SavedBool($"{target.GetType()}.LightRenderTexturesFoldout", true);
            m_LightBlendStylesFoldout = new SavedBool($"{target.GetType()}.LightBlendStylesFoldout", true);
            m_CameraSortingLayerTextureFoldout = new SavedBool($"{target.GetType()}.CameraSortingLayerTextureFoldout", true);
            m_PostProcessingFoldout = new SavedBool($"{target.GetType()}.PostProcessingFoldout", true);
            m_ShadowFoldout = new SavedBool($"{target.GetType()}.ShadowFoldout", true);

            UpdateEditorList();
        }

        private void OnDestroy()
        {
            ClearEditorsList();
            SendModifiedAnalytics(m_Analytics);
        }

        public override void OnInspectorGUI()
        {

            if (m_RendererFeatures == null)
                OnEnable();
            else if (m_RendererFeatures.arraySize != m_Editors.Count)
                UpdateEditorList();

            serializedObject.Update();

            DrawGeneral();
            DrawLightRenderTextures();
            DrawLightBlendStyles();
            DrawCameraSortingLayerTexture();
            DrawPostProcessing();
            DrawShadows();

            DrawRendererFeatureList();

            m_WasModified |= serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateEditorList()
        {
            ClearEditorsList();
            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                m_Editors.Add(CreateEditor(m_RendererFeatures.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }

        //To avoid leaking memory we destroy editors when we clear editors list
        private void ClearEditorsList()
        {
            for (int i = m_Editors.Count - 1; i >= 0; --i)
            {
                DestroyImmediate(m_Editors[i]);
            }
            m_Editors.Clear();
        }

        public void DrawCameraSortingLayerTexture()
        {
            CoreEditorUtils.DrawSplitter();
            m_CameraSortingLayerTextureFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.cameraSortingLayerTextureHeader, m_CameraSortingLayerTextureFoldout.value);
            if (!m_CameraSortingLayerTextureFoldout.value)
                return;

            SortingLayer[] sortingLayers = SortingLayer.layers;
            string[] optionNames = new string[sortingLayers.Length + 1];
            int[] optionIds = new int[sortingLayers.Length + 1];
            optionNames[0] = "Disabled";
            optionIds[0] = -1;

            int currentOptionIndex = 0;
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                optionNames[i + 1] = sortingLayers[i].name;
                optionIds[i + 1] = sortingLayers[i].id;
                if (sortingLayers[i].id == m_CameraSortingLayersTextureBound.intValue)
                    currentOptionIndex = i + 1;
            }


            int selectedOptionIndex = !m_UseCameraSortingLayersTexture.boolValue ? 0 : currentOptionIndex;
            selectedOptionIndex = EditorGUILayout.Popup(Styles.cameraSortingLayerTextureBound, selectedOptionIndex, optionNames);

            m_UseCameraSortingLayersTexture.boolValue = selectedOptionIndex != 0;
            m_CameraSortingLayersTextureBound.intValue = optionIds[selectedOptionIndex];

            EditorGUI.BeginDisabledGroup(!m_UseCameraSortingLayersTexture.boolValue);
            EditorGUILayout.PropertyField(m_CameraSortingLayerDownsamplingMethod, Styles.cameraSortingLayerDownsampling);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawGeneral()
        {
            CoreEditorUtils.DrawSplitter();
            m_GeneralFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.generalHeader, m_GeneralFoldout.value);
            if (!m_GeneralFoldout.value)
                return;

            EditorGUILayout.PropertyField(m_TransparencySortMode, Styles.transparencySortMode);

            using (new EditorGUI.DisabledGroupScope(m_TransparencySortMode.intValue != (int)TransparencySortMode.CustomAxis))
                EditorGUILayout.PropertyField(m_TransparencySortAxis, Styles.transparencySortAxis);

            EditorGUILayout.PropertyField(m_DefaultMaterialType, Styles.defaultMaterialType);
            if (m_DefaultMaterialType.intValue == (int)Renderer2DData.Renderer2DDefaultMaterialType.Custom)
                EditorGUILayout.PropertyField(m_DefaultCustomMaterial, Styles.defaultCustomMaterial);

            EditorGUILayout.PropertyField(m_UseDepthStencilBuffer, Styles.useDepthStencilBuffer);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && m_HDREmulationScale.floatValue < 1.0f)
                m_HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.Space();
        }

        private void DrawLightRenderTextures()
        {
            CoreEditorUtils.DrawSplitter();
            m_LightRenderTexturesFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.lightRenderTexturesHeader, m_LightRenderTexturesFoldout.value);
            if (!m_LightRenderTexturesFoldout.value)
                return;

            EditorGUILayout.PropertyField(m_LightRenderTextureScale, Styles.lightRTScale);
            EditorGUILayout.PropertyField(m_MaxLightRenderTextureCount, Styles.maxLightRTCount);
            EditorGUILayout.PropertyField(m_MaxShadowRenderTextureCount, Styles.maxShadowRTCount);

            EditorGUILayout.Space();
        }

        private void DrawLightBlendStyles()
        {
            CoreEditorUtils.DrawSplitter();
            m_LightBlendStylesFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.lightBlendStylesHeader, m_LightBlendStylesFoldout.value);
            if (!m_LightBlendStylesFoldout.value)
                return;

            int numBlendStyles = m_LightBlendStyles.arraySize;
            for (int i = 0; i < numBlendStyles; ++i)
            {
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                EditorGUILayout.PropertyField(props.name, Styles.name);
                EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                EditorGUILayout.PropertyField(props.blendMode, Styles.blendMode);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
        }

        private void DrawPostProcessing()
        {
            CoreEditorUtils.DrawSplitter();
            m_PostProcessingFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.postProcessHeader, m_PostProcessingFoldout.value);
            if (!m_PostProcessingFoldout.value)
                return;

            EditorGUI.BeginChangeCheck();
            var postProcessIncluded = EditorGUILayout.Toggle(Styles.postProcessIncluded, m_PostProcessData.objectReferenceValue != null);
            if (EditorGUI.EndChangeCheck())
            {
                m_PostProcessData.objectReferenceValue = postProcessIncluded ? UnityEngine.Rendering.Universal.PostProcessData.GetDefaultPostProcessData() : null;
            }

            // this field is no longer hidden by the checkbox. It is bad UX to begin with
            // also, if the field is hidden, the user could still use Asset Selector to set the value, but it won't stick
            // making it look like a bug(1307128)
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.postProcessData);

            EditorGUILayout.Space();
        }

        private void DrawShadows()
        {
            CoreEditorUtils.DrawSplitter();
            m_ShadowFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.shadowsHeader, m_ShadowFoldout.value);
            if (!m_ShadowFoldout.value)
                return;

            if (GUILayout.Button(Styles.shadowsRebuildMatsButton))
            {
                m_Renderer2DData.RebuildMaterials();
            }
        }

        private void DrawRendererFeatureList()
        {
            EditorGUILayout.LabelField(Styles.RenderFeatures, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_RendererFeatures.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Renderer Features added", MessageType.Info);
            }
            else
            {
                //Draw List
                CoreEditorUtils.DrawSplitter();
                for (int i = 0; i < m_RendererFeatures.arraySize; i++)
                {
                    SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                    DrawRendererFeature(i, ref renderFeaturesProperty);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            EditorGUILayout.Space();

            //Add renderer
            if (GUILayout.Button("Add Renderer Feature", EditorStyles.miniButton))
            {
                AddPassMenu();
            }
        }

        private void DrawRendererFeature(int index, ref SerializedProperty renderFeatureProperty)
        {
            Object rendererFeatureObjRef = renderFeatureProperty.objectReferenceValue;
            if (rendererFeatureObjRef != null)
            {
                bool hasChangedProperties = false;
                string title = ObjectNames.GetInspectorTitle(rendererFeatureObjRef);

                // Get the serialized object for the editor script & update it
                Editor rendererFeatureEditor = m_Editors[index];
                SerializedObject serializedRendererFeaturesEditor = rendererFeatureEditor.serializedObject;
                serializedRendererFeaturesEditor.Update();

                // Foldout header
                EditorGUI.BeginChangeCheck();
                SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
                bool displayContent = CoreEditorUtils.DrawHeaderToggle(title, renderFeatureProperty, activeProperty, pos => OnContextClick(pos, index));
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                // ObjectEditor
                if (displayContent)
                {
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty nameProperty = serializedRendererFeaturesEditor.FindProperty("m_Name");
                    nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
                    if (EditorGUI.EndChangeCheck())
                    {
                        hasChangedProperties = true;

                        // We need to update sub-asset name
                        rendererFeatureObjRef.name = nameProperty.stringValue;
                        AssetDatabase.SaveAssets();

                        // Triggers update for sub-asset name change
                        ProjectWindowUtil.ShowCreatedAsset(target);
                    }

                    EditorGUI.BeginChangeCheck();
                    rendererFeatureEditor.OnInspectorGUI();
                    hasChangedProperties |= EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                }

                // Apply changes and save if the user has modified any settings
                if (hasChangedProperties)
                {
                    serializedRendererFeaturesEditor.ApplyModifiedProperties();
                    serializedObject.ApplyModifiedProperties();
                    ForceSave();
                }
            }
            else
            {
                CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature, renderFeatureProperty, m_FalseBool, pos => OnContextClick(pos, index));
                m_FalseBool.boolValue = false; // always make sure false bool is false
                EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
                if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
                {
                    UnityEngine.Rendering.Universal.ScriptableRendererData data = target as UnityEngine.Rendering.Universal.ScriptableRendererData;
                    data.ValidateRendererFeatures();
                }
            }
        }

        private void OnContextClick(Vector2 position, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, -1));

            if (id == m_RendererFeatures.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void AddPassMenu()
        {
            GenericMenu menu = new GenericMenu();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<UnityEngine.Rendering.Universal.ScriptableRendererFeature>();
            foreach (Type type in types)
            {
                var data = target as UnityEngine.Rendering.Universal.ScriptableRendererData;
                if (data.DuplicateFeatureCheck(type))
                {
                    continue;
                }

                string path = GetMenuNameFromType(type);
                menu.AddItem(new GUIContent(path), false, AddComponent, type.Name);
            }
            menu.ShowAsContext();
        }

                private void AddComponent(object type)
        {
            serializedObject.Update();

            ScriptableObject component = CreateInstance((string)type);
            component.name = $"New{(string)type}";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(target))
            {
                AssetDatabase.AddObjectToAsset(component, target);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_RendererFeatures.arraySize++;
            SerializedProperty componentProp = m_RendererFeatures.GetArrayElementAtIndex(m_RendererFeatures.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            m_RendererFeaturesMap.arraySize++;
            SerializedProperty guidProp = m_RendererFeaturesMap.GetArrayElementAtIndex(m_RendererFeaturesMap.arraySize - 1);
            guidProp.longValue = localId;
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                ForceSave();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponent(int id)
        {
            SerializedProperty property = m_RendererFeatures.GetArrayElementAtIndex(id);
            Object component = property.objectReferenceValue;
            property.objectReferenceValue = null;

            Undo.SetCurrentGroupName(component == null ? "Remove Renderer Feature" : $"Remove {component.name}");

            // remove the array index itself from the list
            m_RendererFeatures.DeleteArrayElementAtIndex(id);
            m_RendererFeaturesMap.DeleteArrayElementAtIndex(id);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }

            // Force save / refresh
            ForceSave();
        }

        private void MoveComponent(int id, int offset)
        {
            Undo.SetCurrentGroupName("Move Render Feature");
            serializedObject.Update();
            m_RendererFeatures.MoveArrayElement(id, id + offset);
            m_RendererFeaturesMap.MoveArrayElement(id, id + offset);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            ForceSave();
        }

        private string GetMenuNameFromType(Type type)
        {
            var path = type.Name;
            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            // Inserts blank space in between camel case strings
            return Regex.Replace(Regex.Replace(path, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled),
                "([A-Z])([A-Z][a-z])", "$1 $2", RegexOptions.Compiled);
        }

        private string ValidateName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
        }

        private void ForceSave()
        {
            EditorUtility.SetDirty(target);
        }

    }
}
