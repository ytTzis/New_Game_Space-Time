#ifndef MK4_URP_COMMON_INCLUDED
#define MK4_URP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_Albedo);
SAMPLER(sampler_Albedo);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_AO);
SAMPLER(sampler_AO);
TEXTURE2D(_SpecularGloss);
SAMPLER(sampler_SpecularGloss);
TEXTURE2D(_RainDropsNormal);
SAMPLER(sampler_RainDropsNormal);
TEXTURE2D(_WaveNormal);
SAMPLER(sampler_WaveNormal);

float4 _Albedo_ST;
float4 _NormalMap_ST;
float4 _AO_ST;
float4 _SpecularGloss_ST;
float4 _Color;
float4 _Color0;
float _Specular;
float _Smoothness;
float _RainMask;
float _Raindropsint;
float _RaindropsUVTile;
float _RainSpeed;
float _WaveNormalint;
float _WaveSpeed;
float _WaveUVTile;
float _Cutoff;

struct MK4Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
};

struct MK4Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    float4 shadowCoord : TEXCOORD4;
};

inline MK4Varyings MK4LitVert(MK4Attributes input)
{
    MK4Varyings output;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = positionInputs.positionCS;
    output.uv = input.uv;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
    output.shadowCoord = GetShadowCoord(positionInputs);
    return output;
}

inline float3 MK4BlendNormals(float3 a, float3 b)
{
    return normalize(float3(a.xy + b.xy, a.z * b.z));
}

inline float3 MK4SampleRainNormal(float2 uv, float3 positionWS)
{
    float2 baseUV = TRANSFORM_TEX(uv, _NormalMap);
    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, baseUV), 1.0);

    float rainTiling = lerp(0.05, 3.0, saturate(_RaindropsUVTile));
    float2 rainUV = positionWS.xz * rainTiling;
    float2 rainFlipUV = float2(frac(rainUV.x + _Time.y * _RainSpeed * 0.04), frac(rainUV.y - _Time.y * _RainSpeed * 0.06));
    float3 rainTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainDropsNormal, sampler_RainDropsNormal, rainFlipUV), _Raindropsint);

    float waveTiling = max(0.1, _WaveUVTile * 4.0 + 0.2);
    float2 waveUV = baseUV * waveTiling;
    float2 wavePannerA = waveUV + float2(_Time.y * (_WaveSpeed + 0.1) * 0.35, _Time.y * (_WaveSpeed + 0.1) * 0.2);
    float2 wavePannerB = waveUV + float2(-_Time.y * (_WaveSpeed + 0.1) * 0.18, _Time.y * (_WaveSpeed + 0.1) * 0.4);
    float3 waveA = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, wavePannerA), _WaveNormalint);
    float3 waveB = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, wavePannerB), _WaveNormalint * 0.75);
    float3 waveTS = MK4BlendNormals(waveA, waveB);

    float mask = saturate(_RainMask);
    return MK4BlendNormals(normalTS, normalize(lerp(float3(0, 0, 1), MK4BlendNormals(rainTS, waveTS), mask)));
}

inline half4 MK4BuildLitColor(float2 uv, float3 positionWS, float4 tint)
{
    float2 albedoUV = TRANSFORM_TEX(uv, _Albedo);
    half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, albedoUV) * tint;
    return albedo;
}

inline half3 MK4ApplySimpleLighting(half3 albedo, half3 specular, half smoothness, float3 normalWS, float3 viewDirWS, float3 positionWS, float4 shadowCoord)
{
    Light mainLight = GetMainLight(shadowCoord);
    float3 lightDir = normalize(mainLight.direction);
    half NdotL = saturate(dot(normalWS, lightDir));
    half shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    half3 ambient = max(SampleSH(normalWS), 0.0h) * albedo;
    half3 diffuse = ambient + albedo * (0.08h + NdotL * mainLight.color * shadow * 1.2h);

    float3 halfDir = normalize(lightDir + viewDirWS);
    half spec = pow(saturate(dot(normalWS, halfDir)), lerp(8.0, 128.0, smoothness)) * saturate(_Specular);
    half3 specCol = specular * spec * mainLight.color * shadow;
    half3 reflection = GlossyEnvironmentReflection(reflect(-viewDirWS, normalWS), 1.0h - smoothness, 1.0h) * specular * (0.15h + smoothness * 0.35h);

    return diffuse + specCol + reflection;
}

#endif
