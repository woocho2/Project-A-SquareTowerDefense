// 특수 타일(정화·비프로스트)·격자·존 표시 셰이더 — 칸 하나를 덮는 판 (uv 0 ~ 1)
//   특수 타일: 안쪽 테두리가 은은하게 숨 쉬듯 빛남 + 옅은 바닥 + 칸 안의 무늬
//   _Shape (숫자는 SpecialTileGlowEffect·ZoneGlowEffect의 표와 같게. 0 ~ 3·6 ~ 9는 지운 맵 버프의 빈 번호)
//     4 정화: 고리가 퍼짐 / 5 비프로스트: 무지개 소용돌이
//     10 격자: 판의 모든 칸에 깔리는 가장자리 선 (특수 타일 아래)
//     11 존: 칸 둘레의 고리가 스택만큼 시계 방향으로 참(_Progress 0 ~ 1). 다 차면(스킬 2) 고리 전체가 숨 쉬고 칸 바닥도 빛남
//        한 칸에 존이 여럿이면 _Inset만큼 안쪽으로 들인 고리를 하나씩 (ZoneGlowEffect)
//   _Phase: 흐르는 시간(초). 코드가 DOTween으로 계속 올림 (배속을 따름) / _Seed: 칸마다 박자를 어긋나게
//   더하기 섞기라 _Intensity가 1보다 크면 블룸으로 번짐
Shader "SquareTowerDefense/SpecialTile"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _Shape ("Shape", Float) = 0
        _Intensity ("Intensity (Bloom)", Float) = 1.5
        _Alpha ("Alpha", Range(0, 1)) = 0.85
        _Phase ("Phase (seconds)", Float) = 0
        _Seed ("Seed", Float) = 0
        _Progress ("Zone Progress", Range(0, 1)) = 0
        _Inset ("Zone Ring Inset", Range(0, 0.4)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Shape, _Intensity, _Alpha, _Phase, _Seed, _Progress, _Inset;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float TAU = 6.2831853;

            float3 Hue(float h)
            {
                float3 k = abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0;
                return saturate(k);
            }

            float Band(float x, float width) { return exp(-x * x / max(width * width, 0.00001)); }

            // 4 정화: 가운데서 고리 셋이 차례로 퍼짐
            float PurifyRings(float2 p, float t)
            {
                float r = length(p);
                float rings = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float life = frac(t * 0.45 + k / 3.0);
                    rings += Band(r - life * 0.9, 0.035) * (1.0 - life);
                }
                return rings * 1.4 + Band(r, 0.12) * 0.5;
            }

            // 5 비프로스트: 무지개 소용돌이 (색은 Hue로 따로)
            float BifrostSwirl(float2 p, float t, out float3 rainbow)
            {
                float r = length(p);
                float angle = atan2(p.y, p.x);
                float arms = 0.5 + 0.5 * sin(angle * 3.0 + r * 9.0 - t * 2.5);
                float ring = smoothstep(0.15, 0.3, r) * (1.0 - smoothstep(0.75, 0.88, r));
                rainbow = Hue(angle / TAU + r * 0.4 - t * 0.15);
                return arms * arms * ring + Band(r, 0.15) * 0.6;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float t = _Phase + _Seed;
                float d = max(abs(p.x), abs(p.y));

                // 11 존: 둘레 고리가 스택만큼 참
                if (_Shape > 10.5)
                {
                    float outer = 0.95 - _Inset;
                    float ring = smoothstep(outer - 0.11, outer - 0.07, d) * (1.0 - smoothstep(outer - 0.02, outer, d));
                    float angle = frac(atan2(p.x, p.y) / TAU + 1.0);             // 12시에서 시계 방향 0 ~ 1
                    float filled = 1.0 - smoothstep(_Progress - 0.004, _Progress + 0.004, angle);
                    float full = step(0.999, _Progress);
                    float pulse = lerp(1.0, 0.7 + 0.3 * sin(t * 3.0), full);
                    float track = ring * 0.22;                                      // 아직 안 찬 자리는 흐리게
                    float lit = ring * filled * lerp(0.85, 1.25, full) * pulse;
                    float floorGlow = (1.0 - smoothstep(0.3, outer - 0.05, d)) * full * 0.14 * pulse * step(_Inset, 0.001);   // 바닥은 바깥 고리만
                    float zoneAlpha = saturate(track + lit + floorGlow) * _Color.a * _Alpha;
                    return float4(_Color.rgb * _Intensity * zoneAlpha, zoneAlpha);
                }

                // 10 격자: 모든 칸의 가장자리 선만 (움직이지 않음). 옆 칸과 선이 맞닿아 판 전체가 격자로 보임
                if (_Shape > 9.5)
                {
                    float gridLine = smoothstep(0.93, 1.0, d);
                    float gridAlpha = gridLine * _Color.a * _Alpha;
                    return float4(_Color.rgb * _Intensity * gridAlpha, gridAlpha);
                }

                // 안쪽 테두리 (둥근 네모) + 옅은 바닥
                float breath = 0.7 + 0.3 * sin(t * 1.6);
                float border = smoothstep(0.66, 0.9, d) * (1.0 - smoothstep(0.9, 0.98, d)) * breath;
                float fill = (1.0 - smoothstep(0.85, 0.98, d)) * 0.06;

                float3 color = _Color.rgb;
                float pattern = 0.0;
                if (_Shape < 4.5) pattern = PurifyRings(p, t);
                else
                {
                    float3 rainbow;
                    pattern = BifrostSwirl(p, t, rainbow);
                    color = lerp(color, rainbow, 0.85);
                }

                float alpha = saturate(border * 0.9 + fill + pattern * 0.75) * _Color.a * _Alpha;
                return float4(color * _Intensity * alpha, alpha);
            }
            ENDCG
        }
    }
}
