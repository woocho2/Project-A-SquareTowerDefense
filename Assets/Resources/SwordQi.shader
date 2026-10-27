// 검기 한 조각을 그림 파일 없이 공식으로 그림 (검성 기본 공격의 투사체, 검객의 반짝임)
//   _Shape 0 = 초승달 검기: 앞(+X)이 둥글고 양 끝(뿔)이 뒤로 향함. 날아가는 방향으로 돌려서 씀
//   _Shape 1 = 반짝임: 가는 십자 별 + 가운데 빛 (칼집에서 칼날이 번쩍하는 순간)
//   _Shape 2 = 부드러운 빛 덩어리 (화면 전체 번쩍, 기 모으기)
//   _Shape 3 = 꽃잎 하나: 끝이 살짝 갈라진 매화 꽃잎 (위쪽 +Y가 끝)
//   _Shape 4 = 매화 한 송이: 둥근 꽃잎 다섯 장 + 가운데 꽃술
//   _Shape 5 = 도끼: 아래로 긴 자루 + 위쪽 오른편에 반달 날 (날 끝이 밝음)
//   _Shape 6 = 충격파 고리: 가는 원 띠 (판을 키우며 흐리게 하면 퍼져 나가는 고리)
//   _Shape 7 = 돌 파편: 모서리 깎인 작은 돌 (테두리가 어두움)
//   _Shape 8 = 화살: 오른쪽(+X)이 촉. 대는 나무색(어둡게), 깃은 조금 밝게, 촉 끝이 번뜩
//   _Shape 9 = 창: 오른쪽(+X)이 창끝. 긴 나무 자루 + 쇠고리 + 나뭇잎 모양 창끝 (가운데 날이 번뜩)
//   _Shape 10 = 깃발 창: 창 + 창끝 뒤 자루 아래에 매달린 제비꼬리 깃발 (_Color2, 펄럭임). 왼쪽을 볼 땐 판 세로를 뒤집어 깃발이 아래로
//   _Shape 11 = 말 탄 워로드 실루엣 (오른쪽으로 달림): 말은 어둡게, 사람은 _Color, 망토는 _Color2. _Phase로 다리가 달림
//   색을 어둡게 주면 바닥을 어둡게 덮을 수도 있음 (그을린 자국)
//   _ClipBelow: 이 월드 높이(y)보다 아래는 지움 → 땅에 꽂힌 도끼의 박힌 부분이 사라짐 (기본값은 아주 낮아서 안 지움)
Shader "SquareTowerDefense/SwordQi"
{
    Properties
    {
        _Color ("Glow Color", Color) = (1, 0.86, 0.55, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Shape ("Shape (0 Crescent, 1 Glint, 2 Soft, 3 Petal, 4 Blossom, 5 Axe, 6 Ring, 7 Chunk, 8 Arrow, 9 Spear, 10 Banner Spear, 11 Cavalry)", Float) = 0
        _Color2 ("Second Color (Banner, Cape)", Color) = (0.85, 0.15, 0.15, 1)
        _Phase ("Gallop Phase", Float) = 0
        _ClipBelow ("Hide Below World Y", Float) = -100000
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
            #include "UnityCG.cginc"

            float4 _Color, _Color2;
            float _Alpha, _Shape, _ClipBelow, _Phase;

            // 두 점을 잇는 둥근 막대까지의 거리 (안이면 음수)
            float Capsule(float2 p, float2 a, float2 b, float r)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h) - r;
            }

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float worldY : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldY = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;            // 가운데가 (0, 0), 가장자리가 ±1
                float alpha;
                float whiteness;
                float shade = 1.0;                      // 1보다 작으면 어둡게 (도끼 자루, 테두리, 돌 파편)
                float second = 0.0;                     // 1이면 _Color2로 칠함 (깃발)

                if (_Shape < 0.5)
                {
                    // 큰 원 안이면서, 뒤로 밀린 작은 원 밖 = 초승달
                    float outer = length(p) - 0.62;
                    float inner = length(p - float2(-0.28, 0.0)) - 0.56;
                    float d = max(outer, -inner);       // 0보다 작으면 초승달 안
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    float body = 1.0 - smoothstep(-aa, aa, d);
                    float glow = exp(-max(d, 0.0) * 9.0) * 0.55;
                    alpha = saturate(body + glow);
                    whiteness = 1.0 - smoothstep(-0.06, 0.0, d);   // 몸통 안쪽일수록 흰색
                }
                else if (_Shape < 1.5)
                {
                    float2 q = abs(p);
                    float star = exp(-q.x * 12.0) * exp(-q.y * 3.0) + exp(-q.y * 12.0) * exp(-q.x * 3.0);
                    float center = exp(-dot(p, p) * 16.0);
                    alpha = saturate(star + center);
                    whiteness = saturate(center * 1.2 + star * 0.5);
                }
                else if (_Shape < 2.5)
                {
                    float r2 = dot(p, p);
                    alpha = saturate(exp(-r2 * 2.2) * (1.0 - smoothstep(0.85, 1.0, sqrt(r2))));
                    whiteness = exp(-r2 * 6.0);
                }
                else if (_Shape < 3.5)
                {
                    // 아래가 좁은 달걀 모양에서 위 끝의 작은 원을 빼면 갈라진 꽃잎
                    float2 q = p * float2(1.0, 0.9);
                    float shape = length(float2(q.x * (1.25 - q.y * 0.45), q.y)) - 0.78;
                    float notch = 0.18 - length((p - float2(0.0, 0.86)) * float2(1.0, 1.3));
                    float d = max(shape, notch);
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = 1.0 - smoothstep(-aa, aa, d);
                    whiteness = saturate(0.55 - p.y * 0.45);           // 꽃잎 뿌리 쪽이 흼
                }
                else if (_Shape > 4.5 && _Shape < 5.5)
                {
                    // 수염 도끼: 왼쪽에 세로 자루, 위쪽 오른편에 날.
                    //   날 = 자루 옆 좁은 목에서 시작해 위(뿔)·아래(수염)로 벌어지고, 앞은 크게 휜 날, 위·아래 가장자리는 오목하게
                    float2 handleQ = abs(p - float2(-0.28, -0.08)) - float2(0.07, 0.86);
                    float handle = length(max(handleQ, 0.0)) + min(max(handleQ.x, handleQ.y), 0.0) - 0.015;

                    float along = saturate((p.x + 0.2) / 0.9);                  // 목(0) → 날 끝(1)
                    float dip = sin(along * 3.14159265) * 0.11;                 // 위·아래 가장자리를 오목하게
                    float topEdge = 0.6 + 0.3 * (p.x + 0.2) - dip;
                    float bottomEdge = 0.38 - 0.62 * (p.x + 0.2) + dip;
                    float front = length(p - float2(-0.6, 0.35)) - 1.32;        // 크게 휜 날
                    float head = max(max((p.y - topEdge) * 0.9, (bottomEdge - p.y) * 0.9), max(front, -0.24 - p.x));

                    float2 socketQ = abs(p - float2(-0.28, 0.47)) - float2(0.12, 0.17);   // 자루를 감싼 쇠
                    float socket = length(max(socketQ, 0.0)) + min(max(socketQ.x, socketQ.y), 0.0) - 0.02;

                    float metal = min(head, socket);
                    float d = min(handle, metal);
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = saturate((1.0 - smoothstep(-aa, aa, d)) + exp(-max(d, 0.0) * 14.0) * 0.25);

                    float rim = smoothstep(-0.055, -0.015, d);                  // 테두리는 어둡게 → 모양이 또렷
                    shade = (handle < metal ? 0.45 : 1.0) * lerp(1.0, 0.35, rim);
                    whiteness = smoothstep(-0.17, -0.04, front) * step(head, 0.0) * (1.0 - rim);   // 날 끝만 하얗게 번뜩
                }
                else if (_Shape > 10.5)
                {
                    // 말 탄 워로드 (오른쪽을 보고 달림): 말 몸통·목·머리·꼬리·다리 넷 + 탄 사람의 몸·머리·팔·다리 + 뒤로 날리는 망토(_Color2)
                    //   _Phase가 오르면 다리가 번갈아 흔들리고 몸이 들썩임 (말발굽)
                    float2 q = p;
                    q.y -= sin(_Phase * 2.0) * 0.025;                          // 들썩임
                    float body = (length((q - float2(-0.05, -0.12)) / float2(0.42, 0.2)) - 1.0) * 0.2;
                    float neck = Capsule(q, float2(0.26, -0.02), float2(0.44, 0.24), 0.085);
                    float head = Capsule(q, float2(0.44, 0.24), float2(0.66, 0.1), 0.07);
                    float tail = Capsule(q, float2(-0.44, -0.04), float2(-0.68, -0.22 + sin(_Phase) * 0.05), 0.035);
                    float horse = min(min(body, neck), min(head, tail));
                    float2 hips[4] = { float2(0.24, -0.22), float2(0.15, -0.22), float2(-0.26, -0.2), float2(-0.35, -0.2) };
                    float offsets[4] = { 0.0, 1.6, 3.1, 4.7 };
                    for (int leg = 0; leg < 4; leg++)
                    {
                        float swing = sin(_Phase + offsets[leg]) * 0.55;
                        float2 foot = hips[leg] + float2(sin(swing), -cos(swing)) * 0.5;
                        horse = min(horse, Capsule(q, hips[leg], foot, 0.04));
                    }
                    float rider = min(Capsule(q, float2(-0.04, 0.06), float2(0.03, 0.42), 0.09), length(q - float2(0.05, 0.56)) - 0.075);
                    rider = min(rider, Capsule(q, float2(0.03, 0.34), float2(0.24, 0.28), 0.035));      // 창을 쥔 팔
                    rider = min(rider, Capsule(q, float2(-0.02, 0.06), float2(0.12, -0.12), 0.045));    // 다리
                    float flap = sin(_Phase * 1.5) * 0.06;
                    float cape = Capsule(q, float2(-0.04, 0.38), float2(-0.34, 0.24 + flap), 0.07);
                    float d = min(min(horse, rider), cape);
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = saturate((1.0 - smoothstep(-aa, aa, d)) + exp(-max(d, 0.0) * 16.0) * 0.2);
                    float rim = smoothstep(-0.03, -0.008, d);
                    bool isCape = cape < min(horse, rider);
                    second = isCape ? 1.0 : 0.0;
                    shade = (isCape ? 0.9 : (horse < rider ? 0.45 : 1.0)) * lerp(1.0, 0.4, rim);
                    whiteness = 0.0;
                }
                else if (_Shape > 9.5)
                {
                    // 깃발 창: 창(모양 9)과 같은 자루·쇠고리·창끝 + 창끝 뒤 자루 아래에 매달린 제비꼬리 깃발 (펄럭임)
                    float2 shaftQ = abs(p - float2(-0.2, 0.0)) - float2(0.72, 0.03);
                    float shaft = length(max(shaftQ, 0.0)) + min(max(shaftQ.x, shaftQ.y), 0.0);
                    float2 collarQ = abs(p - float2(0.47, 0.0)) - float2(0.035, 0.055);
                    float collar = length(max(collarQ, 0.0)) + min(max(collarQ.x, collarQ.y), 0.0);
                    float2 leaf = float2((p.x - 0.72) / 0.27, p.y / (0.11 * saturate(1.15 - (p.x - 0.72) * 1.6)));
                    float tip = (length(leaf) - 1.0) * 0.1;

                    float wave = sin(p.x * 9.0 + _Time.y * 7.0) * 0.035 * saturate((0.42 - p.x) * 3.0);   // 자루에서 멀수록 크게 펄럭
                    float fy = p.y - wave;
                    float2 clothQ = abs(float2(p.x - 0.17, fy + 0.3)) - float2(0.25, 0.27);              // 자루 아래로 매달림
                    float cloth = length(max(clothQ, 0.0)) + min(max(clothQ.x, clothQ.y), 0.0);
                    float notch = max(abs(p.x - 0.17) - (-0.38 - fy) * 0.7, fy + 0.38);            // 아래 가운데를 V자로 파낸 제비꼬리
                    cloth = max(cloth, -notch);
                    float metal = min(collar, tip);
                    float d = min(min(shaft, metal), cloth);
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = saturate((1.0 - smoothstep(-aa, aa, d)) + exp(-max(d, 0.0) * 18.0) * 0.2);
                    float rim = smoothstep(-0.03, -0.008, d);
                    bool isCloth = cloth < min(shaft, metal);
                    second = isCloth ? 1.0 : 0.0;
                    shade = (isCloth ? 0.85 + wave * 3.0 : (shaft < metal ? 0.55 : (collar < tip ? 0.8 : 1.0))) * lerp(1.0, 0.45, rim);
                    whiteness = step(tip, 0.0) * (1.0 - smoothstep(0.0, 0.05, abs(p.y))) * 0.65;
                }
                else if (_Shape > 8.5)
                {
                    // 창: 오른쪽(+X)이 창끝. 긴 자루 + 자루를 감싼 쇠고리 + 나뭇잎 모양 창끝
                    float2 shaftQ = abs(p - float2(-0.2, 0.0)) - float2(0.72, 0.03);
                    float shaft = length(max(shaftQ, 0.0)) + min(max(shaftQ.x, shaftQ.y), 0.0);
                    float2 collarQ = abs(p - float2(0.47, 0.0)) - float2(0.035, 0.055);
                    float collar = length(max(collarQ, 0.0)) + min(max(collarQ.x, collarQ.y), 0.0);
                    float2 leaf = float2((p.x - 0.72) / 0.27, p.y / (0.11 * saturate(1.15 - (p.x - 0.72) * 1.6)));   // 앞쪽이 더 뾰족
                    float tip = (length(leaf) - 1.0) * 0.1;
                    float metal = min(collar, tip);
                    float d = min(shaft, metal);
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = saturate((1.0 - smoothstep(-aa, aa, d)) + exp(-max(d, 0.0) * 18.0) * 0.2);
                    float rim = smoothstep(-0.03, -0.008, d);
                    shade = (shaft < metal ? 0.55 : (collar < tip ? 0.8 : 1.0)) * lerp(1.0, 0.45, rim);
                    whiteness = step(tip, 0.0) * (1.0 - smoothstep(0.0, 0.05, abs(p.y))) * 0.65;   // 창끝 가운데 날이 번뜩
                }
                else if (_Shape > 7.5)
                {
                    // 화살: 오른쪽(+X)이 촉. 가는 대 + 세모 촉 + 꼬리의 깃 두 장
                    float2 shaftQ = abs(p - float2(-0.12, 0.0)) - float2(0.7, 0.035);
                    float shaft = length(max(shaftQ, 0.0)) + min(max(shaftQ.x, shaftQ.y), 0.0);
                    float head = max((abs(p.y) - (0.95 - p.x) * 0.42) * 0.92, 0.56 - p.x);
                    float fletch = max(max(-0.9 - p.x, p.x + 0.52), abs(p.y) - (0.2 - (p.x + 0.9) * 0.28));
                    float d = min(min(shaft, head), fletch);
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = saturate((1.0 - smoothstep(-aa, aa, d)) + exp(-max(d, 0.0) * 18.0) * 0.2);
                    float rim = smoothstep(-0.03, -0.008, d);
                    shade = (head < min(shaft, fletch) ? 1.0 : (fletch < shaft ? 0.85 : 0.6)) * lerp(1.0, 0.45, rim);
                    whiteness = step(head, 0.0) * smoothstep(0.6, 0.9, p.x) * 0.7;     // 촉 끝이 번뜩
                }
                else if (_Shape > 6.5)
                {
                    // 돌 파편: 모서리가 깎인 팔각형, 왼쪽 위가 살짝 밝음
                    float d = max(abs(p.x) + abs(p.y) * 0.45, abs(p.y) + abs(p.x) * 0.45) - 0.62;
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = 1.0 - smoothstep(-aa, aa, d);
                    float rim = smoothstep(-0.12, -0.02, d);
                    shade = 0.8 * lerp(1.0, 0.45, rim);
                    whiteness = saturate(-p.x * 0.3 + p.y * 0.3) * 0.35;
                }
                else if (_Shape > 5.5)
                {
                    float d = abs(length(p) - 0.7) - 0.07;
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    alpha = saturate((1.0 - smoothstep(-aa, aa, d)) + exp(-max(d, 0.0) * 10.0) * 0.5);
                    whiteness = 1.0 - smoothstep(-0.05, 0.0, d);
                }
                else
                {
                    // 각도에 따라 반지름이 다섯 번 부풀었다 줄어듦 = 꽃잎 다섯 장
                    float radius = length(p);
                    float angle = atan2(p.y, p.x);
                    float edge = 0.48 + 0.34 * abs(cos(angle * 2.5));
                    float d = radius - edge;
                    float aa = max(fwidth(d) * 1.5, 0.004);
                    float flower = 1.0 - smoothstep(-aa, aa, d);
                    float heart = 1.0 - smoothstep(0.14, 0.2, radius);   // 가운데 꽃술
                    alpha = saturate(flower + exp(-max(d, 0.0) * 14.0) * 0.35);
                    whiteness = saturate(0.6 - radius * 0.5) * (1.0 - heart * 0.8);
                }

                alpha *= _Alpha;
                alpha *= smoothstep(_ClipBelow - 0.01, _ClipBelow + 0.02, i.worldY);   // 땅에 박힌 부분(이 높이 아래)은 안 보임
                float3 baseColor = lerp(_Color.rgb, _Color2.rgb, second);
                float3 color = lerp(baseColor * shade, float3(1.0, 1.0, 1.0), saturate(whiteness));
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
