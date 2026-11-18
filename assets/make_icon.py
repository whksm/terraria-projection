# -*- coding: utf-8 -*-
"""32x32 像素图标 v15
去掉了“逐像素描边”的廉价感，改用：
  · 高斯外发光（只在剪影外侧，柔和不吃内部细节）
  · 顶部 1px 边缘光（rim light，像投影受光）
  · 极淡扫描线（3 条，透明度 <10%）
  · 树身低透明度 + 轻微青色偏色 + 底部渐暗
文字：TR 青色 3x5 / 投影 白色 12px / MOD 金色 5x7（M 有双斜肩，不会再像 N）
"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

A = os.path.dirname(os.path.abspath(__file__))
W = H = 32

CYAN = (95, 235, 255)
WHITE = (240, 248, 255)
GOLD = (255, 202, 74)
GRID = (15, 23, 40)

G_SMALL = {
    "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "R": ["##.", "#.#", "##.", "#.#", "#.#"],
}
G_BIG = {
    "M": ["#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "D": ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
}


def load_tree(height):
    src = Image.open(os.path.join(A, "terraria_icon_native.png")).convert("RGBA")
    bbox = src.getbbox()
    if bbox:
        src = src.crop(bbox)
    w, h = src.size
    tw = max(1, int(round(height * w / h)))
    return src.resize((tw, height), Image.LANCZOS)


def tint_alpha(img, alpha, tint=0.18):
    out = img.copy()
    px = out.load()
    for y in range(out.size[1]):
        for x in range(out.size[0]):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            r2 = int(r * (1 - tint) + CYAN[0] * tint)
            g2 = int(g * (1 - tint) + CYAN[1] * tint)
            b2 = int(b * (1 - tint) + CYAN[2] * tint)
            px[x, y] = (r2, g2, b2, int(a * alpha))
    return out


def scale_mask(mask, k):
    px = mask.load()
    out = Image.new("L", mask.size, 0)
    op = out.load()
    for y in range(mask.size[1]):
        for x in range(mask.size[0]):
            v = int(px[x, y] * k)
            op[x, y] = 255 if v > 255 else v
    return out


def build(bg_top, bg_bot, tree_alpha, out_name, tint=0.18, glowk=0.72, glow_color=None, dim=1.0, scrim=0.0):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    # ---------- 第一版背景 ----------
    for y in range(H):
        t = y / (H - 1)
        r = int(bg_top[0] * (1 - t) + bg_bot[0] * t)
        g = int(bg_top[1] * (1 - t) + bg_bot[1] * t)
        b = int(bg_top[2] * (1 - t) + bg_bot[2] * t)
        for x in range(W):
            dx = (x - 15.5) / 16.0
            dy = (y - 14.0) / 18.0
            k = 1.0 - min(1.0, (dx * dx + dy * dy) * 0.45)
            px[x, y] = (int(r * (0.88 + 0.22 * k)), int(g * (0.88 + 0.22 * k)),
                        int(b * (0.88 + 0.22 * k)), 255)

    for y in range(2, H, 4):
        for x in range(1, W - 1):
            px[x, y] = GRID + (255,)
    for x in range(2, W, 4):
        for y in range(1, H - 1):
            px[x, y] = GRID + (255,)

    # ---------- 树 ----------
    TH = 27
    tree = load_tree(TH)
    tw = tree.size[0]
    tx = (W - tw) // 2
    ty = 4

    if glow_color is None:
        glow_color = WHITE
    body = tint_alpha(tree, tree_alpha, tint)
    if dim != 1.0:
        bp0 = body.load()
        for yy in range(body.size[1]):
            for xx in range(body.size[0]):
                r0, g0, b0, a0 = bp0[xx, yy]
                bp0[xx, yy] = (int(r0 * dim), int(g0 * dim), int(b0 * dim), a0)
    bp = body.load()
    for yy in range(body.size[1]):
        k = yy / body.size[1]
        if k < 0.5:
            continue
        dim = 1.0 - (k - 0.5) * 1.2
        if dim < 0.4:
            dim = 0.4
        for xx in range(body.size[0]):
            r, g, b, a = bp[xx, yy]
            if a == 0:
                continue
            bp[xx, yy] = (int(r * dim), int(g * dim), int(b * dim), a)
    img.alpha_composite(body, (tx, ty))

    alpha = tree.split()[3]
    soft = alpha.filter(ImageFilter.GaussianBlur(1.6))
    outside = ImageChops.subtract(soft, alpha)          # 只在剪影外侧
    glow = Image.new("RGBA", tree.size, (0, 0, 0, 0))
    gp = glow.load()
    op = outside.load()
    for y in range(tree.size[1]):
        for x in range(tree.size[0]):
            v = op[x, y]
            if v == 0:
                continue
            gp[x, y] = glow_color + (int(v * glowk),)
    img.alpha_composite(glow, (tx, ty))

    # 顶部边缘光：mask 减去“向下移 1px 的 mask”
    shift = Image.new("L", tree.size, 0)
    sp = shift.load()
    ap = alpha.load()
    for y in range(tree.size[1] - 1):
        for x in range(tree.size[0]):
            if ap[x, y + 1] > 64:
                sp[x, y] = 255
    rim = ImageChops.subtract(alpha.point(lambda v: 255 if v > 64 else 0), shift)
    rim_layer = Image.new("RGBA", tree.size, (0, 0, 0, 0))
    rp = rim_layer.load()
    rmp = rim.load()
    for y in range(tree.size[1]):
        for x in range(tree.size[0]):
            if rmp[x, y] > 0:
                rp[x, y] = (235, 250, 255) + (150,)
    img.alpha_composite(rim_layer, (tx, ty))

    # 极淡扫描线
    for yy in (ty + 7, ty + 14, ty + 21):
        if yy >= H - 1:
            continue
        for xx in range(tx, min(W - 1, tx + tw)):
            r, g, b, a = px[xx, yy]
            if a == 0:
                continue
            px[xx, yy] = (int(r * 0.94 + WHITE[0] * 0.06),
                          int(g * 0.94 + WHITE[1] * 0.06),
                          int(b * 0.94 + WHITE[2] * 0.06), 255)

    for x in range(max(0, tx - 1), min(W, tx + tw + 1)):
        px[x, 31] = (34, 92, 120, 255)
    for x in range(max(0, tx), min(W, tx + tw), 2):
        px[x, 30] = (26, 72, 98, 255)

    # ---------- 文字区“渐变压暗”（软过渡，不是黑边）：拉开字与树的明度 ----------
    if scrim > 0:
        import math
        y0, y1 = 7, 22
        for yy in range(y0, y1 + 1):
            t = (yy - y0) / float(y1 - y0)
            k = scrim * math.sin(math.pi * t)      # 中间最深，两端平滑消失
            for xx in range(1, W - 1):
                r, g, b, a = px[xx, yy]
                if a == 0:
                    continue
                px[xx, yy] = (int(r * (1 - k)), int(g * (1 - k)), int(b * (1 - k)), a)

    # ---------- 四角取景框 ----------
    L = 4
    for (cx, cy, dx, dy) in [(1, 1, 1, 1), (W - 2, 1, -1, 1), (1, H - 2, 1, -1), (W - 2, H - 2, -1, -1)]:
        for i in range(L):
            px[cx + dx * i, cy] = CYAN + (255,)
            px[cx, cy + dy * i] = CYAN + (255,)
    for (cx, cy) in [(1, 1), (W - 2, 1), (1, H - 2), (W - 2, H - 2)]:
        px[cx, cy] = WHITE + (255,)

    # ---------- 文字 ----------
    def stamp(table, ch, gx, gy, color):
        for yy, row in enumerate(table[ch]):
            for xx, c in enumerate(row):
                if c == "#":
                    X, Y = gx + xx, gy + yy
                    if 0 <= X < W and 0 <= Y < H:
                        px[X, Y] = color + (255,)

    def word(table, s, gx0, gy, color, adv):
        gx = gx0
        for ch in s:
            stamp(table, ch, gx, gy, color)
            gx += adv

    font = ImageFont.truetype(r"C:\Windows\Fonts\simsun.ttc", 12)
    tmp = Image.new("L", (40, 20), 0)
    ImageDraw.Draw(tmp).text((0, 0), "投影", font=font, fill=255)
    bbox = tmp.getbbox() or (0, 0, 1, 1)
    cn = tmp.crop(bbox)
    cn_x = (W - cn.size[0]) // 2
    cn_y = 9
    for y in range(cn.size[1]):
        for x in range(cn.size[0]):
            if cn.getpixel((x, y)) >= 118:
                X, Y = cn_x + x, cn_y + y
                if 0 <= X < W and 0 <= Y < H:
                    px[X, Y] = WHITE + (255,)

    word(G_SMALL, "TR", (W - 7) // 2, 2, CYAN, 4)
    word(G_BIG, "MOD", (W - 17) // 2, 21, GOLD, 6)

    img.save(os.path.join(A, out_name))
    return img


normal = build((8, 12, 24), (23, 32, 60), 0.50, "icon_32.png")
normal.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(A, "icon_32_x8.png"))
normal.resize((W * 4, H * 4), Image.NEAREST).save(os.path.join(A, "icon_128.png"))

dark = build((4, 6, 12), (12, 18, 34), 0.56, "icon_32_dark.png")
dark.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(A, "icon_32_dark_x8.png"))

print("done v15")


# ---------------- 三版对比（另存 _variants.png，不覆盖 icon_32.png） ----------------
def sheet():
    items = [
        ("A 自然绿", dict(tree_alpha=0.50, tint=0.18, glowk=0.72)),
        ("B 青投影", dict(tree_alpha=0.42, tint=0.45, glowk=0.95)),
        ("C 更实体", dict(tree_alpha=0.62, tint=0.10, glowk=0.45)),
    ]
    shots = []
    for name, kw in items:
        im = build((8, 12, 24), (23, 32, 60), kw["tree_alpha"], "_tmp.png",
                   tint=kw["tint"], glowk=kw["glowk"])
        shots.append((name, im.resize((W * 8, H * 8), Image.NEAREST)))

    pad = 12
    cw = W * 8
    ch = H * 8
    canvas = Image.new("RGBA", (cw * len(shots) + pad * (len(shots) + 1), ch + 34 + pad), (18, 20, 30, 255))
    d = ImageDraw.Draw(canvas)
    f = ImageFont.truetype(r"C:\Windows\Fonts\simhei.ttf", 16)
    x = pad
    for name, im in shots:
        canvas.alpha_composite(im, (x, pad))
        d.text((x, ch + pad + 6), name, font=f, fill=(255, 255, 255, 255))
        x += cw + pad
    canvas.save(os.path.join(A, "_variants.png"))
    os.remove(os.path.join(A, "_tmp.png"))


sheet()


# ---------------- 三版对比（另存 _variants.png，不覆盖 icon_32.png） ----------------
def sheet():
    items = [
        ("A 自然绿", dict(tree_alpha=0.50, tint=0.18, glowk=0.72)),
        ("B 青投影", dict(tree_alpha=0.42, tint=0.45, glowk=0.95)),
        ("C 更实体", dict(tree_alpha=0.62, tint=0.10, glowk=0.45)),
    ]
    shots = []
    for name, kw in items:
        im = build((8, 12, 24), (23, 32, 60), kw["tree_alpha"], "_tmp.png",
                   tint=kw["tint"], glowk=kw["glowk"])
        shots.append((name, im.resize((W * 8, H * 8), Image.NEAREST)))

    pad = 12
    cw = W * 8
    ch = H * 8
    canvas = Image.new("RGBA", (cw * len(shots) + pad * (len(shots) + 1), ch + 34 + pad), (18, 20, 30, 255))
    d = ImageDraw.Draw(canvas)
    f = ImageFont.truetype(r"C:\\Windows\\Fonts\\simhei.ttf", 16)
    x = pad
    for name, im in shots:
        canvas.alpha_composite(im, (x, pad))
        d.text((x, ch + pad + 6), name, font=f, fill=(255, 255, 255, 255))
        x += cw + pad
    canvas.save(os.path.join(A, "_variants.png"))
    os.remove(os.path.join(A, "_tmp.png"))


sheet()

sheet()

# 最终采用 B（青投影）；同时保留 A/C 供选择
for tag, kw in (("A", dict(tint=0.18, glowk=0.72, alpha=0.50)),
                ("B", dict(tint=0.45, glowk=0.95, alpha=0.42)),
                ("C", dict(tint=0.10, glowk=0.45, alpha=0.62))):
    im = build((8, 12, 24), (23, 32, 60), kw["alpha"], "icon_%s.png" % tag,
               tint=kw["tint"], glowk=kw["glowk"])

final = build((8, 12, 24), (23, 32, 60), 0.50, "icon_32.png", tint=0.18, glowk=0.72)
final.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(A, "icon_32_x8.png"))
final.resize((W * 4, H * 4), Image.NEAREST).save(os.path.join(A, "icon_128.png"))

dark = build((4, 6, 12), (12, 18, 34), 0.54, "icon_32_dark.png", tint=0.18, glowk=0.72)
dark.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(A, "icon_32_dark_x8.png"))

# ---------------- D：C 的树 + 颜色分离（文字纯白/青/金，光晕偏青） ----------------
GLOW_CYAN = (150, 220, 255)

def build_d(out_name):
    return build((8, 12, 24), (23, 32, 60), 0.62, out_name,
                 tint=0.05, glowk=0.55, glow_color=GLOW_CYAN, dim=0.86, scrim=0.34)


d = build_d("icon_32.png")
d.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(A, "icon_32_x8.png"))
d.resize((W * 4, H * 4), Image.NEAREST).save(os.path.join(A, "icon_128.png"))
build_d("icon_D.png")

dark_d = build((4, 6, 12), (12, 18, 34), 0.66, "icon_32_dark.png",
               tint=0.05, glowk=0.55, glow_color=GLOW_CYAN, dim=0.86, scrim=0.36)
dark_d.resize((W * 8, H * 8), Image.NEAREST).save(os.path.join(A, "icon_32_dark_x8.png"))