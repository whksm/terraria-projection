# -*- coding: utf-8 -*-
"""由 32x32 图标生成 .ico（16/32/48/256）+ 16x16 简化版（幽灵树 + TR）"""
import os
from PIL import Image

A = os.path.dirname(os.path.abspath(__file__))

CYAN = (95, 235, 255)

GLYPHS = {
    "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "R": ["##.", "#.#", "##.", "#.#", "#.#"],
}

icon32 = Image.open(os.path.join(A, "icon_32.png")).convert("RGBA")
icon32.resize((256, 256), Image.NEAREST).save(
    os.path.join(A, "icon_32.ico"),
    sizes=[(16, 16), (32, 32), (48, 48), (256, 256)],
)
print("saved icon_32.ico")

# ---- 16x16 简化版 ----
img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
px = img.load()


def setp(x, y, c):
    if 0 <= x < 16 and 0 <= y < 16:
        px[x, y] = c


def blend(x, y, c, a):
    if not (0 <= x < 16 and 0 <= y < 16):
        return
    r0, g0, b0, _ = px[x, y]
    r1, g1, b1 = c
    k = a / 255.0
    px[x, y] = (int(r0 * (1 - k) + r1 * k), int(g0 * (1 - k) + g1 * k),
                int(b0 * (1 - k) + b1 * k), 255)


for y in range(16):
    t = y / 15.0
    for x in range(16):
        px[x, y] = (int(8 * (1 - t) + 22 * t), int(12 * (1 - t) + 30 * t),
                    int(24 * (1 - t) + 56 * t), 255)

src = Image.open(os.path.join(A, "terraria_icon_native.png")).convert("RGBA")
bbox = src.getbbox()
if bbox:
    src = src.crop(bbox)
tw = max(1, int(round(9 * src.size[0] / src.size[1])))
tree = src.resize((tw, 9), Image.LANCZOS)
tp = tree.load()
for y in range(9):
    for x in range(tw):
        r, g, b, a = tp[x, y]
        if a > 24:
            blend(8 - tw // 2 + x, 6 + y, (r, g, b), int(a * 0.55))

for x in range(max(0, 8 - tw // 2 - 1), min(16, 8 - tw // 2 + tw + 1)):
    setp(x, 15, (36, 104, 134, 255))

for i, ch in enumerate("TR"):
    for gy, row in enumerate(GLYPHS[ch]):
        for gx, c in enumerate(row):
            if c == "#":
                setp(3 + i * 4 + gx, 1 + gy, CYAN + (255,))

img.save(os.path.join(A, "icon_16.png"))
img.resize((16 * 8, 16 * 8), Image.NEAREST).save(os.path.join(A, "icon_16_x8.png"))
print("saved icon_16.png")
