Shader "MK4/Movie Billboard"
{
    Properties
    {
        _Maintexture("Main texture", 2D) = "white" {}
        _Masks("Masks", 2D) = "white" {}
        _Color("Color", Color) = (0.5807742,0.7100198,0.9632353,0)
        _Animation("Animation", 2D) = "white" {}
        _Columns("Columns", Range(0, 128)) = 4
        _Rows("Rows", Range(0, 128)) = 4
        _MovieSpeed("Movie Speed", Range(0, 50)) = 1
        _Specular("Specular", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _LED("LED", 2D) = "black" {}
        _EmissionAlbedo("Emission Albedo", Range(0, 1)) = 1
        _EmissionLED("Emission LED", Range(0, 1)) = 1
        _Texture0("Texture 0", 2D) = "black" {}
        _Distort("Distort", Range(0, 1)) = 0.1
        _DistortSpeed("Distort Speed", Range(0, 1)) = 0
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="Opaque" }
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

            TEXTURE2D(_Maintexture);
            SAMPLER(sampler_Maintexture);
            TEXTURE2D(_Masks);
            SAMPLER(sampler_Masks);
            TEXTURE2D(_Animation);
            SAMPLER(sampler_Animation);
            TEXTURE2D(_LED);
            SAMPLER(sampler_LED);
            TEXTURE2D(_Texture0);
            SAMPLER(sampler_Texture0);

            float4 _Masks_ST;
            float4 _LED_ST;
            float4 _Color;
            float _Columns;
            float _Rows;
            float _MovieSpeed;
            float _EmissionAlbedo;
            float _EmissionLED;
            float _Distort;
            float _DistortSpeed;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 FlipbookUV(float2 uv, float columns, float rows, float speed)
            {
                float total = max(1.0, columns * rows);
                float index = floor(fmod(_Time.y * speed, total));
                float col = fmod(index, columns);
                float row = floor(index / columns);
                row = rows - 1.0 - row;
                return uv / float2(columns, rows) + float2(col / columns, row / rows);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float distortTime = _Time.y * _DistortSpeed;
                float2 noiseUV = uv * 0.3;
                half distort = (
                    SAMPLE_TEXTURE2D(_Texture0, sampler_Texture0, noiseUV + distortTime * float2(-3.0, 6.0)).a * 0.5 +
                    SAMPLE_TEXTURE2D(_Texture0, sampler_Texture0, noiseUV + distortTime * float2(3.0, 4.0)).a +
                    SAMPLE_TEXTURE2D(_Texture0, sampler_Texture0, noiseUV + distortTime * float2(-3.3, -2.0)).a) * (_Distort * 0.01);

                half4 baseTex = SAMPLE_TEXTURE2D(_Maintexture, sampler_Maintexture, uv + distort);
                half4 masks = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, uv * _Masks_ST.xy + _Masks_ST.zw);
                half scanA = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, uv + _Time.y * float2(-0.1, 0.0)).r * masks.b;
                half scanB = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, uv + _Time.y * float2(0.0, 0.1)).g * masks.a;
                half4 anim = SAMPLE_TEXTURE2D(_Animation, sampler_Animation, FlipbookUV(frac(uv * 2.5 + 0.5), max(1.0, _Columns), max(1.0, _Rows), _MovieSpeed) + distort);
                half4 movie = lerp(baseTex, anim, baseTex.a);
                half4 combined = movie + scanA + scanB;
                half4 led = SAMPLE_TEXTURE2D(_LED, sampler_LED, uv * _LED_ST.xy + _LED_ST.zw);
                half3 albedo = combined.rgb * _Color.rgb;
                half3 emission = combined.rgb * _EmissionAlbedo + led.rgb * _EmissionLED;
                return half4(albedo + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
