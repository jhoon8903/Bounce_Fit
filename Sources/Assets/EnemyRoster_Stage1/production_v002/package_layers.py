#!/usr/bin/env python3
"""Package v002 RGBA 2x2 enemy sources into aligned Unity sprite layers.

Only existing pixels are cropped, proportionally resized, and composited. Native
alpha is preserved; opaque/chroma-keyed inputs are rejected. The v002 source
contract puts every wheel, track, and undercarriage element in Rear, leaving Front
as a thin hood/rim that covers only the rider's waist in the assembled preview.
"""
import argparse
from collections import deque
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

PARTS = ("Rear", "Rider", "Front", "Equipment")
BOSS_PARTS = ("Base", "Core", "LeftTurret", "RightTurret")
ROSTER_IDS = (
    "E01_LoggingCart", "E02_HunterCart", "E03_Bulldozer", "E04_LogTruck", "E05_Sidecar",
    "E06_SurveyRover", "E07_ArmoredRover", "E08_DemolitionCarrier", "E09_Tanker", "E10_RepairSidecar",
    "E11_CommandCart", "E12_DrillTruck", "E13_RoadRoller", "E14_CoilRover", "E15_SignalSidecar",
)
ROOT = Path(__file__).resolve().parents[4]
SOURCE_DIR = Path(__file__).resolve().parent
DEFAULT_OUTPUT = ROOT / "Assets/Projects/MVP/Art/Enemies/Stage1"
DEFAULT_PREVIEWS = SOURCE_DIR / "previews"


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unit", action="append", help="Unit ID to package; may repeat.")
    parser.add_argument("--all", action="store_true", help="Package all available v002 sources.")
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--preview-dir", type=Path, default=DEFAULT_PREVIEWS)
    parser.add_argument("--recipes", type=Path, default=SOURCE_DIR / "layer_recipes.json")
    parser.add_argument("--roster-grid", action="store_true", help="Create 5x3 assembled and Front-only QA grids.")
    parser.add_argument("--boss", action="store_true", help="Package B01_SawmillTower with its boss-only recipe.")
    parser.add_argument("--boss-recipes", type=Path, default=SOURCE_DIR / "boss_recipes.json")
    return parser.parse_args()


def meaningful_bbox(image, threshold):
    return image.getchannel("A").point(lambda value: 255 if value > threshold else 0).getbbox()


def alpha_components(source, threshold):
    """Return whole meaningful-alpha components with centroid and source bounds."""
    alpha = source.getchannel("A")
    width, height = source.size
    values = alpha.tobytes()
    visible = bytearray(value > threshold for value in values)
    visited = bytearray(width * height)
    components = []
    for start, is_visible in enumerate(visible):
        if not is_visible or visited[start]:
            continue
        queue = deque([start])
        visited[start] = 1
        left = right = start % width
        top = bottom = start // width
        total_x = total_y = count = 0
        pixels = []
        while queue:
            index = queue.popleft()
            pixels.append(index)
            x, y = index % width, index // width
            left, right = min(left, x), max(right, x)
            top, bottom = min(top, y), max(bottom, y)
            total_x, total_y, count = total_x + x, total_y + y, count + 1
            for neighbor_y in range(max(0, y - 1), min(height, y + 2)):
                for neighbor_x in range(max(0, x - 1), min(width, x + 2)):
                    neighbor = neighbor_y * width + neighbor_x
                    if visible[neighbor] and not visited[neighbor]:
                        visited[neighbor] = 1
                        queue.append(neighbor)
        if left == 0 or top == 0 or right == width - 1 or bottom == height - 1:
            raise ValueError(f"source component touches outer image edge: {(left, top, right + 1, bottom + 1)}")
        components.append({
            "bbox": (left, top, right + 1, bottom + 1),
            "centroid": (total_x / count, total_y / count),
            "pixels": pixels,
        })
    return components


def crop_cells(source, threshold, parts=PARTS, split_connected=False, pixel_owner=None):
    width, height = source.size
    if width != height or width % 2:
        raise ValueError(f"source must be an even square 2x2 sheet, got {source.size}")
    alpha = source.getchannel("A")
    values = alpha.tobytes()
    if alpha.getextrema()[0] == 255:
        raise ValueError("opaque source rejected: v002 requires native alpha, with no chroma keying")
    half = width // 2
    def default_owner(x, y):
        return parts[(1 if y >= half else 0) * 2 + (1 if x >= half else 0)]

    owner_at = pixel_owner or default_owner
    assigned = {part: [] for part in parts}
    for component in alpha_components(source, threshold):
        if split_connected:
            regions = {part: [] for part in parts}
            for index in component["pixels"]:
                x, y = index % width, index // width
                regions[owner_at(x, y)].append(index)
            for owner, pixels in regions.items():
                if not pixels:
                    continue
                xs = [index % width for index in pixels]
                ys = [index // width for index in pixels]
                assigned[owner].append({
                    "bbox": (min(xs), min(ys), max(xs) + 1, max(ys) + 1),
                    "pixels": pixels,
                    "owner": owner,
                })
            continue
        x, y = component["centroid"]
        # Components centred on an invisible divider cannot be assigned reliably.
        if abs(x - half) < 2 or abs(y - half) < 2:
            raise ValueError(f"ambiguous component centroid on 2x2 divider: {component['centroid']}")
        part = parts[(1 if y >= half else 0) * 2 + (1 if x >= half else 0)]
        assigned[part].append(component)

    crops = {}
    for part, components in assigned.items():
        if not components:
            raise ValueError(f"{part} has no alpha above {threshold}")
        left = max(0, min(component["bbox"][0] for component in components) - 2)
        top = max(0, min(component["bbox"][1] for component in components) - 2)
        right = min(width, max(component["bbox"][2] for component in components) + 2)
        bottom = min(height, max(component["bbox"][3] for component in components) + 2)

        # A rectangular crop can include a nearby component belonging to another
        # quadrant. Build an alpha mask from this part's own components only, with
        # a two-pixel halo for antialiased outlines, before taking the union crop.
        component_mask = bytearray(width * height)
        for component in components:
            for index in component["pixels"]:
                x, y = index % width, index // width
                for mask_y in range(max(0, y - 2), min(height, y + 3)):
                    row = mask_y * width
                    for mask_x in range(max(0, x - 2), min(width, x + 3)):
                        if "owner" in component:
                            if owner_at(mask_x, mask_y) != component["owner"]:
                                continue
                        component_mask[row + mask_x] = values[row + mask_x]
        masked_alpha = Image.frombytes("L", (width, height), bytes(component_mask))
        crop = source.crop((left, top, right, bottom))
        crop.putalpha(masked_alpha.crop((left, top, right, bottom)))
        crops[part] = crop
    return crops


def position(crop, layout, canvas):
    width = int(layout["width"])
    height = max(1, round(crop.height * width / crop.width))
    resized = crop.resize((width, height), Image.Resampling.LANCZOS)
    left = round(float(layout["center_x"]) - width / 2)
    top = round(float(layout["bottom_y"]) - height)
    if left < 0 or top < 0 or left + width > canvas or top + height > canvas:
        raise ValueError(f"layout clips its crop at {(left, top, left + width, top + height)}")
    layer = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    layer.alpha_composite(resized, (left, top))
    return layer


def validate_bounds(name, image, threshold, safe):
    bbox = meaningful_bbox(image, threshold)
    if bbox is None:
        raise ValueError(f"{name} output has no meaningful alpha")
    if bbox[0] < safe[0] or bbox[1] < safe[0] or bbox[2] > safe[1] or bbox[3] > safe[1]:
        raise ValueError(f"{name} output outside safe bounds {safe}: {bbox}")
    return bbox


def make_preview(unit, layers, assembled, destination, parts=PARTS, canvas=512):
    tile, header = 256, 48
    sheet = Image.new("RGBA", (tile * 5, tile + header), (37, 44, 52, 255))
    draw = ImageDraw.Draw(sheet)
    for index, name in enumerate((*parts, "Assembled")):
        art = assembled if name == "Assembled" else layers[name]
        checker = Image.new("RGBA", (canvas, canvas), (83, 92, 103, 255))
        painter = ImageDraw.Draw(checker)
        checker_size = max(16, canvas // 16)
        for y in range(0, canvas, checker_size):
            for x in range(0, canvas, checker_size):
                if (x // checker_size + y // checker_size) % 2:
                    painter.rectangle((x, y, x + checker_size - 1, y + checker_size - 1), fill=(66, 75, 86, 255))
        checker.alpha_composite(art)
        sheet.alpha_composite(checker.resize((tile, tile), Image.Resampling.LANCZOS), (tile * index, header))
        draw.text((tile * index + 6, 28), name, fill=(238, 242, 247, 255))
    draw.text((6, 6), unit + " — v002 alpha packaging", fill=(255, 213, 79, 255))
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(destination)


def make_roster_grid(preview_dir, output_root, part_name=None):
    """Render a labelled 5x3 review grid from packaged output, never source sheets."""
    cell, columns = 320, 5
    grid = Image.new("RGBA", (cell * columns, cell * 3), (28, 34, 41, 255))
    draw = ImageDraw.Draw(grid)
    for index, unit in enumerate(ROSTER_IDS):
        source = (preview_dir / f"{unit}_assembled.png" if part_name is None
                  else output_root / unit / f"{part_name}.png")
        if not source.is_file():
            raise FileNotFoundError(f"missing packaged output for roster grid: {source}")
        art = Image.open(source).convert("RGBA").resize((cell, cell), Image.Resampling.LANCZOS)
        x, y = (index % columns) * cell, (index // columns) * cell
        grid.alpha_composite(art, (x, y))
        draw.rectangle((x, y, x + 126, y + 22), fill=(28, 34, 41, 224))
        draw.text((x + 6, y + 4), unit, fill=(244, 247, 250, 255))
    filename = "Roster_assembled.png" if part_name is None else "Roster_fronts.png"
    preview_dir.mkdir(parents=True, exist_ok=True)
    grid.convert("RGB").save(preview_dir / filename)


def layout_for(unit, recipes):
    unit_recipe = recipes.get("units", {}).get(unit, {})
    name = unit_recipe.get("template", recipes["default_template"])
    if name not in recipes["templates"]:
        raise ValueError(f"unknown recipe template {name!r}")
    layout = {part: dict(recipes["templates"][name][part]) for part in PARTS}
    for part, override in unit_recipe.get("overrides", {}).items():
        layout[part].update(override)
    return layout


def draw_order_for(unit, recipes):
    order = recipes.get("units", {}).get(unit, {}).get("draw_order", PARTS)
    if len(order) != len(PARTS) or set(order) != set(PARTS):
        raise ValueError(f"invalid draw_order for {unit}: {order}")
    return order


def package(unit, args, recipes, ordinal):
    source_path = SOURCE_DIR / f"{unit}_Source.png"
    if not source_path.is_file():
        raise FileNotFoundError(source_path)
    threshold, canvas = int(recipes["alpha_threshold"]), int(recipes["canvas_size"])
    safe = tuple(recipes["safe_content_bounds"])
    source = Image.open(source_path).convert("RGBA")
    crops = crop_cells(source, threshold)
    layout = layout_for(unit, recipes)
    draw_order = draw_order_for(unit, recipes)
    layers = {part: position(crops[part], layout[part], canvas) for part in PARTS}
    metrics = {part: validate_bounds(part, layers[part], threshold, safe) for part in PARTS}
    assembled = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    for part in draw_order:
        assembled.alpha_composite(layers[part])
    metrics["Assembled"] = validate_bounds("Assembled", assembled, threshold, safe)

    target = args.output_root / unit
    target.mkdir(parents=True, exist_ok=True)
    for part in PARTS:
        layers[part].save(target / f"{part}.png")
    args.preview_dir.mkdir(parents=True, exist_ok=True)
    assembled.save(args.preview_dir / f"{unit}_assembled.png")
    make_preview(unit, layers, assembled, args.preview_dir / f"{unit}_layers.png")
    print(unit + ": " + ", ".join(f"{part}={bbox}" for part, bbox in metrics.items()))


def package_boss(args):
    boss_id = "B01_SawmillTower"
    recipes = json.loads(args.boss_recipes.read_text())
    recipe = recipes["bosses"][boss_id]
    threshold, canvas = int(recipes["alpha_threshold"]), int(recipes["canvas_size"])
    source_path = SOURCE_DIR / f"{boss_id}_Source.png"
    source = Image.open(source_path).convert("RGBA")
    crops = crop_cells(source, threshold, BOSS_PARTS)
    layers = {part: position(crops[part], recipe["parts"][part], canvas) for part in BOSS_PARTS}
    safe = tuple(recipes["safe_content_bounds"])
    metrics = {part: validate_bounds(part, layers[part], threshold, safe) for part in BOSS_PARTS}
    draw_order = recipe["draw_order"]
    if len(draw_order) != len(BOSS_PARTS) or set(draw_order) != set(BOSS_PARTS):
        raise ValueError(f"invalid boss draw order: {draw_order}")
    assembled = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    for part in draw_order:
        assembled.alpha_composite(layers[part])
    metrics["Assembled"] = validate_bounds("Assembled", assembled, threshold, safe)
    target = args.output_root / boss_id
    target.mkdir(parents=True, exist_ok=True)
    for part in BOSS_PARTS:
        layers[part].save(target / f"{part}.png")
    args.preview_dir.mkdir(parents=True, exist_ok=True)
    assembled.save(args.preview_dir / f"{boss_id}_assembled.png")
    make_preview(boss_id, layers, assembled, args.preview_dir / f"{boss_id}_layers.png", BOSS_PARTS, canvas)
    print(boss_id + ": " + ", ".join(f"{part}={bbox}" for part, bbox in metrics.items()))


def main():
    args = arguments()
    if args.boss:
        if args.all or args.unit or args.roster_grid:
            raise SystemExit("--boss cannot be combined with --all, --unit, or --roster-grid.")
        package_boss(args)
        return
    if args.roster_grid:
        if args.all or args.unit:
            raise SystemExit("--roster-grid cannot be combined with --all or --unit.")
        make_roster_grid(args.preview_dir, args.output_root)
        make_roster_grid(args.preview_dir, args.output_root, "Front")
        return
    if args.all == bool(args.unit):
        raise SystemExit("Choose exactly one of --all or --unit.")
    recipes = json.loads(args.recipes.read_text())
    available = sorted(p.name.removesuffix("_Source.png") for p in SOURCE_DIR.glob("E*_Source.png"))
    units = args.unit or available
    failures = []
    for ordinal, unit in enumerate(units, 1):
        try:
            package(unit, args, recipes, ordinal)
        except (FileNotFoundError, ValueError, OSError) as error:
            failures.append(f"{unit}: {error}")
    if failures:
        print("\n".join(failures), file=sys.stderr)
        raise SystemExit(1)


if __name__ == "__main__":
    main()
