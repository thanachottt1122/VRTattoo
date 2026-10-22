"""Draws the white star used for the job card's 1-3 star rating (tinted gold / grey in Unity).

Usage: python Tools/ui/draw_star.py [output_dir]
Default output: Assets/_VRTattoo/UI/Icons. Drawn at 4x and downscaled for smooth edges.
"""
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

SIZE = 256
S = 4  # supersampling
W = SIZE * S


def star_points(cx, cy, outer, inner, points=5):
    pts = []
    for i in range(points * 2):
        r = outer if i % 2 == 0 else inner
        a = -math.pi / 2 + i * math.pi / points
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def main():
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("Assets/_VRTattoo/UI/Icons")
    out.mkdir(parents=True, exist_ok=True)

    c = W / 2
    shape = star_points(c, c * 1.06, W * 0.40, W * 0.17)

    # Soft glow behind the star so it reads like the neon signs.
    glow = Image.new("L", (W, W), 0)
    ImageDraw.Draw(glow).polygon(shape, fill=110)
    glow = glow.filter(ImageFilter.GaussianBlur(W * 0.035))

    star = Image.new("L", (W, W), 0)
    ImageDraw.Draw(star).polygon(shape, fill=255)

    alpha = Image.eval(Image.merge("L", [glow]), lambda v: v)
    alpha.paste(255, mask=star)
    img = Image.merge("RGBA", [Image.new("L", (W, W), 255)] * 3 + [alpha])
    img.resize((SIZE, SIZE), Image.LANCZOS).save(out / "star.png")
    print("wrote", out / "star.png")


if __name__ == "__main__":
    main()
