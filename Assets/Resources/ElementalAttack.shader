Shader "SquareTowerDefense/ElementalAttack"
{
    Properties
    {
        [HDR] _Color ("Edge / body", Color) = (0.15,0.6,1,1)
        [HDR] _Color2 ("Core / highlight", Color) = (0.7,1,1,1)
        _Alpha ("Opacity", Range(0,1)) = 1
        _Shape ("0 Water, 1 Rock, 2 Wind, 3 Bolt, 4 Star, 5 Beam, 6 Shadow, 7 Fire, 8 Snow, 9 Fissure", Float) = 0
        _Phase ("Animation phase", Float) = 0
        _Progress ("Expansion", Float) = 0
        _Intensity ("Light intensity", Float) = 1
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
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Color, _Color2;
            float _Alpha, _Shape, _Phase, _Progress, _Intensity;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float fill(float d) { float aa=max(fwidth(d),0.006); return 1-smoothstep(-aa,aa,d); }
            float hash(float x) { return frac(sin(x*127.1+31.7)*43758.5453); }
            float segmentDistance(float2 p,float2 a,float2 b)
            {
                float2 pa=p-a, ba=b-a;
                return length(pa-ba*saturate(dot(pa,ba)/max(dot(ba,ba),0.00001)));
            }
            float lightning(float2 p,float seed)
            {
                float d=10;
                float2 a=float2(-0.92,0);
                [unroll] for(int j=1;j<=10;j++)
                {
                    float x=-0.92+j*0.184;
                    float y=(hash(j+seed)*2-1)*0.33*sin(j*0.314159);
                    float2 b=float2(x,y);
                    d=min(d,segmentDistance(p,a,b)); a=b;
                }
                return d;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float r=length(p), a=atan2(p.y,p.x);
                float body=0, highlight=0, glow=0;
                if(_Shape<0.5) // cohesive water droplet, two broad specular bands
                {
                    float wobble=sin(a*3+_Phase*5)*0.025+sin(a*5-_Phase*3)*0.018;
                    float d=length(p*float2(1,1.12))-0.70-wobble;
                    body=fill(d);
                    highlight=saturate(fill(d+0.11)-fill(d+0.16))*
                        smoothstep(-0.2,0.55,p.y)+fill(length((p-float2(-.23,.28))*float2(1,2.2))-.19);
                    glow=exp(-r*r*4)*0.15;
                }
                else if(_Shape<1.5) // asymmetric, faceted stone silhouette
                {
                    float d=max(max(abs(p.x+.12*p.y)-(.58-.19*p.y),
                        p.y-.72),-p.y-.64);
                    d=max(d, dot(p,float2(.65,.76))-.72);
                    d=max(d,dot(p,float2(-.8,.6))-.67);
                    body=fill(d);
                    highlight=fill(d)*(.20+.45*step(.1,p.y+p.x*.8)+.2*step(.1,-p.x-.4*p.y));
                }
                else if(_Shape<2.5) // tapered crescent / wind blade
                {
                    float d=max(r-.83,-(length(p-float2(-.24,.04))-.77));
                    body=fill(d);
                    highlight=fill(d+.03)*smoothstep(-.4,.5,p.x);
                    glow=fill(d-.07)*.12;
                }
                else if(_Shape<3.5) // stepped lightning with secondary fork
                {
                    float seed=floor(_Phase*18);
                    float d=lightning(p,seed);
                    float branch=segmentDistance(p,float2(-.12,.03),float2(.2,.62));
                    branch=min(branch,segmentDistance(p,float2(.2,.62),float2(.52,.43)));
                    d=min(d,branch+.018);
                    body=fill(d-.035);
                    highlight=fill(d-.012);
                    glow=exp(-d*22)*.65;
                }
                else if(_Shape<4.5) // star singularity, four clean rays
                {
                    float d=pow(abs(p.x),.65)+pow(abs(p.y),.65)-.61;
                    body=fill(d);
                    highlight=fill(d+.22);
                    glow=exp(-r*6)*.5;
                }
                else if(_Shape<5.5) // beam, bright core and soft limited aura
                {
                    float cap=1-smoothstep(.89,.99,abs(p.x));
                    float width=.09+.035*sin(p.x*14-_Phase*20);
                    body=fill(abs(p.y)-width)*cap;
                    highlight=fill(abs(p.y)-.035)*cap;
                    glow=exp(-abs(p.y)*9)*cap*.5;
                }
                else if(_Shape<6.5) // opaque ink core with animated torn corona
                {
                    float edge=.57+sin(a*6-_Phase*7)*.035+sin(a*9+_Phase*4)*.02;
                    body=fill(r-edge);
                    highlight=(fill(r-edge)-fill(r-edge+.09))*.9;
                    glow=exp(-abs(r-edge)*20)*.23;
                }
                else if(_Shape<7.5) // directional flame, head at +X, forked tail at -X
                {
                    float taper=saturate((p.x+.95)/1.4);
                    float center=sin(p.x*9-_Phase*18)*.09*(1-taper);
                    float d=abs(p.y-center)-(.06+.43*taper)*(1-smoothstep(.38,.86,p.x));
                    body=fill(d)*smoothstep(-.98,-.72,p.x);
                    highlight=fill(d+.12)*smoothstep(-.6,.1,p.x);
                    glow=fill(d-.08)*.14;
                }
                else if(_Shape<8.5) // compact snowball; pale shadow plane, no plastic gloss
                {
                    body=fill(r-.68);
                    highlight=fill(r-.68)*(.45+.4*smoothstep(-.4,.4,p.y-p.x*.4));
                    glow=0;
                }
                else // ground fracture: three branching fissures, expanding from center
                {
                    float d=10;
                    [unroll] for(int k=0;k<3;k++)
                    {
                        float angle=k*2.0944+_Phase;
                        float2 q=float2(cos(angle)*p.x-sin(angle)*p.y,sin(angle)*p.x+cos(angle)*p.y);
                        d=min(d,segmentDistance(q,0,float2(.34,.13)));
                        d=min(d,segmentDistance(q,float2(.34,.13),float2(.76,-.03)));
                        d=min(d,segmentDistance(q,float2(.34,.13),float2(.46,.43)));
                    }
                    body=fill(d-.024)*step(r,_Progress);
                    highlight=fill(d-.007)*step(r,_Progress);
                    glow=exp(-d*28)*step(r,_Progress)*.18;
                }
                float alpha=saturate(body+glow)*_Alpha*_Color.a;
                float3 rgb=lerp(_Color.rgb,_Color2.rgb,saturate(highlight))*_Intensity;
                return float4(rgb*alpha,alpha);
            }
            ENDCG
        }
    }
}
