// 타워 테두리(Decoration) 스프라이트를 번쩍이게 그림
//   비스듬한 빛줄기가 _Period초마다 테두리를 훑고(_SweepTime초 동안), 테두리 네 귀퉁이에서 별빛이 번갈아 반짝임
//   _Tier(1 ~ 6)가 높을수록 빛줄기가 밝고 굵어지며 별빛이 많아짐. _Phase로 타워마다 반짝이는 때를 어긋나게 함
//   색은 스프라이트 색(티어 색)
Shader "SquareTowerDefense/FrameShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Tier ("Tier", Range(1, 6)) = 1
        _Phase ("Phase", Float) = 0
        _Period ("Sweep Period", Float) = 2.6
        _SweepTime ("Sweep Time", Float) = 0.55
        _Shine ("Shine", Float) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Tier, _Phase, _Period, _SweepTime, _Shine;

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

            float Star(float2 p, float2 at, float size)
            {
                float2 q = abs(p - at) / size;
                return exp(-q.x * 14.0) * exp(-q.y * 2.5) + exp(-q.y * 14.0) * exp(-q.x * 2.5) + exp(-dot(q, q) * 6.0);
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv) * i.color;
                float tier = saturate((_Tier - 1.0) / 5.0);

                float time = _Time.y + _Phase;
                float cycle = frac(time / _Period) * _Period;
                float sweep = lerp(-0.35, 1.35, saturate(cycle / _SweepTime));
                float diagonal = (i.uv.x + (1.0 - i.uv.y)) * 0.5;
                float width = lerp(0.05, 0.11, tier);
                float band = exp(-pow((diagonal - sweep) / width, 2.0)) * step(cycle, _SweepTime + 0.2);
                float shine = band * _Shine * lerp(0.7, 1.6, tier);

                float2 corners[4] = { float2(0.1, 0.9), float2(0.9, 0.9), float2(0.9, 0.1), float2(0.1, 0.1) };
                float stars = 0.0;
                int count = (int)clamp(floor(_Tier * 0.7), 1.0, 4.0);
                for (int k = 0; k < 4; k++)
                {
                    if (k >= count) break;
                    float blink = pow(saturate(sin(time * 3.1 + k * 2.4 + _Phase)), 10.0);
                    stars += Star(i.uv, corners[k], lerp(0.05, 0.08, tier)) * blink;
                }
                stars *= lerp(0.6, 1.4, tier);

                float3 shineColor = lerp(i.color.rgb, float3(1, 1, 1), 0.65);
                float3 color = tex.rgb * tex.a + shineColor * shine * tex.a + shineColor * stars;
                float alpha = saturate(tex.a + stars * 0.5);
                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
