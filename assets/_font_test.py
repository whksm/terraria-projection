# -*- coding: utf-8 -*-
"""试验：不同字体/字号下“投影”二值化的可读性，输出对比图。"""
import os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets")
os.makedirs(OUT, exist_ok=True)

cands = [
    ("simsun12", r"C:\Windows\Fonts\simsun.ttc", 12),
    ("simsun11", r"C:\Windows\Fonts\simsun.ttc", 11),
    ("simhei10", r"C:\Windows\Fonts\simhei.ttf", 10),
    ("simhei11", r"C:\Windows\Fonts\simhei.ttf", 11),
    ("msyh10", r"C:\Windows\Fonts\msyh.ttc", 10),
    ("msyh09", r"C:\Windows\Fonts\msyh.ttc", 9),
]

rows = []
for name, path, size in cands:
    if not os.path.exists(path):
        continue
    font = ImageFont.truetype(path, size)
    im = Image.new("L", (40, 20), 0)
    d = ImageDraw.Draw(im)
    d.text((0, 0), "投影", font=font, fill=255)
    bbox = im.getbbox() or (0, 0, 1, 1)
    im = im.crop(bbox)
    # 二值化
    bin_im = Image.new("RGBA", im.size, (0, 0, 0, 0))
    bp = bin_im.load()
    ip = im.load()
    for y in range(im.size[1]):
        for x in range(im.size[0]):
            if ip[x, y] >= 120:
                bp[x, y] = (255, 255, 255, 255)
    rows.append((name, bin_im))

# 拼成一张图：左灰度、右二值，逐行标注
pad = 4
label_w = 60
cell_h = max(im.size[1] for _, im in rows) + pad
total_w = label_w + 40 + 40
total_h = cell_h * len(rows) + pad
canvas = Image.new("RGBA", (total_w, total_h), (20, 24, 40, 255))
from PIL import ImageDraw as ID
d = ID.Draw(canvas)
y = pad
for name, im in rows:
    d.text((2, y + 2), name, fill=(255, 255, 0, 255))
    # 二值图
    canvas.alpha_composite(im, (label_w, y))
    # 放大预览放在右边（4x）
    big = im.resize((im.size[0] * 4, im.size[1] * 4), Image.NEAREST)
    if label_w + 40 + big.size[0] <= total_w:
        canvas.alpha_composite(big, (label_w + 40, y))
    y += cell_h

out = os.path.join(OUT, "_font_test.png")
canvas.resize((canvas.size[0] * 2, canvas.size[1] * 2), Image.NEAREST).save(out)
print("saved", out, canvas.size)
