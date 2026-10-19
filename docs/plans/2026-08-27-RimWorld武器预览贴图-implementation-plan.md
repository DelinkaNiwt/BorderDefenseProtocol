# RimWorld 武器预览贴图 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 生成一张只用于预览的 512×512 纯白背景 RimWorld 风格武器贴图。

**Architecture:** 使用内置 `imagegen` 以用户截图作为物体身份参考；以逻辑 128×128 的轮廓与大色块预算约束细节，再输出 512×512 预览。结果不写入模组项目目录，不修改游戏代码或 XML。

**Tech Stack:** 内置 `imagegen`、本地图片检查工具、PowerShell 文件检查。

---

### Task 1: 生成预览图

**Files:**
- Reference: `C:/NiwtDatas/Projects/RimworldModStudio/日志/QQ20260518-145300.png`
- Output: 内置图片生成结果位置，仅用于当前对话预览

**Step 1:** 使用 Image 1 作为物体身份参考调用内置 `imagegen`。

**Step 2:** 要求 512×512、纯白背景、单件完整武器、严格侧面构图和 RimWorld 原版低细节视觉语法。

**Step 3:** 检查轮廓、主体颜色、握把与弹匣位置、背景纯度以及禁用项。

**Step 4:** 在对话中展示通过检查的结果，不复制到模组目录。

**Step 5:** 将本次工作写入工作日志。
