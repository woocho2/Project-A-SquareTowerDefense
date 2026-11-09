Shader "SquareTowerDefense/AxeWhirl"
{
    Properties
    {
        _MainTex ("Slash Brush", 2D) = "white" {}
        _Color ("Outer Blade", Color) = (1, 0.28, 0.065, 1)
        _Color2 ("Hot Edge", Color) = (1.35, 1, 0.55, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Shape ("Shape (0 Sweep, 1 Range, 2 Bonus Tile)", Float) = 0
        _Progress ("Rotation / Edge Pulse", Range(0, 1)) = 0
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
            float _Alpha, _Shape, _Progress;
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
                float radius = length(p);
                float box = max(abs(p.x), abs(p.y));
                float3 color;
                float alpha;
                if (_Shape < 0.5)
                {
                    float angle = frac(atan2(p.y, p.x) / (UNITY_PI * 2.0) + 1.0);
                    float behind = frac(_Progress - angle + 1.0);
                    float swept = step(angle, _Progress);
                    float tip = smoothstep(0.0, 0.035, behind);
                    float tail = pow(saturate(1.0 - behind / 0.42), 0.7) * swept * tip;
                    float head = exp(-pow((behind - 0.035) / 0.025, 2.0)) * swept * tip;
                    float arcRadius = 0.83 - behind * 0.12;
                    float d = radius - arcRadius;
                    float width = 0.006 + tail * 0.10;
                    float4 brush = tex2D(_MainTex, float2(saturate(1.0 - behind / 0.42), saturate(0.5 + d / 0.9)));
                    float blade = exp(-pow(d / width, 2.0)) * tail;
                    float core = exp(-pow((d - 0.015) / 0.024, 2.0)) * tail;
                    float hotTip = exp(-pow(d / 0.06, 2.0)) * head;
                    float secondary = exp(-pow((radius - 0.62) / 0.017, 2.0)) * tail * 0.42;
                    alpha = saturate(blade * (0.7 + brush.a * 0.3) + core + hotTip + secondary);
                    color = lerp(_Color.rgb, _Color2.rgb, saturate(core + hotTip));
                }
                else if (_Shape < 1.5)
                {
                    float boundary = 1.0 - smoothstep(0.008, 0.022, abs(box - 0.965));
                    float fill = (1.0 - smoothstep(0.90, 0.965, box)) * 0.055;
                    alpha = boundary * 0.6 + fill;
                    color = _Color.rgb;
                }
                else
                {
                    float inside = 1.0 - smoothstep(0.85, 0.94, box);
                    float rim = 1.0 - smoothstep(0.014, 0.045, abs(box - 0.86));
                    float innerRim = 1.0 - smoothstep(0.01, 0.025, abs(box - 0.72));
                    alpha = inside * (0.14 + _Progress * 0.14) + rim * (0.7 + _Progress * 0.3) + innerRim * _Progress * 0.2;
                    color = lerp(_Color.rgb, _Color2.rgb, saturate(rim * 0.6 + _Progress * 0.7));
                }
                alpha = saturate(alpha) * _Alpha * _Color.a;
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
