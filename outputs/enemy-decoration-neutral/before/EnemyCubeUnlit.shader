Shader "SquareTowerDefense/EnemyCubeUnlit"
{
    Properties
    {
        _MainTex ("Character Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Toggle] _UseLayers ("Compose Separate Layers", Float) = 0
        _BackgroundTex ("Shared Background / Outer Mask", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (1,1,1,1)
        _EquipmentMask ("Character Equipment Mask", 2D) = "black" {}
        _EquipmentColor ("Equipment Color", Color) = (1,0,0,1)
        [Toggle] _RecolorEquipment ("Recolor Equipment", Float) = 0
        _DecorationTex ("Shared Decoration", 2D) = "black" {}
        _DecorationColor ("Decoration Color", Color) = (1,0,0,1)
        [Toggle] _RecolorDecoration ("Recolor Decoration", Float) = 0
        _BadgeTex ("Shared State Badge Atlas", 2D) = "black" {}
        _BadgeRect ("Badge Placement (UV x y width height)", Vector) = (0.375,0.75,0.25,0.25)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            sampler2D _BackgroundTex, _EquipmentMask, _DecorationTex, _BadgeTex;
            float4 _DecorationTex_ST, _BadgeTex_ST, _BadgeRect;
            fixed4 _BackgroundColor, _EquipmentColor, _DecorationColor;
            float _UseLayers, _RecolorEquipment, _RecolorDecoration;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float shade : TEXCOORD1; float2 faceUV : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.faceUV = v.uv;
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float3 view = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                o.shade = lerp(0.72, 1.0, saturate(abs(dot(normalize(normal), view))));
                return o;
            }
            float3 Recolor(float3 source, float3 target)
            {
                // Hue/saturation work in the artwork's sRGB space, also in Linear projects.
                #ifndef UNITY_COLORSPACE_GAMMA
                    source = LinearToGammaSpace(source);
                    target = LinearToGammaSpace(target);
                #endif
                float value = max(source.r, max(source.g, source.b));
                float minimum = min(source.r, min(source.g, source.b));
                float saturation = (value - minimum) / max(value, 0.00001);
                float targetValue = max(target.r, max(target.g, target.b));
                float3 result = lerp(value.xxx, target / max(targetValue, 0.00001) * value, saturation);
                #ifndef UNITY_COLORSPACE_GAMMA
                    result = GammaToLinearSpace(result);
                #endif
                return result;
            }
            void Over(inout float3 rgb, inout float alpha, float4 layer)
            {
                rgb = layer.rgb * layer.a + rgb * (1.0 - layer.a);
                alpha = layer.a + alpha * (1.0 - layer.a);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                if (_UseLayers > 0.5)
                {
                    float4 background = tex2D(_BackgroundTex, i.faceUV) * _BackgroundColor;
                    float mask = tex2D(_EquipmentMask, i.uv).r * _RecolorEquipment;
                    color.rgb = lerp(color.rgb, Recolor(color.rgb, _EquipmentColor.rgb), saturate(mask));
                    float3 rgb = background.rgb * background.a;
                    float alpha = background.a;
                    Over(rgb, alpha, color);
                    float4 decoration = tex2D(_DecorationTex, TRANSFORM_TEX(i.faceUV, _DecorationTex));
                    decoration.rgb = lerp(decoration.rgb, Recolor(decoration.rgb, _DecorationColor.rgb), _RecolorDecoration);
                    Over(rgb, alpha, decoration);
                    float2 badgeUV = (i.faceUV - _BadgeRect.xy) / max(_BadgeRect.zw, float2(0.00001,0.00001));
                    float inside = step(0.0,badgeUV.x) * step(0.0,badgeUV.y) * step(badgeUV.x,1.0) * step(badgeUV.y,1.0);
                    float4 badge = tex2D(_BadgeTex, saturate(badgeUV) * _BadgeTex_ST.xy + _BadgeTex_ST.zw);
                    badge.a *= inside;
                    Over(rgb, alpha, badge);
                    // Every layer uses the same rounded outer shape, even while rolling.
                    color = float4(rgb / max(alpha, 0.00001), background.a);
                }
                color *= _Color;
                color.rgb *= i.shade;
                return color;
            }
            ENDCG
        }
    }
}
