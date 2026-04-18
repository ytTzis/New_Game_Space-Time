Shader "MK4/StreetLights2"
{
    Properties
    {
        _Emission("Emission", 2D) = "white" {}
        _Specular("Specular", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Color1("Color 1", Color) = (0,0,0,0)
        _Color2("Color 2", Color) = (0,0,0,0)
        _Background("Background", Color) = (0,0,0,0)
        _AlbedoPower("Albedo Power", Range(0, 1)) = 0
        _EmissionPower("Emission Power", Range(0, 1)) = 0
        _SlideSpeed("Slide Speed", Range(0, 1)) = 0
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
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

            TEXTURE2D(_Emission);
            SAMPLER(sampler_Emission);

            float4 _Background;
            float4 _Color1;
            float4 _Color2;
            float _AlbedoPower;
            float _EmissionPower;
            float _SlideSpeed;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float scrollX = lerp(-0.5, 0.5, saturate(_SlideSpeed));
                float2 scanUV = uv + _Time.y * float2(scrollX, 0.0);

                half4 scan = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, scanUV);
                half4 mask = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, uv);
                half4 mixedA = lerp(_Background, _Color1, scan.r);
                half4 mixedB = lerp(_Background, _Color2, mask.g);
                half4 mixed = lerp(mixedA, mixedB, mask.b);

                half3 albedo = mixed.rgb * _AlbedoPower;
                half3 emission = mixed.rgb * _EmissionPower;
                return half4(albedo + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
