Shader "SquareTowerDefense/AxeCleave"
{
    Properties
    {
        _MainTex ("Slash Brush", 2D) = "white" {}
        _Color ("Ember Edge", Color) = (1, 0.42, 0.12, 1)
        _Color2 ("Hot Edge", Color) = (1.35, 1.12, 0.78, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Shape ("Shape (0 Slash, 1 Split, 2 Flash)", Float) = 0
        _Progress ("Draw", Range(0, 1)) = 0
        _Core ("Split Opening", Range(0, 1)) = 0
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color, _Color2;
            float _Alpha, _Shape, _Progress, _Core;
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
                float2 p = i.uv * 2.0 - 1.0;
                float u = (p.x - p.y) * 0.25 + 0.5;
                float d = (p.x + p.y) * 0.707107 - sin(u * UNITY_PI) * 0.10;
                float taper = pow(saturate(sin(u * UNITY_PI)), 0.6);
                float square = 1.0 - smoothstep(0.90, 0.99, max(abs(p.x), abs(p.y)));
                float draw = 1.0 - smoothstep(_Progress - 0.025, _Progress + 0.015, u);
                draw *= step(0.001, _Progress);
                float3 color;
                float alpha;
                if (_Shape < 0.5)
                {
                    float width = 0.11 * taper + 0.004;
                    float2 brushUV = float2(u, 0.5 + d / (width * 7.0));
                    float4 brush = tex2D(_MainTex, saturate(brushUV));
                    float ink = brush.a * step(0.0, brushUV.y) * step(brushUV.y, 1.0);
                    float core = exp(-pow(d / (width * 0.28), 2.0));
                    float fringe = exp(-pow(d / (width * 1.3), 2.0)) * 0.48;
                    float head = exp(-pow((u - _Progress) / 0.045, 2.0) - pow(d / 0.16, 2.0));
                    alpha = saturate(max(ink, core) + fringe + head) * draw;
                    color = lerp(_Color.rgb, _Color2.rgb, saturate(core + ink * brush.r + head));
                }
                else if (_Shape < 1.5)
                {
                    float width = (0.025 * _Core + 0.001) * taper;
                    float slit = 1.0 - smoothstep(width, width + 0.012, abs(d));
                    float rim = exp(-pow((abs(d) - width) / 0.012, 2.0));
                    color = lerp(float3(0.025, 0.013, 0.035), _Color.rgb, rim * 0.8);
                    alpha = max(slit, rim * 0.65) * draw;
                }
                else
                {
                    float streak = exp(-pow(d / 0.075, 2.0)) * taper;
                    float burst = exp(-dot(p, p) * 20.0) * 0.6;
                    color = _Color2.rgb;
                    alpha = saturate(streak + burst);
                }
                alpha *= square * _Alpha * _Color.a;
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
