// 블랙홀처럼 뒤 화면을 휘게 그림 (다크메이지 기본 공격)
//   뒤 화면은 2D 렌더러의 Camera Sorting Layer Texture(Enemy 층까지)를 읽음 → Renderer2D에서 켜 둠
//   _Horizon: 검은 구멍의 반지름 (판 반지름 1 기준)
//   _Strength: 구멍 둘레의 뒤 화면을 바깥에서 끌어오는 세기 (클수록 많이 휨)
//   _Swirl: 가운데로 갈수록 뒤 화면을 비트는 각도 (라디안)
//   _RimColor: 구멍 테두리에 감기는 빛 고리
Shader "SquareTowerDefense/SpaceWarp"
{
    Properties
    {
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Horizon ("Horizon Radius", Range(0, 1)) = 0.25
        _Strength ("Lens Strength", Float) = 0.6
        _Swirl ("Swirl", Float) = 1.5
        [HDR] _RimColor ("Rim Color", Color) = (0.8, 0.3, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraSortingLayerTexture;
            float4 _RimColor;
            float _Alpha, _Horizon, _Strength, _Swirl;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 screen : TEXCOORD1; float4 center : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screen = ComputeScreenPos(o.vertex);
                o.center = ComputeScreenPos(UnityObjectToClipPos(float4(0, 0, 0, 1)));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float edge = 1.0 - smoothstep(0.8, 1.0, r);

                float2 screenUV = i.screen.xy / i.screen.w;
                float2 centerUV = i.center.xy / i.center.w;
                float2 offset = (screenUV - centerUV) * _ScreenParams.xy;

                float h = max(_Horizon, 0.001);
                float lens = 1.0 + _Strength * pow(h / max(r, h), 2.0) * edge;
                float twist = _Swirl * pow(saturate(1.0 - r), 2.0) * (1.0 + 0.3 * sin(_Time.y * 6.0));
                float s = sin(twist), c = cos(twist);
                offset = float2(offset.x * c - offset.y * s, offset.x * s + offset.y * c) * lens;

                float3 color = tex2D(_CameraSortingLayerTexture, centerUV + offset / _ScreenParams.xy).rgb;

                float hole = 1.0 - smoothstep(h * 0.92, h * 1.05, r);
                color = lerp(color, float3(0, 0, 0), hole);
                float rim = exp(-pow((r - h * 1.12) / (h * 0.22 + 0.01), 2.0));
                color += _RimColor.rgb * rim * (1.0 - hole);

                return float4(color, edge * _Alpha);
            }
            ENDCG
        }
    }
}
