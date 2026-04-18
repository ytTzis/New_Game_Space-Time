Shader "MK4/Window"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Albedo("Albedo", 2D) = "white" {}
        _BaseMap("Base Map", 2D) = "white" {}
        _Normalmap("Normalmap", 2D) = "bump" {}
        _SpecularSmooth("Specular Smooth", 2D) = "gray" {}
        _Emission("Emission", 2D) = "white" {}
        _EmissionMap("Emission Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionMultiply("Emission Multiply", Color) = (1,1,1,0)
        _EmissionPower("Emission Power", Range(0, 1)) = 0.5
        _EmissionTilie("Emission Tilie", Range(0, 1)) = 0.3
        _AO("AO", 2D) = "white" {}
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo);
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Emission); SAMPLER(sampler_Emission);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_AO); SAMPLER(sampler_AO);
            float4 _Color; float4 _EmissionColor; float4 _EmissionMultiply; float _EmissionPower; float _EmissionTilie;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 normalWS:TEXCOORD1; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; o.normalWS = TransformObjectToWorldNormal(float3(0,0,1)); return o; }
            half4 frag(V i):SV_Target
            {
                half albedoMask = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, i.uv).r;
                half emissiveMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).r;
                half ao = lerp(1.0h, SAMPLE_TEXTURE2D(_AO, sampler_AO, i.uv).r, 0.85h);
                float tile = lerp(0.5, 8.0, saturate(_EmissionTilie));
                half3 emissionTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, i.uv * tile).rgb;
                half3 glassTint = lerp(half3(0.015h, 0.02h, 0.028h), half3(0.075h, 0.09h, 0.11h), saturate(albedoMask * 0.32h));
                half3 ambient = max(SampleSH(normalize(i.normalWS)), 0.0h) * glassTint;
                half emissionMaskStrength = saturate(emissiveMask * 1.35h);
                half3 em = emissionTex * emissionMaskStrength * _EmissionColor.rgb * _EmissionMultiply.rgb * (0.15h + _EmissionPower * 1.35h);
                half3 litBase = glassTint * ao + ambient * 0.12h;
                return half4(litBase + em, 1.0);
            }
            ENDHLSL
        }
    }
}
