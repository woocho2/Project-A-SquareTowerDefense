Shader "SquareTowerDefense/ScreenSlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Live Scene", 2D) = "white" {}
        _Elapsed ("Elapsed Seconds", Float) = 0
        _LineInterval ("Line Spawn Interval", Float) = 0.2
        _DrawDuration ("Line Draw Duration", Float) = 0.1
        _ViewportSize ("Viewport Size (Pixels)", Vector) = (1920, 1080, 0, 0)
        _LineOpacity ("Line Opacity", Range(0,1)) = 1
        _Movement ("Piece Movement", Float) = 0
        _Flash ("Impact Flash", Range(0,1)) = 0
        _CoverOpacity ("Full Screen Light Opacity", Range(0,1)) = 0
        _CoverSpread ("Light Spread From Cuts", Range(0,1)) = 0
        [HideInInspector] _CutViewport ("Cut Viewport Center / Pixel Scale", Vector) = (0.5,0.5,1,1)
        [HideInInspector] _CoverOnly ("Light Cover Pass", Float) = 0
        _Charge ("Charge", Range(0,1)) = 0
        _Impact ("Impact", Range(0,1)) = 0
        _VfxStrength ("Flair Strength", Range(0,2)) = 1
        _ScreenOffsetPixels ("Impact Offset (Pixels)", Vector) = (0,0,0,0)
        _SceneReady ("Live Scene Ready", Float) = 0
        _MotionTimes ("Move Start / Duration Seconds", Vector) = (2, 0.9, 0, 0)
        _MovePixels ("Horizontal / Vertical Move Pixels", Vector) = (18, 12, 0, 0)
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

            #define CUT_COUNT 4 // Keep in sync with SwordSplashSkill.LineCount.
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Elapsed;
            float _LineInterval;
            float _DrawDuration;
            float4 _ViewportSize;
            float _LineOpacity, _Movement, _Flash, _Charge, _Impact, _VfxStrength;
            float _CoverOpacity, _CoverSpread, _CoverOnly;
            float4 _CutViewport;
            float4 _ScreenOffsetPixels;
            float _SceneReady;
            float4 _MotionTimes;
            float4 _MovePixels;
            // xy: centre relative to the viewport centre, in pixels. zw: unit direction.
            float4 _Cuts[CUT_COUNT];
            float _CutProgress[CUT_COUNT];
            float _CutEnergy[CUT_COUNT];

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

            void Over(inout float4 result, float3 color, float opacity)
            {
                opacity = saturate(opacity);
                result.rgb = color * opacity + result.rgb * (1.0 - opacity);
                result.a = opacity + result.a * (1.0 - opacity);
            }

            float Hash(float seed)
            {
                return frac(sin(seed * 127.1 + 311.7) * 43758.5453);
            }

            void DrawSparks(inout float4 result, float2 local, float first, float last,
                float seed, float age, float strength)
            {
                float life = smoothstep(0.0, 0.018, age) * pow(saturate(1.0 - age / 0.42), 2.0);
                [unroll]
                for (int shard = 0; shard < 6; shard++)
                {
                    float key = seed * 19.0 + shard * 3.7;
                    float a = Hash(key), b = Hash(key + 11.0), c = Hash(key + 23.0);
                    float2 velocity = float2((b - 0.5) * 130.0, (shard % 2 == 0 ? 1.0 : -1.0) * (55.0 + c * 145.0));
                    float2 position = float2(lerp(first, last, a), 0.0) + velocity * age;
                    float2 delta = local - position;
                    [branch]
                    if (dot(delta, delta) < 576.0)
                    {
                        float2 direction = normalize(velocity);
                        float longitudinal = dot(delta, direction);
                        float transverse = dot(delta, float2(-direction.y, direction.x));
                        float shape = abs(longitudinal) / (3.0 + a * 5.0) + abs(transverse) / (0.9 + b * 1.3);
                        float chip = 1.0 - smoothstep(0.65, 1.15, shape);
                        float halo = exp2(-dot(delta, delta) / 45.0);
                        float3 tint = lerp(float3(1.0,0.48,0.025), float3(1.0,0.96,0.70), c);
                        Over(result, tint, halo * life * strength * 0.26);
                        Over(result, tint, chip * life * strength);
                    }
                }
            }

            float3 Scene(float2 uv)
            {
                uv = saturate(uv);
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0) uv.y = 1.0 - uv.y;
                #endif
                return tex2D(_MainTex, uv).rgb;
            }

            float4 frag(v2f i) : SV_Target
            {
                [branch]
                if (_CoverOnly > 0.5)
                {
                    if (_CoverOpacity <= 0.0 || _CoverSpread <= 0.0) return 0.0;
                    float2 screenSize = _ViewportSize.xy * _CutViewport.zw;
                    float2 cutPoint = (i.uv - _CutViewport.xy) * screenSize;
                    float2 screenCenter = (0.5 - _CutViewport.xy) * screenSize;
                    float nearestCut = 1e20;
                    float coverReach = 1e20;
                    [unroll]
                    for (int cut = 0; cut < CUT_COUNT; cut++)
                    {
                        float2 normal = float2(-_Cuts[cut].w, _Cuts[cut].z);
                        nearestCut = min(nearestCut, abs(dot(cutPoint - _Cuts[cut].xy, normal)));
                        // This bounds every screen corner, so spread=1 always hides the entire scene.
                        float farthest = abs(dot(screenCenter - _Cuts[cut].xy, normal))
                            + dot(abs(normal), screenSize * 0.5);
                        coverReach = min(coverReach, farthest);
                    }
                    float spread = saturate(_CoverSpread);
                    float feather = lerp(2.0, 32.0, spread);
                    float width = (coverReach + feather) * spread * spread;
                    float filled = 1.0 - smoothstep(width, width + feather, nearestCut);
                    float haloWidth = width + feather * 2.0;
                    float halo = exp2(-nearestCut * nearestCut / (haloWidth * haloWidth));
                    float core = saturate(1.0 - nearestCut / max(width, 1.0));
                    float shimmer = sin(nearestCut * 0.035 - _Elapsed * 14.0) * 0.05 * _VfxStrength;
                    float3 light = lerp(float3(1.0, 0.80, 0.24), float3(1.0, 1.0, 0.97),
                        saturate(0.35 + core * 0.65 + shimmer));
                    float opacity = max(filled, halo * 0.3) * saturate(_CoverOpacity);
                    return float4(light * opacity, opacity);
                }
                float fade = saturate(_LineOpacity);
                float2 halfSize = _ViewportSize.xy * 0.5;
                float2 p = (i.uv - 0.5) * _ViewportSize.xy;
                float4 result = 0.0;
                float3 gold = float3(1.0, 0.82, 0.06);
                float3 hot = float3(1.0, 0.98, 0.65);
                float region = 0.0;
                float impactAge = _Elapsed - _MotionTimes.x;

                [unroll]
                for (int cut = 0; cut < CUT_COUNT; cut++)
                {
                    float2 center = _Cuts[cut].xy;
                    float2 direction = _Cuts[cut].zw;
                    float2 relative = p - center;
                    float along = dot(relative, direction);
                    float across = dot(relative, float2(-direction.y, direction.x));
                    // The half-plane tests give every cut-out region a stable identity.
                    region += step(0.0, across) * exp2((float)cut);
                    float aa = max(fwidth(across), 0.7);

                    // Directions stay at least 15 degrees away from the horizontal and vertical.
                    float2 endsA = (-halfSize - center) / direction;
                    float2 endsB = (halfSize - center) / direction;
                    float2 entry = min(endsA, endsB);
                    float2 exit = max(endsA, endsB);
                    float first = max(entry.x, entry.y);
                    float last = min(exit.x, exit.y);
                    float age = _Elapsed - cut * _LineInterval;
                    float draw = saturate(_CutProgress[cut]);
                    float energy = saturate(_CutEnergy[cut]) * _VfxStrength;
                    float charge = saturate(_Charge) * _VfxStrength;
                    float tip = lerp(first - 8.0, last + 8.0, draw);
                    float trail = 1.0 - smoothstep(tip - 8.0, tip, along);
                    float visible = saturate(draw * 20.0) * fade;
                    float width = 1.0 + energy * 0.7 + charge * 0.35;
                    float stroke = (1.0 - smoothstep(width, width + aa, abs(across))) * trail;
                    float glow = exp2(-across * across / (24.0 * (1.0 + energy * 2.0 + charge))) * trail;
                    Over(result, gold, glow * visible * (0.28 + energy * 0.2 + charge * 0.2));
                    Over(result, hot, stroke * visible * 0.95);
                    float whiteCore = 1.0 - smoothstep(0.35, 0.35 + aa, abs(across));
                    Over(result, float3(1.0,1.0,0.94), whiteCore * trail * visible * saturate(energy + charge) * 0.8);
                    float trailDistance = across + 4.0 + max(age, 0.0) * 18.0;
                    float afterimage = exp2(-trailDistance * trailDistance / 9.0);
                    Over(result, gold, afterimage * trail * visible * energy * 0.24);
                    float2 tipDistance = float2(along - tip, across);
                    float spark = exp2(-dot(tipDistance, tipDistance) / 60.0);
                    Over(result, hot, spark * visible * (1.0 - draw));
                    float star = exp2(-abs(tipDistance.x) / 18.0 - abs(tipDistance.y) / 1.2)
                        + exp2(-abs(tipDistance.x) / 1.2 - abs(tipDistance.y) / 13.0);
                    Over(result, hot, star * visible * energy * (1.0 - draw) * 0.7);
                    [branch]
                    if (age > 0.0 && age < 0.42 && _VfxStrength > 0.0)
                        DrawSparks(result, float2(along, across), first, last, cut + 1.0, age, _VfxStrength * 0.7 * fade);
                    [branch]
                    if (impactAge > 0.0 && impactAge < 0.42 && _VfxStrength > 0.0)
                        DrawSparks(result, float2(along, across), first, last, cut + 41.0, impactAge, _VfxStrength * fade);
                }
                if (impactAge >= 0.0 && _VfxStrength > 0.0)
                {
                    float wave = saturate(impactAge / max(_MotionTimes.y * 0.42, 0.05));
                    float radius = min(_ViewportSize.x, _ViewportSize.y) * lerp(0.06, 0.72, 1.0 - pow(1.0 - wave, 2.0));
                    float ringDistance = length(p) - radius;
                    float ring = exp2(-ringDistance * ringDistance / 18.0);
                    Over(result, gold, ring * (1.0 - wave) * (1.0 - wave) * _VfxStrength * 0.18);
                }
                Over(result, float3(1.0,1.0,0.95), _Flash);
                if (_SceneReady < 0.5) return result;

                float2 seed = frac(sin(float2(region * 127.1 + 19.3, region * 311.7 + 71.9)) * 43758.5453);
                float2 direction = step(0.5, seed) * 2.0 - 1.0;
                float2 distance = lerp(0.65, 1.0, frac(seed * 7.13)) * _MovePixels.xy;
                float zoom = 1.0 + _Impact * _VfxStrength * 0.012;
                float2 sceneUV = (i.uv - 0.5) / zoom + 0.5
                    + (_ScreenOffsetPixels.xy - direction * distance * _Movement) / _ViewportSize.xy;
                float3 scene = Scene(sceneUV);
                [branch]
                if (_Impact * _VfxStrength > 0.001)
                {
                    float2 fringe = direction * (_Impact * _VfxStrength * 1.4) / _ViewportSize.xy;
                    scene.r = Scene(sceneUV + fringe).r;
                    scene.b = Scene(sceneUV - fringe).b;
                }
                return float4(result.rgb + scene * (1.0 - result.a), 1.0);
            }
            ENDCG
        }
    }
}
