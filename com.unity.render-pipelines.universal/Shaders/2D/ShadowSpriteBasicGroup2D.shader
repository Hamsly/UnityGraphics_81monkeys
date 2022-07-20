Shader "Hidden/ShadowSpriteBasicGroup2D"
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
        BlendOp Max
        Blend One One
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref [_ShadowStencilGroup]
                Comp Always
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
                float2 uv : TEXCOORD0;
                float2 uv_tex : TEXCOORD1;
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
            uniform float3 _ShadowCenter;
            uniform sampler2D _ShadowTexture;
            float4 _ShadowTexture_ST;
            float4 _ShadowBasePos;




            Varyings vert (Attributes v)
            {

                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space

                float2 v1 = _ShadowBasePos.xy;
                float2 v2 = _ShadowBasePos.zw;

                vertexWS.z = _ShadowCenter.z + v.vertex.z;

                float3 lightDir;
                lightDir.xy = _LightPos.xy - vertexWS.xy;
                lightDir.z = 0;

                const float shadowHeightTest = clamp(ceil(_LightPos.z - _ShadowCenter.z), 0, 1);

                const float _BaseOffset = ProjectionDistance(vertexWS, _LightPos) * vertexWS.z;

                // Start of code to see if this point should be extruded
                const float3 lightDirection = normalize(lightDir);

                float3 offset =  (_BaseOffset * -lightDirection);

                const float3 position = vertexWS + offset;

                Varyings o;
                o.vertex = TransformWorldToHClip(position);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color.r = shadowHeightTest;  // v.color;
                o.color.g = 0.5;
                o.color.b = 0;
                o.uv_tex = TRANSFORM_TEX(v.uv_tex,_ShadowTexture);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 main = tex2D(_MainTex, i.uv);
                float4 col = i.color;
                const float aa = tex2D(_ShadowTexture,i.uv_tex.xy).a;
                col.g = main.a * col.g * aa;
                col.r = aa;
                return col;
            }
            ENDHLSL
        }
        /*
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
                o.color.r = 0;
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
        */
    }
}
