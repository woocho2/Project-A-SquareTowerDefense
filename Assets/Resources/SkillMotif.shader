Shader "SquareTowerDefense/SkillMotif"
{
    Properties
    {
        [HDR] _Color ("Body", Color) = (.25,.6,1,1)
        [HDR] _Color2 ("Highlight", Color) = (.9,1,1,1)
        _NoiseTex ("Flow noise", 2D) = "white" {}
        _Alpha ("Opacity", Range(0,1)) = 1
        _Shape ("Motif", Float) = 0
        _Emblem ("Emblem 1-14", Float) = 1
        _Variant ("Variant / polarity", Float) = 0
        _Phase ("Animation", Float) = 0
        _Progress ("Reveal", Range(0,1)) = 1
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
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _NoiseTex;
            float4 _Color,_Color2;
            float _Alpha,_Shape,_Emblem,_Variant,_Phase,_Progress,_Intensity;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float ink(float d) { float aa=max(fwidth(d),.003); return 1-smoothstep(-aa,aa,d); }
            float sdBox(float2 p,float2 b) { float2 q=abs(p)-b; return length(max(q,0))+min(max(q.x,q.y),0); }
            float segmentDistance(float2 p,float2 a,float2 b) {
                float2 q=p-a,v=b-a; return length(q-v*saturate(dot(q,v)/max(dot(v,v),.00001)));
            }
            float2 turn(float2 p,float a) { float s=sin(a),c=cos(a); return float2(c*p.x-s*p.y,s*p.x+c*p.y); }
            float ring(float2 p,float radius,float width) { return ink(abs(length(p)-radius)-width); }
            float hash(float x) { return frac(sin(x*127.1+31.7)*43758.5453); }
            float leaf(float2 p) { return ink(length(p*float2(1.9,1))-.37); }
            float glyph(float2 p)
            {
                float e=_Emblem, d=10;
                if(e<1.5) { // sword
                    d=min(sdBox(p-float2(0,.1),float2(.045,.27)),sdBox(p-float2(0,-.17),float2(.16,.033)));
                    d=min(d,sdBox(p-float2(0,-.27),float2(.035,.08)));
                    d=min(d,max(abs(p.x)*.8+p.y-.48,-p.y+.31));
                } else if(e<2.5) { // crossed axe
                    float2 q=turn(p,.55);
                    d=sdBox(q-float2(0,-.08),float2(.035,.30));
                    d=min(d,max(length(q-float2(-.13,.17))-.30,-q.x-.02));
                } else if(e<3.5) {
                    d=max(abs(length(p-float2(-.16,0))-.34)-.025,-p.x-.04);
                    d=min(d,segmentDistance(p,float2(.10,-.29),float2(.10,.29))-.014);
                    d=min(d,sdBox(p-float2(.06,0),float2(.35,.016)));
                } else if(e<4.5) {
                    d=sdBox(p-float2(0,-.1),float2(.023,.31));
                    d=min(d,max(abs(p.x)+abs(p.y-.25)*.46-.12,-p.y+.14));
                } else if(e<5.5) {
                    d=max(sdBox(p,float2(.25,.30)),abs(p.x)-(.30+.30*p.y));
                } else if(e<6.5) {
                    d=min(sdBox(p-float2(0,-.12),float2(.035,.28)),sdBox(p-float2(0,.17),float2(.28,.105)));
                } else if(e<7.5) {
                    float2 q=p-float2(sin(p.y*5)*.045,0);
                    d=length(q*float2(1.3,1))-.28;
                    d=max(d,-p.y-.27); d=max(d,p.y-.39+abs(p.x)*1.4);
                } else if(e<8.5) {
                    d=max(length((p+float2(0,.08))*float2(1.2,1))-.27,p.y-.37+abs(p.x)*1.7);
                } else if(e<9.5) {
                    d=min(sdBox(p-float2(0,-.16),float2(.021,.21)),length(turn(p-float2(.13,.10),-.7)*float2(1.7,1))-.18);
                    d=min(d,length(turn(p-float2(-.13,.03),.8)*float2(1.7,1))-.18);
                } else if(e<10.5) {
                    float a=atan2(p.y,p.x),r=length(p);
                    d=abs(r-(.18+a*.028))-.025;
                    d=min(d,abs(p.y-.13*sin(p.x*7))-.02+max(0,abs(p.x)-.3));
                } else if(e<11.5) {
                    [unroll] for(int k=0;k<3;k++) {
                        float2 q=turn(p,k*1.0471976); d=min(d,segmentDistance(q,float2(-.33,0),float2(.33,0))-.018);
                        d=min(d,segmentDistance(q,float2(.16,0),float2(.25,.10))-.014);
                        d=min(d,segmentDistance(q,float2(-.16,0),float2(-.25,-.10))-.014);
                    }
                } else if(e<12.5) {
                    d=segmentDistance(p,float2(.12,.34),float2(-.12,-.02))-.035;
                    d=min(d,segmentDistance(p,float2(-.12,-.02),float2(.12,.03))-.035);
                    d=min(d,segmentDistance(p,float2(.12,.03),float2(-.11,-.33))-.035);
                } else if(e<13.5) {
                    d=min(abs(p.x)+abs(p.y)*.45-.14,abs(p.x)*.45+abs(p.y)-.14);
                } else {
                    d=max(length(p)-.32,-(length(p-float2(.13,.05))-.29));
                }
                return ink(d);
            }
            float bolt(float2 p,float seed)
            {
                float d=10; float2 a=float2(-.94,0);
                [unroll] for(int k=1;k<=12;k++) {
                    float2 b=float2(-.94+k*.15666,(hash(k+seed)-.5)*.62*sin(k*.261799));
                    d=min(d,segmentDistance(p,a,b)); a=b;
                }
                d=min(d,segmentDistance(p,float2(-.1,.02),float2(.18,.65)));
                d=min(d,segmentDistance(p,float2(.18,.65),float2(.42,.48)));
                return d;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float r=length(p),a=atan2(p.y,p.x),body=0,hot=0,aura=0;
                if(_Shape<.5) // authored spell seal, distinct emblem in its centre
                {
                    float2 q=turn(p,_Phase*.13);
                    float angle=atan2(q.y,q.x);
                    float ticks=step(.74,abs(cos(angle*12)));
                    float outer=ring(p,.82,.012)*(.35+.65*ticks);
                    float middle=ring(p,.67,.009)*step(.08,abs(sin(angle*3)));
                    float diamond=ink(abs(abs(q.x)+abs(q.y)-.82)-.009);
                    float reveal=1-smoothstep(_Progress*6.28318-.07,_Progress*6.28318+.07,angle+3.14159);
                    body=(outer+middle+diamond*.55)*reveal+glyph(p)*smoothstep(.1,.55,_Progress);
                    hot=glyph(p)*.7; aura=exp(-r*r*8)*.06;
                }
                else if(_Shape<1.5) // travelling wall of water with a rolled white crest
                {
                    float x=p.x*.5+.5;
                    float crest=.18+.43*sin(x*3.3+_Phase*.9);
                    float flow=tex2D(_NoiseTex,i.uv*float2(1.3,2.4)+float2(-_Phase*.35,_Phase*.12)).r;
                    float d=p.y-crest-(flow-.5)*.07;
                    body=ink(d)*smoothstep(-1,-.65,p.y)*(.26+.55*saturate((p.y+1)/1.7));
                    hot=ink(abs(d)-.045)+ring((p-float2(.2,.35))*float2(1,1.2),.26,.023)*step(.07,p.y);
                    aura=exp(-abs(d)*17)*.18;
                }
                else if(_Shape<2.5) // transparent spherical water cage; sparse meridians, not an opaque orb
                {
                    body=ring(p,.78,.026);
                    body+=ring(p*float2(2.4,1),.78,.018)+ring(p*float2(1,2.5),.78,.018);
                    body*=ink(r-.81);
                    float gloss=ink(abs(length(p-float2(-.12,.08))-.65)-.022)*step(.15,p.y);
                    body+=ink(r-.76)*.055+gloss*.6; hot=gloss;
                    aura=exp(-abs(r-.78)*30)*.15;
                }
                else if(_Shape<3.5) // tapered helices / tempest funnel
                {
                    float width=.18+.44*saturate((p.y+.8)/1.6);
                    float envelope=ink(abs(p.x)-width)*smoothstep(-.95,-.7,p.y)*(1-smoothstep(.65,.95,p.y));
                    float coils=abs(sin(p.y*16+_Phase*9+sin(p.x*3)*1.5));
                    body=envelope*(.06+.82*(1-smoothstep(.10,.28,coils)));
                    hot=body*smoothstep(-.1,.7,p.x);
                }
                else if(_Shape<4.5) // laser or widening dragon breath; local +X is the target
                {
                    float x=p.x*.5+.5;
                    float noise=tex2D(_NoiseTex,float2(x*2-_Phase*1.9,i.uv.y*2+_Phase*.3)).r;
                    float width=lerp(.085,.05+x*.67,saturate(_Variant));
                    float d=abs(p.y+sin(x*14-_Phase*13)*.018)-width*(.75+noise*.25);
                    float endMask=smoothstep(0,.04,x)*(1-smoothstep(.91,1,x));
                    body=ink(d)*endMask;
                    hot=ink(abs(p.y)-width*.27)*endMask*(.7+noise*.3);
                    aura=exp(-max(d,0)*20)*endMask*.24;
                }
                else if(_Shape<5.5) // earthquake, concentric discontinuous fissures
                {
                    float d=10;
                    [unroll] for(int k=0;k<7;k++) {
                        float2 q=turn(p,k*.8975979+_Phase*.02);
                        d=min(d,segmentDistance(q,0,float2(.35,.10)));
                        d=min(d,segmentDistance(q,float2(.35,.10),float2(.84,-.06)));
                        d=min(d,segmentDistance(q,float2(.35,.10),float2(.48,.35)));
                    }
                    body=ink(d-.024)*(1-smoothstep(_Progress-.03,_Progress+.03,r));
                    hot=ink(d-.006); aura=exp(-d*30)*.10;
                }
                else if(_Shape<6.5) // faceted prism, clean large surfaces
                {
                    float d=abs(p.x)*.8+abs(p.y)*.48-.39;
                    body=ink(d); hot=body*(.18+.45*step(0,p.x)+.3*step(.05,p.y));
                    body+=ink(abs(d)-.012)*.3; aura=exp(-max(d,0)*18)*.10;
                }
                else if(_Shape<7.5) // paired wing silhouette; six clearly grouped primary feathers
                {
                    float2 q=float2(abs(p.x),p.y);
                    [unroll] for(int k=0;k<6;k++) {
                        float f=k/5.0;
                        float2 start=float2(.04,-.15+f*.09);
                        float2 end=float2(.82-f*.38,.32+f*.46);
                        float width=.052+f*.008;
                        body=max(body,ink(segmentDistance(q,start,end)-width));
                    }
                    body*=smoothstep(0,.10,abs(p.x)); hot=body*saturate(p.y*.5+.3);
                }
                else if(_Shape<8.5) // roots and branching leaves, revealed upwards
                {
                    float d=10;
                    [unroll] for(int k=0;k<3;k++) {
                        float offset=(k-1)*.24;
                        float x=offset+sin(p.y*4+_Phase)*.07;
                        d=min(d,abs(p.x-x)-.022+max(0,abs(p.y)-.77));
                        float2 q=turn(p-float2(offset+.13,.12+(k-1)*.23),-.65);
                        body=max(body,leaf(q));
                        q=turn(p-float2(offset-.13,-.12+(k-1)*.23),.65);
                        body=max(body,leaf(q));
                    }
                    body=max(body,ink(d)); body*=1-smoothstep(_Progress*1.8-1,_Progress*1.8-.88,p.y);
                    hot=body*.35;
                }
                else if(_Shape<9.5) // opening blossom
                {
                    float edge=.34+.22*abs(cos(a*3));
                    body=ink(r-edge); hot=ink(r-.16)+body*.20;
                }
                else if(_Shape<10.5) // closing chains / clamp
                {
                    float2 q=turn(p,.785398);
                    float d=abs(max(abs(q.x),abs(q.y))-.57)-.023;
                    body=ink(d)+ring(p,.77,.018);
                    float links=step(.46,frac((p.x+p.y)*5+_Phase));
                    body+=ink(sdBox(q,float2(.76,.035)))*links*.65;
                    hot=ring(p,.77,.012)*.6;
                }
                else if(_Shape<11.5) // single feather / petal
                {
                    float2 q=p*float2(1.9,1);
                    float d=length(q)-.72;
                    body=ink(d); hot=ink(abs(p.x+p.y*.09)-.014)*ink(d);
                }
                else if(_Shape<12.5) // forked lightning
                {
                    float d=bolt(p,floor(_Phase*16));
                    body=ink(d-.032); hot=ink(d-.010); aura=exp(-d*24)*.65;
                }
                else if(_Shape<13.5) // reticle, corner brackets and focused cross
                {
                    float2 q=abs(p);
                    float corner=step(.37,q.x)*step(.37,q.y);
                    float d=min(abs(q.x-.64)+max(0,q.y-.65),abs(q.y-.64)+max(0,q.x-.65));
                    body=ink(d-.022)*corner+ring(p,.37,.012);
                    body+=ink(sdBox(p,float2(.15,.014)))+ink(sdBox(p,float2(.014,.15)));
                    hot=ring(p,.37,.008);
                }
                else // open tome / abyssal eye, centre seam and distinct paired pages
                {
                    float2 q=float2(abs(p.x),p.y);
                    float page=sdBox(q-float2(.32,.02),float2(.28,.35));
                    page=max(page,-p.y+q.x*.20-.34);
                    float edge=ink(abs(page)-.014);
                    body=edge+ink(page)*.07;
                    body+=ink(sdBox(p,float2(.014,.35)));
                    float eye=abs(length(p*float2(1,2.1))-.29)-.015;
                    body+=ink(eye); hot=ink(length(p*float2(2.8,1))-.10);
                }
                float alpha=saturate(body+aura)*_Alpha*_Color.a;
                float3 color=lerp(_Color.rgb,_Color2.rgb,saturate(hot))*_Intensity;
                return float4(color*alpha,alpha);
            }
            ENDCG
        }
    }
}
