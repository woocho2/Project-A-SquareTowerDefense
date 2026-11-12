// 손그림 연출 플립북(4x4)을 명암 → 3색 램프로 칠해 재생. 스윙 평면 투영과 앞·뒤 반쪽 나눔 지원
Shader "SquareTowerDefense/FlipbookFx"
{
    Properties
    {
        _MainTex ("Flipbook (white/gray + alpha, row-major from top-left)", 2D) = "white" {}
        [HDR] _Color ("Deep (dark gray)", Color) = (.55, .03, .05, 1)
        [HDR] _Color2 ("Mid (light gray)", Color) = (1, .45, .25, 1)
        [HDR] _Color3 ("Hot (white)", Color) = (1, .96, .88, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Intensity ("Intensity", Float) = 1
        _Frame ("Frame (0 ~ Grid*Grid-1, fraction blends)", Float) = 0
        _Grid ("Columns = Rows", Float) = 4
        _Cutoff ("Speckle Cutoff", Range(0, .3)) = .04
        _PlaneFlatten ("Projected Swing Plane", Range(.3, 1)) = 1
        _PivotY ("Swing Origin Y", Float) = 0
        _Hemisphere ("Rear +1 / Front -1 / Whole 0", Float) = 0
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color, _Color2, _Color3;
            float _Alpha, _Intensity, _Frame, _Grid, _Cutoff, _PlaneFlatten, _PivotY, _Hemisphere;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float worldY : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                world.y = _PivotY + (world.y - _PivotY) * _PlaneFlatten;
                o.vertex = UnityWorldToClipPos(world.xyz);
                o.worldY = world.y;
                o.uv = v.uv;
                return o;
            }

            float4 Cell(float2 uv, float index)
            {
                float grid = max(1.0, _Grid);
                float2 tile = float2(fmod(index, grid), grid - 1.0 - floor(index / grid));
                return tex2D(_MainTex, (tile + clamp(uv, .002, .998)) / grid);
            }

            float3 Ramp(float shade)
            {
                float3 color = lerp(_Color.rgb, _Color2.rgb, smoothstep(.25, .62, shade));
                return lerp(color, _Color3.rgb, smoothstep(.68, .95, shade));
            }

            float4 frag(v2f i) : SV_Target
            {
                float last = max(1.0, _Grid * _Grid) - 1.0;
                float frame = clamp(_Frame, 0.0, last);
                float index = floor(frame);
                float blend = frac(frame);
                float4 a = Cell(i.uv, index);
                float4 b = Cell(i.uv, min(index + 1.0, last));
                float coverage = lerp(a.a, b.a, blend);
                float shade = lerp(a.r * a.a, b.r * b.a, blend) / max(coverage, .001);

                // 생성 이미지의 옅은 잡티와 칸 경계 번짐을 지움
                float border = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float alpha = coverage * smoothstep(_Cutoff, _Cutoff * 2.5 + .001, coverage) * smoothstep(.0, .03, border);

                float front = smoothstep(-.012, .012, _PivotY - i.worldY);
                float halfMask = _Hemisphere > .5 ? 1.0 - front : (_Hemisphere < -.5 ? front : 1.0);
                alpha = saturate(alpha) * _Alpha * _Color.a * halfMask;
                return float4(Ramp(shade) * alpha * _Intensity, alpha);
            }
            ENDCG
        }
    }
}
