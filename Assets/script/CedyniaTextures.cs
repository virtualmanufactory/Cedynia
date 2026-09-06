using UnityEngine;

public static class CedyniaTextures
{
    public static Texture2D LoadOrCreate(string resourceName, int size, System.Func<int, Texture2D> factory)
    {
        var tex = Resources.Load<Texture2D>("Cedynia/" + resourceName);
        if (tex != null)
            return tex;
        return factory(size);
    }

    public static Texture2D Grass(int n = 512)
    {
        var t = New(n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float patch = Fbm(u * 6f, v * 6f, 5);
            float fine = Fbm(u * 22f + 3f, v * 22f, 3);
            float tcol = Mathf.Clamp01(0.5f + patch * 0.55f + fine * 0.15f);
            Color dark = new Color(0.16f, 0.26f, 0.09f);
            Color mid = new Color(0.30f, 0.45f, 0.15f);
            Color lite = new Color(0.46f, 0.56f, 0.20f);
            Color dry = new Color(0.48f, 0.46f, 0.20f);
            Color c = Color.Lerp(dark, mid, tcol);
            if (tcol > 0.62f)
                c = Color.Lerp(c, lite, (tcol - 0.62f) / 0.38f);
            if (patch < -0.12f)
                c = Color.Lerp(c, dry, 0.32f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D Earth(int n = 512)
    {
        var t = New(n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float n1 = Fbm(u * 7f, v * 7f, 5);
            float n2 = Fbm(u * 18f + 2f, v * 18f, 3);
            float speckle = Hash(x / 3, y / 3);
            Color dark = new Color(0.20f, 0.13f, 0.07f);
            Color mid = new Color(0.40f, 0.27f, 0.13f);
            Color lite = new Color(0.56f, 0.40f, 0.22f);
            Color c = Color.Lerp(dark, mid, Mathf.Clamp01(0.5f + n1 * 0.65f));
            c = Color.Lerp(c, lite, Mathf.Clamp01(n2 * 0.45f + 0.15f));
            if (speckle > 0.93f)
                c = Color.Lerp(c, new Color(0.50f, 0.46f, 0.38f), 0.5f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D Wood(int n = 512, bool dark = false)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color a = dark ? new Color(0.15f, 0.08f, 0.04f) : new Color(0.30f, 0.17f, 0.07f);
        Color b = dark ? new Color(0.27f, 0.15f, 0.07f) : new Color(0.50f, 0.31f, 0.13f);
        Color c3 = dark ? new Color(0.36f, 0.21f, 0.09f) : new Color(0.64f, 0.44f, 0.20f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float grain = v * 8f + 0.35f * Mathf.Sin(u * 14f) + 0.2f * Fbm(u * 8f, v * 2f, 3);
            float ring = 0.5f + 0.5f * Mathf.Sin(grain * 22f + Fbm(u * 6f, v * 6f, 3) * 3f);
            float knot = Fbm(u * 10f + 4f, v * 3f, 3);
            float tt = Mathf.Clamp01(ring * 0.7f + (0.5f + knot * 0.5f) * 0.3f);
            Color c = Color.Lerp(a, b, tt);
            c = Color.Lerp(c, c3, tt * tt);
            if (knot > 0.4f)
                c = Color.Lerp(c, a, (knot - 0.4f) * 1.3f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D Thatch(int n = 512)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color a = new Color(0.38f, 0.26f, 0.09f);
        Color b = new Color(0.60f, 0.46f, 0.16f);
        Color c3 = new Color(0.76f, 0.62f, 0.28f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float tt = 0.5f + Fbm(x / (float)n * 8f, y / (float)n * 8f, 4) * 0.5f;
            px[y * n + x] = Color.Lerp(a, b, tt);
        }

        int rng = 12345;
        for (int i = 0; i < 2400; i++)
        {
            rng = rng * 1103515245 + 12345;
            int x0 = (rng & 0x7fffffff) % n;
            rng = rng * 1103515245 + 12345;
            int y0 = (rng & 0x7fffffff) % n;
            rng = rng * 1103515245 + 12345;
            int length = 14 + (rng & 0x7fffffff) % 32;
            rng = rng * 1103515245 + 12345;
            float ang = 0.55f + ((rng & 0x7fffffff) % 1000) / 1000f * 0.5f;
            rng = rng * 1103515245 + 12345;
            Color col = Color.Lerp(a, c3, ((rng & 0x7fffffff) % 1000) / 1000f);
            float dx = Mathf.Cos(ang);
            float dy = Mathf.Sin(ang);
            for (int s = 0; s < length; s++)
            {
                int pxI = ((x0 + Mathf.RoundToInt(dx * s)) % n + n) % n;
                int pyI = ((y0 + Mathf.RoundToInt(dy * s)) % n + n) % n;
                int idx = pyI * n + pxI;
                px[idx] = Color.Lerp(px[idx], col, 0.62f);
            }
        }
        return Apply(t, px);
    }

    public static Texture2D Water(int n = 512)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color deep = new Color(0.05f, 0.15f, 0.20f);
        Color mid = new Color(0.10f, 0.32f, 0.38f);
        Color lite = new Color(0.28f, 0.56f, 0.60f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float n1 = Fbm(u * 6f, v * 6f, 5);
            float n2 = Fbm(u * 14f + 2f, v * 14f, 3);
            float caustic = Mathf.Abs(Mathf.Sin((n1 + n2) * 6f));
            Color c = Color.Lerp(deep, mid, Mathf.Clamp01(0.45f + n1 * 0.55f));
            c = Color.Lerp(c, lite, caustic * 0.42f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D Bark(int n = 256)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color a = new Color(0.16f, 0.10f, 0.06f);
        Color b = new Color(0.34f, 0.20f, 0.11f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float crack = Mathf.Abs(Mathf.Sin(u * 28f + Fbm(u * 6f, v * 2f, 3) * 2f));
            float tt = 0.5f + Fbm(u * 5f, v * 2f, 4) * 0.5f;
            Color c = Color.Lerp(a, b, tt);
            if (crack < 0.16f)
                c = Color.Lerp(c, new Color(0.07f, 0.04f, 0.03f), 0.7f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D Leaves(int n = 256)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color a = new Color(0.07f, 0.18f, 0.05f);
        Color b = new Color(0.17f, 0.36f, 0.09f);
        Color c3 = new Color(0.30f, 0.48f, 0.12f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float tt = 0.5f + Fbm(x / (float)n * 8f, y / (float)n * 8f, 4) * 0.55f;
            Color c = Color.Lerp(a, b, Mathf.Clamp01(tt));
            if (tt > 0.55f)
                c = Color.Lerp(c, c3, (tt - 0.55f) / 0.45f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D LogWood(int n = 512)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color bark = new Color(0.42f, 0.32f, 0.20f);
        Color mid = new Color(0.68f, 0.55f, 0.36f);
        Color lite = new Color(0.82f, 0.72f, 0.52f);
        Color grey = new Color(0.62f, 0.56f, 0.44f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float along = v * 10f + 0.25f * Mathf.Sin(u * 18f) + 0.15f * Fbm(u * 6f, v * 2f, 3);
            float ring = 0.5f + 0.5f * Mathf.Sin(along * 16f + Fbm(u * 8f, v * 8f, 3) * 2f);
            float crack = Mathf.Abs(Mathf.Sin(u * 22f + Fbm(u * 4f, v * 1.5f, 3) * 2f));
            float weathered = Fbm(u * 5f + 2f, v * 5f, 4);
            Color c = Color.Lerp(bark, mid, ring);
            c = Color.Lerp(c, lite, Mathf.Clamp01(0.25f + weathered * 0.6f));
            if (weathered > 0.2f)
                c = Color.Lerp(c, grey, (weathered - 0.2f) * 0.45f);
            if (crack < 0.12f)
                c = Color.Lerp(c, bark, 0.55f);
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D MossThatch(int n = 512)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color strawA = new Color(0.42f, 0.30f, 0.12f);
        Color strawB = new Color(0.70f, 0.55f, 0.24f);
        Color mossA = new Color(0.16f, 0.28f, 0.10f);
        Color mossB = new Color(0.28f, 0.42f, 0.14f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float v = y / (float)n;
            float straw = 0.5f + Fbm(u * 10f, v * 10f, 4) * 0.5f;
            float stripe = 0.5f + 0.5f * Mathf.Sin((x * 0.55f + y) * 0.28f);
            Color c = Color.Lerp(strawA, strawB, straw * 0.7f + stripe * 0.3f);
            float moss = Fbm(u * 4f + 6f, v * 4f, 5);
            if (moss > 0.08f)
            {
                float k = Mathf.InverseLerp(0.08f, 0.42f, moss);
                Color mossC = Color.Lerp(mossA, mossB, Fbm(u * 9f, v * 9f, 3) * 0.5f + 0.5f);
                c = Color.Lerp(c, mossC, Mathf.Clamp01(k * 1.15f));
            }
            px[y * n + x] = c;
        }
        return Apply(t, px);
    }

    public static Texture2D Sand(int n = 256)
    {
        var t = New(n);
        var px = new Color[n * n];
        Color a = new Color(0.52f, 0.44f, 0.26f);
        Color b = new Color(0.74f, 0.64f, 0.40f);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float tt = 0.5f + Fbm(x / (float)n * 8f, y / (float)n * 8f, 4) * 0.5f;
            px[y * n + x] = Color.Lerp(a, b, tt);
        }
        return Apply(t, px);
    }

    static Texture2D New(int n)
    {
        return new Texture2D(n, n, TextureFormat.RGB24, true, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 8,
            name = "CedyniaTex"
        };
    }

    static Texture2D Apply(Texture2D t, Color[] px)
    {
        t.SetPixels(px);
        t.Apply(true, false);
        return t;
    }

    static float Hash(int x, int y)
    {
        uint n = (uint)(x * 374761393 + y * 668265263);
        n = (n ^ (n >> 13)) * 1274126177u;
        return (n & 0x00ffffff) / 16777215f;
    }

    static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    static float Grad(int ix, int iy, float x, float y)
    {
        float ang = Hash(ix, iy) * Mathf.PI * 2f;
        return Mathf.Cos(ang) * x + Mathf.Sin(ang) * y;
    }

    static float Noise(float x, float y)
    {
        int period = 256;
        x = Mathf.Repeat(x, period);
        y = Mathf.Repeat(y, period);
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = (x0 + 1) % period;
        int y1 = (y0 + 1) % period;
        float tx = Fade(x - x0);
        float ty = Fade(y - y0);
        float n00 = Grad(x0, y0, x - x0, y - y0);
        float n10 = Grad(x1, y0, x - x0 - 1f, y - y0);
        float n01 = Grad(x0, y1, x - x0, y - y0 - 1f);
        float n11 = Grad(x1, y1, x - x0 - 1f, y - y0 - 1f);
        return Mathf.Lerp(Mathf.Lerp(n00, n10, tx), Mathf.Lerp(n01, n11, tx), ty);
    }

    static float Fbm(float x, float y, int octaves)
    {
        float total = 0f;
        float amp = 1f;
        float freq = 1f;
        float norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * freq, y * freq) * amp;
            norm += amp;
            amp *= 0.5f;
            freq *= 2f;
        }
        return total / norm;
    }
}
