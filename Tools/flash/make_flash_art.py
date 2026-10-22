"""Builds the tattoo flash sheets hanging on the shop walls from the stencil PNGs.

Usage: python Tools/flash/make_flash_art.py
Stencils (black on transparent) come from Assets/_VRTattoo/Stencils; each design gets a coloured
fill printed slightly off-register behind the black line work, on cream paper with a caption.
Output: Assets/_VRTattoo/Textures/FlashArt/Flash_<sheet>.png
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
STENCILS = ROOT / "Assets/_VRTattoo/Stencils"
OUT = ROOT / "Assets/_VRTattoo/Textures/FlashArt"

PAPER = (236, 224, 196)
INK = (22, 18, 16)
RED = (196, 38, 38)
TEAL = (24, 120, 120)
YELLOW = (226, 168, 40)
GREEN = (62, 128, 60)

# Sheet name -> (size, title, [(stencil, fill colour)]). Copyrighted designs are left out
# (angry_duck, ghost_monster, trophy, cloud).
SHEETS = {
    "Skulls": ((1024, 1024), "SKULLS & CROWNS", [
        ("easy_crown_skull", RED), ("hard_skull_crossbones", YELLOW),
        ("hard_royal_crown", YELLOW), ("medium_biohazard", GREEN)]),
    "Classics": ((1024, 1024), "TRADITIONAL", [
        ("easy_melting_smiley", YELLOW), ("easy_x_eyes_face", RED),
        ("medium_dollar_crown", GREEN), ("medium_crown_seven", RED)]),
    "Teddy": ((768, 768), "", [("hard_teddy_bear", RED)]),
    "SadBoy": ((600, 1000), "", [("hard_sad_boy", TEAL)]),
}


def font(size):
    for name in ("arialbd.ttf", "Arial Bold.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def paper(size):
    w, h = size
    img = Image.new("RGB", size, PAPER)
    # darker, aged edges
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rectangle([w * 0.06, h * 0.06, w * 0.94, h * 0.94], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(min(w, h) * 0.08))
    aged = Image.new("RGB", size, (196, 172, 128))
    return Image.composite(img, aged, mask)


def design(name, colour, box):
    """Stencil scaled into box (w, h): coloured fill offset behind black line work."""
    src = Image.open(STENCILS / f"{name}.png").convert("RGBA")
    alpha = src.getchannel("A")
    bbox = alpha.getbbox()
    alpha = alpha.crop(bbox)
    scale = min(box[0] / alpha.width, box[1] / alpha.height)
    alpha = alpha.resize((max(1, int(alpha.width * scale)), max(1, int(alpha.height * scale))), Image.LANCZOS)

    # Fill = the closed shape (line work dilated and its holes blurred shut), shifted a little.
    fill_alpha = alpha.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(2))
    fill_alpha = fill_alpha.point(lambda v: 170 if v > 60 else 0)
    layer = Image.new("RGBA", alpha.size, (0, 0, 0, 0))
    colour_img = Image.new("RGBA", alpha.size, colour + (255,))
    off = max(3, alpha.width // 90)
    shifted = Image.new("L", alpha.size, 0)
    shifted.paste(fill_alpha, (off, off))  # shift without wrapping around the edges
    layer.paste(colour_img, (0, 0), shifted)
    layer.paste(Image.new("RGBA", alpha.size, INK + (255,)), (0, 0), alpha)
    return layer


def build(name, size, title, items):
    img = paper(size).convert("RGBA")
    d = ImageDraw.Draw(img)
    w, h = size
    margin = int(min(w, h) * 0.08)
    top = margin
    if title:
        f = font(int(h * 0.055))
        tw = d.textlength(title, font=f)
        d.text(((w - tw) / 2, margin * 0.7), title, font=f, fill=RED)
        top = int(margin * 0.7 + h * 0.09)

    cols = 2 if len(items) > 1 else 1
    rows = (len(items) + cols - 1) // cols
    cell_w = (w - 2 * margin) / cols
    cell_h = (h - top - margin) / rows
    cap_font = font(int(min(cell_w, cell_h) * 0.07))
    for i, (stencil, colour) in enumerate(items):
        cx = margin + (i % cols) * cell_w
        cy = top + (i // cols) * cell_h
        caption = stencil.split("_", 1)[1].replace("_", " ").upper() if len(items) > 1 else ""
        cap_h = cap_font.size * 1.6 if caption else 0
        art = design(stencil, colour, (int(cell_w * 0.84), int((cell_h - cap_h) * 0.86)))
        ax = int(cx + (cell_w - art.width) / 2)
        ay = int(cy + (cell_h - cap_h - art.height) / 2)
        img.alpha_composite(art, (ax, ay))
        if caption:
            tw = d.textlength(caption, font=cap_font)
            d.text((cx + (cell_w - tw) / 2, cy + cell_h - cap_h), caption, font=cap_font, fill=INK)

    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / f"Flash_{name}.png"
    img.convert("RGB").save(path)
    print(path)


if __name__ == "__main__":
    for sheet, (size, title, items) in SHEETS.items():
        build(sheet, size, title, items)
