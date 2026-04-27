Shader "Custom/VolumetricRays"
{
    Properties
    {
        _MainTex     ("Texture", 2D) = "white" {}
        _RaysTex     ("Rays Texture", 2D) = "black" {}
        _SunPosition ("Sun Position", Vector) = (0.5, 0.5, 0, 0)
        _NumSamples  ("Num Samples", Float) = 64
        _Density     ("Density", Float) = 0.5
        _Weight      ("Weight", Float) = 0.5
        _Decay       ("Decay", Float) = 0.95
        _Exposure    ("Exposure", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        // Pass 0 : calcule les rayons
        Pass
        {
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _SunPosition;
            float  _NumSamples, _Density, _Weight, _Decay, _Exposure;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
{
    return half4(1, 0, 0, 1); // rouge pur
}
            ENDHLSL
        }

        // Pass 1 : additionne les rayons sur l'image originale
        Pass
        {
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_RaysTex); SAMPLER(sampler_RaysTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(SAMPLE_TEXTURE2D(_RaysTex, sampler_RaysTex, IN.uv).rgb, 1);
            }
            ENDHLSL
        }
    }
}