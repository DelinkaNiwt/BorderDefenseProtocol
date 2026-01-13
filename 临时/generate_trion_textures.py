#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""为Trion_MVP生成特定的占位贴图"""

from PIL import Image, ImageDraw, ImageFont
import os

# 输出目录
OUTPUT_DIR = r"C:\NiwtDatas\Projects\RimworldModStudio\临时\Trion_MVP_CodeDeliverables\Textures"

# 贴图定义
TEXTURES = {
    "Things/Building": {
        "TrionDetector_top": {"size": (128, 128), "color": "#4A90E2", "label": "Detector"},
        "TriggerConfigBench_top": {"size": (192, 192), "color": "#7B68EE", "label": "Bench"},
    },
    "Things/Item": {
        "TrionGland": {"size": (64, 64), "color": "#FF69B4", "label": "Gland"},
        "TrionComponent_ArcusBlade": {"size": (64, 64), "color": "#FF4444", "label": "Arcus"},
        "TrionComponent_ShieldGen": {"size": (64, 64), "color": "#44FF44", "label": "Shield"},
        "TrionComponent_ExplosiveBullet": {"size": (64, 64), "color": "#FFFF44", "label": "Bullet"},
        "TrionComponent_Chameleon": {"size": (64, 64), "color": "#44FFFF", "label": "Cloak"},
        "TrionComponent_BailOut": {"size": (64, 64), "color": "#FF8800", "label": "Bail"},
    }
}

def create_placeholder_texture(name, size, color, label):
    """创建占位贴图"""
    width, height = size
    
    # 创建图片
    img = Image.new('RGB', (width, height), color)
    draw = ImageDraw.Draw(img)
    
    # 绘制网格
    grid_size = 32
    line_color = (100, 100, 100)
    
    for x in range(0, width, grid_size):
        draw.line([(x, 0), (x, height)], fill=line_color, width=1)
    
    for y in range(0, height, grid_size):
        draw.line([(0, y), (width, y)], fill=line_color, width=1)
    
    # 绘制尺寸标签
    text = f"{width}×{height}"
    text_color = (200, 200, 200)
    
    # 简单文本绘制（不依赖字体文件）
    try:
        draw.text((width//2 - 20, height//2 - 8), text, fill=text_color)
    except:
        pass  # 字体加载失败，跳过文字
    
    return img

def main():
    print("🎨 生成Trion_MVP占位贴图...")
    
    total_files = 0
    
    for dir_path, textures in TEXTURES.items():
        output_subdir = os.path.join(OUTPUT_DIR, dir_path)
        os.makedirs(output_subdir, exist_ok=True)
        
        print(f"\n📂 {dir_path}/")
        
        for name, config in textures.items():
            size = config['size']
            color = config['color']
            label = config['label']
            
            img = create_placeholder_texture(name, size, color, label)
            
            output_path = os.path.join(output_subdir, f"{name}.png")
            img.save(output_path)
            
            print(f"  ✓ {name}.png ({size[0]}×{size[1]})")
            total_files += 1
    
    print(f"\n✅ 生成完成！")
    print(f"📊 总共生成 {total_files} 个贴图")
    print(f"📁 输出目录: {OUTPUT_DIR}")

if __name__ == "__main__":
    main()
