"""确定性绘制弧面矩形能量盾预览。"""

from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


# 画布与颜色：完全沿用六边形参考图的单色像素逻辑。
CANVAS_SIZE = 512
SUPERSAMPLE = 4
ENERGY_RGB = (0, 200, 220)
BODY_ALPHA = 90
DEPTH_ALPHA = 110
INNER_BAND_ALPHA = 130
RIM_ALPHA = 240
HALO_ALPHA = 20


def cubic_points(
    start: tuple[float, float],
    control_a: tuple[float, float],
    control_b: tuple[float, float],
    end: tuple[float, float],
    count: int = 48,
) -> list[tuple[float, float]]:
    """按三次贝塞尔曲线生成等间隔采样点。"""

    points: list[tuple[float, float]] = []
    for index in range(1, count + 1):
        t = index / count
        inverse = 1.0 - t
        x = (
            inverse**3 * start[0]
            + 3 * inverse**2 * t * control_a[0]
            + 3 * inverse * t**2 * control_b[0]
            + t**3 * end[0]
        )
        y = (
            inverse**3 * start[1]
            + 3 * inverse**2 * t * control_a[1]
            + 3 * inverse * t**2 * control_b[1]
            + t**3 * end[1]
        )
        points.append((x, y))
    return points


def build_shield_outline() -> list[tuple[float, float]]:
    """构造与已确认预览一致的横向弧面矩形轮廓。"""

    points: list[tuple[float, float]] = [(26, 156)]
    points.extend(cubic_points((26, 156), (143, 126), (369, 126), (486, 156)))
    points.extend([(486, 169), (476, 182)])
    points.extend(cubic_points((476, 182), (482, 230), (482, 282), (476, 330)))
    points.extend([(487, 343), (487, 356)])
    points.extend(cubic_points((487, 356), (369, 386), (143, 386), (25, 356)))
    points.extend([(25, 343), (36, 330)])
    points.extend(cubic_points((36, 330), (30, 282), (30, 230), (36, 182)))
    points.extend([(25, 169), (26, 156)])
    return points


def scaled_points(points: list[tuple[float, float]]) -> list[tuple[int, int]]:
    """把最终画布坐标转换到高倍抗锯齿画布。"""

    return [
        (round(x * SUPERSAMPLE), round(y * SUPERSAMPLE))
        for x, y in points
    ]


def draw_alpha_mask() -> Image.Image:
    """绘制透明主体、明亮内边和克制外缘光晕。"""

    size = CANVAS_SIZE * SUPERSAMPLE
    shape = Image.new("L", (size, size), 0)
    ImageDraw.Draw(shape).polygon(scaled_points(build_shield_outline()), fill=255)

    # 只用透明度塑造柔和弧面：上下保持 90，中部最高 110，不改变单色 RGB。
    gradient_values: list[int] = []
    for y in range(size):
        final_y = y / SUPERSAMPLE
        normalized = min(1.0, abs(final_y - CANVAS_SIZE / 2) / 125.0)
        weight = (1.0 - normalized) ** 1.6
        gradient_values.append(round(BODY_ALPHA + (DEPTH_ALPHA - BODY_ALPHA) * weight))
    gradient_strip = Image.new("L", (1, size))
    gradient_strip.putdata(gradient_values)
    body_gradient = gradient_strip.resize((size, size), Image.Resampling.NEAREST)
    body = ImageChops.multiply(body_gradient, shape)

    # 三像素内侧亮边，避免光晕污染透明背景。
    eroded = shape.filter(ImageFilter.MinFilter(25))
    rim_mask = ImageChops.subtract(shape, eroded)
    rim = rim_mask.point(lambda value: RIM_ALPHA if value else 0)

    # 在亮边内侧增加四像素次级边缘，提供厚度但不做机械分块。
    deeply_eroded = shape.filter(ImageFilter.MinFilter(57))
    broad_rim_mask = ImageChops.subtract(shape, deeply_eroded)
    inner_band_mask = ImageChops.subtract(broad_rim_mask, rim_mask)
    inner_band = inner_band_mask.point(
        lambda value: INNER_BAND_ALPHA if value else 0
    )

    # 两像素弱外缘只负责柔化边界，不引入颜色杂质。
    expanded = shape.filter(ImageFilter.MaxFilter(17))
    halo_mask = ImageChops.subtract(expanded, shape)
    halo = halo_mask.point(lambda value: HALO_ALPHA if value else 0)

    combined = ImageChops.lighter(body, inner_band)
    combined = ImageChops.lighter(combined, rim)
    combined = ImageChops.lighter(combined, halo)
    resized = combined.resize(
        (CANVAS_SIZE, CANVAS_SIZE),
        Image.Resampling.LANCZOS,
    )
    # Lanczos（高质量缩放算法）会在高反差边缘产生轻微过冲；限制上限以保持参考图无全不透明像素。
    return resized.point(lambda value: min(value, RIM_ALPHA))


def save_checkerboard_preview(image: Image.Image, destination: Path) -> None:
    """把透明贴图叠到深色棋盘，仅用于观察通透度和亮边。"""

    checker = Image.new("RGBA", image.size, (18, 20, 26, 255))
    draw = ImageDraw.Draw(checker)
    square = 32
    for y in range(0, CANVAS_SIZE, square):
        for x in range(0, CANVAS_SIZE, square):
            if (x // square + y // square) % 2:
                draw.rectangle(
                    (x, y, x + square - 1, y + square - 1),
                    fill=(86, 92, 104, 255),
                )
    Image.alpha_composite(checker, image).save(destination, "PNG", optimize=True)


def main() -> None:
    """生成 512×512 透明 PNG 预览并输出像素校验信息。"""

    alpha = draw_alpha_mask()
    # 完全透明区域同时清空 RGB，避免预览器或游戏缩放采样出整张青色底。
    visible_mask = alpha.point(lambda value: 255 if value else 0)
    red = Image.new("L", (CANVAS_SIZE, CANVAS_SIZE), ENERGY_RGB[0])
    green = visible_mask.point(lambda value: ENERGY_RGB[1] if value else 0)
    blue = visible_mask.point(lambda value: ENERGY_RGB[2] if value else 0)
    output = Image.merge("RGBA", (red, green, blue, alpha))

    destination = Path(os.environ["BDP_SHIELD_PREVIEW_OUT"])
    output.save(destination, "PNG", optimize=True)

    checker_destination = os.environ.get("BDP_SHIELD_CHECKER_OUT")
    if checker_destination:
        save_checkerboard_preview(output, Path(checker_destination))

    visible = [pixel for pixel in output.get_flattened_data() if pixel[3] > 0]
    unique_rgb = {pixel[:3] for pixel in visible}
    opaque_pixels = sum(pixel[3] == 255 for pixel in visible)
    print(
        {
            "path": str(destination),
            "size": output.size,
            "alpha_bbox": alpha.getbbox(),
            "visible_rgb": sorted(unique_rgb),
            "opaque_pixels": opaque_pixels,
        }
    )


if __name__ == "__main__":
    main()
