Shader "SquareTD/BoardOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Cells ("Cells", Float) = 3
        _Warning ("Placement warning", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Cells, _Warning;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 local : TEXCOORD0; float2 world : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.local = v.uv * 2 - 1;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = v.color;
                return o;
            }
            float box(float2 p, float2 size, float radius)
            {
                float2 q = abs(p) - size + radius;
                return length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
            }
            float stroke(float d, float width, float aa) { return 1 - smoothstep(width, width + aa, abs(d)); }
            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.local;
                float aa = max(fwidth(p.x), fwidth(p.y));
                float pulse = .5 + .5 * sin(_Time.y * 2.2);
                if (_Warning > .5)
                {
                    float d = box(p, float2(.68, .68), .1);
                    float rim = stroke(d, .018, aa);
                    float corners = smoothstep(.38, .56, min(abs(p.x), abs(p.y)));
                    float glow = exp(-abs(d) * 24) * .18;
                    float disc = 1 - smoothstep(.145, .145 + aa, length(p - float2(.55, .68)));
                    float bar = 1 - smoothstep(0, aa, box(p - float2(.55, .72), float2(.018, .048), .01));
                    float dot = 1 - smoothstep(.022, .022 + aa, length(p - float2(.55, .623)));
                    float amount = max((rim * (.5 + .5 * corners) + glow) * (.7 + .3 * pulse), disc);
                    float3 color = lerp(i.color.rgb, float3(1, .94, .88), saturate(bar + dot));
                    return float4(color, saturate(amount) * i.color.a);
                }
                float cells = max(1, _Cells);
                float d = box(p, float2(.99, .99), .018);
                float inside = 1 - smoothstep(-aa, aa, d);
                float rim = stroke(d, max(.002, .009 / cells), aa);
                float2 gridDistance = abs(frac((p + 1) * .5 * cells + .5) - .5) * 2 / cells;
                float grid = 1 - smoothstep(.0025 / cells, .0025 / cells + aa, min(gridDistance.x, gridDistance.y));
                float corner = smoothstep(.69, .8, min(abs(p.x), abs(p.y)));
                float accents = rim * corner;
                float fill = (.07 + grid * .13) * inside;
                float sweep = pow(.5 + .5 * cos(atan2(p.y, p.x) * 2 - _Time.y * .75), 8);
                float amount = fill + rim * (.8 + sweep * .17) + accents * .3;
                return float4(lerp(i.color.rgb, float3(.86, 1, 1), accents), saturate(amount) * i.color.a);
            }
            ENDCG
        }
    }
}
