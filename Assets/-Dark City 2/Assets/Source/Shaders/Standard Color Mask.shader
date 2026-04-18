Shader "MK4/Standard Color Mask"
{
    Properties
    {
        _AlbedoMaskA("Albedo Mask (A)", 2D) = "gray" {}
        _MaskColor("Mask Color", Color) = (0,0,0,0)
        _Normalmap("Normalmap", 2D) = "bump" {}
        _MetallicGloss("Metallic Gloss", 2D) = "black" {}
        _Emission("Emission", 2D) = "black" {}
        [Toggle]_EmissionTexture("Emission Texture", Range(0, 1)) = 0
        [HDR]_EmissionColor("Emission Color", Color) = (0.5,0.5,0.5,0)
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
            TEXTURE2D(_AlbedoMaskA); SAMPLER(sampler_AlbedoMaskA);
            TEXTURE2D(_Normalmap); SAMPLER(sampler_Normalmap);
            TEXTURE2D(_MetallicGloss); SAMPLER(sampler_MetallicGloss);
            TEXTURE2D(_Emission); SAMPLER(sampler_Emission);
            TEXTURE2D(_AO); SAMPLER(sampler_AO);
            float4 _AlbedoMaskA_ST; float4 _Normalmap_ST; float4 _Emission_ST; float4 _MaskColor; float4 _EmissionColor; float _EmissionTexture;
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                float2 uv = TRANSFORM_TEX(i.uv, _AlbedoMaskA);
                half4 baseTex = SAMPLE_TEXTURE2D(_AlbedoMaskA, sampler_AlbedoMaskA, uv);
                half3 albedo = lerp(baseTex.rgb, _MaskColor.rgb, baseTex.a);
                half ao = SAMPLE_TEXTURE2D(_AO, sampler_AO, i.uv).r;
                half4 metal = SAMPLE_TEXTURE2D(_MetallicGloss, sampler_MetallicGloss, i.uv);
                half3 emission = lerp(_EmissionColor.rgb, SAMPLE_TEXTURE2D(_Emission, sampler_Emission, TRANSFORM_TEX(i.uv, _Emission)).rgb * _EmissionColor.rgb, saturate(_EmissionTexture));
                half3 color = albedo * ao + emission;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
