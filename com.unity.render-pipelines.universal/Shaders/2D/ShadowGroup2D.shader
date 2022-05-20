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

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space

                float3 lightDir = _LightPos - vertexWS;
                lightDir.z = 0;

                float shadowHeightTest = clamp(ceil(_LightPos.z - _ShadowCenter.z), 0, 1);

                float3 relativePosition = _ShadowCenter - _LightPos;
                float h = sqrt((relativePosition.x * relativePosition.x) + (relativePosition.y * relativePosition.y));
                float a = max(0, _LightPos.z - (_ShadowCenter.z + _ShadowHeight));
                float th = acos(a / h);

                float _ShadowRadius = max(tan(th) * (_ShadowHeight  + 1),0.001);


                // Start of code to see if this point should be extruded
                float3 lightDirection = normalize(lightDir);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float sharedShadowTest = saturate(ceil(-dot(lightDirection, worldTangent)));

                
                //float3 endpoint = vertexWS + (_ShadowRadius * -lightDirection);

                /*
                // Start of code to calculate offset
                float3 vertexWS0 = TransformObjectToWorld(float3(v.extrusion.xy, 0));
                float3 vertexWS1 = TransformObjectToWorld(float3(v.extrusion.zw, 0));
                float3 shadowDir0 = vertexWS0 - _LightPos;
                shadowDir0.z = 0;
                shadowDir0 = normalize(shadowDir0);

                float3 shadowDir1 = vertexWS1 -_LightPos;
                shadowDir1.z = 0;
                shadowDir1 = normalize(shadowDir1);

                float3 shadowDir = normalize(shadowDir0 + shadowDir1);
                */


                float3 sharedShadowOffset = sharedShadowTest * _ShadowRadius * -lightDirection;

                float3 position;
                position = vertexWS + sharedShadowOffset;

                o.vertex = TransformWorldToHClip(position);

                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                o.color = (1 - (length(position - _ShadowCenter.xy) / _ShadowRadius)) * shadowHeightTest * clamp(_ShadowRadius,0,1);  // v.color;
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
                o.color = 1;
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
