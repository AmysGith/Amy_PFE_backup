Shader "Paro222/UnderwaterEffects_RTHandle"
{
    Properties
    {
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _color ("Color", Color) = (0.1, 0.5, 0.6, 1)
        _dis ("Distance", Float) = 10
        _alpha ("Alpha", Range(0,1)) = 0.3
        _refraction ("Refraction", Float) = 0.1
        _normalUV ("Normal UV", Vector) = (1, 1, 0.2, 0.1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float4 _normalUV;
            float4 _color;
            float  _dis;
            float  _alpha;
            float  _refraction;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(
                    input.vertexID == 0 ? -1 : (input.vertexID == 1 ? -1 : 3),
                    input.vertexID == 0 ? -1 : (input.vertexID == 1 ?  3 : -1),
                    0, 1);
                output.uv = float2(
                    input.vertexID == 0 ?  0 : (input.vertexID == 1 ?  0 :  2),
                    input.vertexID == 0 ?  0 : (input.vertexID == 1 ?  2 :  0));
                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1 - output.uv.y;
                #endif
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Double couche normal map
                float2 normalUV1 = uv * _normalUV.xy + _normalUV.zw * _Time.y;
                float2 normalUV2 = uv * _normalUV.xy * 0.7 - _normalUV.zw * _Time.y * 0.5;
                float3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV1));
                float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV2));
                float3 normal = normalize(normal1 + normal2);

                // Distorsion
                float2 offset = normal.xy * _refraction * 0.01;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + offset);

                // Teinte progressive depuis les bords
                float2 centeredUV = uv - 0.5;
                float dist = saturate(length(centeredUV) * 2);
                float fogAmount = saturate(dist * _alpha * _dis * 0.1);

                // Teinte sans écraser la scène
                col.rgb *= lerp(float3(1,1,1), _color.rgb * 1.5, fogAmount);

                // Vignette subtile
                float vignette = 1 - saturate(dot(centeredUV, centeredUV) * 0.8);
                col.rgb *= lerp(0.85, 1.0, vignette);

                return col;
            }
            ENDHLSL
        }
    }
}