// 고른 타워 둘레의 선택 표시 (타워 프리팹의 HightLight 스프라이트에 씀)
//   그림 대신 판의 자리(오브젝트 공간)로 모양을 그림: 타워 칸 가장자리(_Edge)를 따라 밝은 테두리 + 바깥으로 번지는 빛
//   테두리를 따라 빛 두 줄기가 돌고(_Speed), 전체가 숨 쉬듯 밝아졌다 어두워짐(_Pulse)
//   색은 스프라이트 색(SpriteRenderer.color)
Shader "SquareTowerDefense/SelectionGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _HalfSize ("Quad Half Size", Float) = 0.5
        _Edge ("Tile Edge (0~1)", Range(0.1, 1)) = 0.7
        _Corner ("Corner Round", Range(0, 0.5)) = 0.12
        _Line ("Line Width", Range(0.001, 0.2)) = 0.03
        _Glow ("Glow Falloff", Float) = 9
        _Speed ("Sweep Speed", Float) = 2.2
        _Pulse ("Pulse", Range(0, 1)) = 0.25
        _Intensity ("Intensity", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _HalfSize, _Edge, _Corner, _Line, _Glow, _Speed, _Pulse, _Intensity;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 local : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.local = v.vertex.xy / max(_HalfSize, 0.0001);
                return o;
            }

            float RoundBox(float2 p, float half, float round)
            {
                float2 q = abs(p) - (half - round);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - round;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.local;
                float d = RoundBox(p, _Edge, _Corner * _Edge);
                float outside = max(d, 0.0);

                float rim = exp(-pow(d / _Line, 2.0));
                float glow = exp(-outside * _Glow) * step(0.0, d);

                float angle = atan2(p.y, p.x);
                float sweep = pow(0.5 + 0.5 * cos(2.0 * (angle - _Time.y * _Speed)), 12.0);
                float pulse = 1.0 - _Pulse + _Pulse * (0.5 + 0.5 * sin(_Time.y * 4.0));

                float edgeFade = 1.0 - smoothstep(0.85, 1.0, max(abs(p.x), abs(p.y)));
                float amount = (rim * (0.7 + sweep * 1.3) + glow * (0.45 + sweep * 0.5)) * pulse * edgeFade;
                float3 color = lerp(i.color.rgb, float3(1, 1, 1), saturate(rim * sweep * 0.6));
                return float4(color * amount * _Intensity * i.color.a, 0.0);
            }
            ENDCG
        }
    }
}
