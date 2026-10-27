Shader "SquareTowerDefense/BoardFrameWhiteNeon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Board Plate", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CenterLuminance ("Surface Gray (0-255)", Range(0,255)) = 255
        _RimMiddleLuminance ("Rim Middle Gray (0-255)", Range(0,255)) = 220
        _EdgeLuminance ("Rim Ends Gray (0-255)", Range(0,255)) = 180
        [Enum(CornersOnly,0,OuterRing,1)] _EdgeTileMode ("Shaded Tiles", Float) = 1
        [HideInInspector] _PlateUvBounds ("Visible Plate Bounds", Vector) = (0.196969697,0.195374801,0.803030303,0.784688995)
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
            float4 _Color, _PlateUvBounds;
            float _CenterLuminance, _RimMiddleLuminance, _EdgeLuminance, _EdgeTileMode;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; float4 color : COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.uv).a * i.color.a;
                // TL/TR/BL/BR are the four corner cells of the 3x3 board master.
                float2 cell = floor(min(saturate(i.uv), 0.999999) * 3.0);
                float2 outerAxes = step(0.5, abs(cell - 1.0));
                float corner = outerAxes.x * outerAxes.y;
                float outerRing = max(outerAxes.x, outerAxes.y);
                float shadedTile = lerp(corner, outerRing, saturate(_EdgeTileMode));
                // Measure across the visible rim, excluding transparent padding in the master.
                float2 lowerRim = saturate((1.0 / 3.0 - i.uv)
                    / max(1.0 / 3.0 - _PlateUvBounds.xy, float2(0.00001, 0.00001)));
                float2 upperRim = saturate((i.uv - 2.0 / 3.0)
                    / max(_PlateUvBounds.zw - 2.0 / 3.0, float2(0.00001, 0.00001)));
                float depth = max(max(lowerRim.x, lowerRim.y), max(upperRim.x, upperRim.y));
                float ridge = 1.0 - smoothstep(0.0, 1.0, abs(depth * 2.0 - 1.0));
                float rimGray = lerp(_EdgeLuminance, _RimMiddleLuminance, ridge);
                float gray = lerp(_CenterLuminance, rimGray, shadedTile) / 255.0;
                float3 surface = float3(gray, gray, gray);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                surface = GammaToLinearSpace(surface);
                #endif
                return float4(surface * i.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
