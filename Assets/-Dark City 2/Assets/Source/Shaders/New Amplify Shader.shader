Shader "MK4/Standard Translucency"
{
    Properties
    {
        _Cutoff("Mask Clip Value", Float) = 0.5
        _Albedo("Albedo", 2D) = "gray" {}
        _Normalmap("Normalmap", 2D) = "bump" {}
        _MetallicSmoothness("Metallic Smoothness", 2D) = "black" {}
        _Trans("Trans", 2D) = "black" {}
        _Translucency("Strength", Range(0, 50)) = 1
        _TransNormalDistortion("Normal Distortion", Range(0, 1)) = 0.1
        _TransScattering("Scaterring Falloff", Range(1, 50)) = 2
        _TransDirect("Direct", Range(0, 1)) = 1
        _TransAmbient("Ambient", Range(0, 1)) = 0.2
        _TransShadow("Shadow", Range(0, 1)) = 0.9
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
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
            TEXTURE2D(_MetallicSmoothness); SAMPLER(sampler_MetallicSmoothness);
            TEXTURE2D(_Trans); SAMPLER(sampler_Trans);
            float _Cutoff; float _Translucency; float _TransAmbient;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, i.uv);
                clip(albedo.a - _Cutoff);
                half4 metal = SAMPLE_TEXTURE2D(_MetallicSmoothness, sampler_MetallicSmoothness, i.uv);
                half trans = SAMPLE_TEXTURE2D(_Trans, sampler_Trans, i.uv).r;
                half3 backlight = albedo.rgb * trans * (_Translucency * 0.05 + _TransAmbient);
                return half4(albedo.rgb + metal.rgb * 0.05 + backlight, 1.0);
            }
            ENDHLSL
        }
    }
}
