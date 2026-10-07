using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Offline pixel processing only; this file is outside Unity's Assets folder.
// Inputs are preserved snapshots, so repeated builds never recolor an output.
public static class RealmTileAtlasBuilder
{
    public static readonly string[] Realms = {
        "Asgard", "Alfheim", "Vanaheim", "Midgard", "Jotunheim",
        "Nidavellir", "Niflheim", "Muspelheim", "Hel", "Ragnarok"
    };
    static readonly int[] Positions = { 0, 1, 2, 4, 5, 6, 8, 9, 10, 3 };
    static readonly Rectangle[] Arrows = {
        Rectangle.FromLTRB(98, 15, 158, 59), Rectangle.FromLTRB(166, 40, 220, 93),
        Rectangle.FromLTRB(199, 97, 246, 159), Rectangle.FromLTRB(164, 162, 218, 215),
        Rectangle.FromLTRB(97, 194, 159, 242), Rectangle.FromLTRB(38, 161, 92, 216),
        Rectangle.FromLTRB(9, 97, 56, 159), Rectangle.FromLTRB(36, 39, 90, 92)
    };
    static Color Hex(string hex) { return ColorTranslator.FromHtml("#" + hex); }
    static Color Mix(Color a, Color b, double t, int alpha)
    {
        t = Math.Max(0, Math.Min(1, t));
        return Color.FromArgb(alpha, (int)Math.Round(a.R + (b.R - a.R) * t),
            (int)Math.Round(a.G + (b.G - a.G) * t), (int)Math.Round(a.B + (b.B - a.B) * t));
    }
    static Color Ramp(Color c, string dark, string mid, string light, double t)
    {
        return t < .5 ? Mix(Hex(dark), Hex(mid), t * 2, c.A)
            : Mix(Hex(mid), Hex(light), (t - .5) * 2, c.A);
    }
    static Color Frame(Color c, int realm, double t)
    {
        switch (realm) {
            case 0: return Ramp(c, "5B350C", "DC9D1C", "FFDD6A", t);
            case 1: return Ramp(c, "391D11", "844F26", "CB914A", t);
            case 4: return Ramp(c, "31353C", "6F7985", "D1D9E1", t);
            case 5: return Ramp(c, "27133E", "7D41B7", "DEA8FF", t);
            case 8: return Ramp(c, "082F31", "149289", "79F0D8", t);
            case 9: return Ramp(c, "4E473A", "9A8760", "D5C9A6", t); // Asgard's gold, faded to antique gold.
            default: return c;
        }
    }
    static double Luma(Color c) { return .2126 * c.R + .7152 * c.G + .0722 * c.B; }
    static Color Floor(Color c, int realm, bool spawn)
    {
        double lum = Luma(c);
        if (realm == 8 && !spawn) {
            return Ramp(c, "020405", "06090B", "101417", (lum - 35) / 28);
        }
        if (realm == 5) {
            // Plain violet recolor: retain the base shading without added motifs/veins.
            double t = spawn ? (lum - 215) / 40 : (lum - 58) / 27;
            t = Math.Max(.12, Math.Min(.86, t));
            return spawn ? Ramp(c, "766195", "B39AD4", "E6D4FF", t)
                : Ramp(c, "20112F", "452561", "765098", t);
        }
        return c;
    }
    static Color FloorBaseColor(Bitmap source, int realm, bool spawn)
    {
        // Bright concept = tower-spawn floor; dark concept = enemy-path floor.
        // Keep these pairs explicit so rebuilding cannot restore an older palette.
        switch (realm) {
            case 1: return Hex(spawn ? "F4F8EB" : "6B4933"); // Leaf-tinted white / fallen leaves.
            case 2: return Hex(spawn ? "EFF7F0" : "254B35"); // Green-tinted white / deep forest green.
            case 3: return Hex(spawn ? "EDF8F5" : "15364F"); // Jade-tinted white / deep sea.
            case 4: return Hex(spawn ? "F3F5F5" : "35383C"); // Snow / dark mountain rock.
            case 5: if (spawn) return Hex("F4EFF9"); break; // Amethyst-tinted white.
            case 7: return Hex(spawn ? "FFF5EC" : "88421F"); // Orange-tinted white / deep lava orange.
            case 9: return Hex(spawn ? "DCD8CA" : "424B58"); // Ruined Asgard: dusty ivory / ash-toned navy.
        }
        // Average a clean interior patch for its palette only. Do not enlarge
        // its grain, mottling or compression marks into the new floor surface.
        long red = 0, green = 0, blue = 0;
        int count = 0;
        for (int y = 80; y <= 175; y++) for (int x = 80; x <= 175; x++) {
            Color c = Floor(source.GetPixel(x, y), realm, spawn);
            red += c.R; green += c.G; blue += c.B; count++;
        }
        return Color.FromArgb((int)Math.Round(red / (double)count),
            (int)Math.Round(green / (double)count), (int)Math.Round(blue / (double)count));
    }
    static Color SmoothFloor(Color baseColor, int x, int y, int alpha)
    {
        // Just one gentle diagonal light falloff: at most four channel levels
        // across a tile, without any texture, symbols, veins or random noise.
        double shade = 2.0 * (255 - x - y) / 255;
        return Color.FromArgb(alpha,
            Math.Max(0, Math.Min(255, (int)Math.Round(baseColor.R + shade))),
            Math.Max(0, Math.Min(255, (int)Math.Round(baseColor.G + shade))),
            Math.Max(0, Math.Min(255, (int)Math.Round(baseColor.B + shade))));
    }
    static Bitmap Slice(Bitmap source, int realm, bool spawnHalf = false)
    {
        int p = Positions[realm];
        Bitmap b = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
        // SetPixel preserves straight alpha channel values; bitmap cloning does not.
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            b.SetPixel(x, y, source.GetPixel((spawnHalf ? 1024 : 0) + (p % 4) * 256 + x, (p / 4) * 256 + y));
        return b;
    }
    static int[] NearestFace(Bitmap master)
    {
        int[] nearest = new int[256 * 256];
        Queue<int> pending = new Queue<int>();
        for (int i = 0; i < nearest.Length; i++) {
            Color c = master.GetPixel(i % 256, i / 256);
            bool face = c.A > 0 && c.B > c.R + 5;
            nearest[i] = face ? i : -1;
            if (face) pending.Enqueue(i);
        }
        while (pending.Count > 0) {
            int i = pending.Dequeue(), x = i % 256, y = i / 256;
            int[] adjacent = { x > 0 ? i - 1 : -1, x < 255 ? i + 1 : -1,
                y > 0 ? i - 256 : -1, y < 255 ? i + 256 : -1 };
            foreach (int n in adjacent) if (n >= 0 && nearest[n] < 0) {
                nearest[n] = nearest[i]; pending.Enqueue(n);
            }
        }
        return nearest;
    }
    static void Save(Bitmap bitmap, string path) { bitmap.Save(path, ImageFormat.Png); }

    public static void Build(string sourceRoot, string targetRoot, string previewRoot)
    {
        Directory.CreateDirectory(targetRoot);
        Directory.CreateDirectory(previewRoot);
        using (Bitmap pathAtlas = new Bitmap(Path.Combine(sourceRoot, "Tile_Path_Atlas.png")))
        using (Bitmap spawnAtlas = new Bitmap(Path.Combine(sourceRoot, "Tile_TowerSpawn_Atlas.png")))
        using (Bitmap master = Slice(pathAtlas, 0))
        using (Bitmap labelTemplate = new Bitmap(Path.Combine(sourceRoot, "Tile_Path_Overlay_Asgard_Atlas.png")))
        using (Bitmap floors = new Bitmap(2048, 768, PixelFormat.Format32bppArgb))
        using (Bitmap contact = new Bitmap(1260, 10 * 180 + 80))
        using (Graphics g = Graphics.FromImage(contact))
        using (Font font = new Font("Segoe UI", 13))
        using (Font small = new Font("Segoe UI", 11)) {
            int[] nearest = NearestFace(master);
            g.Clear(Hex("19212C"));
            g.DrawString("REALM TILE LAYERS / ORIGINAL -> UPDATED", font, Brushes.White, 16, 12);
            string[] titles = { "Original path", "Updated path", "Path floor", "Frame + arrow", "Original spawn", "Updated spawn" };
            for (int j = 0; j < titles.Length; j++) g.DrawString(titles[j], small, Brushes.LightGray, 164 + j * 180, 48);
            for (int realm = 0; realm < Realms.Length; realm++) {
                // Niflheim inherits Jotunheim's complete previous tile palette.
                int sourceRealm = realm == 6 ? 4 : realm;
                using (Bitmap originalPath = Slice(pathAtlas, realm))
                using (Bitmap originalSpawn = Slice(spawnAtlas, realm))
                using (Bitmap path = Slice(pathAtlas, sourceRealm))
                using (Bitmap spawn = Slice(spawnAtlas, sourceRealm))
                using (Bitmap floor = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
                using (Bitmap spawnFloor = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
                using (Bitmap frame = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
                using (Bitmap directionSource = new Bitmap(Path.Combine(sourceRoot, "Tile_Path_Overlay_" + Realms[realm] + "_Atlas.png")))
                using (Bitmap directions = new Bitmap(1024, 768, PixelFormat.Format32bppArgb)) {
                    Color pathBase = FloorBaseColor(path, realm, false);
                    Color spawnBase = FloorBaseColor(spawn, realm, true);
                    for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
                        int i = y * 256 + x, n = nearest[i];
                        Color silhouette = path.GetPixel(x, y);
                        if (silhouette.A == 0) continue;
                        floor.SetPixel(x, y, SmoothFloor(pathBase, x, y, silhouette.A));
                        spawnFloor.SetPixel(x, y, SmoothFloor(spawnBase, x, y, silhouette.A));
                        if (n != i) {
                            Color c = path.GetPixel(x, y);
                            Color m = master.GetPixel(x, y);
                            frame.SetPixel(x, y, Frame(c, realm, (Luma(m) - 55) / 176));
                        }
                    }
                    for (int tile = 0; tile < 12; tile++) {
                        int ox = tile % 4 * 256, oy = tile / 4 * 256;
                        int arrow = tile;
                        bool isArrow = arrow < 8;
                        int dx = isArrow ? 128 - (Arrows[arrow].Left + Arrows[arrow].Width / 2) : 0;
                        int dy = isArrow ? 128 - (Arrows[arrow].Top + Arrows[arrow].Height / 2) : 0;
                        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
                            Color result = frame.GetPixel(x, y);
                            int sx = x - dx, sy = y - dy;
                            Color c = isArrow && !Arrows[arrow].Contains(sx, sy) ? Color.Transparent
                                : directionSource.GetPixel(isArrow ? arrow % 4 * 256 + sx : ox + x,
                                    isArrow ? arrow / 4 * 256 + sy : oy + y);
                            bool keep;
                            if (isArrow) keep = Arrows[arrow].Contains(sx, sy);
                            else if (tile == 10) keep = false; // Former JUMP slot: empty themed frame.
                            else {
                                bool gold = c.R > c.B + 20 && c.G > c.B + 12;
                                bool letters = Math.Min(c.R, Math.Min(c.G, c.B)) > 170
                                    && Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) < 45;
                                // Midgard's blue rim is not gold; use the shared original
                                // word-plate geometry to retain its themed border too.
                                Color template = labelTemplate.GetPixel(ox + x, oy + y);
                                bool blueRim = realm == 3 && template.A > 0
                                    && template.R > template.B + 20 && template.G > template.B + 12;
                                keep = gold || letters || blueRim;
                            }
                            if (keep && c.A > 0) {
                                bool white = !isArrow && Math.Min(c.R, Math.Min(c.G, c.B)) > 170
                                    && Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) < 45;
                                if (!white) {
                                    double t = (Luma(c) - 55) / 176;
                                    if (realm == 6) c = Ramp(c, "274C61", "5895A9", "BAE3EE", t);
                                    else c = Frame(c, realm, t);
                                }
                                result = Over(c, result);
                            }
                            if (result.A > 0) directions.SetPixel(ox + x, oy + y, result);
                        }
                    }
                    DrawStartArrow(directions, FramePalette(frame));
                    int p = Positions[realm];
                    for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
                        floors.SetPixel(p % 4 * 256 + x, p / 4 * 256 + y, floor.GetPixel(x, y));
                        floors.SetPixel(1024 + p % 4 * 256 + x, p / 4 * 256 + y, spawnFloor.GetPixel(x, y));
                    }
                    Save(directions, Path.Combine(targetRoot, "Tile_Path_Overlay_" + Realms[realm] + "_Atlas.png"));
                    Save(frame, Path.Combine(previewRoot, Realms[realm] + "-frame.png"));
                    int rowY = 80 + realm * 180;
                    g.DrawString(Realms[realm], font, Brushes.White, 12, rowY + 65);
                    Draw(g, originalPath, 160, rowY);
                    using (Bitmap combined = Composite(floor, directions, 0)) {
                        Draw(g, combined, 340, rowY);
                        Save(combined, Path.Combine(previewRoot, Realms[realm] + "-path.png"));
                    }
                    using (Bitmap start = Composite(floor, directions, 8))
                        Save(start, Path.Combine(previewRoot, Realms[realm] + "-start.png"));
                    Draw(g, floor, 520, rowY);
                    Draw(g, directions, new Rectangle(0, 0, 256, 256), 700, rowY);
                    Draw(g, originalSpawn, 880, rowY);
                    Draw(g, spawnFloor, 1060, rowY);
                }
            }
            Save(floors, Path.Combine(targetRoot, "Tile_Floor_Atlas.png"));
            Save(contact, Path.Combine(previewRoot, "realm-comparison.png"));
            RenderFinal(floors, targetRoot, previewRoot);
        }
    }
    static void RenderFinal(Bitmap floors, string targetRoot, string previewRoot)
    {
        int[] selected = { 0, 1, 8, 4, 6, 5 };
        using (Bitmap b = new Bitmap(1200, 600))
        using (Graphics g = Graphics.FromImage(b))
        using (Font title = new Font("Segoe UI", 17))
        using (Font label = new Font("Segoe UI", 11)) {
            g.Clear(Hex("19212C"));
            for (int index = 0; index < selected.Length; index++) {
                int realm = selected[index], x = index % 3 * 400 + 14, y = index / 3 * 300 + 12;
                g.DrawString(Realms[realm], title, Brushes.White, x, y);
                g.DrawString("Path", label, Brushes.LightGray, x, y + 42);
                g.DrawString("Spawn", label, Brushes.LightGray, x + 128, y + 42);
                g.DrawString("Start", label, Brushes.LightGray, x + 256, y + 42);
                using (Bitmap path = Slice(floors, realm))
                using (Bitmap directions = new Bitmap(Path.Combine(targetRoot, "Tile_Path_Overlay_" + Realms[realm] + "_Atlas.png")))
                using (Bitmap combined = Composite(path, directions, 2))
                using (Bitmap start = Composite(path, directions, 8)) {
                    int p = Positions[realm];
                    g.DrawImage(combined, new Rectangle(x, y + 72, 120, 120));
                    g.DrawImage(floors, new Rectangle(x + 128, y + 72, 120, 120),
                        new Rectangle(1024 + p % 4 * 256, p / 4 * 256, 256, 256), GraphicsUnit.Pixel);
                    g.DrawImage(start, new Rectangle(x + 256, y + 72, 120, 120));
                }
            }
            Save(b, Path.Combine(previewRoot, "updated-realms.png"));
        }
        using (Bitmap sheet = new Bitmap(1120, 520))
        using (Graphics g = Graphics.FromImage(sheet))
        using (Font label = new Font("Segoe UI", 12)) {
            g.Clear(Hex("19212C"));
            for (int realm = 0; realm < Realms.Length; realm++) {
                int x = realm % 5 * 224 + 12, y = realm / 5 * 260 + 8;
                g.DrawString(Realms[realm], label, Brushes.White, x, y);
                using (Bitmap start = new Bitmap(Path.Combine(previewRoot, Realms[realm] + "-start.png")))
                    g.DrawImage(start, new Rectangle(x, y + 32, 200, 200));
            }
            Save(sheet, Path.Combine(previewRoot, "start-all-realms.png"));
        }
        RenderDecoratedFloorPairs(floors, targetRoot, previewRoot);
    }
    static void RenderDecoratedFloorPairs(Bitmap floors, string targetRoot, string previewRoot)
    {
        string[] names = { "아스가르드", "알프헤임", "바나헤임", "미드가르드", "요툰헤임",
            "니다벨리르", "니플헤임", "무스펠헤임", "헬", "라그나로크" };
        using (Bitmap sheet = new Bitmap(1200, 520))
        using (Graphics g = Graphics.FromImage(sheet))
        using (Font label = new Font("Malgun Gothic", 13))
        using (Font small = new Font("Malgun Gothic", 10)) {
            g.Clear(Hex("19212C"));
            for (int realm = 0; realm < Realms.Length; realm++) {
                int x = realm % 5 * 240 + 12, y = realm / 5 * 260 + 10;
                g.DrawString(names[realm], label, Brushes.White, x, y);
                g.DrawString("타워 스폰", small, Brushes.LightGray, x, y + 38);
                g.DrawString("패스", small, Brushes.LightGray, x + 112, y + 38);
                using (Bitmap pathFloor = Slice(floors, realm))
                using (Bitmap spawnFloor = Slice(floors, realm, true))
                using (Bitmap direction = new Bitmap(Path.Combine(targetRoot, "Tile_Path_Overlay_" + Realms[realm] + "_Atlas.png")))
                using (Bitmap path = Composite(pathFloor, direction, 10))
                using (Bitmap spawn = Composite(spawnFloor, direction, 10)) {
                    g.DrawImage(spawn, new Rectangle(x, y + 70, 100, 100));
                    g.DrawImage(path, new Rectangle(x + 112, y + 70, 100, 100));
                }
            }
            Save(sheet, Path.Combine(previewRoot, "decorated-floor-pairs.png"));
        }
    }
    static Color Over(Color top, Color bottom)
    {
        if (top.A == 255 || bottom.A == 0) return top;
        double a = top.A / 255.0, b = bottom.A / 255.0 * (1 - a), total = a + b;
        return Color.FromArgb((int)Math.Round(total * 255),
            (int)Math.Round((top.R * a + bottom.R * b) / total),
            (int)Math.Round((top.G * a + bottom.G * b) / total),
            (int)Math.Round((top.B * a + bottom.B * b) / total));
    }
    static Color[] FramePalette(Bitmap frame)
    {
        // Sample the actual themed ornament instead of defaulting some realms to gold.
        var colors = new List<Color>();
        for (int y = 0; y < frame.Height; y++) for (int x = 0; x < frame.Width; x++) {
            Color c = frame.GetPixel(x, y);
            if (c.A > 240) colors.Add(c);
        }
        colors.Sort((a, b) => Luma(a).CompareTo(Luma(b)));
        return new[] { colors[colors.Count / 10], colors[colors.Count / 2], colors[colors.Count * 9 / 10] };
    }
    static Color ArrowTint(Color c, Color[] palette)
    {
        double t = (Luma(c) - 55) / 176;
        return t < .5 ? Mix(palette[0], palette[1], t * 2, c.A)
            : Mix(palette[1], palette[2], (t - .5) * 2, c.A);
    }
    static void DrawStartArrow(Bitmap target, Color[] palette)
    {
        // Leave the original START word plate intact, matching END/RETURN.
        // The long line ends in one fin above its baseline; nothing points below it.
        const int baseline = 201;
        for (int y = 195; y <= baseline; y++) for (int x = 45; x <= 186; x++) {
            Color c = y == 195 || y == baseline || x == 45 ? Hex("90651F")
                : y == 196 ? Hex("F5D57E") : y == 200 ? Hex("BA8126") : Hex("D9A443");
            target.SetPixel(x, 512 + y, ArrowTint(c, palette));
        }
        // Sketch silhouette: apex (177,176), rear foot (185,201), tip (217,201).
        for (int y = 176; y <= baseline; y++) {
            double t = (y - 176) / 25.0;
            double left = 177 + 8 * t, right = 177 + 40 * t;
            for (int x = (int)Math.Floor(left); x <= (int)Math.Ceiling(right); x++) {
                double coverage = Math.Max(0, Math.Min(x + 1, right + .5) - Math.Max(x, left - .5));
                if (coverage <= 0) continue;
                Color c = y == baseline || x - left < 1 || right - x < 1 ? Hex("90651F")
                    : x - left < 3 ? Hex("F5D57E") : Mix(Hex("E6BA58"), Hex("D9A443"), t, 255);
                c = ArrowTint(Color.FromArgb((int)Math.Round(coverage * 255), c.R, c.G, c.B), palette);
                target.SetPixel(x, 512 + y, Over(c, target.GetPixel(x, 512 + y)));
            }
        }
    }
    static Bitmap Composite(Bitmap floor, Bitmap directions, int tile)
    {
        Bitmap b = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            b.SetPixel(x, y, Over(directions.GetPixel(tile % 4 * 256 + x, tile / 4 * 256 + y), floor.GetPixel(x, y)));
        return b;
    }
    static void Draw(Graphics g, Bitmap b, int x, int y) { Draw(g, b, new Rectangle(0, 0, b.Width, b.Height), x, y); }
    static void Draw(Graphics g, Bitmap b, Rectangle src, int x, int y)
    {
        g.DrawImage(b, new Rectangle(x, y, 166, 166), src, GraphicsUnit.Pixel);
    }
}
