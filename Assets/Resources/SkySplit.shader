// 천지일섬의 화면 갈라짐: 찍어 둔 화면(_MainTex)을 베기 선 네 줄을 따라 조각내고, 조각을 선의 양쪽으로 벌림
//   벌어진 틈(어느 조각에서도 오지 않는 자리)은 금빛으로 채움. 선 근처는 빛이 번짐
//   좌표는 전부 "텍스처 픽셀" 기준: 점 = (x, y) 픽셀, 방향 = 단위 벡터. C#이 화면 좌표를 바꿔서 넘김
Shader "SquareTowerDefense/SkySplit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured Screen", 2D) = "white" {}
        _GlowColor ("Light Color", Color) = (1, 0.85, 0.5, 1)
        _Shift ("Split Distance (px)", Float) = 0
        _Glow ("Seam Light", Range(0, 1)) = 0
        _Flash ("Flash", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define CUT_COUNT 4

            sampler2D _MainTex;
            float4 _GlowColor;
            float4 _ScreenSize;             // x, y = 찍은 화면의 픽셀 크기
            float4 _Cuts[CUT_COUNT];        // xy = 선 위의 한 점(픽셀), zw = 선 방향(단위 벡터)
            float _Shift, _Glow, _Flash;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // 이 점이 선마다 어느 쪽에 있는지 → 조각 번호 (선 넷이면 0 ~ 15)
            float RegionOf(float2 px)
            {
                float region = 0.0;
                for (int i = 0; i < CUT_COUNT; i++)
                {
                    float2 normal = float2(-_Cuts[i].w, _Cuts[i].z);
                    if (dot(px - _Cuts[i].xy, normal) >= 0.0) region += exp2((float)i);
                }
                return region;
            }

            // 조각이 움직인 거리: 선마다 자기 쪽으로 _Shift의 절반씩
            float2 OffsetOf(float2 px)
            {
                float2 offset = float2(0.0, 0.0);
                for (int i = 0; i < CUT_COUNT; i++)
                {
                    float2 normal = float2(-_Cuts[i].w, _Cuts[i].z);
                    float side = dot(px - _Cuts[i].xy, normal) >= 0.0 ? 1.0 : -1.0;
                    offset += normal * side * (_Shift * 0.5);
                }
                return offset;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 size = max(_ScreenSize.xy, float2(1.0, 1.0));
                float2 px = i.uv * size;

                // 이 자리에 와 있는 조각 = 원래 자리에서 그 조각의 이동만큼 거슬러 간 곳
                float2 source = px - OffsetOf(px);
                bool gap = abs(RegionOf(source) - RegionOf(px)) > 0.5;
                bool outside = any(source < 0.0) || any(source > size);

                float3 scene = tex2D(_MainTex, source / size).rgb;

                // 선(벌어진 틈의 가운데)까지의 거리 → 빛 번짐
                float nearest = 100000.0;
                for (int c = 0; c < CUT_COUNT; c++)
                {
                    float2 normal = float2(-_Cuts[c].w, _Cuts[c].z);
                    nearest = min(nearest, abs(dot(px - _Cuts[c].xy, normal)));
                }
                float spread = _Shift * 0.5 + 6.0;
                float bloom = exp(-nearest / spread) * _Glow;

                float3 light = _GlowColor.rgb * (1.4 + bloom);
                float3 color = (gap || outside) ? light * max(_Glow, 0.35) : scene + _GlowColor.rgb * bloom * 0.9;
                color = lerp(color, float3(1.0, 0.97, 0.9), _Flash);
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}
