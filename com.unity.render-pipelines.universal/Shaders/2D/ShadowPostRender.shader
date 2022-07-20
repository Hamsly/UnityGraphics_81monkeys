Shader "Hidden/Universal Render Pipeline/ShadowPostRender"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white"
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #pragma vertex vert
        #pragma fragment frag

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);

        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;
        float4 _MainTex_ST;

        int _BlurStrength;

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            return OUT;
        }
        ENDHLSL


        Pass
        {
            HLSLPROGRAM
            half4 frag(Varyings IN) : SV_TARGET
            {
                half4 sumH = 0;
                half4 sumV = 0;

                #define GRABPIXELH(weight,kernel) SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(IN.uv.x + kernel * _MainTex_TexelSize.x, IN.uv.y)) * weight

                sumH += GRABPIXELH(0.05, -4.0);
                sumH += GRABPIXELH(0.09, -3.0);
                sumH += GRABPIXELH(0.12, -2.0);
                sumH += GRABPIXELH(0.15, -1.0);
                sumH += GRABPIXELH(0.18,  0.0);
                sumH += GRABPIXELH(0.15, +1.0);
                sumH += GRABPIXELH(0.12, +2.0);
                sumH += GRABPIXELH(0.09, +3.0);
                sumH += GRABPIXELH(0.05, +4.0);

                #define GRABPIXELV(weight,kernel) SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(IN.uv.x, IN.uv.y + kernel * _MainTex_TexelSize.y)) * weight

                sumV += GRABPIXELV(0.05, -4.0);
                sumV += GRABPIXELV(0.09, -3.0);
                sumV += GRABPIXELV(0.12, -2.0);
                sumV += GRABPIXELV(0.15, -1.0);
                sumV += GRABPIXELV(0.18,  0.0);
                sumV += GRABPIXELV(0.15, +1.0);
                sumV += GRABPIXELV(0.12, +2.0);
                sumV += GRABPIXELV(0.09, +3.0);
                sumV += GRABPIXELV(0.05, +4.0);

                return (sumH + sumV) / 2;
            }
            ENDHLSL
        }
    }
}
