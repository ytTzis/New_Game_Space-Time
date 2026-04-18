Shader "MK4/Rain_triplanar_street1"
{
    Properties
    {
        _Color("Color", Color) = (0.5807742,0.7100198,0.9632353,1)
        _Albedo("Albedo", 2D) = "gray" {}
        _UVTiling("UV Tiling", Range(0, 1)) = 0.2
        _Normalmap("Normalmap", 2D) = "bump" {}
        _SpecularSmoothness("Specular Smoothness", 2D) = "gray" {}
        _Specular("Specular", Range(0, 1)) = 0
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
        _RoadSymbols("Road Symbols", 2D) = "black" {}
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

            TEXTURE2D(_Normalmap);
            SAMPLER(sampler_Normalmap);
            TEXTURE2D(_SpecularSmoothness);
            SAMPLER(sampler_SpecularSmoothness);
            TEXTURE2D(_RoadSymbols);
            SAMPLER(sampler_RoadSymbols);
            float _UVTiling;

            half4 frag(MK4Varyings input) : SV_Target
            {
                float tiling = lerp(0.01, 2.0, _UVTiling);
                float2 worldUV = input.positionWS.xz * tiling;
                half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, worldUV);
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * _Color.rgb, saturate(_RainMask * 0.2));
                half4 road = SAMPLE_TEXTURE2D(_RoadSymbols, sampler_RoadSymbols, input.uv);
                albedo.rgb = lerp(albedo.rgb, road.rgb, road.a);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_Normalmap, sampler_Normalmap, worldUV), 1.0);
                float2 rainUV = input.positionWS.xz * lerp(0.05, 3.0, saturate(_RaindropsUVTile));
                float2 rainAnim = float2(frac(rainUV.x + _Time.y * _RainSpeed * 0.04), frac(rainUV.y - _Time.y * _RainSpeed * 0.06));
                float3 rainTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainDropsNormal, sampler_RainDropsNormal, rainAnim), _Raindropsint);
                float2 waveUV = worldUV * max(0.1, _WaveUVTile * 4.0 + 0.2);
                float3 waveTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, waveUV + _Time.y * float2(0.15, 0.25) * (_WaveSpeed + 0.1)), _WaveNormalint);
                normalTS = MK4BlendNormals(normalTS, normalize(lerp(float3(0,0,1), MK4BlendNormals(rainTS, waveTS), saturate(_RainMask))));

                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(normalize(input.tangentWS.xyz), normalize(bitangentWS), normalize(input.normalWS));
                float3 normalWS = normalize(mul(normalTS, tbn));
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 specGloss = SAMPLE_TEXTURE2D(_SpecularSmoothness, sampler_SpecularSmoothness, worldUV).rgb;
                half3 lit = MK4ApplySimpleLighting(albedo.rgb, max(specGloss, _Specular.xxx), _Smoothness, normalWS, viewDirWS, input.positionWS, input.shadowCoord);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }
    }
}
