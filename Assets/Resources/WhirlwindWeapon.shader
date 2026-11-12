Shader "SquareTowerDefense/WhirlwindWeapon"
{
    Properties
    {
        _MainTex ("Painted Great Axe", 2D) = "white" {}
        [HDR] _Color ("Weapon Tint", Color) = (1,1,1,1)
        [HDR] _Color2 ("Edge Glint", Color) = (1,.96,.86,1)
        _Alpha ("Opacity", Range(0,1)) = 1
        _Intensity ("Intensity", Float) = 1
        _Phase ("Edge Glint Position", Float) = 0
        _Whiten ("Motion Edge", Range(0,1)) = 0
        _Dissolve ("Release From Grip", Range(0,1)) = 0
        _Ghost ("Blade Only Afterimage", Range(0,1)) = 0
        _PlaneFlatten ("Projected Swing Plane", Range(.4,1)) = 1
        _PivotY ("Swing Origin Y", Float) = 0
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off
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
            float _Alpha, _Intensity, _Phase, _Whiten, _Dissolve, _Ghost, _PlaneFlatten, _PivotY;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                world.y = _PivotY + (world.y - _PivotY) * _PlaneFlatten;
                o.vertex = UnityWorldToClipPos(world.xyz);
                o.uv = v.uv;
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                // The neutral painting carries the material shading, not an analytic silhouette.
                float value = dot(tex.rgb, float3(.2126,.7152,.0722));
                float3 color = value * _Color.rgb;
                float blade = smoothstep(.66,.82,i.uv.x);
                float bevel = smoothstep(.62,.92,value) * blade;
                float glint = exp(-pow((i.uv.x + i.uv.y * .15 - (.5 + _Phase * .65)) / .055, 2));
                color += _Color2.rgb * bevel * (glint * .20 + _Whiten * .16);
                float ghostMask = lerp(1, smoothstep(.62,.76,i.uv.x), _Ghost);
                float threshold = lerp(-.04,1.04,_Dissolve);
                float release = smoothstep(threshold-.025,threshold+.025,i.uv.x);
                float dissolveEdge = (1-smoothstep(.015,.055,abs(i.uv.x-threshold)))
                    * step(.001,_Dissolve) * (1-_Ghost);
                color += _Color2.rgb * dissolveEdge * .28;
                float alpha = tex.a * _Color.a * _Alpha * release * ghostMask;
                return float4(color * alpha * _Intensity, alpha);
            }
            ENDCG
        }
    }
}
