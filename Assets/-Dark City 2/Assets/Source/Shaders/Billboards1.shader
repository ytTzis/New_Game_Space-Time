Shader "MK4/Billboards"
{
    Properties
    {
        _Logo("Logo", 2D) = "black" {}
        _PannerX("Panner X", Range(0, 1)) = 0
        _PannerY("Panner Y", Range(0, 1)) = 0
        _DistortPower("Distort Power", Range(0, 1)) = 0
        _Distortions("Distortions", 2D) = "white" {}
        _AlbedoColor("Albedo Color", Color) = (0.490566,0.490566,0.490566,0)
        _Background("Background", 2D) = "gray" {}
        _BackgroundEm("Background Em", Range(0, 1)) = 0
        _LEDint("LED int", Range(0, 1)) = 0
        _LED("LED", 2D) = "white" {}
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0
        _LEDGlow("LED Glow", 2D) = "white" {}
        _Glitch1("Glitch1", Color) = (0.5197807,0.4306336,0.9926471,0)
        _Glitch2("Glitch2", Color) = (0.5588235,0.08093307,0,0)
        _Smoothness("Smoothness", Range(0, 1)) = 0
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

            TEXTURE2D(_Logo);
            SAMPLER(sampler_Logo);
            TEXTURE2D(_Background);
            SAMPLER(sampler_Background);
            TEXTURE2D(_Distortions);
            SAMPLER(sampler_Distortions);
            TEXTURE2D(_LED);
            SAMPLER(sampler_LED);
            TEXTURE2D(_LEDGlow);
            SAMPLER(sampler_LEDGlow);

            float4 _Logo_ST;
            float4 _LED_ST;
            float4 _AlbedoColor;
            float4 _Glitch1;
            float4 _Glitch2;
            float _PannerX;
            float _PannerY;
            float _DistortPower;
            float _BackgroundEm;
            float _LEDint;
            float _GlitchIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvLogo = TRANSFORM_TEX(input.uv, _Logo);
                float2 uvLed = input.uv * _LED_ST.xy + _LED_ST.zw;
                half4 led = SAMPLE_TEXTURE2D(_LED, sampler_LED, uvLed);

                float2 distortSeed = uvLogo + led.a * 0.1;
                float2 pannerA = distortSeed + _Time.y * float2(1.0, 2.0);
                float2 pannerB = distortSeed + _Time.y * float2(-1.8, 0.3);
                float2 pannerC = distortSeed + _Time.y * float2(0.8, -1.5);
                float2 pannerD = distortSeed + _Time.y * float2(-0.8, -1.5);
                half distort = (
                    SAMPLE_TEXTURE2D(_Distortions, sampler_Distortions, pannerA).r +
                    SAMPLE_TEXTURE2D(_Distortions, sampler_Distortions, pannerB).g +
                    SAMPLE_TEXTURE2D(_Distortions, sampler_Distortions, pannerC).b +
                    SAMPLE_TEXTURE2D(_Distortions, sampler_Distortions, pannerD).a) * (_DistortPower * 0.125);

                float2 screenUV = uvLogo + distort;
                float2 logoPanner = uvLogo + _Time.y * float2(_PannerX, _PannerY);

                half4 background = SAMPLE_TEXTURE2D(_Background, sampler_Background, screenUV) * _AlbedoColor;
                half4 logo = SAMPLE_TEXTURE2D(_Logo, sampler_Logo, logoPanner + distort);
                half4 glowA = SAMPLE_TEXTURE2D(_LEDGlow, sampler_LEDGlow, distortSeed + _Time.y * float2(2.0, 3.0));
                half4 glowB = SAMPLE_TEXTURE2D(_LEDGlow, sampler_LEDGlow, distortSeed + _Time.y * float2(0.0, 0.6));
                half4 glowC = SAMPLE_TEXTURE2D(_LEDGlow, sampler_LEDGlow, distortSeed + _Time.y * float2(-5.0, -2.3));
                half4 glowMask = SAMPLE_TEXTURE2D(_LEDGlow, sampler_LEDGlow, distortSeed + _Time.y * float2(-6.0, 5.0));

                half pulse = saturate(0.7 + sin(_Time.y * 4.0) * 0.3);
                half glitch = saturate(_GlitchIntensity + glowMask.a);
                half3 screenBase = lerp(background.rgb, logo.rgb, saturate(logo.a));
                half3 emission = background.rgb * _BackgroundEm;
                emission += logo.rgb * (logo.a * (0.35h + _BackgroundEm));
                emission += glowC.b * lerp(logo.rgb, led.rgb, 0.5h);
                emission += glitch * (((_Glitch1.rgb * glowA.g) + (_Glitch2.rgb * glowB.r) + (led.rgb * pulse)) * _LEDint);

                half alpha = saturate(max(logo.a, background.a));
                clip(alpha - 0.1h);

                return half4(screenBase + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
