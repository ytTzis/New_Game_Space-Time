Shader "MK4/Billboards_tilex4"
{
    Properties
    {
        _TextureSample6("Texture Sample 6", 2D) = "gray" {}
        _AlbedoPower("Albedo Power", Float) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
        _EmissionMultiply("Emission Multiply", Color) = (0,0,0,0)
        _MaskTexture("Mask Texture", 2D) = "gray" {}
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
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

            TEXTURE2D(_TextureSample6);
            SAMPLER(sampler_TextureSample6);
            TEXTURE2D(_MaskTexture);
            SAMPLER(sampler_MaskTexture);

            float4 _EmissionMultiply;
            float _AlbedoPower;

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
                float frameIndex = floor(fmod(_Time.y * 0.25, 4.0));
                float2 frameUV = float2(uv.x * 0.25 + frameIndex * 0.25, uv.y);
                float2 maskPanner = float2(uv.x * 0.0625 + _Time.y * 0.25, uv.y);
                half maskDistort = SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, maskPanner).g;
                half4 frame = SAMPLE_TEXTURE2D(_TextureSample6, sampler_TextureSample6, frameUV + maskDistort);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, uv * float2(1.0, 2.0));

                half3 baseCol = ((mask.r + frame.rgb) * _AlbedoPower).rgb;
                half alpha = saturate(max(frame.a, mask.r));
                clip(alpha - 0.1h);
                half3 emission = frame.rgb * _EmissionMultiply.rgb;
                return half4(baseCol + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
