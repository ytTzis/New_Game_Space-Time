Shader "MK4/StreetLights3"
{
    Properties
    {
        _Emission("Emission", 2D) = "white" {}
        _Texture0("Texture 0", 2D) = "black" {}
        _Specular("Specular", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Color1("Color 1", Color) = (1,1,1,0)
        _Background("Background", Color) = (0,0,0,0)
        _AlbedoPower("Albedo Power", Range(0, 1)) = 0
        _EmissionPower("Emission Power", Range(0, 1)) = 0
        _Distort("Distort", Range(0, 1)) = 0.35
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
            TEXTURE2D(_Emission); SAMPLER(sampler_Emission);
            TEXTURE2D(_Texture0); SAMPLER(sampler_Texture0);
            float4 _Color1; float4 _Background; float _AlbedoPower; float _EmissionPower; float _Distort;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                half noise = SAMPLE_TEXTURE2D(_Texture0, sampler_Texture0, i.uv * 0.5 + _Time.y * float2(0.05, 0.07)).a * _Distort * 0.1;
                half4 mask = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, i.uv + noise);
                half4 mixed = lerp(_Background, _Color1, max(mask.r, max(mask.g, mask.b)));
                half3 result = mixed.rgb * _AlbedoPower + mixed.rgb * _EmissionPower;
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
