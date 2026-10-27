using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 연출 그림(텍스처 에셋) 찾기: Resources/Effects/FX_*.png를 이름으로 불러옴
    //   아직 없으면 코드로 임시 그림을 만들어 씀 (Codex의 그림이 들어오면 자동으로 그쪽을 씀)
    public static class EffectTextures
    {
        public const string SlashTrail = "FX_SlashTrail";    // 가로 띠: 왼쪽 꼬리(흐림) → 오른쪽 머리(밝고 날카로움)
        public const string SlashLine = "FX_SlashLine";      // 가로 빛줄기: 가운데 밝고 양 끝 뾰족
        public const string SwordQi = "FX_SwordQi";          // 초승달 검기: 둥근 쪽이 오른쪽
        public const string Glow = "FX_Glow";                // 동그란 부드러운 빛
        public const string Flare = "FX_Flare";              // 4갈래 별빛
        public const string Spark = "FX_Spark";              // 가로로 긴 불티: 오른쪽이 머리
        public const string Ring = "FX_Ring";                // 가는 고리
        public const string Noise = "FX_Noise";              // 이어 붙는 구름 무늬 (회색)
        public const string PlumPetal = "FX_PlumPetal";      // 매화 꽃잎 한 장 (색 있음, 끝이 위)
        public const string PlumBlossom = "FX_PlumBlossom";  // 매화 한 송이 (색 있음)

        private static readonly Dictionary<string, Texture2D> s_cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> s_warned = new HashSet<string>();

        public static Texture2D Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return Texture2D.whiteTexture;
            if (s_cache.TryGetValue(name, out Texture2D cached) && cached != null) return cached;

            Texture2D texture = Resources.Load<Texture2D>("Effects/" + name);
            if (texture == null)
            {
                if (s_warned.Add(name)) Debug.Log($"[EffectTextures] Resources/Effects/{name}.png가 아직 없어 임시 그림을 씀");
                texture = MakePlaceholder(name);
            }
            s_cache[name] = texture;
            return texture;
        }

        // ── 임시 그림 (에셋이 오기 전에 연출을 시험하려고) ──
        private static Texture2D MakePlaceholder(string name)
        {
            bool strip = name == SlashTrail || name == SlashLine || name == Spark;
            int width = strip ? 256 : 128;
            int height = strip ? 32 : 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name + " (임시)",
                wrapMode = name == Noise ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color[width * height];
            var random = new System.Random(name.GetHashCode());
            float[] noise = name == Noise ? MakeNoise(width, height, random) : null;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    Vector2 p = new Vector2(u * 2f - 1f, v * 2f - 1f);
                    pixels[y * width + x] = Sample(name, u, v, p, noise, x, y, width);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Color Sample(string name, float u, float v, Vector2 p, float[] noise, int x, int y, int width)
        {
            float across = Mathf.Abs(v * 2f - 1f);
            switch (name)
            {
                case SlashTrail:
                {
                    float taper = Mathf.Sin(u * Mathf.PI * 0.5f);                          // 꼬리는 가늘고 머리는 굵게
                    float body = 1f - Mathf.SmoothStep(0f, 1f, across / Mathf.Max(0.05f, taper));
                    float head = Mathf.SmoothStep(0.7f, 1f, u) * (1f - Mathf.SmoothStep(0.97f, 1f, u));
                    float a = body * Mathf.Pow(u, 1.5f);
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(a + head * body * 0.5f));
                }
                case SlashLine:
                {
                    float taper = Mathf.Sin(u * Mathf.PI);
                    float a = (1f - Mathf.SmoothStep(0f, 1f, across / Mathf.Max(0.05f, taper))) * taper;
                    return new Color(1f, 1f, 1f, a);
                }
                case Spark:
                {
                    float a = Mathf.Exp(-across * across * 8f) * u * (1f - Mathf.SmoothStep(0.9f, 1f, u));
                    return new Color(1f, 1f, 1f, a);
                }
                case SwordQi:
                {
                    float outer = p.magnitude - 0.62f;
                    float inner = (p - new Vector2(-0.28f, 0f)).magnitude - 0.56f;
                    float d = Mathf.Max(outer, -inner);
                    float a = Mathf.Clamp01((1f - Mathf.SmoothStep(-0.02f, 0.02f, d)) + Mathf.Exp(-Mathf.Max(d, 0f) * 9f) * 0.5f);
                    return new Color(1f, 1f, 1f, a);
                }
                case Flare:
                {
                    float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y);
                    float star = Mathf.Exp(-ax * 14f) * Mathf.Exp(-ay * 3f) + Mathf.Exp(-ay * 14f) * Mathf.Exp(-ax * 3f);
                    float center = Mathf.Exp(-p.sqrMagnitude * 16f);
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(star + center));
                }
                case Ring:
                {
                    float d = Mathf.Abs(p.magnitude - 0.75f);
                    return new Color(1f, 1f, 1f, Mathf.Exp(-d * d * 300f) * (1f - Mathf.SmoothStep(0.95f, 1f, p.magnitude)));
                }
                case Noise:
                {
                    float n = noise[y * width + x];
                    return new Color(n, n, n, 1f);
                }
                case PlumPetal:
                {
                    Vector2 q = new Vector2(p.x * (1.25f - p.y * 0.45f), p.y * 0.9f);
                    float shape = q.magnitude - 0.78f;
                    float notch = 0.18f - new Vector2(p.x, (p.y - 0.86f) * 1.3f).magnitude;
                    float a = 1f - Mathf.SmoothStep(-0.02f, 0.02f, Mathf.Max(shape, notch));
                    Color pink = Color.Lerp(new Color(1f, 0.55f, 0.72f), new Color(1f, 0.95f, 0.97f), Mathf.Clamp01(0.55f - p.y * 0.45f));
                    pink.a = a;
                    return pink;
                }
                case PlumBlossom:
                {
                    float radius = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float edge = 0.48f + 0.34f * Mathf.Abs(Mathf.Cos(angle * 2.5f));
                    float a = 1f - Mathf.SmoothStep(-0.02f, 0.02f, radius - edge);
                    Color color = radius < 0.16f ? new Color(1f, 0.85f, 0.35f)
                        : Color.Lerp(new Color(1f, 0.92f, 0.95f), new Color(1f, 0.5f, 0.7f), radius / Mathf.Max(0.01f, edge));
                    color.a = a;
                    return color;
                }
                default:                                                                    // Glow와 모르는 이름
                    return new Color(1f, 1f, 1f, Mathf.Exp(-p.sqrMagnitude * 3f) * (1f - Mathf.SmoothStep(0.85f, 1f, p.magnitude)));
            }
        }

        private static float[] MakeNoise(int width, int height, System.Random random)
        {
            // 이어 붙는 값 노이즈: 8×8 격자 값을 부드럽게 섞음 (가장자리를 감싸서 타일링)
            const int grid = 8;
            var values = new float[grid * grid];
            for (int i = 0; i < values.Length; i++) values[i] = (float)random.NextDouble();
            var result = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float gx = x / (float)width * grid, gy = y / (float)height * grid;
                    int x0 = (int)gx, y0 = (int)gy;
                    float tx = Mathf.SmoothStep(0f, 1f, gx - x0), ty = Mathf.SmoothStep(0f, 1f, gy - y0);
                    float a = values[(y0 % grid) * grid + x0 % grid], b = values[(y0 % grid) * grid + (x0 + 1) % grid];
                    float c = values[((y0 + 1) % grid) * grid + x0 % grid], d = values[((y0 + 1) % grid) * grid + (x0 + 1) % grid];
                    result[y * width + x] = Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
                }
            }
            return result;
        }
    }
}
