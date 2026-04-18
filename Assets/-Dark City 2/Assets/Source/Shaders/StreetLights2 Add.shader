Shader "MK4/StreetLights2 Add"
{
    Properties
    {
        _Emission("Emission", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Color1("Color 1", Color) = (0,0,0,0)
        _Color2("Color 2", Color) = (0,0,0,0)
        _Background("Background", Color) = (0,0,0,0)
        _EmissionPower("Emission Power", Range(0, 1)) = 0
        _SlideSpeed("Slide Speed", Range(0, 1)) = 0
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        Cull Back
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
            float4 _Background; float4 _Color1; float4 _Color2; float _EmissionPower; float _SlideSpeed;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                float2 scanUV = i.uv + _Time.y * float2(lerp(-0.5,0.5,saturate(_SlideSpeed)), 0.0);
                half4 scan = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, scanUV);
                half4 mask = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, i.uv);
                half4 mixed = lerp(lerp(_Background,_Color1,scan.r), lerp(_Background,_Color2,mask.g), mask.b);
                return half4(mixed.rgb * _EmissionPower, saturate(max(mask.r, max(mask.g, mask.b))));
            }
            ENDHLSL
        }
    }
}
