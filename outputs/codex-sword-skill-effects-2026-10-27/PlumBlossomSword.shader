Shader "SquareTowerDefense/PlumBlossomSword"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Elapsed ("Elapsed", Float) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            float _Elapsed, _Opacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 timing : TEXCOORD1;
                float4 motion : TEXCOORD2;
                float4 kind : TEXCOORD3;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 timing : TEXCOORD1;
                float2 kind : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float age = _Elapsed - v.timing.x;
                float time = max(age, 0.0);
                float4 position = v.vertex;
                o.color = v.color;
                if (v.kind.x > 0.5 && v.kind.x < 1.5)
                {
                    float seed = v.timing.w;
                    float flutter = 0.3 + 0.7 * abs(sin(time * 5.5 + seed));
                    float angle = v.motion.z + time * v.motion.w;
                    float s, c;
                    sincos(angle, s, c);
                    float2 petal = v.uv * v.timing.z * float2(flutter, 0.8);
                    position.xy += float2(c * petal.x - s * petal.y, s * petal.x + c * petal.y);
                    position.xy += v.motion.xy * time + float2(sin(time * 3.0 + seed) - sin(seed), sin(time * 2.4 + seed) - sin(seed)) * 24.0;
                    position.y -= time * time * (v.kind.y > 0.5 ? 35.0 : 12.0);
                    o.color.a *= smoothstep(0.0, 0.12, age) * (1.0 - smoothstep(v.timing.y * 0.65, v.timing.y, age));
                }
                o.vertex = UnityObjectToClipPos(position);
                o.uv = v.uv;
                o.timing = float4(age, v.timing.y, v.timing.z, v.motion.x);
                o.kind = v.kind.xy;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 color;
                float opacity;
                if (i.kind.x > 1.5)
                {
                    float age = i.timing.x;
                    float life = saturate(age / max(i.timing.y, 0.001));
                    float active = step(0.0, age) * (1.0 - life) * (1.0 - life);
                    float2 q = i.uv;
                    float s, c;
                    sincos(life * 0.5, s, c);
                    q = float2(q.x * c - q.y * s, q.x * s + q.y * c);
                    float rays = exp2(-abs(q.x) * 4.5 - abs(q.y) * 38.0)
                        + exp2(-abs(q.y) * 5.0 - abs(q.x) * 38.0);
                    float flare = exp2(-dot(q, q) * 18.0);
                    color = lerp(float3(1.0, 0.40, 0.68), float3(1.0, 0.98, 1.0), saturate(flare + rays));
                    opacity = saturate(rays + flare * 0.7) * active * _Opacity;
                }
                else if (i.kind.x > 0.5)
                {
                    // A tapered petal with the small split characteristic of cherry blossoms.
                    float2 q = i.uv;
                    float shape = length(float2(q.x * (1.1 - q.y * 0.35), q.y * 0.85)) - 0.72;
                    float notch = 0.16 - length((q - float2(0.0, 0.82)) * float2(1.0, 1.4));
                    shape = max(shape, notch);
                    float aa = max(fwidth(shape), 0.015);
                    float petal = 1.0 - smoothstep(-aa, aa, shape);
                    float veinDistance = q.x + sin(q.y * 2.0) * 0.08;
                    float vein = exp2(-veinDistance * veinDistance * 260.0) * 0.12;
                    color = lerp(i.color.rgb * float3(1.0, 0.78, 0.88), float3(1.0, 0.97, 0.99),
                        saturate(0.45 - q.y * 0.3 + vein));
                    opacity = petal * i.color.a * _Opacity;
                }
                else
                {
                    float age = i.timing.x;
                    float draw = saturate(age / max(i.timing.y, 0.001));
                    float along = i.uv.x;
                    float tip = 1.0 - pow(1.0 - draw, 3.0);
                    float front = 1.0 - smoothstep(tip - 0.015, tip + 0.015, along);
                    // Keep the completed slash in place and fade it as a whole instead of retracting its tail.
                    float back = smoothstep(0.0, 0.10, along);
                    float headTaper = saturate((tip - along) / 0.14);
                    float tailTaper = saturate(along / 0.22);
                    float thickness = 0.02 + 0.34 * sqrt(headTaper * tailTaper);
                    float aa = max(fwidth(i.uv.y), 0.015);
                    float across = i.uv.y + thickness * 0.30;
                    float blade = 1.0 - smoothstep(thickness, thickness + aa, abs(across));
                    float edge = 1.0 - smoothstep(0.025, 0.025 + aa, abs(across - thickness * 0.55));
                    float glow = exp2(-across * across * 9.0) * 0.45;
                    float2 tipDelta = float2((along - tip) * 24.0, i.uv.y * 2.0);
                    float glint = exp2(-dot(tipDelta, tipDelta) * 4.0) * (1.0 - draw);
                    float visible = step(0.0, age) * (1.0 - smoothstep(i.timing.y, i.timing.z, age));
                    color = lerp(i.color.rgb, float3(1.0, 0.98, 1.0), saturate(edge * (1.0 - i.kind.y) + glint));
                    opacity = saturate((blade * 0.85 + glow) * front * back + glint)
                        * visible * i.color.a * _Opacity;
                }
                return float4(color * opacity, opacity);
            }
            ENDCG
        }
    }
}
