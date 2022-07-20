Shader "Hidden/ShadowSpriteGroup2D"
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
            #pragma geometry geom

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShadowsShared.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 uv_tex : TEXCOORD1;
            };


            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 uv_tex : TEXCOORD1;
            };

            struct GeomData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 uv_tex : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            //uniform sampler2D _ShadowImage;
            uniform float3 _LightPos;
            uniform sampler2D _ShadowTexture;
            uniform float4 _ShadowInfo;
            float4 _ShadowTexture_ST;
            float4 _ShadowBasePos;

            Varyings newShadowVertex(float alpha, float3 vertexPos,float2 uv)
            {
                // Already in world space
                // Already in world space
                float3 vertexWS = vertexPos;//TransformObjectToWorld(vertexPos);  // This should be in world space

                float3 lightDir;
                lightDir.xy = _LightPos.xy - vertexWS.xy;
                lightDir.z = 0;

                const float _BaseOffset = ProjectionDistance(vertexWS, _LightPos) * vertexWS.z;

                // Start of code to see if this point should be extruded
                const float3 lightDirection = normalize(lightDir);

                float3 offset =  (_BaseOffset * -lightDirection);

                float3 position = vertexWS + offset;
                position.z = 0;

                Varyings v;
                v.vertex = TransformWorldToHClip(position);

                v.uv = TRANSFORM_TEX(uv, _MainTex);
                v.uv_tex = float3(0,0,0);
                // RGB - R is shadow value (to support soft shadows), G is Self Shadow Mask, B is No Shadow Mask
                v.color = alpha;
                v.color.g = 0.5;
                v.color.b = 0;

                return v;
            }

            float2 lineIntersection(float2 s1, float2 e1, float2 s2, float2 e2)
            {
                const float a1 = e1.y - s1.y;
                const float b1 = s1.x - e1.x;
                const float c1 = a1 * s1.x + b1 * s1.y;

                const float a2 = e2.y- s2.y;
                const float b2 = s2.x - e2.x;
                const float c2 = a2 * s2.x + b2 * s2.y;

                const float delta = a1 * b2 - a2 * b1;
                return float2((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
            }

            GeomData vert (Attributes v)
            {
                GeomData o;

                o.vertex = _ShadowBasePos;
                o.uv_tex = v.uv_tex;
                o.uv = v.uv;

                return o;
            }

            float InvLerp(float a, float b, float v)
            {
                return (v - a) / (b - a);
            }

            [maxvertexcount(6)]
            void geom(point GeomData input[1], inout TriangleStream<Varyings> triStream)
            {
                //Shadow Height Test
                const float aa = clamp(ceil(_LightPos.z - input[0].vertex.z), 0, 1);

                const float3 offset = float3(cos(_ShadowInfo.z) * _ShadowInfo.x * 0.5,sin(_ShadowInfo.z) * _ShadowInfo.x * 0.5,0);


                const float shadowTop = _ShadowInfo.y + input[0].vertex.z;
                float lightZDiff =  clamp(InvLerp(input[0].vertex.z,shadowTop,_LightPos.z),0,1);

                Varyings v0 = newShadowVertex(aa,(input[0].vertex.xyz + offset),input[0].uv);
                Varyings v1 = newShadowVertex(aa,(input[0].vertex.xyz + offset) + float3(0,0,_ShadowInfo.y),input[0].uv);
                Varyings v2 = newShadowVertex(aa,(input[0].vertex.xyz - offset) + float3(0,0,_ShadowInfo.y),input[0].uv);
                Varyings v3 = newShadowVertex(aa,(input[0].vertex.xyz - offset),input[0].uv);


                const float2 intersect = lineIntersection(v0.vertex.xy,v2.vertex.xy,v1.vertex.xy,v3.vertex.xy);

                const float d0 = distance(v0.vertex.xy,intersect);
                const float d1 = distance(v1.vertex.xy,intersect);
                const float d2 = distance(v2.vertex.xy,intersect);
                const float d3 = distance(v3.vertex.xy,intersect);

                const float3 uvq0 = float3(1,_ShadowInfo.w,1) * ((d0 + d2) / d2);
                const float3 uvq1 = float3(1,lightZDiff,1) * ((d1 + d3) / d3);
                const float3 uvq2 = float3(0,lightZDiff,1) * ((d2 + d0) / d0);
                const float3 uvq3 = float3(0,_ShadowInfo.w,1) * ((d3 + d1) / d1);

                v0.uv_tex = uvq0;
                v1.uv_tex = uvq1;
                v2.uv_tex = uvq2;
                v3.uv_tex = uvq3;

                /// Triangle 0-1-3
                triStream.Append(v0);
                triStream.Append(v1);
                triStream.Append(v3);

                /// Triangle 1-2-3
                triStream.Append(v1);
                triStream.Append(v2);
                triStream.Append(v3);
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 main = tex2D(_MainTex, i.uv);
                float4 col = i.color;
                const float aa = tex2D(_ShadowTexture,i.uv_tex.xy / i.uv_tex.z).a;
                clip(aa - 0.1);
                col.r = aa;
                col.g = main.a * col.g * aa;

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
