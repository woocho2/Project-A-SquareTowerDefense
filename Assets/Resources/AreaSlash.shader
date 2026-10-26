Shader "SquareTowerDefense/ScreenSlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Live Scene", 2D) = "white" {}
        _Elapsed ("Elapsed Seconds", Float) = 0
        _Timings ("Per Line / Wait / Slide Seconds", Vector) = (0.18, 1, 1.35, 0)
        _SlideDistance ("Horizontal Slide (Screen Width)", Range(0, 0.1)) = 0.025
        _CutDirection ("Cut Direction", Vector) = (0.94, -0.342, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define CUT_COUNT 7

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Elapsed;
            float4 _Timings;
            float _SlideDistance;
            float4 _CutDirection;
            float _DrawStarts[CUT_COUNT];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 Scene(float2 uv)
            {
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0) uv.y = 1.0 - uv.y;
                #endif
                return tex2D(_MainTex, uv).rgb;
            }

            float Glow(float distance, float width)
            {
                float d = distance / max(width, 0.0001);
                return 1.0 / (1.0 + d * d * 4.0);
            }

            float3 Hash3(float2 cell)
            {
                float3 p = frac(float3(cell.x, cell.y, cell.x) * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            // Moving cells produce irregular luminous fragments without spawning particle objects.
            void GoldFragments(inout float3 color, float2 cutPosition, float t, float layer)
            {
                float life = smoothstep(0.025, 0.10, t)
                    * (1.0 - smoothstep(0.20 + layer * 0.06, 0.64 + layer * 0.08, t));
                float spread = 1.0 - pow(1.0 - saturate(t / 0.38), 3.0);
                float band = 1.0 - smoothstep(0.12 + spread * 0.22, 0.18 + spread * 0.55, abs(cutPosition.y));
                float side = cutPosition.y >= 0.0 ? 1.0 : -1.0;
                float density = 8.0 + layer * 9.0;
                float2 drift = float2(-0.3, side * (0.4 + layer * 0.18)) * t;
                float2 field = (cutPosition - drift) * density + layer * 17.3;
                float3 seed = Hash3(floor(field));
                float2 local = frac(field) - (0.35 + seed.xy * 0.3);
                float sine, cosine;
                sincos(seed.z * 6.283185, sine, cosine);
                local = float2(cosine * local.x + sine * local.y, -sine * local.x + cosine * local.y);
                float size = lerp(0.7, 1.0, seed.x) * (1.0 - smoothstep(0.12, 0.85, t) * 0.75);
                float2 chip = local / (float2(0.28, 0.115) * max(size, 0.01));
                float shape = length(chip) + 0.18 * sin(chip.x * 6.0 + seed.z * 7.0) * sin(chip.y * 5.0);
                float coverage = 1.0 - smoothstep(0.78, 1.05, shape);
                float opacity = life * band * step(0.27, seed.z);
                float3 gold = lerp(float3(1.0, 0.36, 0.015), float3(1.0, 0.86, 0.10), seed.y);
                float whiteCore = (1.0 - smoothstep(0.02, 0.22, abs(cutPosition.y))) * step(0.35, seed.x);
                float3 tint = lerp(gold, float3(1.0, 0.98, 0.78), whiteCore);
                color += tint * exp2(-dot(chip, chip) * 1.8) * opacity * 0.35;
                color = lerp(color, tint, coverage * opacity);
            }

            float4 frag(v2f i) : SV_Target
            {
                float drawDuration = max(_Timings.x, 0.001);
                float splitStart = drawDuration * CUT_COUNT + _Timings.y;
                float splitDuration = max(_Timings.z, 0.1);
                float t = saturate((_Elapsed - splitStart) / splitDuration);
                if (_Elapsed <= 0.0 || _Elapsed >= splitStart + splitDuration)
                    return float4(Scene(i.uv), 1.0);

                // Coordinates are in screen-height units so wide displays keep the same cut angle.
                float2 aspect = float2(_MainTex_TexelSize.z / _MainTex_TexelSize.w, 1.0);
                float2 p = (i.uv - 0.5) * aspect;
                float2 direction = normalize(_CutDirection.xy);
                float2 normal = float2(-direction.y, direction.x);
                float across = dot(p, normal);
                float along = dot(p, direction);
                float aa = max(fwidth(across), 0.0001);
                // Wait one full second after all seven yellow lines have been drawn.
                float slide = smoothstep(0.0, 0.065, t) * (1.0 - smoothstep(0.48, 1.0, t));
                float3 gold = float3(1.0, 0.82, 0.06);
                float3 hot = float3(1.0, 0.99, 0.86);
                float3 amber = float3(1.0, 0.35, 0.025);
                float charging = smoothstep(splitStart - 0.14, splitStart, _Elapsed)
                    * (1.0 - smoothstep(0.0, 0.05, t));

                float normalExtent = dot(abs(normal), aspect) * 0.5;
                float2 safeDirection = float2(
                    abs(direction.x) < 0.0001 ? 0.0001 : direction.x,
                    abs(direction.y) < 0.0001 ? 0.0001 : direction.y);
                float nearestAcross = 1000.0;
                float band = 0.0;
                float3 lineLight = 0.0;
                float lineCore = 0.0;

                [unroll]
                for (int cut = 0; cut < CUT_COUNT; cut++)
                {
                    float offset = (cut - 3.0) * normalExtent * 0.22;
                    float localAcross = across - offset;
                    band += step(0.0, localAcross);
                    if (abs(localAcross) < abs(nearestAcross)) nearestAcross = localAcross;

                    // Clip each offset line to the screen so its drawing tip starts at an edge.
                    float2 lineOrigin = normal * offset;
                    float2 endsA = (-aspect * 0.5 - lineOrigin) / safeDirection;
                    float2 endsB = (aspect * 0.5 - lineOrigin) / safeDirection;
                    float2 entry = min(endsA, endsB);
                    float2 exit = max(endsA, endsB);
                    float first = max(entry.x, entry.y);
                    float last = min(exit.x, exit.y);
                    float lineElapsed = _Elapsed - _DrawStarts[cut];
                    float draw = saturate(lineElapsed / drawDuration);
                    float tip = lerp(first - 0.06, last + 0.06, draw);
                    float trail = 1.0 - smoothstep(tip - 0.06, tip, along);
                    float blade = saturate(lineElapsed / 0.02)
                        * (1.0 - smoothstep(0.0, 0.08, t)) * trail;
                    lineLight += gold * Glow(localAcross, 0.008) * blade * 0.35;
                    lineLight += hot * Glow(length(float2(along - tip, localAcross)), 0.018)
                        * blade * (1.0 - draw);
                    lineLight += hot * Glow(localAcross, 0.0025) * charging * 0.5;
                    lineCore = max(lineCore,
                        (1.0 - smoothstep(0.0015, 0.0015 + aa, abs(localAcross))) * blade);
                }

                // Eight adjacent pieces alternate left/right; the displacement never accumulates.
                float side = fmod(band, 2.0) * 2.0 - 1.0;
                float2 sourceUV = i.uv + float2(side * _SlideDistance * slide, 0.0);
                float3 color = Scene(saturate(sourceUV)) + lineLight;
                color = lerp(color, gold, lineCore);

                // A narrow afterglow marks the seam while the two images slide back into place.
                color += amber * Glow(nearestAcross, 0.006) * slide * 0.35;
                color += gold * Glow(nearestAcross, 0.0015) * slide * 0.45;

                if (t > 0.0 && t < 0.8)
                {
                    [unroll]
                    for (int layer = 0; layer < 3; layer++)
                        GoldFragments(color, float2(along, across), t, (float)layer);
                }

                // A brief full-screen white impact hides the snap, then reveals the flying fragments.
                float flash = smoothstep(0.0, 0.018, t) * (1.0 - smoothstep(0.05, 0.14, t));
                color = lerp(color, float3(1.0, 1.0, 0.98), flash);
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}
