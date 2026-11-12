Shader "SquareTowerDefense/AxeWhirl"
{
    Properties
    {
        _MainTex ("Slash Brush", 2D) = "white" {}
        [HDR] _Color ("Metal / Outer Wake", Color) = (.68, .76, .84, 1)
        [HDR] _Color2 ("Blade Edge", Color) = (1, .97, .89, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Shape ("0 Sweep, 1 Range, 2 Bonus Tile, 3 Great Axe", Float) = 0
        _Progress ("Sweep Progress", Range(0, 1)) = 0
        _TrailSpan ("Wake Length (Turn Fraction)", Range(.02, .5)) = .28
        _Width ("Wake Width", Range(.005, .15)) = .065
        _Intensity ("Intensity", Float) = 1
        _PlaneFlatten ("Projected Swing Plane", Range(.4,1)) = 1
        _PivotY ("Swing Origin Y", Float) = 0
        _Hemisphere ("Rear +1 / Front -1 / Whole 0", Float) = 0
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color, _Color2;
            float _Alpha, _Shape, _Progress, _TrailSpan, _Width, _Intensity, _PlaneFlatten, _PivotY, _Hemisphere;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float worldY : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                world.y = _PivotY + (world.y - _PivotY) * _PlaneFlatten;
                o.vertex = UnityWorldToClipPos(world.xyz);
                o.worldY = world.y;
                o.uv = v.uv;
                return o;
            }
            float coverage(float d)
            {
                float aa = max(fwidth(d), .0015);
                return 1.0 - smoothstep(-aa, aa, d);
            }
            float rectangle(float2 p, float2 size)
            {
                float2 q = abs(p) - size;
                return length(max(q, 0)) + min(max(q.x, q.y), 0);
            }
            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float radius = length(p);
                float box = max(abs(p.x), abs(p.y));
                float3 color;
                float alpha;
                if (_Shape < .5)
                {
                    // The newest edge follows the physical blade. Nothing is drawn ahead of it.
                    // Wrapped angular distance keeps the wake continuous across the 0/360 seam.
                    float angle = frac(atan2(p.y, p.x) / (UNITY_PI * 2.0) + 1.0);
                    float behind = frac(_Progress - angle + 1.0);
                    float drawn = 1.0 - smoothstep(_Progress, _Progress + .003, behind);
                    float tail = pow(saturate(1.0 - behind / max(.02, _TrailSpan)), 1.35) * drawn;
                    float arcRadius = .83 - behind * .035;
                    float d = radius - arcRadius;
                    float width = max(.002, _Width * (.15 + .85 * tail));
                    float band = exp(-pow(d / width, 2.0)) * tail;
                    float edge = exp(-pow(d / max(.002, width * .15), 2.0)) * tail;
                    float4 brush = tex2D(_MainTex, float2(
                        saturate(1.0 - behind / max(.02, _TrailSpan)),
                        saturate(.5 + d / max(.02, width * 6.0))));
                    alpha = saturate(band * (.75 + brush.a * .25));
                    color = lerp(_Color.rgb, _Color2.rgb, edge * .45);
                }
                else if (_Shape < 1.5)
                {
                    float boundary = 1.0 - smoothstep(.008, .022, abs(box - .965));
                    float fill = (1.0 - smoothstep(.90, .965, box)) * .055;
                    alpha = boundary * .6 + fill;
                    color = _Color.rgb;
                }
                else if (_Shape < 2.5)
                {
                    float inside = 1.0 - smoothstep(.85, .94, box);
                    float rim = 1.0 - smoothstep(.014, .045, abs(box - .86));
                    float innerRim = 1.0 - smoothstep(.01, .025, abs(box - .72));
                    alpha = inside * (.14 + _Progress * .14) + rim * (.7 + _Progress * .3)
                        + innerRim * _Progress * .2;
                    color = lerp(_Color.rgb, _Color2.rgb, saturate(rim * .6 + _Progress * .7));
                }
                else
                {
                    // Crisp broad bearded axe, with a solid wooden haft and steel cutting edge.
                    // All distances use the physical 1:.60 aspect of the weapon quad.
                    float2 q = float2(p.x, p.y * .60);
                    float shaft = rectangle(q - float2(-.12, 0), float2(.81, .033));
                    float grip = rectangle(q - float2(-.58, 0), float2(.26, .047));
                    float pommel = rectangle(q - float2(-.91, 0), float2(.04, .058));
                    float outer = length(q - float2(.61, -.24)) - .75;
                    float inner = length(q - float2(.21, -.19)) - .48;
                    float bounds = max(.18 - q.x, max(q.x - .95, max(-q.y - .10, q.y - .57)));
                    float head = max(max(outer, -inner), bounds);
                    float socket = rectangle(q - float2(.72, .015), float2(.105, .10));
                    float d = min(min(shaft, grip), min(pommel, min(head, socket)));
                    float solid = coverage(d);
                    float fill = coverage(d + .008);
                    float steel = coverage(head);
                    float bevel = saturate(steel - coverage(head + .038));
                    float mount = coverage(socket);
                    float band = coverage(abs(q.x - .64) - .018) * mount;
                    float wrap = step(.48, frac((q.x + .84) * 14.0)) * coverage(grip);
                    float plane = smoothstep(-.12, .38, q.y - q.x * .23);
                    float3 woodColor = lerp(float3(.19, .095, .06), float3(.33, .21, .14), plane);
                    woodColor = lerp(woodColor, float3(.12, .075, .065), wrap * .35);
                    float3 metalColor = lerp(_Color.rgb * .58, _Color.rgb, plane);
                    metalColor = lerp(metalColor, _Color2.rgb, bevel * .88);
                    float3 mountColor = lerp(float3(.36, .22, .11), float3(.78, .57, .29), plane);
                    color = lerp(woodColor, metalColor, steel);
                    color = lerp(color, mountColor, mount * (1.0 - steel * .30));
                    color = lerp(color, _Color2.rgb * .78, band * .65);
                    color = lerp(float3(.075, .065, .07), color, fill);
                    alpha = solid;
                }
                // Complementary halves put the rear wake underneath towers without a doubled seam.
                float front = smoothstep(-.012,.012,_PivotY-i.worldY);
                float halfMask = _Hemisphere > .5 ? 1-front : (_Hemisphere < -.5 ? front : 1);
                alpha = saturate(alpha) * _Alpha * _Color.a * halfMask;
                return float4(color * alpha * _Intensity, alpha);
            }
            ENDCG
        }
    }
}

