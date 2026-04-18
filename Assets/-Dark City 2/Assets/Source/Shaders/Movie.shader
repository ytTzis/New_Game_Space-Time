Shader "MK4/Movie"
{
    Properties
    {
        _Color("Color", Color) = (0.5807742,0.7100198,0.9632353,1)
        _Albedo("Albedo", 2D) = "white" {}
        _Columns("Columns", Range(0, 128)) = 4
        _Rows("Rows", Range(0, 128)) = 16
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

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);
            TEXTURE2D(_LED);
            SAMPLER(sampler_LED);
            TEXTURE2D(_Texture0);
            SAMPLER(sampler_Texture0);

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
                float cols = max(1.0, columns);
                float rs = max(1.0, rows);
                float total = cols * rs;
                float index = floor(fmod(_Time.y * speed, total));
                float col = fmod(index, cols);
                float row = floor(index / cols);
                row = rs - 1.0 - row;
                return uv / float2(cols, rs) + float2(col / cols, row / rs);
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

                half4 movie = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, FlipbookUV(uv, _Columns, _Rows, _MovieSpeed) + distort);
                half4 led = SAMPLE_TEXTURE2D(_LED, sampler_LED, uv * _LED_ST.xy + _LED_ST.zw);
                half3 albedo = movie.rgb * _Color.rgb;
                half3 emission = movie.rgb * _EmissionAlbedo + led.rgb * _EmissionLED;
                return half4(albedo + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
