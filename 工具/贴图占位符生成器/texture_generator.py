#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
RimWorld 贴图占位符生成器 v1.0
Purpose: 批量生成带网格线和标注的占位贴图（开发验证用）
Description: 快速生成符合规范的占位贴图，用于模组开发的验证和测试阶段
             生成的贴图用于验证XML配置、尺寸规范、命名规则等
             最终应由美术制作真实贴图替换这些占位符

Author: RimWorld Mod Studio
Date: 2026-01-24
"""

import argparse
import json
import os
import sys
from pathlib import Path
from typing import Dict, List, Tuple, Optional, Union
import math

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    print("[ERROR] 需要安装 Pillow 库")
    print("运行命令: pip install Pillow")
    sys.exit(1)


class TextureGenerator:
    """贴图生成器主类"""

    # 色彩预设库
    PRESETS = {
        "blue": (100, 150, 200),
        "red": (200, 100, 100),
        "green": (100, 180, 100),
        "purple": (150, 100, 200),
        "yellow": (200, 180, 80),
        "gray": (120, 120, 120),
    }

    # 2的幂次方列表
    VALID_SIZES = [64, 128, 256, 512, 1024]

    def __init__(self, output_dir: str):
        """初始化生成器

        Args:
            output_dir: 输出目录路径
        """
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.generated_files = []
        self.generation_log = []

    def validate_parameters(self, config: Dict) -> Tuple[bool, str]:
        """验证配置参数

        Args:
            config: 配置字典

        Returns:
            (是否有效, 错误信息)
        """
        errors = []

        # 检查必需字段
        if "width" not in config:
            errors.append("缺少必需参数: width")
        if "height" not in config:
            errors.append("缺少必需参数: height")
        if "output_prefix" not in config:
            errors.append("缺少必需参数: output_prefix")

        if errors:
            return False, "; ".join(errors)

        # 检查尺寸是否为2的幂次方
        width = config.get("width")
        height = config.get("height")

        if width not in self.VALID_SIZES:
            errors.append(f"width {width} 不是2的幂次方。允许值: {self.VALID_SIZES}")
        if height not in self.VALID_SIZES:
            errors.append(f"height {height} 不是2的幂次方。允许值: {self.VALID_SIZES}")

        # 检查grid_size
        grid_size = config.get("grid_size", 32)
        if width % grid_size != 0 or height % grid_size != 0:
            errors.append(f"grid_size {grid_size} 无法整除宽度或高度")

        # 检查后缀类型
        suffix_type = config.get("suffix_type", "single")
        if suffix_type not in ["single", "multi", "random", "apparel"]:
            errors.append(f"suffix_type '{suffix_type}' 无效。允许值: single, multi, random, apparel")

        # 检查色彩方案
        color_scheme = config.get("color_scheme", "blue")
        if isinstance(color_scheme, str):
            if color_scheme not in self.PRESETS:
                errors.append(f"色彩预设 '{color_scheme}' 不存在")
        elif isinstance(color_scheme, dict):
            for key in ["r", "g", "b"]:
                if key not in color_scheme:
                    errors.append(f"自定义色彩缺少参数: {key}")
                else:
                    val = color_scheme[key]
                    if not isinstance(val, int) or not (0 <= val <= 255):
                        errors.append(f"色彩参数 {key}={val} 超出范围 [0-255]")
        else:
            errors.append(f"color_scheme 类型无效")

        # 检查输出前缀
        prefix = config.get("output_prefix", "")
        if not prefix:
            errors.append("output_prefix 不能为空")
        elif not self._is_valid_filename(prefix):
            errors.append(f"output_prefix '{prefix}' 包含非法字符")

        if errors:
            return False, "; ".join(errors)

        return True, ""

    def validate_naming_constraints(self, config: Dict) -> Tuple[bool, str]:
        """验证命名约束（大小写和顺序）

        Args:
            config: 配置字典

        Returns:
            (是否有效, 错误信息)
        """
        suffix_type = config.get("suffix_type", "single")
        errors = []

        if suffix_type == "multi":
            # 验证方向为小写
            suffixes = config.get("suffix_names", [])
            for s in suffixes:
                # 检查方向是否为小写
                if s not in ["_north", "_east", "_south", "_west"]:
                    errors.append(f"方向后缀 '{s}' 无效。必须使用小写: _north, _east, _south, _west")

        elif suffix_type == "apparel":
            # 验证体型为大写首字母
            body_types = config.get("suffix_names", [])
            valid_body_types = ["_Male", "_Female", "_Thin", "_Hulk", "_Fat"]
            for bt in body_types:
                if bt not in valid_body_types:
                    errors.append(f"体型后缀 '{bt}' 无效。允许值: {valid_body_types}")

        elif suffix_type == "random":
            # 变体无特殊约束
            pass

        if errors:
            return False, "; ".join(errors)

        return True, ""

    @staticmethod
    def _is_valid_filename(name: str) -> bool:
        """检查文件名是否有效"""
        invalid_chars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*']
        return not any(char in name for char in invalid_chars)

    def get_color(self, color_scheme: Union[str, Dict]) -> Tuple[int, int, int]:
        """获取RGB色彩

        Args:
            color_scheme: 预设名称或自定义{r,g,b}字典

        Returns:
            (R, G, B) 元组
        """
        if isinstance(color_scheme, str):
            return self.PRESETS.get(color_scheme, self.PRESETS["blue"])
        elif isinstance(color_scheme, dict):
            return (
                color_scheme.get("r", 100),
                color_scheme.get("g", 100),
                color_scheme.get("b", 100)
            )
        return self.PRESETS["blue"]

    def get_contrasting_color(self, bg_color: Tuple[int, int, int]) -> Tuple[int, int, int]:
        """根据背景色获取对比色（文字颜色）

        Args:
            bg_color: 背景RGB色

        Returns:
            文字RGB色（黑或白）
        """
        # 计算亮度
        brightness = (bg_color[0] * 299 + bg_color[1] * 587 + bg_color[2] * 114) / 1000
        return (255, 255, 255) if brightness < 128 else (0, 0, 0)

    def get_font_size(self, image_size: int) -> int:
        """根据图像大小计算字体大小

        Args:
            image_size: 图像像素尺寸

        Returns:
            字体大小（点）
        """
        # 64x64 -> 8pt, 128x128 -> 10pt, 256x256 -> 14pt, 512x512 -> 20pt
        font_sizes = {
            64: 8,
            128: 10,
            256: 14,
            512: 20,
            1024: 28
        }
        return font_sizes.get(image_size, 10)

    def add_grid(self, image: Image.Image, grid_size: int, grid_color: Tuple[int, int, int],
                 grid_width: int = 1):
        """在图像上绘制网格线

        Args:
            image: PIL Image 对象
            grid_size: 网格尺寸（像素）
            grid_color: 网格颜色RGB
            grid_width: 网格线宽度
        """
        draw = ImageDraw.Draw(image)
        width, height = image.size

        # 竖线
        for x in range(grid_size, width, grid_size):
            draw.line([(x, 0), (x, height)], fill=grid_color, width=grid_width)

        # 横线
        for y in range(grid_size, height, grid_size):
            draw.line([(0, y), (width, y)], fill=grid_color, width=grid_width)

    def add_center_labels(self, image: Image.Image, width: int, height: int, suffix: str,
                         text_color: Tuple[int, int, int], bg_color: Tuple[int, int, int],
                         font_size: int):
        """在图像中央添加标签（尺寸和后缀）

        Args:
            image: PIL Image 对象
            width: 宽度
            height: 高度
            suffix: 后缀字符串
            text_color: 文字颜色
            bg_color: 背景颜色
            font_size: 字体大小
        """
        draw = ImageDraw.Draw(image)
        img_width, img_height = image.size

        # 尝试加载系统字体，失败则使用默认字体
        try:
            font = ImageFont.truetype("arial.ttf", font_size)
        except:
            font = ImageFont.load_default()

        # 文本内容
        size_text = f"{width}×{height}"
        suffix_text = suffix if suffix else "single"

        # 计算文本位置（中央）
        # 第一行：尺寸
        bbox1 = draw.textbbox((0, 0), size_text, font=font)
        text1_width = bbox1[2] - bbox1[0]
        text1_height = bbox1[3] - bbox1[1]
        x1 = (img_width - text1_width) // 2
        y1 = (img_height - text1_height - 15) // 2  # 向上偏移

        # 第二行：后缀
        bbox2 = draw.textbbox((0, 0), suffix_text, font=font)
        text2_width = bbox2[2] - bbox2[0]
        text2_height = bbox2[3] - bbox2[1]
        x2 = (img_width - text2_width) // 2
        y2 = (img_height + text2_height + 15) // 2  # 向下偏移

        # 绘制半透明黑色背景框
        padding = 4
        box_margin = 2

        # 框1
        box1 = [x1 - padding, y1 - padding, x1 + text1_width + padding, y1 + text1_height + padding]
        overlay = Image.new('RGBA', image.size, (0, 0, 0, 0))
        overlay_draw = ImageDraw.Draw(overlay)
        overlay_draw.rectangle(box1, fill=(0, 0, 0, 128))
        image = Image.alpha_composite(image.convert('RGBA'), overlay).convert('RGB')
        draw = ImageDraw.Draw(image)

        # 框2
        overlay = Image.new('RGBA', image.size, (0, 0, 0, 0))
        overlay_draw = ImageDraw.Draw(overlay)
        overlay_draw.rectangle([x2 - padding, y2 - padding, x2 + text2_width + padding, y2 + text2_height + padding],
                              fill=(0, 0, 0, 128))
        image = Image.alpha_composite(image.convert('RGBA'), overlay).convert('RGB')
        draw = ImageDraw.Draw(image)

        # 绘制文字
        draw.text((x1, y1), size_text, fill=text_color, font=font)
        draw.text((x2, y2), suffix_text, fill=text_color, font=font)

        return image

    def generate_single(self, config: Dict) -> bool:
        """生成单张贴图

        Args:
            config: 配置字典

        Returns:
            是否成功
        """
        is_valid, error_msg = self.validate_parameters(config)
        if not is_valid:
            self.generation_log.append(f"[FAIL] 验证失败: {error_msg}")
            return False

        try:
            width = config.get("width")
            height = config.get("height")
            grid_size = config.get("grid_size", 32)
            color_scheme = config.get("color_scheme", "blue")
            grid_color = config.get("grid_color", (50, 50, 50))
            grid_width = config.get("grid_width", 1)
            output_prefix = config.get("output_prefix")

            # 获取色彩
            bg_color = self.get_color(color_scheme)
            text_color = self.get_contrasting_color(bg_color)
            font_size = self.get_font_size(width)

            # 创建图像
            image = Image.new('RGB', (width, height), bg_color)

            # 添加网格
            self.add_grid(image, grid_size, grid_color, grid_width)

            # 添加标签
            suffix = config.get("suffix_names", [""])[0] if config.get("suffix_names") else ""
            image = self.add_center_labels(image, width, height, suffix, text_color, bg_color, font_size)

            # 保存文件
            filename = f"{output_prefix}.png"
            filepath = self.output_dir / filename
            image.save(filepath, 'PNG')

            self.generated_files.append(str(filepath))
            self.generation_log.append(f"[OK] 生成成功: {filename}")
            return True

        except Exception as e:
            self.generation_log.append(f"[FAIL] 生成失败: {str(e)}")
            return False

    def generate_multi(self, config: Dict) -> bool:
        """生成4方向贴图（Graphic_Multi）

        Args:
            config: 配置字典

        Returns:
            是否成功
        """
        is_valid, error_msg = self.validate_parameters(config)
        if not is_valid:
            self.generation_log.append(f"[FAIL] 验证失败: {error_msg}")
            return False

        # 验证命名约束
        is_valid, error_msg = self.validate_naming_constraints(config)
        if not is_valid:
            self.generation_log.append(f"[FAIL] 命名约束验证失败: {error_msg}")
            return False

        try:
            width = config.get("width")
            height = config.get("height")
            grid_size = config.get("grid_size", 32)
            color_scheme = config.get("color_scheme", "blue")
            grid_color = config.get("grid_color", (50, 50, 50))
            grid_width = config.get("grid_width", 1)
            output_prefix = config.get("output_prefix")

            # 方向后缀（必须小写，符合RimWorld规范）
            directions = config.get("suffix_names", ["_north", "_east", "_south", "_west"])

            # 获取色彩
            bg_color = self.get_color(color_scheme)
            text_color = self.get_contrasting_color(bg_color)
            font_size = self.get_font_size(width)

            success_count = 0
            for direction in directions:
                image = Image.new('RGB', (width, height), bg_color)
                self.add_grid(image, grid_size, grid_color, grid_width)
                image = self.add_center_labels(image, width, height, direction, text_color, bg_color, font_size)

                filename = f"{output_prefix}{direction}.png"
                filepath = self.output_dir / filename
                image.save(filepath, 'PNG')

                self.generated_files.append(str(filepath))
                success_count += 1

            self.generation_log.append(f"[OK] 生成成功: {len(directions)}个文件（{output_prefix}）")
            return True

        except Exception as e:
            self.generation_log.append(f"[FAIL] 生成失败: {str(e)}")
            return False

    def generate_random(self, config: Dict) -> bool:
        """生成随机变体贴图（Graphic_Random）

        Args:
            config: 配置字典

        Returns:
            是否成功
        """
        is_valid, error_msg = self.validate_parameters(config)
        if not is_valid:
            self.generation_log.append(f"[FAIL] 验证失败: {error_msg}")
            return False

        try:
            width = config.get("width")
            height = config.get("height")
            grid_size = config.get("grid_size", 32)
            color_scheme = config.get("color_scheme", "blue")
            grid_color = config.get("grid_color", (50, 50, 50))
            grid_width = config.get("grid_width", 1)
            output_prefix = config.get("output_prefix")

            # 变体后缀
            variants = config.get("suffix_names", ["_a", "_b", "_c"])

            # 获取色彩
            bg_color = self.get_color(color_scheme)
            text_color = self.get_contrasting_color(bg_color)
            font_size = self.get_font_size(width)

            success_count = 0
            for variant in variants:
                image = Image.new('RGB', (width, height), bg_color)
                self.add_grid(image, grid_size, grid_color, grid_width)
                image = self.add_center_labels(image, width, height, variant, text_color, bg_color, font_size)

                filename = f"{output_prefix}{variant}.png"
                filepath = self.output_dir / filename
                image.save(filepath, 'PNG')

                self.generated_files.append(str(filepath))
                success_count += 1

            self.generation_log.append(f"[OK] 生成成功: {len(variants)}个文件（{output_prefix}）")
            return True

        except Exception as e:
            self.generation_log.append(f"[FAIL] 生成失败: {str(e)}")
            return False

    def generate_apparel(self, config: Dict) -> bool:
        """生成衣物贴图 - 体型×方向组合（5种体型 × 4个方向 = 20个文件）

        按照RimWorld规范：ApparelGraphicRecordGetter先添加体型后缀，
        然后Graphic_Multi添加方向后缀

        命名格式: <base>_<bodytype>_<direction>.png

        Args:
            config: 配置字典

        Returns:
            是否成功
        """
        is_valid, error_msg = self.validate_parameters(config)
        if not is_valid:
            self.generation_log.append(f"[FAIL] 验证失败: {error_msg}")
            return False

        try:
            width = config.get("width")
            height = config.get("height")
            grid_size = config.get("grid_size", 32)
            color_scheme = config.get("color_scheme", "blue")
            grid_color = config.get("grid_color", (50, 50, 50))
            grid_width = config.get("grid_width", 1)
            output_prefix = config.get("output_prefix")

            # 身体类型后缀（大写首字母）
            body_types = config.get("suffix_names", ["_Male", "_Female", "_Thin", "_Hulk", "_Fat"])
            # 方向后缀（小写）- 用户可灵活指定，默认为4个方向
            directions = config.get("directions", ["_north", "_east", "_south", "_west"])

            # 获取色彩
            bg_color = self.get_color(color_scheme)
            text_color = self.get_contrasting_color(bg_color)
            font_size = self.get_font_size(width)

            # 验证体型后缀
            valid_body_types = ["_Male", "_Female", "_Thin", "_Hulk", "_Fat"]
            for bt in body_types:
                if bt not in valid_body_types:
                    self.generation_log.append(f"[WARN] 警告: 体型后缀 '{bt}' 不标准，标准值: {valid_body_types}")

            file_count = 0
            # 双重循环：先体型，后方向（符合源代码执行顺序）
            for body_type in body_types:
                for direction in directions:
                    image = Image.new('RGB', (width, height), bg_color)
                    self.add_grid(image, grid_size, grid_color, grid_width)

                    # 标签显示：体型 + 方向
                    label = f"{body_type}\n{direction}"
                    image = self.add_center_labels(image, width, height, label, text_color, bg_color, font_size)

                    # 文件名：<base>_<bodytype>_<direction>
                    filename = f"{output_prefix}{body_type}{direction}.png"
                    filepath = self.output_dir / filename
                    image.save(filepath, 'PNG')

                    self.generated_files.append(str(filepath))
                    file_count += 1

            self.generation_log.append(f"[OK] 生成成功: {file_count}个文件（{len(body_types)}体型 × {len(directions)}方向）")
            self.generation_log.append(f"   命名格式: {output_prefix}_<体型>_<方向>.png")
            return True

        except Exception as e:
            self.generation_log.append(f"[FAIL] 生成失败: {str(e)}")
            return False

    def generate(self, config: Dict) -> bool:
        """根据配置生成贴图

        Args:
            config: 配置字典

        Returns:
            是否成功
        """
        suffix_type = config.get("suffix_type", "single")

        if suffix_type == "single":
            return self.generate_single(config)
        elif suffix_type == "multi":
            return self.generate_multi(config)
        elif suffix_type == "random":
            return self.generate_random(config)
        elif suffix_type == "apparel":
            return self.generate_apparel(config)
        else:
            self.generation_log.append(f"[FAIL] 未知的后缀类型: {suffix_type}")
            return False

    def batch_generate(self, configs: List[Dict]) -> bool:
        """批量生成贴图

        Args:
            configs: 配置字典列表

        Returns:
            是否全部成功
        """
        self.generation_log.append(f"[INFO] 开始批量生成 {len(configs)} 个贴图配置...")

        all_success = True
        for i, config in enumerate(configs, 1):
            self.generation_log.append(f"\n[{i}/{len(configs)}] 处理配置: {config.get('output_prefix', '未命名')}")
            success = self.generate(config)
            all_success = all_success and success

        return all_success

    def save_report(self):
        """保存生成报告"""
        report_file = self.output_dir / "生成报告.txt"
        with open(report_file, 'w', encoding='utf-8') as f:
            f.write("=" * 60 + "\n")
            f.write("RimWorld 贴图生成报告\n")
            f.write("=" * 60 + "\n\n")

            for line in self.generation_log:
                f.write(line + "\n")

            f.write("\n" + "=" * 60 + "\n")
            f.write(f"总生成文件数: {len(self.generated_files)}\n")
            f.write(f"输出目录: {self.output_dir}\n")
            f.write("=" * 60 + "\n")

        # 保存文件列表
        list_file = self.output_dir / "生成文件列表.json"
        with open(list_file, 'w', encoding='utf-8') as f:
            json.dump({
                "总数": len(self.generated_files),
                "文件": self.generated_files
            }, f, ensure_ascii=False, indent=2)

    def print_summary(self):
        """打印生成摘要"""
        print("\n" + "=" * 60)
        print("[OK] RimWorld 贴图生成完成")
        print("=" * 60)
        for line in self.generation_log:
            print(line)
        print("=" * 60)
        print(f"[DIR] 输出目录: {self.output_dir}")
        print(f"[COUNT] 总生成文件: {len(self.generated_files)}")
        print("=" * 60 + "\n")


class ConfigParser:
    """配置文件解析器"""

    @staticmethod
    def from_json(json_file: str) -> List[Dict]:
        """从JSON文件读取配置

        Args:
            json_file: JSON文件路径

        Returns:
            配置字典列表
        """
        with open(json_file, 'r', encoding='utf-8') as f:
            data = json.load(f)

        # 支持两种格式
        if isinstance(data, list):
            return data
        elif isinstance(data, dict) and "textures" in data:
            return data["textures"]
        else:
            return [data]

    @staticmethod
    def from_yaml(yaml_file: str) -> List[Dict]:
        """从YAML文件读取配置（简单支持）

        Args:
            yaml_file: YAML文件路径

        Returns:
            配置字典列表
        """
        try:
            import yaml
            with open(yaml_file, 'r', encoding='utf-8') as f:
                data = yaml.safe_load(f)
        except ImportError:
            print("[WARN] 需要安装 PyYAML 库来支持YAML格式")
            print("运行命令: pip install PyYAML")
            return []

        if isinstance(data, list):
            return data
        elif isinstance(data, dict) and "textures" in data:
            return data["textures"]
        else:
            return [data]


def main():
    """主函数"""
    parser = argparse.ArgumentParser(
        description='RimWorld 贴图自动生成工具',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例:
  # 使用JSON配置
  python texture_generator.py --config config.json --output ./output/

  # 使用YAML配置
  python texture_generator.py --config config.yaml --output ./textures/

  # 命令行参数
  python texture_generator.py --width 128 --height 128 --name Sword_Iron \\
    --color blue --suffix single --output ./output/
        """
    )

    # 输出目录（必需）
    parser.add_argument('-o', '--output', required=True, type=str,
                       help='输出目录路径（必需）')

    # 配置文件方式
    parser.add_argument('-c', '--config', type=str,
                       help='配置文件路径（JSON或YAML格式）')

    # 命令行参数方式
    parser.add_argument('-w', '--width', type=int, help='贴图宽度（64/128/256/512/1024）')
    parser.add_argument('-h_', '--height', type=int, help='贴图高度（64/128/256/512/1024）')
    parser.add_argument('-n', '--name', type=str, help='输出前缀（文件名）')
    parser.add_argument('--color', type=str, default='blue',
                       help='色彩预设（blue/red/green/purple/yellow/gray）或自定义RGB')
    parser.add_argument('-s', '--suffix', type=str, default='single',
                       help='''后缀类型：
                     single  - 单张贴图（无后缀）
                     multi   - 4方向贴图（小写: _north, _east, _south, _west）
                     random  - 随机变体（_a, _b, _c 或 _0, _1, _2）
                     apparel - 衣物贴图（5体型 × 4方向 = 20个文件）
                   ''')
    parser.add_argument('--grid-size', type=int, default=32, help='网格大小（像素）')
    parser.add_argument('--grid-width', type=int, default=1, help='网格线宽度')

    args = parser.parse_args()

    # 初始化生成器
    generator = TextureGenerator(args.output)

    try:
        if args.config:
            # 使用配置文件
            config_ext = Path(args.config).suffix.lower()

            if config_ext == '.json':
                configs = ConfigParser.from_json(args.config)
            elif config_ext == '.yaml' or config_ext == '.yml':
                configs = ConfigParser.from_yaml(args.config)
            else:
                print("[ERROR] 不支持的配置格式（只支持JSON/YAML）")
                sys.exit(1)

            if not configs:
                print("[ERROR] 配置文件为空或格式错误")
                sys.exit(1)

            generator.batch_generate(configs)

        elif args.width and args.height and args.name:
            # 使用命令行参数
            config = {
                "width": args.width,
                "height": args.height,
                "output_prefix": args.name,
                "color_scheme": args.color,
                "suffix_type": args.suffix,
                "grid_size": args.grid_size,
                "grid_width": args.grid_width,
            }

            # 添加后缀名称
            if args.suffix == "multi":
                # 方向后缀（必须小写）
                config["suffix_names"] = ["_north", "_east", "_south", "_west"]
            elif args.suffix == "random":
                config["suffix_names"] = ["_a", "_b", "_c"]
            elif args.suffix == "apparel":
                # 衣物后缀（体型为大写首字母）
                config["suffix_names"] = ["_Male", "_Female", "_Thin", "_Hulk", "_Fat"]

            generator.generate(config)

        else:
            print("[ERROR] 必须提供配置文件（-c）或完整的命令行参数（-w, -h_, -n）")
            parser.print_help()
            sys.exit(1)

        # 保存报告并打印摘要
        generator.save_report()
        generator.print_summary()

    except Exception as e:
        print(f"[ERROR] 发生错误: {str(e)}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
