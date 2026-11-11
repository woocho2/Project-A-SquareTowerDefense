// 보이드 슬래시의 화면 베기: 찍어 둔 화면(_MainTex)을 한 줄로 베어 두 조각으로 갈라 벌림
//   _Dark 0 → 1: 화면이 어둡고 흐려짐 (가장자리가 더 어두움) / _Gather 0 → 1: 화면 가운데 한 점으로 빛이 모임
//   _Draw 0 → 1 (_Line 1): 흰 심과 _GlowColor 빛무리의 한 줄이 8시 쪽 끝에서 2시 쪽으로 그어짐. 갈라지면 꺼짐
//   _Impact 1: 임팩트 프레임. _ImpactBlack 0 = 흑백 반전 화면에 까만 줄, 1 = 까만 화면에 흰 줄
//   _Shift: 화면을 갈라 틈을 만듦 (틈에는 별이 있는 우주) / _Seep: 그 틈에서 우주가 조금 새어 나옴
//   _Run 0 → 1: 빛이 틈을 따라 8시 → 2시로 달림 / _Glow: 틈 전체의 빛 / _Flash: 화면 전체가 하얘짐
//   좌표는 "텍스처 픽셀" 기준. 선은 화면 가운데를 지남 (C#이 _CutDir로 방향만 넘김, 찍은 화면이 뒤집혔으면 y를 뒤집어서)
Shader "SquareTowerDefense/SkySplit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured Screen", 2D) = "white" {}
        _GlowColor ("Light Color", Color) = (0.45, 0.34, 1, 1)
        _Dark ("Darken", Range(0, 1)) = 0
        _Gather ("Gather Light", Range(0, 1)) = 0
        _Line ("Slash Light", Range(0, 1)) = 0
        _Draw ("Slash Draw", Range(0, 1)) = 0
        _Impact ("Impact Frame", Range(0, 1)) = 0
        _ImpactBlack ("Impact Frame Black", Range(0, 1)) = 0
        _Shift ("Split Distance (px)", Float) = 0
        _Seep ("Space Seeping From Cut", Range(0, 1)) = 0
        _Run ("Light Run", Range(0, 1)) = 0
        _Glow ("Seam Light", Range(0, 1)) = 0
        _Flash ("Flash", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _GlowColor;
            float4 _ScreenSize;             // x, y = 찍은 화면의 픽셀 크기
            float4 _CutDir;                 // xy = 베는 선의 방향 (단위 벡터, 8시 → 2시)
            float _Dark, _Gather, _Line, _Draw, _Impact, _ImpactBlack, _Shift, _Seep, _Run, _Glow, _Flash;

            static const float TAU = 6.2831853;
            static const float3 LUMA = float3(0.299, 0.587, 0.114);

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float StarLayer(float2 sky, float density, float seed, float threshold)
            {
                float2 p = sky * density;
                float2 cell = floor(p);
                float random = Hash(cell + seed);
                float2 center = 0.2 + 0.6 * float2(Hash(cell + seed + 13.7), Hash(cell + seed + 31.3));
                float2 delta = frac(p) - center;
                float distance = length(delta);
                float radius = lerp(0.018, 0.045, random);
                float aa = max(fwidth(distance), 0.002);
                float starPoint = 1.0 - smoothstep(radius - aa, radius + aa, distance);
                float halo = exp(-dot(delta, delta) / (radius * radius * 6.0)) * 0.18;
                float twinkle = 0.5 + 0.5 * sin(_Time.y * (0.8 + random * 2.0) + Hash(cell + seed + 73.0) * TAU);
                twinkle = 0.35 + 0.65 * twinkle * twinkle;
                float rays = exp(-abs(delta.x) * 240.0 - abs(delta.y) * 30.0)
                           + exp(-abs(delta.y) * 240.0 - abs(delta.x) * 30.0);
                return (starPoint + halo + rays * step(0.985, random) * 0.4) * twinkle * step(threshold, random);
            }

            float3 Starfield(float2 screenUV)
            {
                // 별은 화면 좌표에 고정 (조각이 벌어져도 별은 움직이지 않음)
                float2 sky = screenUV * float2(_ScreenParams.x / max(_ScreenParams.y, 1.0), 1.0);
                float center = saturate(1.0 - length(screenUV - 0.5) * 1.3);
                float3 background = lerp(float3(0.004, 0.003, 0.016), float3(0.03, 0.016, 0.075), center);
                float nearStars = StarLayer(sky, 24.0, 5.0, 0.90);
                float farStars = StarLayer(sky, 47.0, 19.0, 0.96) * 0.55;
                return background + float3(0.8, 0.82, 1.0) * nearStars + float3(0.7, 0.6, 1.0) * farStars;
            }

            float SeepMask(float2 px, float2 size, float2 dir, float gapHalf, out float mist)
            {
                float progress = saturate(_Seep);
                float2 normal = float2(-dir.y, dir.x);
                float2 relative = px - size * 0.5;
                float signedDistance = dot(relative, normal);
                float shortSide = max(min(size.x, size.y), 1.0);
                float along = dot(relative, dir) / shortSide;
                float side = sign(signedDistance);
                float billow = sin(along * 11.0 + _Time.y * 0.6 + side * 1.7) * 0.5
                             + sin(along * 23.0 - _Time.y * 0.8 + side * 2.3) * 0.3
                             + sin(along * 41.0 + _Time.y * 0.4) * 0.2;
                float moving = sin(progress * TAU * 0.5);
                float feather = lerp(2.0, shortSide * 0.026, moving);
                float maxDistance = dot(abs(normal), size * 0.5);
                float front = lerp(gapHalf, maxDistance + shortSide * 0.08, progress)
                            + billow * shortSide * 0.055 * moving;
                float edge = abs(signedDistance) - front;
                float started = smoothstep(0.0, 0.035, progress);
                mist = exp(-edge * edge / max(feather * feather * 2.0, 1.0)) * moving * started;
                return (1.0 - smoothstep(-feather, feather, edge)) * started;
            }

            bool Inside(float2 p, float2 size) { return all(p >= 0.0) && all(p <= size); }

            // 조각의 바깥 둘레(화면 가장자리)와 벤 자리까지의 거리(원래 화면 픽셀)
            float EdgeDistance(float2 source, float2 size, float cutDistance)
            {
                float screenEdge = min(min(source.x, size.x - source.x), min(source.y, size.y - source.y));
                return min(screenEdge, abs(cutDistance));
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 screenUV = i.vertex.xy / max(_ScreenParams.xy, float2(1.0, 1.0));
                float3 space = Starfield(screenUV);
                float alpha = saturate(i.color.a);

                float2 size = max(_ScreenSize.xy, float2(1.0, 1.0));
                float2 px = i.uv * size;
                float2 center = size * 0.5;
                float2 dir = normalize(_CutDir.xy);
                float2 normal = float2(-dir.y, dir.x);
                float shortSide = max(min(size.x, size.y), 1.0);

                float2 halfShift = normal * (_Shift * 0.5);
                float2 sourceA = px - halfShift;                // 선의 위쪽 조각 (normal 쪽)
                float2 sourceB = px + halfShift;                // 아래쪽 조각
                float cutA = dot(sourceA - center, normal);
                float cutB = dot(sourceB - center, normal);
                bool inA = cutA >= 0.0 && Inside(sourceA, size);
                bool inB = cutB < 0.0 && Inside(sourceB, size);

                // 조각이 없는 자리(벌어진 틈): 별이 반짝이는 우주
                float3 color = space;
                float outline = 0.0;
                if (inA || inB)
                {
                    float2 source = inA ? sourceA : sourceB;
                    color = tex2D(_MainTex, source / size).rgb;
                    float split = step(0.5, _Shift);                 // 벌어지기 시작해야 테두리
                    outline = (1.0 - smoothstep(1.8, 3.0, EdgeDistance(source, size, inA ? cutA : cutB))) * split;
                }
                float3 rawColor = color;

                // along: 베는 선 위의 위치 (8시 쪽 끝 0 → 2시 쪽 끝 1) / lineDistance: 선까지의 거리(픽셀)
                float along = dot(px - center, dir) / length(center) * 0.5 + 0.5;
                float lineDistance = abs(dot(px - center, normal));
                float gapHalf = _Shift * 0.5;

                // 모으기 전: 화면이 어둡고 흐려짐
                float vignette = smoothstep(0.25, 0.85, length(screenUV - 0.5) * 1.4);
                float3 dimmed = lerp(dot(color, LUMA).xxx, color, 0.45) * lerp(0.4, 0.15, vignette);
                color = lerp(color, dimmed, saturate(_Dark));

                // 빛이 화면 가운데 한 점으로 모임
                float centerDistance = length(px - center) / shortSide;
                float gather = _Gather * _Gather;
                float gatherCore = exp(-centerDistance * lerp(120.0, 40.0, gather)) * gather * 2.5;
                float gatherHalo = exp(-centerDistance * 8.0) * gather * 0.4;
                float gatherHint = exp(-lineDistance / 1.5) * gather * 0.3 * (0.6 + 0.4 * sin(_Time.y * 90.0));
                color += gatherCore + _GlowColor.rgb * (gatherHalo + gatherHint);

                // 베는 순간: 흰 심 + 빛무리의 한 줄. 머리가 가장 밝음
                float drawn = 1.0 - smoothstep(_Draw - 0.01, _Draw + 0.01, along);
                float lineOn = drawn * saturate(_Line) * (1.0 - step(0.5, _Shift));
                float lineCore = (1.0 - smoothstep(1.2, 3.2, lineDistance)) * lineOn;
                float lineHalo = exp(-lineDistance / (shortSide * 0.018)) * lineOn;
                float headAlong = (along - _Draw) * 25.0;
                float drawHead = exp(-headAlong * headAlong) * exp(-lineDistance / (shortSide * 0.012)) * lineOn * (1.0 - step(0.999, _Draw));
                color += lineCore * 1.6 + drawHead * 2.5 + _GlowColor.rgb * lineHalo * 1.3;

                // 갈라진 뒤: 빛이 틈을 따라 8시 → 2시로 달림. 머리가 가장 밝고, 지나온 자리에 꼬리가 남음
                float behind = _Run * 1.1 - along;
                float head = exp(-behind * behind * 900.0);
                float trail = step(0.0, behind) * exp(-behind * 3.0) * 0.8;
                float inGap = exp(-lineDistance / (gapHalf + 2.5));
                float runLight = (head * 2.5 + trail) * inGap * step(0.001, _Run);
                float runHalo = head * exp(-lineDistance / (gapHalf + 40.0)) * 0.8 * step(0.001, _Run);
                float3 light = lerp(_GlowColor.rgb, float3(1.0, 1.0, 1.0), 0.55);

                color = lerp(color, float3(1.0, 1.0, 1.0), outline);
                color += light * (runLight + runHalo) + _GlowColor.rgb * _Glow * (inGap * 1.5 + exp(-lineDistance / (gapHalf + shortSide * 0.05)) * 0.5);

                // 틈에서 우주가 조금 새어 나옴 (조각은 더 움직이지 않음)
                float mist;
                float seep = SeepMask(px, size, dir, gapHalf, mist);
                color = lerp(color, space, seep);
                color += _GlowColor.rgb * 0.18 * mist;
                color = lerp(color, float3(1.0, 1.0, 1.0), saturate(_Flash));

                // 임팩트 프레임: 흑백 반전 화면에 까만 줄 → 까만 화면에 흰 줄
                float ink = 1.0 - smoothstep(0.28, 0.48, dot(rawColor, LUMA));
                float slashMask = saturate(lineCore + lineHalo * 0.8);
                float3 invertFrame = ink.xxx * (1.0 - slashMask);
                float3 blackFrame = saturate(lineCore * 1.6) + _GlowColor.rgb * lineHalo * 1.6;
                color = lerp(color, lerp(invertFrame, blackFrame, saturate(_ImpactBlack)), saturate(_Impact));

                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
