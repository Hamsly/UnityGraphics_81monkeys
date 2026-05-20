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
                half4 sum = 0;

                #define GRABPIXEL(weight,kernelX,kernelY) SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + (_MainTex_TexelSize.xy * float2(kernelX,kernelY) )) * weight
    
                sum += GRABPIXEL( 1, -2.0,-2.0);
                sum += GRABPIXEL( 4, -1.0,-2.0);
                sum += GRABPIXEL( 6,  0.0,-2.0);
                sum += GRABPIXEL( 4, +1.0,-2.0);
                sum += GRABPIXEL( 1, +2.0,-2.0);

                sum += GRABPIXEL( 4, -2.0,-1.0);
                sum += GRABPIXEL(16, -1.0,-1.0);
                sum += GRABPIXEL(24,  0.0,-1.0);
                sum += GRABPIXEL(16, +1.0,-1.0);
                sum += GRABPIXEL( 4, +2.0,-1.0);

                sum += GRABPIXEL( 6, -2.0, 0.0);
                sum += GRABPIXEL(24, -1.0, 0.0);
                sum += GRABPIXEL(32,  0.0, 0.0);
                sum += GRABPIXEL(24, +1.0, 0.0);
                sum += GRABPIXEL( 6, +2.0, 0.0);

                sum += GRABPIXEL( 4, -2.0,+1.0);
                sum += GRABPIXEL(16, -1.0,+1.0);
                sum += GRABPIXEL(24,  0.0,+1.0);
                sum += GRABPIXEL(16, +1.0,+1.0);
                sum += GRABPIXEL( 4, +2.0,+1.0);

                sum += GRABPIXEL( 1, -2.0,+2.0);
                sum += GRABPIXEL( 4, -1.0,+2.0);
                sum += GRABPIXEL( 6,  0.0,+2.0);
                sum += GRABPIXEL( 4, +1.0,+2.0);
                sum += GRABPIXEL( 1, +2.0,+2.0);
                
                return sum / 256.0;
            }
            ENDHLSL
        }
    }
}
