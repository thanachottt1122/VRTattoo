"""Draws the four customer mood emotes (simple flat faces) as transparent PNGs.

Usage: python Tools/emotes/draw_emotes.py [output_dir]
Default output: Assets/_VRTattoo/UI/Emotes. Drawn at 4x and downscaled for smooth edges.
Replace the PNGs with any other art of the same name to change the look.
"""
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

SIZE = 256
S = 4  # supersampling
W = SIZE * S

OUTLINE = (60, 35, 20, 255)
YELLOW = (255, 206, 64, 255)
RED_FACE = (235, 70, 50, 255)
HEART = (230, 30, 70, 255)
MOUTH = (110, 45, 30, 255)
TONGUE = (235, 95, 95, 255)
CHEEK = (255, 140, 110, 150)


def p(v):
    return v * S


def face(fill):
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([p(12), p(12), p(244), p(244)], fill=OUTLINE)
    d.ellipse([p(22), p(22), p(234), p(234)], fill=fill)
    return img, d


def eye(d, cx, cy, rx=13, ry=19):
    d.ellipse([p(cx - rx), p(cy - ry), p(cx + rx), p(cy + ry)], fill=OUTLINE)


def heart(d, cx, cy, r):
    pts = []
    for i in range(200):
        t = i / 200 * 2 * math.pi
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((p(cx + x * r / 16), p(cy - y * r / 16)))
    d.polygon(pts, fill=HEART)


def thick_arc(d, box, start, end, width):
    d.arc([p(v) for v in box], start, end, fill=OUTLINE, width=p(width))


def ecstatic():
    img, d = face(YELLOW)
    heart(d, 85, 100, 30)
    heart(d, 171, 100, 30)
    # big open smile
    d.chord([p(66), p(105), p(190), p(215)], 0, 180, fill=OUTLINE)
    d.chord([p(77), p(116), p(179), p(204)], 0, 180, fill=MOUTH)
    d.chord([p(100), p(170), p(156), p(212)], 180, 360, fill=TONGUE)
    return img


def happy():
    img, d = face(YELLOW)
    eye(d, 90, 100)
    eye(d, 166, 100)
    d.ellipse([p(45), p(140), p(85), p(165)], fill=CHEEK)
    d.ellipse([p(171), p(140), p(211), p(165)], fill=CHEEK)
    thick_arc(d, (72, 105, 184, 195), 20, 160, 11)
    return img


def neutral():
    img, d = face(YELLOW)
    eye(d, 90, 105, 12, 15)
    eye(d, 166, 105, 12, 15)
    # half-lidded: flat brows
    d.line([p(70), p(76), p(108), p(80)], fill=OUTLINE, width=p(8))
    d.line([p(148), p(80), p(186), p(76)], fill=OUTLINE, width=p(8))
    # flat, slightly crooked mouth
    d.line([p(88), p(172), p(170), p(164)], fill=OUTLINE, width=p(11))
    for x, y in ((88, 172), (170, 164)):
        d.ellipse([p(x - 5.5), p(y - 5.5), p(x + 5.5), p(y + 5.5)], fill=OUTLINE)
    return img


def angry():
    img, d = face(RED_FACE)
    eye(d, 92, 112, 12, 14)
    eye(d, 164, 112, 12, 14)
    # brows slanting down to the middle
    d.polygon([(p(58), p(70)), (p(116), p(92)), (p(112), p(104)), (p(56), p(84))], fill=OUTLINE)
    d.polygon([(p(198), p(70)), (p(140), p(92)), (p(144), p(104)), (p(200), p(84))], fill=OUTLINE)
    # frown
    thick_arc(d, (80, 160, 176, 230), 200, 340, 11)
    return img


def main():
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / "Assets/_VRTattoo/UI/Emotes"
    out.mkdir(parents=True, exist_ok=True)
    for name, fn in (("Ecstatic", ecstatic), ("Happy", happy), ("Neutral", neutral), ("Angry", angry)):
        img = fn().resize((SIZE, SIZE), Image.LANCZOS)
        path = out / f"Emote_{name}.png"
        img.save(path)
        print(path)


if __name__ == "__main__":
    main()
