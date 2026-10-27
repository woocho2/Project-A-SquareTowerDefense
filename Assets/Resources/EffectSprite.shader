// 텍스처(그림 에셋)를 입히는 연출 셰이더 — 실무식 "그림 + 셰이더"
//   그림은 흰색·회색 + 투명(Resources/Effects/FX_*.png). 색과 밝기는 여기서 입힘
//   _Intensity가 1보다 크면 블룸(후처리)에서 빛이 번짐
//   _Progress 0 → 1: 왼쪽(u=0)에서 오른쪽(u=1)으로 그어짐. _HeadFollow 1이면 그림의 오른쪽 끝(밝은 머리)이 그어지는 끝을 따라감
//   _Tail 0 → 1: 왼쪽부터 지워짐 (베기 궤적이 휙 지나가 사라지게)
//   _Dissolve 0 → 1: 노이즈 무늬대로 타들어 가듯 사라짐 (가장자리가 빛남)
//   _Scroll: 그림을 가로로 흘림 / _ClipBelow: 이 월드 높이 아래는 지움
//   섞기: _DstBlend 10 = 보통(반투명), 1 = 더하기(빛)
Shader "SquareTowerDefense/EffectSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity (Bloom)", Float) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Progress ("Draw Progress", Range(0, 1)) = 1
        _HeadFollow ("Head Follows Progress", Range(0, 1)) = 0
        _Tail ("Tail Erase", Range(0, 1)) = 0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Scroll ("Scroll U", Float) = 0
        _ClipBelow ("Hide Below World Y", Float) = -100000
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 10
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _NoiseTex;
            float4 _Color;
            float _Intensity, _Alpha, _Progress, _HeadFollow, _Tail, _Dissolve, _Scroll, _ClipBelow;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float worldY : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;                       // 파티클은 알마다 색이 다름 (판은 흰색)
                o.worldY = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float u = i.uv.x;
                float2 uv = float2(u + (1.0 - _Progress) * _HeadFollow + _Scroll, i.uv.y);
                float4 tex = tex2D(_MainTex, uv);

                float drawn = 1.0 - smoothstep(_Progress, _Progress + 0.02, u);          // 아직 안 그어진 쪽은 안 보임
                float tail = smoothstep(_Tail, _Tail + 0.08, u);                         // 지나간 쪽부터 지워짐
                float noise = tex2D(_NoiseTex, i.uv * 1.5).r;
                float dissolve = smoothstep(_Dissolve, _Dissolve + 0.06, noise);
                float edge = (1.0 - smoothstep(_Dissolve, _Dissolve + 0.12, noise)) * step(0.001, _Dissolve);   // 타들어 가는 가장자리

                float alpha = tex.a * _Color.a * i.color.a * _Alpha * drawn * tail * dissolve;
                alpha *= smoothstep(_ClipBelow - 0.01, _ClipBelow + 0.02, i.worldY);
                float3 color = tex.rgb * _Color.rgb * i.color.rgb * _Intensity * (1.0 + edge * 2.0);
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
