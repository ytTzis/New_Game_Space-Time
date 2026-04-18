Shader "MK4/Standar Panner"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "gray" {}
        _Normal("Normal", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Range(0, 3)) = 1
        _MetallicGloss("Metallic Gloss", 2D) = "black" {}
        _AO("AO", 2D) = "white" {}
        _PannerXYSpeed("Panner XY Speed", Vector) = (0,0,0,0)
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
            TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo);
            TEXTURE2D(_MetallicGloss); SAMPLER(sampler_MetallicGloss);
            TEXTURE2D(_AO); SAMPLER(sampler_AO);
            float4 _PannerXYSpeed;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                float2 uv = i.uv + _Time.y * _PannerXYSpeed.xy;
                half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, uv);
                half ao = SAMPLE_TEXTURE2D(_AO, sampler_AO, uv).r;
                half metal = SAMPLE_TEXTURE2D(_MetallicGloss, sampler_MetallicGloss, uv).a;
                return half4(albedo.rgb * ao + metal * 0.05, albedo.a);
            }
            ENDHLSL
        }
    }
}
