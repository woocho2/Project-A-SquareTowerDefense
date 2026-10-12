Shader "SquareTowerDefense/Enemy Equipment Palette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ArmorColor ("Dark Equipment", Color) = (0.72,0.19,0.25,1)
        _GemColor ("Jewels", Color) = (1,0.39,0.49,1)
        [Toggle] _SquareHead ("Square Head Layout", Float) = 0
        _CornerRadius ("Head Corner Radius (pixels)",Range(0,32)) = 8
        [HideInInspector] _HeadRect ("Head Pixels",Vector) = (16,16,224,224)
        [HideInInspector] _FaceIndex ("Face", Float) = 0
        [HideInInspector] _SpriteRect ("Original Sprite Pixels", Vector) = (0,0,256,256)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment EquipmentFrag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "CharacterCornerMask.cginc"
            float4 _MainTex_TexelSize;
            float4 _SpriteRect;
            float _FaceIndex;
            float _SquareHead;
            float _CornerRadius;
            float4 _HeadRect;
            fixed4 _ArmorColor, _GemColor;

            float InBox(float2 p, float4 box)
            {
                return step(box.x,p.x)*step(box.y,p.y)*step(p.x,box.z)*step(p.y,box.w);
            }
            fixed4 EquipmentFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord);
                float3 original = color.rgb;
                float3 armor = _ArmorColor.rgb;
                float3 gem = _GemColor.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    original = LinearToGammaSpace(original);
                    armor = LinearToGammaSpace(armor);
                    gem = LinearToGammaSpace(gem);
                #endif
                // Classify before tint/shading. Coordinates remain local to the original
                // face even when it is cropped, mirrored, compressed or atlas sliced.
                float2 p = (input.texcoord * _MainTex_TexelSize.zw - _SpriteRect.xy) / _SpriteRect.zw * 256;
                p.y = 256 - p.y;
                color.a *= CharacterCornerAlpha(p,_HeadRect,_CornerRadius);
                float faceIndex = _FaceIndex;
                if (faceIndex > 4.5) { p.x = 256 - p.x; faceIndex = 1; }
                float jewelArea = 0;
                float equipmentArea = 0;
                if (faceIndex < 0.5)
                {
                    equipmentArea = max(InBox(p,float4(55,0,202,94)),
                        max(InBox(p,float4(0,0,61,130)),InBox(p,float4(193,0,256,130))));
                    jewelArea = InBox(p,float4(111,86,144,123));
                }
                else if (faceIndex < 1.5)
                {
                    equipmentArea = max(InBox(p,float4(0,0,148,96)),InBox(p,float4(137,0,256,127)));
                    jewelArea = max(InBox(p,float4(32,87,58,127)),InBox(p,float4(139,88,160,123)));
                }
                else if (faceIndex < 2.5)
                {
                    equipmentArea = max(InBox(p,float4(55,0,202,95)),
                        max(InBox(p,float4(0,0,61,126)),InBox(p,float4(193,0,256,126))));
                    jewelArea = max(InBox(p,float4(42,87,64,119)),InBox(p,float4(191,87,214,119)));
                }
                else if (faceIndex < 3.5)
                {
                    equipmentArea = max(InBox(p,float4(50,0,206,176)),
                        max(InBox(p,float4(0,0,52,139)),InBox(p,float4(204,0,256,139))));
                    jewelArea = InBox(p,float4(111,167,145,199));
                }
                else
                {
                    equipmentArea = step(p.y,77);
                    jewelArea = InBox(p,float4(116,30,147,60));
                }
                if (_SquareHead > 0.5)
                {
                    if (faceIndex < 0.5)
                    {
                        equipmentArea = step(p.y,125);
                        jewelArea = max(InBox(p,float4(105,32,151,108)),
                            max(InBox(p,float4(16,48,43,112)),InBox(p,float4(213,48,240,112))));
                    }
                    else if (faceIndex < 1.5)
                    {
                        equipmentArea = step(p.y,130);
                        jewelArea = max(InBox(p,float4(0,36,35,113)),InBox(p,float4(95,38,164,112)));
                    }
                    else if (faceIndex < 2.5)
                    {
                        equipmentArea = step(p.y,140);
                        jewelArea = max(InBox(p,float4(96,36,160,148)),
                            max(InBox(p,float4(16,48,48,116)),InBox(p,float4(208,48,240,116))));
                    }
                    else if (faceIndex < 3.5)
                    {
                        equipmentArea = InBox(p,float4(30,16,226,230));
                        jewelArea = InBox(p,float4(104,192,154,242));
                    }
                    else
                    {
                        equipmentArea = step(p.y,53);
                        jewelArea = InBox(p,float4(109,12,151,48));
                    }
                }
                float value = max(original.r,max(original.g,original.b));
                float minimum = min(original.r,min(original.g,original.b));
                float saturation = (value-minimum)/max(value,0.001);
                // Navy equipment only; purple feathers, pale hair, gold and ink stay intact.
                float armorMask = equipmentArea*step(0.18,saturation)*step(0.08,value)*step(value,0.72)
                    *step(original.r+0.035,original.b)*step(original.g+0.012,original.b)*step(original.r-original.g,0.025);
                float gemMask = jewelArea*step(0.3,saturation)*step(0.12,value)
                    *step(original.g*1.22,original.r)*step(original.g*1.3,original.b);
                float3 result = lerp(original,armor*clamp(value/0.42,0.12,1.18),armorMask);
                result = lerp(result,gem*clamp(value/0.8,0.25,1.2),gemMask);
                #ifndef UNITY_COLORSPACE_GAMMA
                    result = GammaToLinearSpace(result);
                #endif
                color.rgb = result;
                color *= input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
