Shader "MK4/StreetLights"
{
    Properties
    {
        _Color("Color", Color) = (0.5807742,0.7100198,0.9632353,1)
        _Albedo("Albedo", 2D) = "white" {}
        _Columns("Columns", Range(0, 128)) = 4
        _Rows("Rows", Range(0, 128)) = 16
        _MovieSpeed("Movie Speed", Range(0, 50)) = 1
        _Specular("Specular", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _EmissionAlbedo("Emission Albedo", Range(0, 1)) = 1
        _Distortion("Distortion", 2D) = "black" {}
        _Distort("Distort", Range(0, 1)) = 0.1
        _DistortSpeed("Distort Speed", Range(0, 1)) = 0
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
            TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo);
            TEXTURE2D(_Distortion); SAMPLER(sampler_Distortion);
            float4 _Color; float _Columns; float _Rows; float _MovieSpeed; float _EmissionAlbedo; float _Distort; float _DistortSpeed;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i){ V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            float2 FlipbookUV(float2 uv,float columns,float rows,float speed){ float cols=max(1.0,columns); float rs=max(1.0,rows); float total=cols*rs; float index=floor(fmod(_Time.y*speed,total)); float col=fmod(index,cols); float row=rs-1.0-floor(index/cols); return uv/float2(cols,rs)+float2(col/cols,row/rs);}            
            half4 frag(V i):SV_Target
            {
                float2 distortUV = i.uv + _Time.y * _DistortSpeed * float2(0.1, 0.15);
                half distort = SAMPLE_TEXTURE2D(_Distortion, sampler_Distortion, distortUV).a * _Distort * 0.1;
                half4 movie = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, FlipbookUV(i.uv, _Columns, _Rows, _MovieSpeed) + distort) * _Color;
                return half4(movie.rgb + movie.rgb * _EmissionAlbedo, 1.0);
            }
            ENDHLSL
        }
    }
}
