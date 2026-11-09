// 화염 분출 연속 프레임과 원형 전조
Shader "SquareTowerDefense/FirePillar"
{
    Properties
    {
        _MainTex ("Fire Eruption Flipbook (4x4)", 2D) = "white" {}
        _NoiseTex ("Flow Noise", 2D) = "gray" {}
        _Color ("Outer Flame", Color) = (0.65, 0.065, 0.012, 1)
        _Color2 ("Hot Flame", Color) = (1, 0.36, 0.025, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Phase ("Flow Seed", Float) = 0
        _Shape ("Shape (0 Fire, 1 Telegraph, 2 Blast)", Float) = 0
        _Progress ("Charge / Eruption Time", Float) = 0
        _Dissolve ("Fuel Dissipation", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _NoiseTex;
            float4 _Color, _Color2;
            float _Alpha, _Phase, _Shape, _Progress, _Dissolve;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 Frame(float2 uv, float index)
            {
                float2 tile = float2(fmod(index, 4.0), 3.0 - floor(index / 4.0));
                return tex2D(_MainTex, (tile + clamp(uv, 0.002, 0.998)) * 0.25);
            }

            float3 FireColor(float heat)
            {
                float3 color = lerp(_Color.rgb * 0.5, _Color2.rgb, smoothstep(0.12, 0.55, heat));
                color = lerp(color, float3(1.0, 0.77, 0.18), smoothstep(0.5, 0.82, heat));
                return lerp(color, float3(1.4, 1.3, 1.08), smoothstep(0.8, 1.0, heat));
            }

            float4 Fire(float2 uv)
            {
                float frame = clamp(_Progress * 40.0, 0.0, 15.0);
                float index = floor(frame);
                float blend = frac(frame);
                float4 a = Frame(uv, index);
                float4 b = Frame(uv, min(index + 1.0, 15.0));
                float coverage = lerp(a.a, b.a, blend);
                float heat = lerp(a.r * a.a, b.r * b.a, blend) / max(coverage, 0.001);
                float border = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float alpha = coverage * smoothstep(0.035, 0.12, coverage) * smoothstep(0.02, 0.07, border);
                alpha *= _Alpha * _Color.a;
                return float4(FireColor(heat) * alpha, alpha);
            }

            float4 frag(v2f i) : SV_Target
            {
                if (_Shape < 0.5) return Fire(i.uv);

                float2 p = i.uv * 2.0 - 1.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float uneven = tex2D(_NoiseTex, p * 0.3 + _Phase).r;
                float rim = 0.79 + (uneven - 0.5) * 0.035;
                float ring = 1.0 - smoothstep(0.012, 0.035, abs(radius - rim));
                float alpha;
                float3 color;
                if (_Shape < 1.5)
                {
                    float disk = (1.0 - smoothstep(0.6, 0.83, radius)) * 0.18;
                    float heat = exp(-radius * radius * 5.0) * _Progress * 0.35;
                    float inner = 1.0 - smoothstep(0.008, 0.025, abs(radius - 0.59));
                    float sweep = pow(saturate(sin(angle * 2.0 - _Progress * 5.0)), 5.0);
                    float middle = (1.0 - smoothstep(0.012, 0.035, abs(radius - 0.7))) * sweep;
                    alpha = saturate(disk + ring * 0.8 + inner * 0.32 + middle * 0.7 + heat * 0.15);
                    color = lerp(_Color.rgb, _Color2.rgb, saturate(ring * 0.9 + inner * 0.7 + middle + heat));
                    color = lerp(color, float3(1.2, 0.94, 0.4), middle * _Progress * 0.6);
                }
                else
                {
                    float teeth = pow(saturate(sin(angle * 7.0 + radius * 14.0)), 5.0);
                    float shockRadius = 0.72 + teeth * 0.08;
                    float shockRing = 1.0 - smoothstep(0.015, 0.065, abs(radius - shockRadius));
                    float flash = exp(-radius * radius * 9.0) * pow(1.0 - saturate(_Progress), 3.0);
                    alpha = saturate(shockRing * 0.8 + flash);
                    color = lerp(_Color.rgb, _Color2.rgb, shockRing);
                    color = lerp(color, float3(1.5, 1.35, 1.05), saturate(flash * 2.0));
                }
                alpha *= _Alpha * _Color.a;
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
