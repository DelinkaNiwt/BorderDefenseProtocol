# RimWorld模组初始化Skill实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 创建rimworld-mod-init skill，通过交互式问答引导用户创建RimWorld模组的目录结构和必要文件

**Architecture:** 单一Python脚本实现，通过input()收集用户输入，根据配置对象生成目录结构和文件内容，支持预设模板和完全自定义两种模式

**Tech Stack:** Python 3.x, pathlib, os, xml.etree.ElementTree

---

## 前置准备

### Task 0: 确定Skill存放位置

**Files:**
- 查看: `C:\Users\Niwt\.claude\skills\` 目录结构

**Step 1: 确认skill目录**

检查用户的skill目录位置，确定rimworld-mod-init skill应该创建在哪里。

根据系统提示，额外工作目录包含：`C:\Users\Niwt\.claude\skills\rimworld-mod-init`

这意味着skill应该创建在这个目录下。

**Step 2: 创建skill目录结构**

```bash
mkdir -p "C:\Users\Niwt\.claude\skills\rimworld-mod-init"
cd "C:\Users\Niwt\.claude\skills\rimworld-mod-init"
```

---

## 核心实现

### Task 1: 创建Skill元数据文件

**Files:**
- Create: `C:\Users\Niwt\.claude\skills\rimworld-mod-init\skill.json`

**Step 1: 创建skill.json**

```json
{
  "name": "rimworld-mod-init",
  "version": "1.0.0",
  "description": "创建并初始化RimWorld模组目录结构和必要文件",
  "author": "Niwt",
  "main": "main.py",
  "category": "development",
  "tags": ["rimworld", "mod", "initialization", "scaffold"]
}
```

**Step 2: 提交元数据文件**

```bash
git add skill.json
git commit -m "feat: add rimworld-mod-init skill metadata"
```

---

### Task 2: 实现配置收集模块

**Files:**
- Create: `C:\Users\Niwt\.claude\skills\rimworld-mod-init\main.py`

**Step 1: 创建主文件框架**

```python
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
RimWorld模组初始化工具
用于快速创建RimWorld模组的目录结构和必要文件
"""

import os
import sys
from pathlib import Path
from typing import Dict, List, Optional


def main():
    """主函数"""
    print("=" * 60)
    print("欢迎使用RimWorld模组初始化工具！")
    print("=" * 60)
    print()

    # 收集配置
    config = collect_all_config()

    # 显示预览
    if not confirm_creation(config):
        print("已取消创建")
        return

    # 执行创建
    create_mod_structure(config)

    # 显示摘要
    show_summary(config)


if __name__ == "__main__":
    main()
```

**Step 2: 实现预设模板选择**

```python
def select_preset() -> str:
    """选择预设模板"""
    print("请选择模组类型（这会影响后续的默认选项）：")
    print("1. 纯内容模组 - 添加武器、装备、建筑等，不需要编写代码")
    print("2. 功能模组 - 需要编写C#代码来实现新功能或修改游戏机制")
    print("3. 框架/库模组 - 为其他模组提供功能支持")
    print("4. 完全自定义 - 我想自己决定所有细节")
    print()

    while True:
        choice = input("你的选择（1-4）：").strip()
        if choice == "1":
            return "content"
        elif choice == "2":
            return "function"
        elif choice == "3":
            return "framework"
        elif choice == "4":
            return "custom"
        else:
            print("无效选择，请输入1-4之间的数字")
```

**Step 3: 实现基础信息收集**

```python
def collect_basic_info() -> Dict[str, str]:
    """收集基础信息"""
    print("\n--- 基础信息 ---")

    mod_name = input("模组名称（英文，如：MyAwesomeMod）：").strip()
    while not mod_name or not mod_name.replace("_", "").isalnum():
        print("模组名称不能为空，且只能包含字母、数字和下划线")
        mod_name = input("模组名称（英文，如：MyAwesomeMod）：").strip()

    author = input("作者名称（英文，如：YourName）：").strip()
    while not author or not author.replace("_", "").isalnum():
        print("作者名称不能为空，且只能包含字母、数字和下划线")
        author = input("作者名称（英文，如：YourName）：").strip()

    description = input("模组描述（中文，简短描述你的模组功能）：").strip()
    if not description:
        description = "一个RimWorld模组"

    url = input("项目URL（可选，如GitHub链接，直接回车跳过）：").strip()

    return {
        'mod_name': mod_name,
        'author': author,
        'description': description,
        'url': url if url else None,
    }
```

**Step 4: 提交配置收集模块**

```bash
git add main.py
git commit -m "feat: implement config collection module"
```

---

### Task 3: 实现技术配置收集

**Files:**
- Modify: `C:\Users\Niwt\.claude\skills\rimworld-mod-init\main.py`

**Step 1: 实现技术配置收集函数**

在main.py中添加：

```python
def collect_technical_config(preset: str) -> Dict:
    """收集技术配置"""
    print("\n--- 技术配置 ---")

    config = {
        'needs_code': False,
        'needs_harmony': False,
        'multi_version': False,
        'versions': ['1.6'],
        'dependencies': [],
        'content_types': [],
    }

    # 根据预设调整问题
    if preset == "content":
        config['needs_code'] = False
        content_types = collect_content_types()
        config['content_types'] = content_types
    elif preset in ["function", "framework"]:
        config['needs_code'] = True
        needs_harmony = input("需要使用Harmony来修改游戏原有功能吗？（是/否）：").strip().lower()
        config['needs_harmony'] = needs_harmony in ['是', 'y', 'yes']
    else:  # custom
        needs_code = input("你的模组需要编写C#代码吗？（是/否）：").strip().lower()
        config['needs_code'] = needs_code in ['是', 'y', 'yes']

        if config['needs_code']:
            needs_harmony = input("需要使用Harmony来修改游戏原有功能吗？（是/否）：").strip().lower()
            config['needs_harmony'] = needs_harmony in ['是', 'y', 'yes']
        else:
            content_types = collect_content_types()
            config['content_types'] = content_types

    # 通用问题
    multi_version = input("\n你的模组需要支持多个游戏版本吗？（是/否）：").strip().lower()
    config['multi_version'] = multi_version in ['是', 'y', 'yes']

    if config['multi_version']:
        versions_input = input("需要支持哪些版本？（如：1.5,1.6）：").strip()
        config['versions'] = [v.strip() for v in versions_input.split(',') if v.strip()]

    has_deps = input("\n你的模组依赖其他模组吗？（是/否）：").strip().lower()
    if has_deps in ['是', 'y', 'yes']:
        deps_input = input("请输入依赖的模组packageId（多个用逗号分隔）：").strip()
        config['dependencies'] = [d.strip() for d in deps_input.split(',') if d.strip()]

        # 如果需要Harmony但没有在依赖中，自动添加
        if config['needs_harmony'] and 'brrainz.harmony' not in [d.lower() for d in config['dependencies']]:
            config['dependencies'].append('brrainz.harmony')
    elif config['needs_harmony']:
        config['dependencies'].append('brrainz.harmony')

    return config


def collect_content_types() -> List[str]:
    """收集内容类型"""
    print("\n你的模组需要添加哪些类型的内容？（可多选，用逗号分隔）")
    print("1. 武器和装备")
    print("2. 建筑和家具")
    print("3. 研究项目")
    print("4. 派系和事件")
    print("5. 其他（会创建通用的Defs目录）")

    choice = input("你的选择（如：1,2,3）：").strip()

    type_map = {
        '1': 'weapons',
        '2': 'buildings',
        '3': 'research',
        '4': 'factions',
        '5': 'other',
    }

    selected = []
    for c in choice.split(','):
        c = c.strip()
        if c in type_map:
            selected.append(type_map[c])

    return selected if selected else ['other']
```

**Step 2: 提交技术配置模块**

```bash
git add main.py
git commit -m "feat: implement technical config collection"
```

---

