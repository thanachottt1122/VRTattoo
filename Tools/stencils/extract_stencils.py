"""Cut individual tattoo stencils out of a flash sheet.

    python Tools/stencils/extract_stencils.py

Reads SourceArt/flash_sheet.jpg, writes 1024x1024 transparent PNGs (black lines) to
Assets/_VRTattoo/Stencils named <difficulty>_<name>.png, plus SourceArt/contact_sheet.png.
Needs: pip install --user opencv-python numpy pillow potracer
"""
from pathlib import Path

import cv2
import numpy as np
import potrace

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "SourceArt" / "flash_sheet.jpg"
OUT_DIR = ROOT / "Assets" / "_VRTattoo" / "Stencils"
CONTACT_SHEET = ROOT / "SourceArt" / "contact_sheet.png"

INK_THRESHOLD = 160      # gray value below this counts as ink
GROUP_DILATE = 15        # px; parts closer than about this are one design
MIN_GROUP_AREA = 800     # px (dilated); smaller groups are noise
OUT_SIZE = 1024
SUPERSAMPLE = 4          # render at 4x then downscale for smooth edges
FILL_RATIO = 0.8         # longest side of the design / image size

# One box per design (sheet pixel coordinates, x0 y0 x1 y1), drawn around each design found by
# the automatic grouping (ink dilated by GROUP_DILATE, connected components). The boxes fix the
# grouping where it is wrong: designs that touch (the two crowns, biohazard + crest) are split and
# detached bits (the X eye) are joined. Each ink part goes to the box containing its centre.
# name=None means skip (brand logos). difficulty=None means decide from the complexity score.
# threshold=None uses INK_THRESHOLD; light pencil sketches need a higher one.
DESIGNS = [
    # (box, name, difficulty override, threshold)
    ((40, 41, 161, 233), "crown_seven", None, None),
    ((183, 53, 360, 251), "skull_crossbones", None, None),
    ((373, 54, 549, 251), "crown_skull", None, None),
    ((545, 54, 736, 251), "royal_crown", None, 215),
    ((3, 321, 210, 404), None, None, None),          # Nike swoosh (brand)
    ((221, 281, 428, 420), None, None, None),        # adidas logo (brand)
    ((464, 258, 601, 423), None, None, None),        # Apple logo (brand)
    ((604, 254, 724, 426), "karate_kick", None, None),
    ((30, 466, 189, 633), "sad_boy", None, None),
    ((240, 435, 391, 637), "angry_duck", None, 200),
    ((434, 454, 582, 623), "ghost_monster", None, None),
    ((590, 459, 715, 651), "melting_smiley", None, None),
    ((31, 655, 168, 903), "dollar_crown", "medium", None),
    ((244, 663, 362, 894), "trophy", None, None),
    ((398, 692, 603, 904), None, None, None),        # three-stripe football boot (brand)
    ((9, 917, 215, 1129), "x_eyes_face", None, None),
    ((219, 904, 445, 1105), "biohazard", None, None),
    ((265, 1105, 430, 1308), None, None, None),      # Real Madrid crest (brand)
    ((462, 876, 603, 1110), "teddy_bear", None, None),
    ((10, 1130, 252, 1294), "cloud", None, None),
    ((462, 1121, 644, 1308), "flying_money", None, 215),
]

LEVELS = ["easy", "medium", "hard"]
LEVEL_COLORS = {"easy": (60, 160, 60), "medium": (0, 140, 230), "hard": (40, 40, 210)}  # BGR


def load_gray():
    return cv2.cvtColor(cv2.imread(str(SOURCE)), cv2.COLOR_BGR2GRAY)


def ink_mask(gray, threshold):
    ink = (gray < threshold).astype(np.uint8)
    return cv2.morphologyEx(ink, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))


def split_designs(gray):
    """Returns {design index: binary mask} for every design in DESIGNS."""
    ink = ink_mask(gray, INK_THRESHOLD)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (GROUP_DILATE, GROUP_DILATE))
    _, groups, gstats, _ = cv2.connectedComponentsWithStats(cv2.dilate(ink, kernel))
    n_parts, parts, pstats, pcent = cv2.connectedComponentsWithStats(ink)

    boxes = np.array([d[0] for d in DESIGNS], np.float32)
    areas = (boxes[:, 2] - boxes[:, 0]) * (boxes[:, 3] - boxes[:, 1])
    centres = (boxes[:, :2] + boxes[:, 2:]) / 2
    masks = {i: np.zeros_like(ink) for i in range(len(DESIGNS))}

    for p in range(1, n_parts):
        x, y = pstats[p, cv2.CC_STAT_LEFT], pstats[p, cv2.CC_STAT_TOP]
        ys, xs = np.nonzero(parts[y:y + pstats[p, cv2.CC_STAT_HEIGHT], x:x + pstats[p, cv2.CC_STAT_WIDTH]] == p)
        if gstats[groups[y + ys[0], x + xs[0]], cv2.CC_STAT_AREA] < MIN_GROUP_AREA:
            continue  # isolated speck
        cx, cy = pcent[p]
        inside = [i for i, (x0, y0, x1, y1) in enumerate(boxes) if x0 <= cx <= x1 and y0 <= cy <= y1]
        if inside:
            owner = min(inside, key=lambda i: areas[i])
        else:
            owner = int(np.argmin(np.linalg.norm(centres - pcent[p], axis=1)))
            print(f"note: part at {x},{y} is outside every box, given to {DESIGNS[owner][1]}")
        masks[owner][parts == p] = 1

    # Light sketches: re-threshold inside the box, near the parts already found, to keep faint lines.
    for i, (box, _, _, threshold) in enumerate(DESIGNS):
        if threshold is None or not masks[i].any():
            continue
        x0, y0, x1, y1 = box
        near = cv2.dilate(masks[i], cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (GROUP_DILATE, GROUP_DILATE)))
        region = np.zeros_like(ink)
        region[y0:y1, x0:x1] = 1
        masks[i] = ink_mask(gray, threshold) & near & region
    return masks


def bezier(p0, p1, p2, p3, steps=24):
    t = np.linspace(0, 1, steps)[:, None]
    return ((1 - t) ** 3) * p0 + 3 * ((1 - t) ** 2) * t * p1 + 3 * (1 - t) * t * t * p2 + t ** 3 * p3


def xy(point):
    return np.array([point.x, point.y], np.float64)


def curve_points(curve):
    pts = [xy(curve.start_point)]
    for seg in curve.segments:
        if seg.is_corner:
            pts.append(xy(seg.c))
            pts.append(xy(seg.end_point))
        else:
            pts.extend(bezier(pts[-1], xy(seg.c1), xy(seg.c2), xy(seg.end_point))[1:])
    return np.array(pts, np.float64)


def render_vector(mask):
    """Trace the mask with potrace and render it as a smooth 1024x1024 alpha image."""
    ys, xs = np.nonzero(mask)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    crop = mask[y0:y1, x0:x1].astype(bool)
    # potracer traces the False (dark) pixels, so pass the inverted mask.
    paths = potrace.Bitmap(~crop).trace(turdsize=3, alphamax=1.0, opticurve=True, opttolerance=0.2)

    big = OUT_SIZE * SUPERSAMPLE
    scale = big * FILL_RATIO / max(crop.shape)
    offset = np.array([(big - crop.shape[1] * scale) / 2, (big - crop.shape[0] * scale) / 2])
    canvas = np.zeros((big, big), np.uint8)
    shift = 4  # sub-pixel precision for fillPoly
    for curve in paths:
        pts = curve_points(curve) * scale + offset
        layer = np.zeros_like(canvas)
        cv2.fillPoly(layer, [np.round(pts * (1 << shift)).astype(np.int32)], 255, cv2.LINE_8, shift)
        canvas ^= layer  # even-odd: holes cancel out
    return cv2.resize(canvas, (OUT_SIZE, OUT_SIZE), interpolation=cv2.INTER_AREA)


def render_fallback(mask):
    """Upscale + blur + threshold, used if potrace fails."""
    ys, xs = np.nonzero(mask)
    crop = mask[ys.min():ys.max() + 1, xs.min():xs.max() + 1].astype(np.float32)
    big = OUT_SIZE * SUPERSAMPLE
    scale = big * FILL_RATIO / max(crop.shape)
    w, h = max(1, round(crop.shape[1] * scale)), max(1, round(crop.shape[0] * scale))
    up = cv2.GaussianBlur(cv2.resize(crop, (w, h), interpolation=cv2.INTER_CUBIC), (0, 0), scale * 0.4)
    canvas = np.zeros((big, big), np.uint8)
    x, y = (big - w) // 2, (big - h) // 2
    canvas[y:y + h, x:x + w] = (up > 0.5) * 255
    return cv2.resize(canvas, (OUT_SIZE, OUT_SIZE), interpolation=cv2.INTER_AREA)


def skeleton_length(binary):
    skel = np.zeros_like(binary)
    img = binary.copy()
    cross = cv2.getStructuringElement(cv2.MORPH_CROSS, (3, 3))
    while img.any():
        eroded = cv2.erode(img, cross)
        skel |= img - cv2.dilate(eroded, cross)
        img = eroded
    return int(skel.sum())


def complexity(alpha):
    """Raw features from the rendered stencil (same 1024 scale for every design)."""
    binary = (alpha > 127).astype(np.uint8)
    contours, _ = cv2.findContours(binary, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_NONE)
    n, _, stats, _ = cv2.connectedComponentsWithStats(binary)
    solid = cv2.morphologyEx(binary, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (41, 41)))
    return {
        "lines": skeleton_length(binary),                               # total stroke length
        "contours": len(contours),                                      # outlines + holes
        "small": int(sum(1 for i in range(1, n) if stats[i, cv2.CC_STAT_AREA] < 1500)),  # tiny details
        "fill": float(solid.sum()) / (OUT_SIZE * OUT_SIZE),             # solid areas to pack
    }


def score(features):
    weights = {"lines": 0.4, "contours": 0.3, "small": 0.15, "fill": 0.15}
    keys = list(weights)
    table = np.array([[f[k] for k in keys] for f in features], np.float64)
    lo, hi = table.min(0), table.max(0)
    norm = (table - lo) / np.where(hi > lo, hi - lo, 1)
    return norm @ np.array([weights[k] for k in keys])


def contact_sheet(items):
    cols, cell, label_h, pad = 4, 260, 46, 12
    rows = (len(items) + cols - 1) // cols
    sheet = np.full((rows * (cell + label_h) + pad, cols * cell + pad, 3), 255, np.uint8)
    for i, (filename, level, alpha) in enumerate(items):
        r, c = divmod(i, cols)
        x, y = pad + c * cell, pad + r * (cell + label_h)
        thumb = cv2.resize(255 - alpha, (cell - pad, cell - pad), interpolation=cv2.INTER_AREA)
        sheet[y:y + cell - pad, x:x + cell - pad] = cv2.cvtColor(thumb, cv2.COLOR_GRAY2BGR)
        cv2.rectangle(sheet, (x, y), (x + cell - pad - 1, y + cell - pad - 1), (200, 200, 200), 1)
        cv2.putText(sheet, filename, (x, y + cell + 4), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (30, 30, 30), 1, cv2.LINE_AA)
        cv2.putText(sheet, level.upper(), (x, y + cell + 26), cv2.FONT_HERSHEY_SIMPLEX, 0.6, LEVEL_COLORS[level], 2, cv2.LINE_AA)
    cv2.imwrite(str(CONTACT_SHEET), sheet)


def main():
    masks = split_designs(load_gray())

    designs = []
    for i, (_, name, override, _) in enumerate(DESIGNS):
        if name is None:
            continue
        if not masks[i].any():
            print(f"warning: {name} is empty")
            continue
        try:
            alpha = render_vector(masks[i])
            method = "potrace"
        except Exception as e:  # noqa: BLE001
            print(f"potrace failed for {name} ({e}), using fallback")
            alpha = render_fallback(masks[i])
            method = "fallback"
        designs.append({"name": name, "override": override, "alpha": alpha, "method": method,
                        "features": complexity(alpha)})

    scores = score([d["features"] for d in designs])
    order = np.argsort(scores)
    for rank, idx in enumerate(order):
        auto = LEVELS[min(2, rank * 3 // len(designs))]
        designs[idx]["score"] = scores[idx]
        designs[idx]["level"] = designs[idx]["override"] or auto

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for old in OUT_DIR.glob("*.png"):
        old.unlink()
        meta = old.with_suffix(".png.meta")
        if meta.exists():
            meta.unlink()

    items = []
    for d in sorted(designs, key=lambda d: (LEVELS.index(d["level"]), d["score"])):
        filename = f"{d['level']}_{d['name']}.png"
        rgba = np.zeros((OUT_SIZE, OUT_SIZE, 4), np.uint8)
        rgba[..., 3] = d["alpha"]  # black lines, alpha = coverage
        cv2.imwrite(str(OUT_DIR / filename), rgba)
        items.append((filename, d["level"], d["alpha"]))
        f = d["features"]
        print(f"{filename:32s} score={d['score']:.2f} lines={f['lines']} contours={f['contours']} "
              f"small={f['small']} fill={f['fill']:.3f} [{d['method']}]")

    contact_sheet(items)
    counts = {lv: sum(1 for d in designs if d["level"] == lv) for lv in LEVELS}
    print("counts:", counts, "skipped:", sum(1 for d in DESIGNS if d[1] is None))


if __name__ == "__main__":
    main()
