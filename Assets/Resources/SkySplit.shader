// 천지일섬의 화면 베기: 찍어 둔 화면(_MainTex)을 한 줄로 베어 두 조각으로 갈라 벌림
//   _Draw 0 → 1: 까만 벤 자국이 8시 쪽 끝에서 2시 쪽으로 그어짐 (빛 없이)
//   _Shift: 두 조각이 선의 양쪽으로 벌어진 거리(픽셀). 벌어진 틈은 까맣고(_BackColor), 조각 둘레는 흰 테두리
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
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _GlowColor, _BackColor;
            float4 _ScreenSize;             // x, y = 찍은 화면의 픽셀 크기
            float4 _CutDir;                 // xy = 베는 선의 방향 (단위 벡터, 8시 → 2시)
            float _Draw, _Shift, _Scale, _Glow, _Run, _Beam, _Flash;

            static const float TAU = 6.2831853;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
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

                // 조각이 없는 자리(벌어진 틈): 까맣게
                float3 color = _BackColor.rgb;
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
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}
