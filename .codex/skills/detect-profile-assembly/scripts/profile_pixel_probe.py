#!/usr/bin/env python3
"""Find candidate construction lines and grayscale minima in an image or crop."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import cv2
import numpy as np


def parse_numbers(value: str, count: int, label: str) -> tuple[int, ...]:
    try:
        result = tuple(int(round(float(item.strip()))) for item in value.split(","))
    except ValueError as exc:
        raise argparse.ArgumentTypeError(f"{label} vereist {count} getallen") from exc
    if len(result) != count:
        raise argparse.ArgumentTypeError(f"{label} vereist {count} getallen")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--crop", type=lambda value: parse_numbers(value, 4, "crop"), help="x,y,b,h")
    parser.add_argument("--scan", type=lambda value: parse_numbers(value, 4, "scan"), help="x1,y1,x2,y2 in originele beeldcoördinaten")
    parser.add_argument("--module-pixels", type=float, help="Pixelafstand die kandidaat 40 mm vertegenwoordigt")
    parser.add_argument("--min-line", type=int, default=60, help="Minimale Hough-lijnlengte")
    parser.add_argument("--overlay", type=Path)
    parser.add_argument("--json-output", type=Path)
    args = parser.parse_args()

    image = cv2.imread(str(args.image), cv2.IMREAD_COLOR)
    if image is None:
        raise SystemExit(f"Afbeelding niet leesbaar: {args.image}")
    original_h, original_w = image.shape[:2]
    offset_x = offset_y = 0
    if args.crop:
        x, y, width, height = args.crop
        x = max(0, min(original_w - 1, x)); y = max(0, min(original_h - 1, y))
        width = max(1, min(original_w - x, width)); height = max(1, min(original_h - y, height))
        image = image[y:y + height, x:x + width].copy(); offset_x, offset_y = x, y

    gray = cv2.GaussianBlur(cv2.cvtColor(image, cv2.COLOR_BGR2GRAY), (5, 5), 0)
    edges = cv2.Canny(gray, 45, 135)
    raw = cv2.HoughLinesP(edges, 1, np.pi / 180, threshold=35, minLineLength=args.min_line, maxLineGap=14)
    segments, overlay = [], image.copy()
    palette = ((40, 190, 40), (40, 160, 230), (220, 80, 180), (180, 180, 40))
    if raw is not None:
        for row in raw[:, 0, :]:
            x1, y1, x2, y2 = map(int, row)
            dx, dy = x2 - x1, y2 - y1
            length = math.hypot(dx, dy); angle = math.degrees(math.atan2(dy, dx)) % 180.0
            cluster = int(((angle + 22.5) % 180.0) // 45.0)
            segments.append({"x1": x1 + offset_x, "y1": y1 + offset_y, "x2": x2 + offset_x, "y2": y2 + offset_y,
                             "length_px": round(length, 2), "angle_deg": round(angle, 2), "angle_cluster": cluster})
            cv2.line(overlay, (x1, y1), (x2, y2), palette[cluster], 2, cv2.LINE_AA)
    segments.sort(key=lambda item: item["length_px"], reverse=True)

    scan_report = None
    if args.scan:
        x1, y1, x2, y2 = args.scan
        crop_line = (x1 - offset_x, y1 - offset_y, x2 - offset_x, y2 - offset_y)
        samples = max(2, int(round(math.hypot(crop_line[2] - crop_line[0], crop_line[3] - crop_line[1]))) + 1)
        xs = np.linspace(crop_line[0], crop_line[2], samples); ys = np.linspace(crop_line[1], crop_line[3], samples)
        xi = np.clip(np.rint(xs).astype(int), 0, gray.shape[1] - 1); yi = np.clip(np.rint(ys).astype(int), 0, gray.shape[0] - 1)
        values = gray[yi, xi].astype(float)
        smooth = np.convolve(values, np.ones(5) / 5.0, mode="same")
        threshold = float(np.percentile(smooth[2:-2] if len(smooth) > 4 else smooth, 35))
        minima, last = [], -10
        for index in range(2, len(smooth) - 2):
            if index - last >= 4 and smooth[index] <= threshold and smooth[index] == min(smooth[index - 2:index + 3]):
                minima.append(index); last = index
        scan_report = {"line": [x1, y1, x2, y2], "sample_count": samples, "dark_minima_indices": minima,
                       "successive_distances_px": [minima[i] - minima[i - 1] for i in range(1, len(minima))],
                       "threshold_gray": round(threshold, 2)}
        cv2.line(overlay, crop_line[:2], crop_line[2:], (0, 0, 255), 2, cv2.LINE_AA)
        for index in minima:
            cv2.circle(overlay, (int(round(xs[index])), int(round(ys[index]))), 4, (0, 255, 255), -1)

    result = {"image": str(args.image), "image_size_px": [original_w, original_h], "crop": args.crop,
              "line_segments": segments, "scan": scan_report}
    if args.module_pixels and args.module_pixels > 0:
        result["candidate_mm_per_pixel"] = 40.0 / args.module_pixels
    if args.overlay:
        args.overlay.parent.mkdir(parents=True, exist_ok=True); cv2.imwrite(str(args.overlay), overlay)
    output = json.dumps(result, ensure_ascii=False, indent=2)
    if args.json_output: args.json_output.write_text(output, encoding="utf-8")
    else: print(output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
