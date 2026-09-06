#!/usr/bin/env python3
"""Fast tileable albedo textures for Cedynia."""
import math
import os
import struct
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Cedynia")


def write_png(path, w, h, rgb):
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    raw = bytearray()
    row = w * 3
    for y in range(h):
        raw.append(0)
        raw.extend(rgb[y * row : (y + 1) * row])
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(bytes(raw), 6)))
        f.write(chunk(b"IEND", b""))


def h2(ix, iy):
    n = (ix * 374761393 + iy * 668265263) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return n / 4294967295.0


def noise(x, y, period):
    x %= period
    y %= period
    x0 = int(math.floor(x))
    y0 = int(math.floor(y))
    x1 = (x0 + 1) % period
    y1 = (y0 + 1) % period
    tx = x - x0
    ty = y - y0
    tx = tx * tx * (3 - 2 * tx)
    ty = ty * ty * (3 - 2 * ty)
    n00 = h2(x0, y0)
    n10 = h2(x1, y0)
    n01 = h2(x0, y1)
    n11 = h2(x1, y1)
    return n00 + (n10 - n00) * tx + (n01 - n00) * ty + (n00 - n10 - n01 + n11) * tx * ty


def fbm(x, y, period, octaves=3):
    t = 0.0
    a = 1.0
    nrm = 0.0
    p = period
    for _ in range(octaves):
        t += noise(x, y, max(1, int(p))) * a
        nrm += a
        a *= 0.5
        x *= 2
        y *= 2
        p *= 2
    return t / nrm


def rgb(pixels, n):
    out = bytearray(n * n * 3)
    i = 0
    for r, g, b in pixels:
        out[i] = max(0, min(255, int(r * 255)))
        out[i + 1] = max(0, min(255, int(g * 255)))
        out[i + 2] = max(0, min(255, int(b * 255)))
        i += 3
    return bytes(out)


def lerp(a, b, t):
    return a + (b - a) * t


def mix(a, b, t):
    return (lerp(a[0], b[0], t), lerp(a[1], b[1], t), lerp(a[2], b[2], t))


def gen(n, fn):
    px = []
    for y in range(n):
        for x in range(n):
            px.append(fn(x, y, n))
    return px


def grass(x, y, n):
    u, v = x / n * 8, y / n * 8
    t = fbm(u, v, 8)
    c = mix((0.16, 0.26, 0.09), (0.34, 0.48, 0.16), t)
    if t > 0.62:
        c = mix(c, (0.46, 0.56, 0.20), (t - 0.62) / 0.38)
    return c


def earth(x, y, n):
    t = fbm(x / n * 7, y / n * 7, 7)
    return mix((0.20, 0.13, 0.07), (0.52, 0.36, 0.18), t)


def wood(x, y, n, dark=False):
    u, v = x / n, y / n
    grain = v * 8 + 0.3 * math.sin(u * 14)
    ring = 0.5 + 0.5 * math.sin(grain * 22 + fbm(u * 6, v * 6, 6, 2) * 3)
    a = (0.15, 0.08, 0.04) if dark else (0.30, 0.17, 0.07)
    b = (0.32, 0.18, 0.08) if dark else (0.58, 0.38, 0.16)
    return mix(a, b, ring)


def thatch(x, y, n):
    t = fbm(x / n * 10, y / n * 10, 10, 2)
    stripe = 0.5 + 0.5 * math.sin((x * 0.7 + y) * 0.35)
    return mix((0.38, 0.26, 0.09), (0.74, 0.60, 0.26), t * 0.65 + stripe * 0.35)


def water(x, y, n):
    t = fbm(x / n * 6, y / n * 6, 6)
    c = mix((0.05, 0.15, 0.20), (0.14, 0.38, 0.42), t)
    cau = abs(math.sin(t * 8))
    return mix(c, (0.30, 0.58, 0.62), cau * 0.4)


def bark(x, y, n):
    t = fbm(x / n * 5, y / n * 2, 5, 2)
    crack = abs(math.sin(x / n * 28 + t * 2))
    c = mix((0.16, 0.10, 0.06), (0.34, 0.20, 0.11), t)
    if crack < 0.16:
        c = mix(c, (0.07, 0.04, 0.03), 0.7)
    return c


def leaves(x, y, n):
    t = fbm(x / n * 8, y / n * 8, 8)
    return mix((0.07, 0.18, 0.05), (0.28, 0.46, 0.12), t)


def sand(x, y, n):
    t = fbm(x / n * 8, y / n * 8, 8, 2)
    return mix((0.52, 0.44, 0.26), (0.74, 0.64, 0.40), t)


def main():
    os.makedirs(OUT, exist_ok=True)
    jobs = [
        ("grass.png", 512, grass),
        ("earth.png", 512, earth),
        ("wood.png", 512, lambda x, y, n: wood(x, y, n, False)),
        ("wood_dark.png", 512, lambda x, y, n: wood(x, y, n, True)),
        ("thatch.png", 512, thatch),
        ("water.png", 512, water),
        ("bark.png", 256, bark),
        ("leaves.png", 256, leaves),
        ("sand.png", 256, sand),
    ]
    for name, size, fn in jobs:
        print("write", name, flush=True)
        write_png(os.path.join(OUT, name), size, size, rgb(gen(size, fn), size))
    print("done", flush=True)


if __name__ == "__main__":
    main()
