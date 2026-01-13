#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Generate placeholder textures for Trion MVP"""

import os
import shutil

base_dir = r"C:\NiwtDatas\Projects\RimworldModStudio"
source_dir = os.path.join(base_dir, r"参考资源\通用资源\占位贴图")
target_dir = os.path.join(base_dir, r"临时\Trion_MVP_CodeDeliverables\Textures")

# Create target directories
os.makedirs(os.path.join(target_dir, "Things", "Building"), exist_ok=True)
os.makedirs(os.path.join(target_dir, "Things", "Item"), exist_ok=True)

# Texture mapping: source -> target
texture_mappings = [
    (os.path.join(source_dir, r"Things\Building\Production\Workbench_North.png"),
     os.path.join(target_dir, r"Things\Building\TrionDetector_top.png")),

    (os.path.join(source_dir, r"Things\Building\Decoration\Painting_North.png"),
     os.path.join(target_dir, r"Things\Building\TriggerConfigBench_top.png")),

    (os.path.join(source_dir, r"Items\Resources\Steel.png"),
     os.path.join(target_dir, r"Things\Item\TrionGland.png")),

    (os.path.join(source_dir, r"Items\Weapon\Sword_Iron.png"),
     os.path.join(target_dir, r"Things\Item\TrionComponent_ArcusBlade.png")),

    (os.path.join(source_dir, r"Items\Resources\Cloth.png"),
     os.path.join(target_dir, r"Things\Item\TrionComponent_ShieldGen.png")),

    (os.path.join(source_dir, r"Items\Weapon\Gun_Pistol.png"),
     os.path.join(target_dir, r"Things\Item\TrionComponent_ExplosiveBullet.png")),

    (os.path.join(source_dir, r"Items\Resources\Wood.png"),
     os.path.join(target_dir, r"Things\Item\TrionComponent_Chameleon.png")),

    (os.path.join(source_dir, r"Effects\Impact\Explosion.png"),
     os.path.join(target_dir, r"Things\Item\TrionComponent_BailOut.png")),
]

print("Copying textures...")
count = 0
for src, dst in texture_mappings:
    try:
        if os.path.exists(src):
            shutil.copy2(src, dst)
            print(f"  [OK] {os.path.basename(dst)}")
            count += 1
        else:
            print(f"  [SKIP] Source not found: {src}")
    except Exception as e:
        print(f"  [ERROR] {dst}: {e}")

print(f"\nTotal: {count} textures copied")
print(f"Output: {target_dir}")
