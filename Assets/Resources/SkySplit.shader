// 천지일섬의 화면 베기: 찍어 둔 화면(_MainTex)을 한 줄로 베어 두 조각으로 갈라 벌림
//   _Draw 0 → 1: 까만 벤 자국이 8시 쪽 끝에서 2시 쪽으로 그어짐 (빛 없이)
//   _Shift: 화면을 조금만 갈라 틈을 만듦 / _Seep: 그 틈에서 우주가 새어 나와 화면 위로 번짐
//   _VoidOnly: 별이 있는 우주만 그림. UI 정점 알파로 우주 전체를 서서히 걷어냄
//   _Run 0 → 1: 빛이 틈을 따라 8시 → 2시로 달림 (머리가 밝고 꼬리가 남음)
//   _Beam 0 → 1: 벤 자리에서 빛이 넓게 퍼져 화면을 덮음 / _Flash: 화면 전체가 하얘짐
//   _Scale: 조각 크기 (1 = 그대로. 지금은 안 씀) / _Glow: 틈 전체의 은은한 빛 (지금은 안 씀)
//   좌표는 "텍스처 픽셀" 기준. 선은 화면 가운데를 지남 (C#이 _CutDir로 방향만 넘김, 찍은 화면이 뒤집혔으면 y를 뒤집어서)
Shader "SquareTowerDefense/SkySplit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured Screen", 2D) = "white" {}
        _GlowColor ("Light Color", Color) = (1, 0.9, 0.6, 1)
        _BackColor ("Cut Color", Color) = (0, 0, 0, 1)
        _Run ("Light Run", Range(0, 1)) = 0
        _Draw ("Slash Draw", Range(0, 1)) = 0
        _Shift ("Split Distance (px)", Float) = 0
        _Scale ("Piece Scale", Range(0.1, 1)) = 1
        _Glow ("Seam Light", Range(0, 1)) = 0
        _Beam ("Light Beam", Range(0, 1)) = 0
        _Flash ("Flash", Range(0, 1)) = 0
        [HideInInspector] _VoidOnly ("Starfield Only", Float) = 0
        _Seep ("Space Seeping From Cut", Range(0, 1)) = 0
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
            float4 _GlowColor, _BackColor;
            float4 _ScreenSize;             // x, y = 찍은 화면의 픽셀 크기
            float4 _CutDir;                 // xy = 베는 선의 방향 (단위 벡터, 8시 → 2시)
            float _Draw, _Shift, _Scale, _Glow, _Run, _Beam, _Flash, _VoidOnly, _Seep;

            static const float TAU = 6.2831853;

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
                // Screen coordinates stay fixed while the captured halves slide apart.
                // The full-screen fade uses the same coordinates, so the stars never jump.
                float2 sky = screenUV * float2(_ScreenParams.x / max(_ScreenParams.y, 1.0), 1.0);
                float center = saturate(1.0 - length(screenUV - 0.5) * 1.3);
                float3 background = lerp(float3(0.002, 0.004, 0.014), float3(0.018, 0.012, 0.05), center);
                float nearStars = StarLayer(sky, 24.0, 5.0, 0.90);
                float farStars = StarLayer(sky, 47.0, 19.0, 0.96) * 0.55;
                return background + float3(0.72, 0.84, 1.0) * nearStars + float3(0.78, 0.72, 1.0) * farStars;
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
                if (_VoidOnly > 0.5) return float4(space * alpha, alpha);

                float2 size = max(_ScreenSize.xy, float2(1.0, 1.0));
                float2 px = i.uv * size;
                float2 center = size * 0.5;
                float2 dir = normalize(_CutDir.xy);
                float2 normal = float2(-dir.y, dir.x);
                float scale = max(_Scale, 0.1);

                // 줄어든 화면 → 원래 화면 좌표. 선이 가운데를 지나므로 줄여도 선의 자리는 그대로
                float2 q = center + (px - center) / scale;
                float2 halfShift = normal * (_Shift * 0.5);
                float2 sourceA = q - halfShift;                 // 선의 위쪽 조각 (normal 쪽)
                float2 sourceB = q + halfShift;                 // 아래쪽 조각
                float cutA = dot(sourceA - center, normal);
                float cutB = dot(sourceB - center, normal);
                bool inA = cutA >= 0.0 && Inside(sourceA, size);
                bool inB = cutB < 0.0 && Inside(sourceB, size);

                // 조각이 없는 자리(벌어진 틈): 화면 좌표에 고정된 별이 반짝이는 우주
                float3 color = space;
                float outline = 0.0;
                if (inA || inB)
                {
                    float2 source = inA ? sourceA : sourceB;
                    color = tex2D(_MainTex, source / size).rgb;
                    float borderPx = 3.0 / scale;                    // 화면에서 3픽셀 굵기의 흰 테두리 (줄면 원래 좌표로는 더 굵게)
                    float split = step(0.5, _Shift);                 // 벌어지기 시작해야 테두리
                    outline = (1.0 - smoothstep(borderPx * 0.6, borderPx, EdgeDistance(source, size, inA ? cutA : cutB))) * split;
                }

                // along: 베는 선 위의 위치 (8시 쪽 끝 0 → 2시 쪽 끝 1) / lineDistance: 선까지의 거리(픽셀)
                float along = dot(px - center, dir) / length(center) * 0.5 + 0.5;
                float lineDistance = abs(dot(px - center, normal));
                float gapHalf = _Shift * 0.5 * scale;

                // 베는 순간: 빛 없이 까만 자국만 8시 쪽부터 그어짐
                float drawn = 1.0 - smoothstep(_Draw - 0.01, _Draw + 0.01, along);
                float mark = (1.0 - smoothstep(1.0, 2.5, lineDistance)) * drawn * (1.0 - step(0.5, _Shift));
                color = lerp(color, _BackColor.rgb, mark);

                // 갈라진 뒤: 빛이 벤 자국(틈)을 따라 8시 → 2시로 달림. 머리가 가장 밝고, 지나온 자리에 꼬리가 남음
                float behind = _Run * 1.1 - along;                                       // 0보다 크면 머리가 이미 지나간 자리
                float head = exp(-behind * behind * 900.0);
                float trail = step(0.0, behind) * exp(-behind * 3.0) * 0.8;
                float inGap = exp(-lineDistance / (gapHalf + 2.5));
                float runLight = (head * 2.5 + trail) * inGap * step(0.001, _Run);
                float runHalo = head * exp(-lineDistance / (gapHalf + 40.0)) * 0.8 * step(0.001, _Run);

                // 끝에 벤 자리에서 빛이 넓게 퍼져 화면을 덮음
                float beamWidth = lerp(gapHalf + 4.0, length(center) * 0.9, _Beam * _Beam);
                float beam = exp(-lineDistance / beamWidth) * _Beam * 2.2 + inGap * _Beam * 3.0;

                float3 warm = lerp(_GlowColor.rgb, float3(1.0, 1.0, 1.0), 0.6);
                color = lerp(color, float3(1.0, 1.0, 1.0), outline);
                color += warm * (runLight + runHalo + beam) + _GlowColor.rgb * _Glow * inGap;
                color = lerp(color, float3(1.0, 1.0, 1.0), _Flash);
                // Space bleeds over the still-visible captured halves from the slash itself.
                // Wavy, feathered fronts expand outward; the screen pieces do not keep moving.
                float mist;
                float seep = SeepMask(px, size, dir, gapHalf, mist);
                color = lerp(color, space, seep);
                color += float3(0.045, 0.025, 0.11) * mist;
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
