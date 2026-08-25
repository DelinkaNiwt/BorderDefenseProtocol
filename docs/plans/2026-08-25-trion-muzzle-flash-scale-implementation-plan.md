# Trion 构型枪口闪光增强 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 Trion 隐藏默认武装型的原版枪口闪光尺寸从默认值提高到 `1.8`。

**Architecture:** 复用现有 `ChipArmamentFormDef` 覆盖链，仅在 Trion 构型的 `overrides` 中写入枪口闪光尺寸。通用 BDP 射击流程和其他构型保持不变。

**Tech Stack:** RimWorld Def XML、PowerShell 静态烟测、Git。

---

### Task 1: 增加 Trion 枪口闪光配置断言

**Files:**
- Modify: `Source/BDP.Tests/TrionCubeVisualSmokeTests.ps1`
- Test: 同一脚本

**Step 1:** 在读取 `BDP_ArmamentForm_TrionCube` 后，断言 `overrides.muzzleFlashScale` 等于 `1.8`。

**Step 2:** 运行脚本，预期因当前配置缺少该字段而失败。

### Task 2: 实现最小 XML 配置

**Files:**
- Modify: `1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`

**Step 1:** 在 Trion 隐藏默认型的 `overrides` 中增加带中文注释的 `<muzzleFlashScale>1.8</muzzleFlashScale>`。

**Step 2:** 保持其他构型、贴图、阶段可见性和通用 C# 不变。

### Task 3: 验证并记录

**Files:**
- Create or update: `日志/Agent工作日志/Agent日志<编号>.md`

**Step 1:** 运行 Trion 视觉烟测和枪口闪光归属烟测。

**Step 2:** 检查 XML 可解析，确认工作区只包含本次目标文件和测试/日志记录的改动；不触碰用户已有无关改动。

**Step 3:** 提交本次设计记录与实现改动，便于回退。
