Shader "SquareTowerDefense/Rounded Character Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D) = "white" {}
        _Color ("Tint",Color) = (1,1,1,1)
        _CornerRadius ("Head Corner Radius (pixels)",Range(0,32)) = 8
        [HideInInspector] _HeadRect ("Head Pixels",Vector) = (16,16,224,224)
        [HideInInspector] _SpriteRect ("Original Sprite Pixels",Vector) = (0,0,256,256)
        [MaterialToggle] PixelSnap ("Pixel snap",Float) = 0
        [HideInInspector] _RendererColor ("RendererColor",Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha",2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha",Float) = 0
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
            #pragma fragment RoundedFrag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "CharacterCornerMask.cginc"
            float4 _MainTex_TexelSize, _SpriteRect, _HeadRect;
            float _CornerRadius;
            fixed4 RoundedFrag(v2f input) : SV_Target
            {
                fixed4 color=SampleSpriteTexture(input.texcoord)*input.color;
                float2 pixel=(input.texcoord*_MainTex_TexelSize.zw-_SpriteRect.xy)/_SpriteRect.zw*256;
                pixel.y=256-pixel.y;
                color.a*=CharacterCornerAlpha(pixel,_HeadRect,_CornerRadius);
                color.rgb*=color.a;
                return color;
            }
            ENDCG
        }
    }
}
