Shader "MK4/Rain"
{
    Properties
    {
        _Color("Color", Color) = (0.5807742,0.7100198,0.9632353,1)
        _Albedo("Albedo", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _AO("AO", 2D) = "white" {}
        _SpecularGloss("Specular Gloss", 2D) = "white" {}
        _Specular("Specular", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _RainMask("Rain Mask", Range(0, 1)) = 0.5
        _RainDropsNormal("RainDrops Normal", 2D) = "bump" {}
        _Raindropsint("Raindrops  int", Range(0, 5)) = 0
        _RaindropsUVTile("Raindrops UV Tile", Range(0, 1)) = 0
        _RainSpeed("Rain Speed", Range(0, 50)) = 0
        _WaveNormal("Wave Normal", 2D) = "bump" {}
        _WaveNormalint("Wave Normal int", Range(0, 5)) = 0
        _WaveSpeed("Wave Speed", Range(0, 1)) = 0
        _WaveUVTile("Wave UV Tile", Range(0, 1)) = 0
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
            #pragma vertex MK4LitVert
            #pragma fragment frag
            #pragma target 3.0
            #include "MK4UrpCommon.hlsl"

            half4 frag(MK4Varyings input) : SV_Target
            {
                half4 albedo = MK4BuildLitColor(input.uv, input.positionWS, _Color);
                half ao = SAMPLE_TEXTURE2D(_AO, sampler_AO, TRANSFORM_TEX(input.uv, _AO)).r;
                half3 specGloss = SAMPLE_TEXTURE2D(_SpecularGloss, sampler_SpecularGloss, TRANSFORM_TEX(input.uv, _SpecularGloss)).rgb;

                float3 normalTS = MK4SampleRainNormal(input.uv, input.positionWS);
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(normalize(input.tangentWS.xyz), normalize(bitangentWS), normalize(input.normalWS));
                float3 normalWS = normalize(mul(normalTS, tbn));
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                half3 lit = MK4ApplySimpleLighting(albedo.rgb * ao, max(specGloss, _Specular.xxx), _Smoothness, normalWS, viewDirWS, input.positionWS, input.shadowCoord);
                return half4(lit, albedo.a);
            }
            ENDHLSL
        }
    }
}
