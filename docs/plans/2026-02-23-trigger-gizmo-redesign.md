# 触发器Gizmo架构重设计 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让每个芯片攻击模式拥有独立的Gizmo按钮，解决RimWorld GroupsWith自动合并问题。

**Architecture:** 芯片Verb设hasStandardCommand=false脱离标准Gizmo路径，CompGetEquippedGizmosExtra中用自定义Command_BDPChipAttack生成独立Gizmo。IVerbOwner始终返回占位Verb+芯片Verb，保证占位gizmo常驻。

**Tech Stack:** C# (.NET 4.8), RimWorld 1.6 modding API, MSBuild

**设计文档:** `临时/BorderDefenseProtocol/第六阶段：深度系统设计/6.2.1_触发器Gizmo架构重设计.md`

---
