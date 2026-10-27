Shader "SquareTowerDefense/BoardGridGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Board Silhouette", 2D) = "white" {}
        [HDR] _GridColor ("Neon Colour", Color) = (0.02,0.85,1,1)
        _CellSize ("Grid Spacing", Vector) = (1.5,1.5,0,0)
        _GridOrigin ("Grid Origin", Vector) = (0,0,0,0)
        _Intensity ("Emission", Range(0,5)) = 1.8
        _CoreWidth ("Core Width (Screen Pixels)", Range(0.3,3)) = 0.85
        _HaloWidth ("Halo Width (Screen Pixels)", Range(1,12)) = 3
        _PulseAmount ("Pulse Amount", Range(0,0.5)) = 0.1
        _PulseSpeed ("Pulse Speed", Range(0,3)) = 0.7
        _FlowSpeed ("Pattern Cycles Per Second", Range(0,2)) = 0.25
        _WaveSpacing ("Pattern Wave Spacing", Range(0.15,2)) = 0.8
        _MinimumBrightness ("Dim Brightness", Range(0,1)) = 0.1
        [HideInInspector] _BoardRect ("Board Bounds", Vector) = (-9,-6,18,12)
        [HideInInspector] _PatternA ("Previous Pattern", Float) = 0
        [HideInInspector] _PatternB ("Current Pattern", Float) = 0
        [HideInInspector] _PatternBlend ("Pattern Blend", Range(0,1)) = 1
        [HideInInspector] _AnimationTime ("Animation Time", Float) = 0
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _GridColor, _CellSize, _GridOrigin, _BoardRect;
            float _Intensity, _CoreWidth, _HaloWidth, _PulseAmount, _PulseSpeed, _FlowSpeed;
            float _WaveSpacing, _MinimumBrightness, _PatternA, _PatternB, _PatternBlend, _AnimationTime;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; float alpha : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
                o.alpha = v.color.a;
                return o;
            }

            float BrightnessPattern(float pattern, float2 world)
            {
                float2 size = max(_BoardRect.zw, float2(0.001, 0.001));
                float2 position = (world - _BoardRect.xy) / size;
                float2 centered = world - (_BoardRect.xy + size * 0.5);
                float radius = length(centered) / max(length(size) * 0.5, 0.001);
                float coordinate;
                // 0..7: right, left, down, up, down-right, up-left, up-right, down-left.
                if (pattern < 0.5) coordinate = position.x;
                else if (pattern < 1.5) coordinate = 1.0 - position.x;
                else if (pattern < 2.5) coordinate = 1.0 - position.y;
                else if (pattern < 3.5) coordinate = position.y;
                else if (pattern < 4.5) coordinate = (position.x + 1.0 - position.y) * 0.5;
                else if (pattern < 5.5) coordinate = (1.0 - position.x + position.y) * 0.5;
                else if (pattern < 6.5) coordinate = (position.x + position.y) * 0.5;
                else if (pattern < 7.5) coordinate = (2.0 - position.x - position.y) * 0.5;
                else if (pattern < 8.5) coordinate = radius;
                else if (pattern < 9.5) coordinate = 1.0 - radius;
                else
                {
                    // Interfering curved waves resemble ripples moving across water.
                    float2 p = centered / max(max(size.x, size.y), 0.001) * 2.0;
                    float time = _AnimationTime * _FlowSpeed;
                    float warped = p.x + sin(p.y * 6.0 - time * 2.0) * 0.22;
                    float wave = 0.5 + 0.5 * cos((warped * 1.5 - time) * 6.283185);
                    float crossing = 0.5 + 0.5 * sin((p.y * 1.8 + p.x * 0.25 - time * 0.6) * 6.283185);
                    return saturate(wave * 0.75 + crossing * 0.25);
                }
                float phase = coordinate / max(_WaveSpacing, 0.15) - _AnimationTime * _FlowSpeed;
                return pow(saturate(0.5 + 0.5 * cos(phase * 6.283185)), 2.5);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 texel = abs(_MainTex_TexelSize.xy) * 12.0;
                float mask = tex2D(_MainTex, i.uv).a * i.alpha;
                mask *= min(min(tex2D(_MainTex, i.uv + float2(texel.x, 0)).a,
                    tex2D(_MainTex, i.uv - float2(texel.x, 0)).a),
                    min(tex2D(_MainTex, i.uv + float2(0, texel.y)).a,
                    tex2D(_MainTex, i.uv - float2(0, texel.y)).a));
                float2 grid = (i.world - _GridOrigin.xy) / max(_CellSize.xy, float2(0.001, 0.001));
                float2 pixels = abs(frac(grid + 0.5) - 0.5) / max(fwidth(grid), float2(0.00001, 0.00001));
                float distance = min(pixels.x, pixels.y);
                float core = 1.0 - smoothstep(_CoreWidth * 0.25, _CoreWidth, distance);
                float halo = exp2(-distance * distance / max(_HaloWidth * _HaloWidth, 0.001));
                float previous = BrightnessPattern(_PatternA, i.world);
                float current = BrightnessPattern(_PatternB, i.world);
                float brightness = lerp(_MinimumBrightness, 1.0, lerp(previous, current, _PatternBlend));
                float pulse = 1.0 + sin(_AnimationTime * _PulseSpeed * 6.283185) * _PulseAmount;
                float alpha = saturate(core * 0.85 + halo * 0.12) * mask * brightness;
                float3 emission = _GridColor.rgb * _Intensity * pulse * brightness;
                return float4(emission * alpha, alpha);
            }
            ENDCG
        }
    }
}
