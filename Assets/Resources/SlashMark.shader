// 베기 선 하나를 그림 파일 없이 공식으로 그림 (검성의 X자 베기, 검객의 발도 · 빛줄기)
//   네모 판의 왼쪽(uv.x = 0)에서 오른쪽(1)으로 그어짐. 판을 돌리고 늘려서 방향·길이·두께를 정함
//   _Progress 0 → 1: 그어지는 정도 (머리 부분이 더 밝음) / _Fade 1 → 0: 사라지기
//   _Whiten 1 = 가운데가 하얗게 빛나는 베기 / 0 = 빛 없이 _Color 그대로 (어두운 색이면 땅이 갈라진 금)
Shader "SquareTowerDefense/SlashMark"
{
    Properties
    {
        _Color ("Glow Color", Color) = (1, 0.9, 0.6, 1)
        _Progress ("Draw Progress", Range(0, 1)) = 1
        _Fade ("Fade", Range(0, 1)) = 1
        _Core ("Core Width", Range(0.02, 0.6)) = 0.22
        _Whiten ("Whiten Core", Range(0, 1)) = 1
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Progress, _Fade, _Core, _Whiten;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float x = i.uv.x;                          // 0 = 시작, 1 = 끝
                float y = abs(i.uv.y * 2.0 - 1.0);         // 0 = 선의 한가운데, 1 = 판의 위·아래 끝

                float drawn = 1.0 - smoothstep(_Progress - 0.03, _Progress + 0.001, x);   // 그어진 만큼만
                float taper = sin(saturate(x) * 3.14159265);                              // 가운데 두껍고 양 끝 뾰족
                float width = _Core * taper;
                float core = 1.0 - smoothstep(width * 0.45, width + 0.001, y);
                float glow = exp(-y * y / max(width * width * 5.0, 0.0001)) * 0.55 * taper;
                float drawing = 1.0 - step(0.999, _Progress);                             // 다 그어지면 머리 빛은 꺼짐
                float headOffset = (x - _Progress) * 16.0;
                float head = exp(-headOffset * headOffset) * drawing * taper;

                float alpha = saturate(core + glow + head * 0.6) * drawn * _Fade;
                float3 color = lerp(_Color.rgb, float3(1.0, 1.0, 1.0), saturate(core * 0.85 + head) * _Whiten);
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
