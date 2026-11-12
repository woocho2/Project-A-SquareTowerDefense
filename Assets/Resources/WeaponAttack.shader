Shader "SquareTowerDefense/WeaponAttack"
{
    Properties
    {
        [HDR] _Color ("Metal / impact", Color) = (.48,.65,.82,1)
        [HDR] _Color2 ("Rim / highlight", Color) = (.96,.86,.58,1)
        _Alpha ("Opacity", Range(0,1)) = 1
        _Shape ("0 Tower shield, 1 Round shield, 2 Hammer, 3 Fissure, 4 Impact, 5 Shock arc, 6 Explosive charge", Float) = 0
        _Phase ("Phase", Float) = 0
        _Progress ("Draw", Range(0,1)) = 1
        _Intensity ("Intensity", Float) = 1
        [HideInInspector] _DstBlend ("Destination blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest LEqual
        Blend One [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Color, _Color2;
            float _Alpha, _Shape, _Phase, _Progress, _Intensity;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float mask(float d) { float aa=max(fwidth(d),.004); return 1-smoothstep(-aa,aa,d); }
            float box(float2 p,float2 size) { float2 q=abs(p)-size; return length(max(q,0))+min(max(q.x,q.y),0); }
            float segmentDistance(float2 p,float2 a,float2 b) {
                float2 q=p-a,v=b-a;
                return length(q-v*saturate(dot(q,v)/max(.00001,dot(v,v))));
            }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float r=length(p), body=0, light=0, shade=1;
                if(_Shape<.5) // tall sentinel shield, bevelled plate and solid reinforced rim
                {
                    float d=max(box(p,float2(.62,.83)),abs(p.x)+max(0,-p.y-.35)*.30-.65);
                    body=mask(d);
                    float rim=mask(d)-mask(d+.075);
                    float spine=mask(box(p,float2(.026,.68)));
                    light=saturate(rim*.85+spine*.55+step(.05,p.y-p.x*.5)*.18);
                    shade=.68+.27*step(0,-p.x);
                }
                else if(_Shape<1.5) // round shield with metal boss and four broad sectors
                {
                    float d=r-.79;
                    body=mask(d);
                    float rim=mask(d)-mask(d+.085);
                    float boss=mask(r-.18);
                    float bossEdge=mask(abs(r-.18)-.018);
                    light=saturate(rim*.8+boss*.7+bossEdge*.25);
                    shade=.62+.20*step(0,p.x*p.y)+.16*step(.03,p.y-p.x*.6);
                }
                else if(_Shape<2.5) // heavy hammer; the head is centred at local UV y=.52
                {
                    float handle=box(p-float2(0,-.28),float2(.065,.56));
                    float pommel=box(p-float2(0,-.81),float2(.10,.045));
                    float head=max(box(p-float2(0,.52),float2(.72,.24)),abs(p.x)+abs(p.y-.52)*.5-.80);
                    float d=min(min(handle,pommel),head);
                    body=mask(d);
                    float metal=mask(head);
                    float bevel=mask(head)-mask(head+.06);
                    light=saturate(bevel*.8+metal*step(.55,p.y)*.22+
                        mask(box(p-float2(0,.52),float2(.08,.21)))*.45);
                    shade=lerp(.33,.8,metal)+metal*.16*step(.0,-p.x);
                }
                else if(_Shape<3.5) // expanding branched ground fractures
                {
                    float d=10;
                    [unroll] for(int k=0;k<5;k++)
                    {
                        float angle=k*1.256637+_Phase;
                        float2 q=float2(cos(angle)*p.x-sin(angle)*p.y,sin(angle)*p.x+cos(angle)*p.y);
                        d=min(d,segmentDistance(q,0,float2(.30,.09)));
                        d=min(d,segmentDistance(q,float2(.30,.09),float2(.76,-.06)));
                        d=min(d,segmentDistance(q,float2(.30,.09),float2(.41,.37)));
                    }
                    body=mask(d-.024)*(1-smoothstep(_Progress-.03,_Progress+.03,r));
                    light=mask(d-.006)*.65;
                }
                else if(_Shape<4.5) // compact jagged impact star
                {
                    float a=atan2(p.y,p.x);
                    float edge=.32+.28*pow(abs(cos(a*5+_Phase)),5);
                    float d=r-edge;
                    body=mask(d);
                    light=mask(r-.20);
                }
                else if (_Shape<5.5) // broad directional shock front
                {
                    float d=max(r-.84,-(length(p-float2(-.25,0))-.75));
                    body=mask(d);
                    light=mask(d+.025)*.8;
                }
                else // explosive packet attached to the arrow shaft
                {
                    float d=box(p,float2(.68,.40));
                    body=mask(d);
                    light=(mask(d)-mask(d+.05))*.5+mask(box(p,float2(.07,.40)))*.6;
                    shade=.65;
                }
                float alpha=saturate(body)*_Alpha*_Color.a;
                float3 color=lerp(_Color.rgb*shade,_Color2.rgb,saturate(light))*_Intensity;
                return float4(color*alpha,alpha);
            }
            ENDCG
        }
    }
}
