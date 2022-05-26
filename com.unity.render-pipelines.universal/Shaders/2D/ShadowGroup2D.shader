Shader "Hidden/ShadowGroup2D"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ShadowStencilGroup("__ShadowStencilGroup", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        BlendOp Add
        Blend One One
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref [_ShadowStencilGroup]
                Comp NotEqual
                Pass Replace
                Fail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShadowsShared.hlsl"

            struct Attributes
            {
                float3 vertex : POSITION;
                float4 tangent: TANGENT;
                float2 uv : TEXCOORD0;
                float2 uv_tex : TEXCOORD1;
                float4 extrusion : COLOR;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv_tex : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            //uniform sampler2D _ShadowImage;
            uniform float3 _LightPos;
            uniform float _ShadowHeight;
            uniform float3 _ShadowCenter;
            uniform float _FalloffRate;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space

                float3 lightDir;
                lightDir.xy = _LightPos.xy - vertexWS.xy;
                lightDir.z = 0;

                float shadowHeightTest = clamp(ceil(_LightPos.z - _ShadowCenter.z), 0, 1);

                /// Calculate the projection distance of the bottom of the shadow
                float3 botVert = vertexWS;
                botVert.z = _ShadowCenter.z;
                float _BaseOffset = ProjectionDistance(botVert, _LightPos) * botVert.z;

                /// Calculate the projection distance of the top of the shadow
                float3 topVert = botVert;
                topVert.z += _ShadowHeight;
                float _ProjectionOffset = ProjectionDistance(topVert, _LightPos) * topVert.z;

                // Start of code to see if this point should be extruded
                float3 lightDirection = normalize(lightDir);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float sharedShadowTest = saturate(ceil(-dot(lightDirection, worldTangent)));  

                float3 o1 = (sharedShadowTest * (_ProjectionOffset * -lightDirection));
                float3 o2 = ((1 - sharedShadowTest) * (_BaseOffset * -lightDirection));

                float3 position = vertexWS + o1 + o2;

                o.vertex = TransformWorldToHClip(position);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color =  (1 - (length(position - vertexWS.xy) / max(_ProjectionOffset, _FalloffRate))) * shadowHeightTest;  // v.color;
                o.color.g = 0.5;
                o.color.b = 0;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float2 shadUV;
                shadUV = saturate(round((normalize(position - _ShadowCenter) / 2) + 0.5));

                o.uv_tex = shadUV;

                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 main = tex2D(_MainTex, i.uv);
                float4 col = i.color;
                col.g = main.a * col.g; //* max(tex2D(_ShadowImage,i.uv_tex).r,0.5);

                return col;
            }
            ENDHLSL
        }
        Pass
        {
            Stencil
            {
                Ref [_ShadowStencilGroup]
                Comp NotEqual
                Pass Replace
                Fail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = 0;// 1;
                o.color.g = 0.5;
                o.color.b = 1;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 main = tex2D(_MainTex, i.uv);
                half4 color = i.color;
                color.b = 1 - main.a;

                return color;
            }
            ENDHLSL
        }
    }
}
