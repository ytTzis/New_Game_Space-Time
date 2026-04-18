Shader "MK4/Second UV AO"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "gray" {}
        _MetalicGloss("Metalic Gloss", 2D) = "black" {}
        _Normalmap("Normalmap", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 5)) = 1
        _AO1("AO1", 2D) = "white" {}
        _AO1Intensity("AO1 Intensity", Range(0, 1)) = 1
        _AO2("AO2", 2D) = "white" {}
        _AO2Intensity("AO2 Intensity", Range(0, 1)) = 0
        _AO2GlossMultiply("AO2 Gloss Multiply", Range(0, 1)) = 0
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] _texcoord2("", 2D) = "white" {}
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
            TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo);
            TEXTURE2D(_MetalicGloss); SAMPLER(sampler_MetalicGloss);
            TEXTURE2D(_AO1); SAMPLER(sampler_AO1);
            TEXTURE2D(_AO2); SAMPLER(sampler_AO2);
            float _AO1Intensity; float _AO2Intensity; float _AO2GlossMultiply;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float2 uv2:TEXCOORD1; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 uv2:TEXCOORD1; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; o.uv2 = i.uv2; return o; }
            half4 frag(V i):SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, i.uv);
                half4 metal = SAMPLE_TEXTURE2D(_MetalicGloss, sampler_MetalicGloss, i.uv);
                half ao1 = lerp(1.0h, SAMPLE_TEXTURE2D(_AO1, sampler_AO1, i.uv).r, _AO1Intensity);
                half ao2 = lerp(1.0h, SAMPLE_TEXTURE2D(_AO2, sampler_AO2, i.uv2).r, _AO2Intensity);
                half glossBoost = lerp(1.0h, ao2, _AO2GlossMultiply);
                return half4(albedo.rgb * ao1 * ao2 + metal.rgb * glossBoost * 0.1, albedo.a);
            }
            ENDHLSL
        }
    }
}
