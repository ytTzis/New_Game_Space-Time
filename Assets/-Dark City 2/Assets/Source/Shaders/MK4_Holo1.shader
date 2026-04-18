Shader "MK4/Holo1"
{
    Properties
    {
        _Emission("Emission", 2D) = "black" {}
        _Composite("Composite", 2D) = "black" {}
        [HDR]_Color("Color", Color) = (1,0.8168357,0.05147058,0)
        _ColorbyTexture("Color by Texture", Float) = 0
        _ColorTexture("Color Texture", 2D) = "white" {}
        _TextureExposition("Texture Exposition", Range(0, 1)) = 0
        _Opacity("Opacity", Range(0, 1)) = 0
        _Panner1("Panner1", Range(0, 2)) = 0
        _Panner2("Panner2", Range(0, 2)) = 0
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            TEXTURE2D(_Composite); SAMPLER(sampler_Composite);
            TEXTURE2D(_ColorTexture); SAMPLER(sampler_ColorTexture);
            float4 _Color; float _ColorbyTexture; float _TextureExposition; float _Opacity; float _Panner1; float _Panner2;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                half4 comp = SAMPLE_TEXTURE2D(_Composite, sampler_Composite, i.uv + _Time.y * float2(_Panner1 * 0.1, _Panner2 * 0.1));
                half4 emiss = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, i.uv + _Time.y * float2(-_Panner2 * 0.08, _Panner1 * 0.08));
                half4 texColor = SAMPLE_TEXTURE2D(_ColorTexture, sampler_ColorTexture, i.uv);
                half3 tint = lerp(_Color.rgb, texColor.rgb * lerp(1.0h, 4.0h, _TextureExposition), saturate(_ColorbyTexture));
                half alpha = saturate((_Opacity + comp.a + emiss.a) * 0.5);
                return half4((comp.rgb + emiss.rgb) * tint, alpha);
            }
            ENDHLSL
        }
    }
}
