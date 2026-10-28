// 특수 타일(맵 버프 등) 표시 셰이더 — 칸 하나를 덮는 판 (uv 0 ~ 1)
//   모든 칸: 안쪽 테두리가 은은하게 숨 쉬듯 빛남 + 옅은 바닥
//   _Shape마다 칸 안의 무늬 (숫자는 SpecialTileGlowEffect의 표와 같게)
//     길 (적에게 이로움)   0 방어: 육각 격자가 사선으로 반짝 / 1 이동력: 갈매기 무늬가 위로 흐름 / 2 행동 카운트: 시계 눈금이 돎
//                         3 회복: 십자가 숨 쉬고 알갱이가 올라감 / 4 정화: 고리가 퍼짐 / 5 비프로스트: 무지개 소용돌이
//     스폰 (타워에게 이로움) 6 공격력: 화살촉이 위로 / 7 행동 카운트: 시계 눈금 / 8 공격 횟수: 마름모 셋이 돎 / 9 Mastery: 반짝이는 별
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

            // 0 방어: 육각 격자 + 사선으로 지나가는 반짝임
            float HexShimmer(float2 p, float t)
            {
                float2 s = float2(1.0, 1.7320508);
                float2 h = p * 2.4;
                float2 a = (frac(h / s) - 0.5) * s;
                float2 b = (frac((h - s * 0.5) / s) - 0.5) * s;
                float2 g = dot(a, a) < dot(b, b) ? a : b;
                float hexDist = max(dot(abs(g), normalize(s)), abs(g.x));
                float edgeLine = 1.0 - smoothstep(0.03, 0.09, 0.5 - hexDist);
                float sweep = Band((p.x + p.y) * 0.5 - (frac(t * 0.35) * 3.0 - 1.5), 0.25);
                return edgeLine * (0.25 + sweep * 1.2);
            }

            // 1 이동력: 갈매기 무늬(∧)가 위로 흐름
            float Chevrons(float2 p, float t)
            {
                float v = p.y - abs(p.x) * 0.7;
                float c = frac(v * 1.5 - t * 1.2);
                float stripe = smoothstep(0.0, 0.08, c) * (1.0 - smoothstep(0.22, 0.32, c));
                float mask = (1.0 - smoothstep(0.5, 0.6, abs(p.x))) * (1.0 - smoothstep(0.65, 0.75, abs(p.y)));
                return stripe * mask;
            }

            // 2·7 행동 카운트: 시계 눈금 12개, 불이 한 칸씩 돌며 꼬리를 남김
            float ClockTicks(float2 p, float t)
            {
                float angle = frac(atan2(p.x, p.y) / TAU + 1.0);     // 12시에서 시계 방향 0 ~ 1
                float index = floor(angle * 12.0);
                float local = frac(angle * 12.0) - 0.5;
                float r = length(p);
                float tick = (1.0 - smoothstep(0.12, 0.2, abs(local))) * (1.0 - smoothstep(0.08, 0.13, abs(r - 0.58)));
                float head = frac(t * 0.6) * 12.0;
                float behind = frac((head - index) / 12.0);           // 0 = 지금 불 / 1에 가까울수록 오래전
                float lit = exp(-behind * 7.0);
                return tick * (0.2 + lit * 1.2) + Band(r, 0.07) * 0.6;
            }

            // 3 회복: 십자가가 숨 쉬고, 알갱이가 올라감
            float HealCross(float2 p, float t)
            {
                float breath = 0.85 + 0.15 * sin(t * 2.2);
                float2 q = abs(p) / breath;
                float vertical = (1.0 - smoothstep(0.12, 0.17, q.x)) * (1.0 - smoothstep(0.42, 0.47, q.y));
                float horizontal = (1.0 - smoothstep(0.12, 0.17, q.y)) * (1.0 - smoothstep(0.42, 0.47, q.x));
                float plus = max(vertical, horizontal);
                float motes = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float x = (k - 1) * 0.55;
                    float y = frac(t * 0.35 + k * 0.37) * 1.8 - 0.9;
                    motes += Band(length(p - float2(x, y)), 0.06) * (1.0 - smoothstep(0.4, 0.9, y));
                }
                return plus * 0.9 + motes;
            }

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

            // 6 공격력: 속이 찬 화살촉이 위로 올라감
            float RisingArrows(float2 p, float t)
            {
                float y = frac(p.y * 0.9 - t * 0.8);
                float halfWidth = (1.0 - y) * 0.42;
                float head = (1.0 - smoothstep(halfWidth - 0.04, halfWidth, abs(p.x))) * smoothstep(0.0, 0.06, y) * (1.0 - smoothstep(0.55, 0.62, y));
                float mask = 1.0 - smoothstep(0.62, 0.75, abs(p.y));
                return head * mask * 0.8;
            }

            // 8 공격 횟수: 마름모 셋이 가운데를 돎
            float OrbitDiamonds(float2 p, float t)
            {
                float result = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float a = t * 1.6 + k * TAU / 3.0;
                    float2 q = p - float2(cos(a), sin(a)) * 0.48;
                    result += 1.0 - smoothstep(0.1, 0.15, abs(q.x) + abs(q.y));
                }
                return result + Band(length(p), 0.1) * 0.4;
            }

            // 9 Mastery: 네 갈래 별이 반짝이며 천천히 돎
            float Star(float2 p, float t)
            {
                float a = t * 0.4;
                float2 q = float2(p.x * cos(a) - p.y * sin(a), p.x * sin(a) + p.y * cos(a));
                float twinkle = 0.75 + 0.25 * sin(t * 3.0);
                q /= twinkle;
                float rays = max(exp(-abs(q.x) * 14.0) * exp(-abs(q.y) * 2.6), exp(-abs(q.y) * 14.0) * exp(-abs(q.x) * 2.6));
                return rays * 1.3 + Band(length(q), 0.12) * 0.8;
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
                if (_Shape < 0.5) pattern = HexShimmer(p, t);
                else if (_Shape < 1.5) pattern = Chevrons(p, t);
                else if (_Shape < 2.5) pattern = ClockTicks(p, t);
                else if (_Shape < 3.5) pattern = HealCross(p, t);
                else if (_Shape < 4.5) pattern = PurifyRings(p, t);
                else if (_Shape < 5.5)
                {
                    float3 rainbow;
                    pattern = BifrostSwirl(p, t, rainbow);
                    color = lerp(color, rainbow, 0.85);
                }
                else if (_Shape < 6.5) pattern = RisingArrows(p, t);
                else if (_Shape < 7.5) pattern = ClockTicks(p, t);
                else if (_Shape < 8.5) pattern = OrbitDiamonds(p, t);
                else pattern = Star(p, t);

                float alpha = saturate(border * 0.9 + fill + pattern * 0.75) * _Color.a * _Alpha;
                return float4(color * _Intensity * alpha, alpha);
            }
            ENDCG
        }
    }
}
