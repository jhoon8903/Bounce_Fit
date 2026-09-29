#!/usr/bin/env python3
"""Package approved 2x2 modular enemy sources into aligned Unity sprite layers.

This tool only crops and resizes existing RGBA source pixels. It performs no image
generation, recoloring, inpainting, or chroma-key removal. Each output layer keeps
the original pixels' alpha and shares a 512px centred-canvas registration frame.
"""

import argparse
import json
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PARTS = ("Rear", "Rider", "Front", "Equipment")
DRAW_ORDER = PARTS
SOURCE_SUFFIX = "_Source.png"
ROOT = Path(__file__).resolve().parents[4]
DEFAULT_OUTPUT = ROOT / "Assets/Projects/MVP/Art/Enemies/Stage1"
DEFAULT_PREVIEW = Path(__file__).resolve().parent / "previews"


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unit", action="append", help="Package one ID; may be repeated.")
    parser.add_argument("--all", action="store_true", help="Package every available *_Source.png.")
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--preview-dir", type=Path, default=DEFAULT_PREVIEW)
    parser.add_argument("--recipes", type=Path, default=Path(__file__).with_name("layer_recipes.json"))
    return parser.parse_args()


def alpha_bbox(image, threshold):
    alpha = image.getchannel("A")
    return alpha.point(lambda value: 255 if value > threshold else 0).getbbox()


def source_parts(image, threshold):
    width, height = image.size
    if width != height or width % 2 or height % 2:
        raise ValueError(f"source must be an even square 2x2 sheet, got {image.size}")
    if image.getchannel("A").getbbox() is None:
        raise ValueError("source has no alpha content")
    if image.getchannel("A").getextrema()[0] == 255:
        raise ValueError("source is fully opaque; native alpha is required (chroma keying is disabled)")

    half = width // 2
    cells = ((0, 0, half, half), (half, 0, width, half),
             (0, half, half, height), (half, half, width, height))
    result = {}
    for name, cell in zip(PARTS, cells):
        part = image.crop(cell)
        bbox = alpha_bbox(part, threshold)
        if bbox is None:
            raise ValueError(f"{name} cell has no meaningful alpha (threshold > {threshold})")
        # Keep a two-pixel source margin so antialiased outline pixels are preserved.
        left, top, right, bottom = bbox
        bbox = (max(0, left - 2), max(0, top - 2), min(half, right + 2), min(half, bottom + 2))
        result[name] = part.crop(bbox)
    return result


def place_part(source, recipe, canvas_size):
    width = int(recipe["width"])
    height = max(1, round(source.height * width / source.width))
    if width > canvas_size or height > canvas_size:
        raise ValueError(f"recipe produces oversized part {width}x{height}")
    resized = source.resize((width, height), Image.Resampling.LANCZOS)
    left = round(float(recipe["center_x"]) - width / 2)
    top = round(float(recipe["bottom_y"]) - height)
    if left < 0 or top < 0 or left + width > canvas_size or top + height > canvas_size:
        raise ValueError(f"recipe clips part at {(left, top, left + width, top + height)}")
    output = Image.new("RGBA", (canvas_size, canvas_size), (0, 0, 0, 0))
    output.alpha_composite(resized, (left, top))
    return output


def validate_output(unit, part_name, image, threshold):
    bbox = alpha_bbox(image, threshold)
    if bbox is None:
        raise ValueError(f"{unit}/{part_name} has no meaningful output alpha")
    left, top, right, bottom = bbox
    if left < 1 or top < 1 or right > image.width - 1 or bottom > image.height - 1:
        raise ValueError(f"{unit}/{part_name} touches a canvas edge: {bbox}")
    return bbox


def make_preview(unit, layers, preview_path, threshold):
    tile = 256
    sheet = Image.new("RGBA", (tile * 5, tile + 34), (37, 44, 52, 255))
    draw = ImageDraw.Draw(sheet)
    for index, name in enumerate((*PARTS, "Assembled")):
        image = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        if name == "Assembled":
            for part_name in DRAW_ORDER:
                image.alpha_composite(layers[part_name])
        else:
            image = layers[name]
        checker = Image.new("RGBA", (512, 512), (82, 91, 102, 255))
        checker_draw = ImageDraw.Draw(checker)
        for y in range(0, 512, 32):
            for x in range(0, 512, 32):
                if (x // 32 + y // 32) % 2:
                    checker_draw.rectangle((x, y, x + 31, y + 31), fill=(65, 73, 84, 255))
        checker.alpha_composite(image)
        sheet.alpha_composite(checker.resize((tile, tile), Image.Resampling.LANCZOS), (index * tile, 34))
        draw.text((index * tile + 6, 8), name, fill=(238, 242, 247, 255))
    draw.text((6, 8), unit + "  ", fill=(255, 213, 79, 255))
    preview_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(preview_path)


def recipe_for(unit, recipes):
    unit_config = recipes.get("units", {}).get(unit, {})
    template_name = unit_config.get("template", recipes["default_template"])
    try:
        template = recipes["templates"][template_name]
    except KeyError as error:
        raise ValueError(f"unknown recipe template {template_name!r} for {unit}") from error
    merged = {part: dict(template[part]) for part in PARTS}
    for part, values in unit_config.get("overrides", {}).items():
        merged[part].update(values)
    return merged


def package_unit(unit, args, recipes, ordinal):
    source_path = Path(__file__).with_name(unit + SOURCE_SUFFIX)
    if not source_path.is_file():
        raise FileNotFoundError(source_path)
    source = Image.open(source_path).convert("RGBA")
    threshold = int(recipes["alpha_threshold"])
    cells = source_parts(source, threshold)
    recipe = recipe_for(unit, recipes)
    canvas_size = int(recipes["canvas_size"])
    layers = {part: place_part(cells[part], recipe[part], canvas_size) for part in PARTS}
    metrics = {part: validate_output(unit, part, layers[part], threshold) for part in PARTS}

    output_folder = args.output_root / unit
    output_folder.mkdir(parents=True, exist_ok=True)
    for part in PARTS:
        layers[part].save(output_folder / f"{part}.png")

    assembled = Image.new("RGBA", (canvas_size, canvas_size), (0, 0, 0, 0))
    for part in DRAW_ORDER:
        assembled.alpha_composite(layers[part])
    metrics["Assembled"] = validate_output(unit, "Assembled", assembled, threshold)
    args.preview_dir.mkdir(parents=True, exist_ok=True)
    assembled.save(args.preview_dir / f"{ordinal:02d}_{unit}_assembled.png")
    make_preview(unit, layers, args.preview_dir / f"{ordinal:02d}_{unit}_layers.png", threshold)
    print(unit + ": " + ", ".join(f"{name}={bbox}" for name, bbox in metrics.items()))


def main():
    args = parse_args()
    if args.all == bool(args.unit):
        raise SystemExit("Choose exactly one of --all or --unit.")
    recipes = json.loads(args.recipes.read_text())
    available = sorted(path.name[:-len(SOURCE_SUFFIX)] for path in Path(__file__).parent.glob("*" + SOURCE_SUFFIX))
    units = args.unit if args.unit else available
    if not units:
        raise SystemExit("No source PNGs found.")
    failures = []
    for ordinal, unit in enumerate(units, start=1):
        try:
            package_unit(unit, args, recipes, ordinal)
        except (FileNotFoundError, ValueError, OSError) as error:
            failures.append(f"{unit}: {error}")
    if failures:
        print("\n".join(failures), file=sys.stderr)
        raise SystemExit(1)


if __name__ == "__main__":
    main()
