#ifndef HDRP_SPEEDTREE8_INPUT_INCLUDED
#define HDRP_SPEEDTREE8_INPUT_INCLUDED

#define SPEEDTREE_Y_UP

#ifdef EFFECT_BUMP
#define _NORMALMAP
#endif

#define _ALPHATEST_ON

// Enabling both TEXCOORD0 and TEXCOORD1 means you get 4 components instead of 2, and we pretty much always need that
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3) || defined(DYNAMICLIGHTMAP_ON) || defined(DEBUG_DISPLAY) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
#define ATTRIBUTES_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD2
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

///////////////////////////////////////////////////////////////////////
//  struct SpeedTreeVertexInput

// texcoord setup
//
//      BRANCHES                        FRONDS                      LEAVES
// 0    diffuse uv, branch wind xy      "                           "
// 1    lod xyz, 0                      lod xyz, 0                  anchor xyz, lod scalar
// 2    detail/seam uv, seam amount, 0  frond wind xyz, 0           leaf wind xyz, leaf group


struct SpeedTreeVertexInput
{
    float4 vertex       : POSITION;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float4 texcoord3    : TEXCOORD3;
    float4 color        : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

///////////////////////////////////////////////////////////////////////
//  SpeedTree winds

#ifdef ENABLE_WIND

#define WIND_QUALITY_NONE       0
#define WIND_QUALITY_FASTEST    1
#define WIND_QUALITY_FAST       2
#define WIND_QUALITY_BETTER     3
#define WIND_QUALITY_BEST       4
#define WIND_QUALITY_PALM       5

uniform float _WindQuality;
uniform float _WindEnabled;

#include "SpeedTreeWind.hlsl"

UNITY_INSTANCING_BUFFER_START(STWind)
UNITY_DEFINE_INSTANCED_PROP(float, _GlobalWindTime)
UNITY_INSTANCING_BUFFER_END(STWind)

#endif

half4 _Color;
int _TwoSided;
float3 _EmissiveColor;
float _ZBias;

#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

#if defined(GEOM_TYPE_FROND) || defined(GEOM_TYPE_LEAF) || defined(GEOM_TYPE_FACINGLEAF)
#define SPEEDTREE_ALPHATEST
float _Cutoff;
#endif

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

#ifdef EFFECT_EXTRA_TEX
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);
#else
float _Glossiness;
float _Metallic;
#endif

#ifdef EFFECT_HUE_VARIATION
half4 _HueVariationColor;
#endif

#if defined(EFFECT_BUMP) && !defined(LIGHTMAP_ON)
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
#endif

#ifdef EFFECT_BILLBOARD
half _BillboardShadowFade;
#endif

#ifdef EFFECT_SUBSURFACE
TEXTURE2D(_SubsurfaceTex);
SAMPLER(sampler_SubsurfaceTex);
half4 _SubsurfaceColor;
half _SubsurfaceIndirect;
#endif

float3 _LightDirection;

// ---------------------------------------------------------

// Attributes
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define VARYINGS_NEED_TANGENT_TO_WORLD      // Necessary to get interpolators for normal
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_POSITION_WS
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_COLOR

#ifdef EFFECT_BUMP
#define ATTRIBUTES_NEED_TANGENT
#endif

// Branch detail UV needs 3 components, BUT using TEXCOORD1 and TEXCOORD0 gives us 4, and we
// are only using 3 out of those 4, so we can make do with just 2 on TEXCOORD2
#ifdef GEOM_TYPE_BRANCH_DETAIL
#define VARYINGS_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(EFFECT_HUE_VARIATION) || defined(GEOM_TYPE_BRANCH_DETAIL)
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD1
#endif

#if (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_DEPTH_ONLY)
#undef VARYINGS_NEED_TEXCOORD2
#undef ATTRIBUTES_NEED_TEXCOORD2
#undef EFFECT_BUMP
#undef EFFECT_HUE_VARIATION
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"


#endif
