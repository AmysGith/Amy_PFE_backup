Shader "Custom/FishInstancedIndirect"
{
    Properties
    {
    _FlipX        ("Flip X",         Float) = 0
    _FlipY        ("Flip Y",         Float) = 0
    _RotCorrection("Rot Correction", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Fish
            {
                float3 pos;
                float3 vel;
                float  yaw;
                float  pitch;
                float  colorIndex;
            };

            StructuredBuffer<Fish> _Fish;

            float4 _FishColors[8];
            int _ColorCount;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float3 tint  : TEXCOORD0;
            };

            float3 RotY(float3 v, float a)
            {
                float s, c; sincos(a, s, c);
                return float3(v.x*c + v.z*s, v.y, -v.x*s + v.z*c);
            }

            float3 RotZ(float3 v, float a)
            {
                float s, c; sincos(a, s, c);
                return float3(v.x*c - v.y*s, v.x*s + v.y*c, v.z);
            }

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Fish f = _Fish[instanceID];

                float3 p = RotY(RotZ(IN.positionOS, f.pitch), f.yaw);
                float3 n = RotY(RotZ(IN.normalOS,   f.pitch), f.yaw);

                int ci = clamp((int)f.colorIndex, 0, _ColorCount - 1);

                Light mainLight = GetMainLight();
                float diff = saturate(dot(normalize(n), mainLight.direction));

                Varyings o;
                o.posCS = TransformWorldToHClip(p + f.pos);
                o.tint  = _FishColors[ci].rgb * (diff * 0.75 + 0.25);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(i.tint, 1);
            }

            ENDHLSL
        }
    }
}