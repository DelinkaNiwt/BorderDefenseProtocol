# 占位贴图生成器 - 颜色对比度修复

**修复时间**: 2026-01-03
**版本**: v1.1
**严重性**: 中等（影响用户体验，部分贴图标签不可见）

## 问题描述

### 现象
在浅色背景的占位贴图上（如Colonist的浅粉色背景），变式标签（variant_label）文本无法看见。用户看到的是纯色的贴图，没有中央的标识文本。

### 根本原因
脚本中存在颜色对比度逻辑不一致：

- **第388行**（尺寸标签）：根据背景亮度计算文本颜色
  ```python
  text_color = (255, 255, 255, 255) if sum(hex_to_rgb(color)) < 382 else (0, 0, 0, 255)
  ```
  - 暗色背景 → 白色文本 ✓
  - 亮色背景 → 黑色文本 ✓

- **第424行**（变式标签）：背景色逻辑相反
  ```python
  bg_color = (0, 0, 0, 200) if sum(hex_to_rgb(color)) > 382 else (255, 255, 255, 200)
  ```
  - 暗色背景 → 白色背景 ✓
  - **亮色背景 → 黑色背景** + **黑色文本** ✗ 看不见！

## 修复内容

### 改动位置
文件：`generate_placeholders.py` 第423-432行

### 修复前
```python
# 使用对比色背景
bg_color = (0, 0, 0, 200) if sum(hex_to_rgb(color)) > 382 else (255, 255, 255, 200)
draw.rectangle([...], fill=bg_color)
draw.text((x, center_y), variant_label, fill=text_color)  # text_color与bg_color冲突
```

### 修复后
```python
# 使用对比色背景和文本（与背景互补）
bg_color = (255, 255, 255, 220) if sum(hex_to_rgb(color)) < 382 else (0, 0, 0, 220)
variant_text_color = (0, 0, 0, 255) if sum(hex_to_rgb(color)) < 382 else (255, 255, 255, 255)

draw.rectangle([...], fill=bg_color)
draw.text((x, center_y), variant_label, fill=variant_text_color)
```

## 修复逻辑

| 背景亮度 | 背景色 | 文本色 | 效果 |
|---------|-------|-------|------|
| < 382（暗色）| 白色 (255,255,255) | 黑色 (0,0,0) | ✓ 可见 |
| >= 382（亮色）| 黑色 (0,0,0) | 白色 (255,255,255) | ✓ 可见 |

## 受影响的贴图

主要是背景颜色较亮的贴图，包括：
- **Colonist**（浅粉色 #FFB6C1）- 优先级最高
- **Shirt**（红色 #FF6B6B）
- **Pants**（青绿色 #4ECDC4）
- **Health_Status**（红色 #FF0000）
- **Happy_Status**（金色 #FFD700）
- **Happy_Status**（橙色 #FFA500）
- 及其他浅色背景贴图

## 验证方法

1. 生成后查看任何Colonist贴图（如 `Colonist_Female_East.png`）
2. 应能清晰看到中央的"Female_East"黑色文本
3. 文本背景应为深灰色，确保对比度充分

## 生成统计

**最新生成**（修复后）：
- 时间: 2026-01-03 18:52
- 贴图总数: 120
- 目录数: 24
- 状态: ✅ 全部成功

## 备注

此修复确保了所有变式贴图的标签都能在各种背景颜色下清晰可见，完全满足用户的"能看清标签文本"需求。
