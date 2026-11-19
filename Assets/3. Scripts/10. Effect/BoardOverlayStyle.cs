using UnityEngine;

namespace SquareTD
{
    // 사거리 격자와 미배치 존 경고가 공유하는 절차형 보드 표시
    public static class BoardOverlayStyle
    {
        private static Sprite s_quad;
        private static Material s_range, s_warning;
        public static Sprite Quad
        {
            get
            {
                if (s_quad == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    s_quad = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, texture.width, 0, SpriteMeshType.FullRect);
                    s_quad.name = "Board Overlay Quad"; s_quad.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_quad;
            }
        }
        public static Material Material(bool warning)
        {
            if (warning ? s_warning != null : s_range != null) return warning ? s_warning : s_range;
            Shader shader = Resources.Load<Shader>("BoardOverlay");
            if (shader == null || !shader.isSupported) return null;
            var material = new Material(shader) { name = warning ? "Zone Placement Warning" : "Tower Range Grid", hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_Warning", warning ? 1 : 0);
            if (warning) s_warning = material; else s_range = material;
            return material;
        }
    }
}
