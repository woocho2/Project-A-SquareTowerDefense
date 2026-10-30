using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 연출 그림을 이름으로 불러옴
    public static class EffectTextures
    {
        public const string SlashTrail = "FX_SlashTrail";
        public const string SlashLine = "FX_SlashLine";
        public const string SwordQi = "FX_SwordQi";
        public const string Glow = "FX_Glow";
        public const string Flare = "FX_Flare";
        public const string Spark = "FX_Spark";
        public const string Ring = "FX_Ring";
        public const string Noise = "FX_Noise";
        public const string PlumPetal = "FX_PlumPetal";
        public const string PlumBlossom = "FX_PlumBlossom";
        public const string Smoke = "FX_Smoke";
        public const string Snowflake = "FX_Snowflake";
        public const string IceShard = "FX_IceShard";
        public const string MeteorCore = "FX_MeteorCore";
        public const string MeteorBurst = "FX_MeteorBurst";
        public const string BlizzardCrystal = "FX_BlizzardCrystal";
        public const string BlackHoleVortex = "FX_BlackHoleVortex";
        public const string Fireball = "FX_Fireball";
        public const string Snowball = "FX_Snowball";

        private static readonly Dictionary<string, Texture2D> s_cache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> s_proceduralCache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> s_warned = new HashSet<string>();

        public static Texture2D Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return Texture2D.whiteTexture;
            if (s_cache.TryGetValue(name, out Texture2D cached) && cached != null) return cached;

            Texture2D texture = Resources.Load<Texture2D>("Effects/" + name);
            if (texture == null)
            {
                if (s_warned.Add(name)) Debug.Log($"[EffectTextures] Resources/Effects/{name}.png가 아직 없어 임시 그림을 씀");
                texture = GetProcedural(name);
            }
            s_cache[name] = texture;
            return texture;
        }

        // PNG 에셋을 읽지 않고 기존 수식으로 기본 도형을 만듭니다. 그림 캐시와 분리합니다.
        public static Texture2D GetProcedural(string name)
        {
            if (string.IsNullOrEmpty(name)) return Texture2D.whiteTexture;
            if (s_proceduralCache.TryGetValue(name, out Texture2D cached) && cached != null) return cached;
            Texture2D texture = MakeProcedural(name);
            s_proceduralCache[name] = texture;
            return texture;
        }

        private static Texture2D MakeProcedural(string name)
        {
            bool strip = name == SlashTrail || name == SlashLine || name == Spark;
            int width = strip ? 256 : 128;
            int height = strip ? 32 : 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name + " (Procedural)",
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
                    float taper = Mathf.Sin(u * Mathf.PI * 0.5f);
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
                case Smoke:
                {
                    float radius = p.magnitude;
                    float lumps = 0.5f + 0.25f * Mathf.Sin(p.x * 7f + p.y * 3f) + 0.25f * Mathf.Sin(p.y * 8f - p.x * 4f);
                    float a = Mathf.Exp(-radius * radius * 2.5f) * (1f - Mathf.SmoothStep(0.8f, 1f, radius)) * (0.55f + 0.45f * lumps);
                    return new Color(1f, 1f, 1f, a);
                }
                case Snowflake:
                {
                    float radius = p.magnitude;
                    float sector = Mathf.PI / 3f;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float local = angle - Mathf.Round(angle / sector) * sector;
                    float side = radius * Mathf.Sin(local);
                    float along = radius * Mathf.Cos(local);
                    float arm = Mathf.Exp(-side * side * 600f) * (1f - Mathf.SmoothStep(0.75f, 0.9f, along));
                    float twig = Mathf.Abs(along - 0.5f) < 0.2f ? Mathf.Exp(-Mathf.Pow(Mathf.Abs(side) - (along - 0.3f) * 0.6f, 2f) * 900f) : 0f;
                    float a = Mathf.Clamp01(arm + twig * (1f - Mathf.SmoothStep(0.65f, 0.72f, along)) + Mathf.Exp(-radius * radius * 40f));
                    return new Color(1f, 1f, 1f, a);
                }
                case IceShard:
                {
                    float d = Mathf.Abs(p.x) * 0.9f + Mathf.Abs(p.y) * 3f - 0.85f;
                    float a = 1f - Mathf.SmoothStep(-0.04f, 0.02f, d);
                    float shine = 0.7f + 0.3f * Mathf.Clamp01(p.y * 3f + 0.5f);
                    return new Color(shine, shine, shine, a);
                }
                default:
                    return new Color(1f, 1f, 1f, Mathf.Exp(-p.sqrMagnitude * 3f) * (1f - Mathf.SmoothStep(0.85f, 1f, p.magnitude)));
            }
        }

        private static float[] MakeNoise(int width, int height, System.Random random)
        {
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
