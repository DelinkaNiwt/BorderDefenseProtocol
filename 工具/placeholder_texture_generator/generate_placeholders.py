#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
RimWorld占位贴图生成器
按照贴图指导规范生成规范的占位贴图，用于测试和开发

功能：
- 自动生成各类别占位贴图
- 创建规范的目录结构
- 按照命名规范生成文件
- 支持多方向、随机变体、身体类型等贴图类型
"""

import os
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

# 处理Windows编码问题
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer)

# 占位贴图定义：包含所有需要生成的贴图类别、尺寸、路径等
PLACEHOLDER_TEXTURES = {

    # ===================== Trion MVP相关 =====================
    'Things/Building/Trion': {
        'Building_TriggerConfigBench': {
            'size': (192, 192),
            'color': '#8B008B',  # 深紫色，代表控制
            'label': 'ConfigBench',
        },
        'Building_BailOutAnchor': {
            'size': (128, 128),
            'color': '#FF6347',  # 番茄红，代表紧急脱离点
            'label': 'BailOutAnchor',
        },
        'VitalsMonitor': {
            'size': (128, 128),
            'color': '#00CED1',  # 深青色，代表医疗设备
            'label': 'VitalsMonitor',
        },
    },

    'Things/Building': {
        'TrionDetector': {
            'size': (128, 128),
            'color': '#4169E1',  # 皇家蓝，代表高科技检测仪
            'label': 'Detector',
        },
    },

    'Items/Trion': {
        'TrionTrigger_Standard': {
            'size': (64, 64),
            'color': '#9932CC',  # 深兰花紫，代表触发器
            'label': 'Trigger',
        },
    },

    'Things/Pawn/Humanlike/TrionComponents': {
        'ArcBlade': {
            'size': (64, 64),
            'color': '#FFD700',  # 金色，代表近战武器
            'label': 'ArcBlade',
        },
        'Shield': {
            'size': (64, 64),
            'color': '#00CED1',  # 深青色，代表防御
            'label': 'Shield',
        },
        'ExplosiveBullet': {
            'size': (64, 64),
            'color': '#FF4500',  # 橙红色，代表爆炸
            'label': 'Bullet',
        },
        'Chameleon': {
            'size': (64, 64),
            'color': '#228B22',  # 森林绿，代表隐身
            'label': 'Chameleon',
        },
        'BailOut': {
            'size': (64, 64),
            'color': '#DC143C',  # 深绯红，代表紧急脱离
            'label': 'BailOut',
        },
    },
}


def hex_to_rgb(hex_color):
    """将16进制颜色转换为RGB元组"""
    hex_color = hex_color.lstrip('#')
    return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))


def create_placeholder_image(size, color, label, variant_label=None, with_text=True):
    """
    创建占位贴图

    Args:
        size: 图片大小 (width, height)
        color: 背景颜色（16进制）
        label: 标签文本
        variant_label: 变式标签（如 "South"、"Female"、"_a" 等）
        with_text: 是否添加文本标签

    Returns:
        PIL Image对象
    """
    # 创建图像
    img = Image.new('RGBA', size, hex_to_rgb(color) + (255,))
    draw = ImageDraw.Draw(img)

    # 添加网格线（显示尺寸）
    grid_color = tuple(
        int(c * 0.5) if c < 128 else int(c * 1.2)
        for c in hex_to_rgb(color)[:3]
    ) + (128,)

    # 绘制网格（每32px一条）
    for i in range(0, size[0], 32):
        draw.line([(i, 0), (i, size[1])], fill=grid_color, width=1)
    for i in range(0, size[1], 32):
        draw.line([(0, i), (size[0], i)], fill=grid_color, width=1)

    # 文本颜色
    text_color = (255, 255, 255, 255) if sum(hex_to_rgb(color)) < 382 else (0, 0, 0, 255)

    # 添加尺寸文本（上方中央）
    if with_text:
        size_text = f"{size[0]}×{size[1]}"

        # 计算文本位置（上方）
        bbox = draw.textbbox((0, 0), size_text)
        text_width = bbox[2] - bbox[0]
        text_height = bbox[3] - bbox[1]
        x = (size[0] - text_width) // 2
        y = 5

        # 绘制文本背景
        padding = 3
        draw.rectangle(
            [x-padding, y-padding, x+text_width+padding, y+text_height+padding],
            fill=hex_to_rgb(color) if sum(hex_to_rgb(color)) < 382 else (255, 255, 255),
            outline=text_color
        )
        # 绘制文本
        draw.text((x, y), size_text, fill=text_color)

    # 添加变式标签（如果有）
    if variant_label:
        # 绘制中央标签（主要标识）
        center_y = (size[1] - 20) // 2

        # 绘制标签背景
        padding = 8
        bbox = draw.textbbox((0, 0), variant_label)
        text_width = bbox[2] - bbox[0]
        text_height = bbox[3] - bbox[1]
        x = (size[0] - text_width) // 2

        # 使用对比色背景和文本（与背景互补）
        bg_color = (255, 255, 255, 220) if sum(hex_to_rgb(color)) < 382 else (0, 0, 0, 220)
        variant_text_color = (0, 0, 0, 255) if sum(hex_to_rgb(color)) < 382 else (255, 255, 255, 255)

        draw.rectangle(
            [x-padding, center_y-padding, x+text_width+padding, center_y+text_height+padding],
            fill=bg_color
        )
        # 绘制文本
        draw.text((x, center_y), variant_label, fill=variant_text_color)

    return img


def create_mask_image(size):
    """创建遮罩贴图（灰度）"""
    # 遮罩使用灰度，创建一个中等灰度的遮罩
    img = Image.new('L', size, 128)
    draw = ImageDraw.Draw(img)

    # 绘制圆形渐变区域（示意衣物形状）
    center_x, center_y = size[0] // 2, size[1] // 2
    radius = min(size[0], size[1]) // 3

    # 绘制椭圆
    draw.ellipse(
        [center_x - radius, center_y - radius, center_x + radius, center_y + radius],
        fill=200
    )

    return img


def generate_placeholders(output_base_path):
    """
    生成所有占位贴图

    Args:
        output_base_path: 输出基础路径
    """
    output_base = Path(output_base_path)
    created_files = []
    created_dirs = []

    print("🎨 开始生成占位贴图...")
    print(f"📁 输出路径: {output_base}\n")

    for category_path, textures in PLACEHOLDER_TEXTURES.items():
        category_dir = output_base / category_path
        category_dir.mkdir(parents=True, exist_ok=True)
        created_dirs.append(str(category_dir.relative_to(output_base)))

        print(f"📂 {category_path}/")

        for texture_name, config in textures.items():
            size = config['size']
            color = config['color']
            label = config.get('label', texture_name)

            # ========== 基础贴图 ==========
            if 'directions' not in config and 'variants' not in config and 'states' not in config and 'body_types' not in config:
                # 简单贴图
                img = create_placeholder_image(size, color, label)
                file_path = category_dir / f"{texture_name}.png"
                img.save(file_path)
                created_files.append(str(file_path.relative_to(output_base)))
                print(f"  ✓ {texture_name}.png ({size[0]}×{size[1]})")

            # ========== 多方向+身体类型组合（人物） ==========
            elif 'directions' in config and 'body_types' in config:
                body_types = config['body_types']
                directions = config['directions']

                # 为每个方向和身体类型的组合生成贴图
                for body_type in body_types:
                    for direction in directions:
                        variant_text = f"{body_type}_{direction}"
                        img = create_placeholder_image(size, color, label, variant_label=variant_text)
                        file_path = category_dir / f"{texture_name}_{body_type}_{direction}.png"
                        img.save(file_path)
                        created_files.append(str(file_path.relative_to(output_base)))

                body_types_str = " | ".join(body_types)
                dirs_str = " | ".join(directions)
                print(f"  ✓ {texture_name}_[{body_types_str}]_[{dirs_str}].png ({size[0]}×{size[1]})")
                print(f"     共 {len(body_types) * len(directions)} 个文件")

            # ========== 多方向贴图 ==========
            elif 'directions' in config:
                for direction in config['directions']:
                    img = create_placeholder_image(size, color, label, variant_label=direction)
                    file_path = category_dir / f"{texture_name}_{direction}.png"
                    img.save(file_path)
                    created_files.append(str(file_path.relative_to(output_base)))
                print(f"  ✓ {texture_name}_[North|East|South|West].png ({size[0]}×{size[1]})")

            # ========== 身体类型变体（衣物） ==========
            if 'variants' in config and any(v in ['Male', 'Female', 'Thin', 'Hulk', 'Fat'] for v in config['variants']):
                for variant in config['variants']:
                    img = create_placeholder_image(size, color, label, variant_label=variant)
                    file_path = category_dir / f"{texture_name}_{variant}.png"
                    img.save(file_path)
                    created_files.append(str(file_path.relative_to(output_base)))

                    # 生成遮罩贴图
                    if config.get('mask'):
                        mask_img = create_mask_image(size)
                        mask_path = category_dir / f"{texture_name}_{variant}_mask.png"
                        mask_img.save(mask_path)
                        created_files.append(str(mask_path.relative_to(output_base)))

                variants_str = " | ".join(config['variants'])
                print(f"  ✓ {texture_name}_[{variants_str}].png ({size[0]}×{size[1]})")
                if config.get('mask'):
                    print(f"  ✓ {texture_name}_[{variants_str}]_mask.png (遮罩)")

            # ========== 随机变体（植物等） ==========
            elif 'variants' in config:
                for variant in config['variants']:
                    img = create_placeholder_image(size, color, label, variant_label=f"_{variant}")
                    file_path = category_dir / f"{texture_name}_{variant}.png"
                    img.save(file_path)
                    created_files.append(str(file_path.relative_to(output_base)))

                variants_str = " | ".join(config['variants'])
                print(f"  ✓ {texture_name}_[{variants_str}].png ({size[0]}×{size[1]})")

            # ========== 状态变体（动物等） ==========
            if 'states' in config:
                for state in config['states'].keys():
                    state_label = state.capitalize()
                    img = create_placeholder_image(size, color, label, variant_label=state_label)
                    file_path = category_dir / f"{texture_name}_{state_label}.png"
                    img.save(file_path)
                    created_files.append(str(file_path.relative_to(output_base)))

                states_str = " | ".join(config['states'].keys())
                print(f"  ✓ {texture_name}_[{states_str}].png (状态贴图)")

        print()

    # 生成统计信息
    print("\n" + "="*60)
    print(f"✅ 生成完成！")
    print(f"📊 统计信息:")
    print(f"   - 创建目录数: {len(created_dirs)}")
    print(f"   - 创建贴图文件数: {len([f for f in created_files if f.endswith('.png')])}")
    print(f"   - 总文件数: {len(created_files)}")
    print("="*60)

    return created_files, created_dirs


def main():
    """主函数"""
    # 获取输出路径
    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent

    # 优先检查MVP项目目录是否存在
    mvp_output_path = project_root / "临时" / "Trion_MVP_CodeDeliverables" / "Textures"

    if mvp_output_path.exists():
        output_path = mvp_output_path
        print(f"📍 检测到MVP项目目录，输出到: {output_path}\n")
    else:
        output_path = project_root / "参考资源" / "通用资源" / "占位贴图"
        print(f"📍 使用默认输出路径: {output_path}\n")

    # 验证路径
    if not output_path.parent.exists():
        print(f"❌ 输出目录不存在: {output_path.parent}")
        sys.exit(1)

    # 生成占位贴图
    try:
        files, dirs = generate_placeholders(str(output_path))
        print(f"\n✨ 占位贴图已保存到: {output_path}")
    except Exception as e:
        print(f"❌ 生成失败: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == '__main__':
    main()
