using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SquareTD
{
    // 정보 카드 오른쪽 위의 접힌 귀퉁이 — 누르면 다른 카드로 넘김 (타워·적 카드 ↔ 타일 카드, 2026-10-28)
    //   귀퉁이 그림은 코드로 그린 삼각형 (Codex 아이콘이 오면 Image의 sprite만 바꾸면 됨). 위에 작은 글자("타일" 등)
    public static class CardCorner
    {
        private const float Size = 46f;
        private static Sprite s_fold;

        public static Button Create(RectTransform card, string label, UnityAction onClick)
        {
            var corner = new GameObject("Btn_CardCorner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)corner.transform;
            rect.SetParent(card, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(Size, Size);
            rect.SetAsLastSibling();

            Image image = corner.GetComponent<Image>();
            image.sprite = FoldSprite();
            image.alphaHitTestMinimumThreshold = 0f;
            Button button = corner.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            TextMeshProUGUI sample = card.GetComponentInChildren<TextMeshProUGUI>(true);
            var text = new GameObject("Txt_CardCorner", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(rect, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = new Vector2(0.62f, 0.62f);           // 접힌 면(왼쪽 아래 삼각형) 안쪽
            text.rectTransform.offsetMin = new Vector2(2f, 2f);
            text.rectTransform.offsetMax = Vector2.zero;
            if (sample != null) text.font = sample.font;
            text.fontSize = 11f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.color = new Color(0.25f, 0.2f, 0.3f);
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        public static void SetLabel(Button corner, string label)
        {
            if (corner == null) return;
            TextMeshProUGUI text = corner.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = label;
        }

        // 오른쪽 위 귀퉁이를 접은 모양: 접힌 면 = 왼쪽 아래 삼각형 (접힌 선 쪽이 밝고, 두 변에 그림자 선)
        private static Sprite FoldSprite()
        {
            if (s_fold != null) return s_fold;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Card Corner Fold", wrapMode = TextureWrapMode.Clamp };
            var paper = new Color(0.97f, 0.93f, 0.84f);
            var shade = new Color(0.78f, 0.72f, 0.62f);
            var edge = new Color(0.45f, 0.38f, 0.3f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float fold = 1f - (u + v);                                   // 0 = 접힌 선, 클수록 왼쪽 아래 끝
                    if (fold < 0f) { texture.SetPixel(x, y, Color.clear); continue; }
                    Color color = Color.Lerp(paper, shade, Mathf.Clamp01(fold * 1.4f));
                    bool outline = fold < 0.035f || u < 0.035f || v < 0.035f;
                    texture.SetPixel(x, y, outline ? edge : color);
                }
            }
            texture.Apply();
            s_fold = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_fold;
        }
    }
}
