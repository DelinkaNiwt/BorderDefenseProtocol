# RimWorld伤害类型完整清单与适配策略

**文档版本**: v1.0
**创建日期**: 2026-03-04
**用途**: 记录RimWorld所有伤害类型，指导战斗体Trion伤口系统的映射策略

---

## 一、伤害类型完整清单（30种）

### 1. 物理战斗伤害（8种）✅ 需要映射

| DefName | 中文名 | 说明 | 映射状态 |
|---------|--------|------|----------|
| `Cut` | 切割伤 | 刀剑类武器造成 | ✅ 映射为 `BDP_CombatWound_Cut` |
| `Crush` | 压碎伤 | 重型钝器、坍塌造成 | ✅ 映射为 `BDP_CombatWound_Crush` |
| `Blunt` | 钝击伤 | 棍棒、拳头造成 | ✅ 映射为 `BDP_CombatWound_Blunt` |
| `Stab` | 刺伤 | 长矛、匕首造成 | ✅ 映射为 `BDP_CombatWound_Stab` |
| `Bullet` | 枪伤 | 枪械子弹造成 | ✅ 映射为 `BDP_CombatWound_Bullet` |
| `Bomb` | 爆炸伤 | 炸弹、手榴弹造成 | ✅ 映射为 `BDP_CombatWound_Bomb` |
| `Scratch` | 抓伤 | 动物爪子造成 | ✅ 映射为 `BDP_CombatWound_Scratch` |
| `Bite` | 咬伤 | 动物牙齿造成 | ✅ 映射为 `BDP_CombatWound_Bite` |

### 2. 特殊战斗伤害（9种）✅ 需要映射

| DefName | 中文名 | 说明 | 映射状态 | 备注 |
|---------|--------|------|----------|------|
| `ScratchToxic` | 毒性抓伤 | 有毒动物爪子造成 | ✅ 映射为 `BDP_CombatWound_ScratchToxic` | 未来可能需要特殊毒素效果 |
| `TornadoScratch` | 龙卷风抓伤 | 龙卷风造成 | ✅ 映射为 `BDP_CombatWound_TornadoScratch` | |
| `Flame` | 火焰伤 | 火焰喷射器造成 | ✅ 映射为 `BDP_CombatWound_Flame` | |
| `Burn` | 烧伤 | 火灾、高温造成 | ✅ 映射为 `BDP_CombatWound_Burn` | |
| `AcidBurn` | 酸烧伤 | 酸液造成 | ✅ 映射为 `BDP_CombatWound_AcidBurn` | 未来可能需要持续腐蚀效果 |
| `ElectricalBurn` | 电烧伤 | 电击造成（Anomaly DLC） | ✅ 映射为 `BDP_CombatWound_ElectricalBurn` | |
| `Psychic` | 心灵伤害 | 心灵攻击造成（Anomaly DLC） | ✅ 映射为 `BDP_CombatWound_Psychic` | ⚠️ 未来需要特殊处理：可能直接伤害Trion而不产生伤口 |
| `Frostbite` | 冻伤 | 极寒环境造成 | ✅ 映射为 `BDP_CombatWound_Frostbite` | |
| `VacuumBurn` | 真空烧伤 | 真空环境造成（Odyssey DLC） | ✅ 映射为 `BDP_CombatWound_VacuumBurn` | |

### 3. 手术/处决伤害（2种）⚠️ 特殊处理

| DefName | 中文名 | 说明 | 映射状态 | 备注 |
|---------|--------|------|----------|------|
| `SurgicalCut` | 手术切口 | 手术操作造成 | ⚠️ 暂不映射 | ⚠️ 未来需要特殊处理：战斗体无法接受原版手术（NR-024） |
| `ExecutionCut` | 处决切口 | 处决操作造成 | ✅ 映射为 `BDP_CombatWound_ExecutionCut` | 虽然是处决，但仍然是战斗伤害 |

### 4. 控制效果伤害（4种）⚠️ 特殊处理

| DefName | 中文名 | 说明 | 映射状态 | 备注 |
|---------|--------|------|----------|------|
| `Stun` | 眩晕 | 眩晕武器造成 | ⚠️ 暂不映射 | ⚠️ 未来需要特殊处理：可能需要专门的"眩晕"机制，而不是伤口 |
| `EMP` | 电磁脉冲 | EMP武器造成 | ⚠️ 暂不映射 | ⚠️ 未来需要特殊处理：可能直接影响Trion系统，而不产生伤口 |
| `NerveStun` | 神经眩晕 | 神经眩晕武器造成 | ⚠️ 暂不映射 | ⚠️ 未来需要特殊处理：类似Stun |
| `MechBandShockwave` | 机械波冲击 | 机械波武器造成（Biotech DLC） | ⚠️ 暂不映射 | ⚠️ 未来需要特殊处理：类似EMP |

### 5. 环境/非战斗伤害（7种）❌ 不映射

| DefName | 中文名 | 说明 | 映射状态 | 备注 |
|---------|--------|------|----------|------|
| `Extinguish` | 灭火 | 灭火操作 | ❌ 不映射 | 不是真正的伤害 |
| `Smoke` | 烟雾 | 烟雾伤害 | ❌ 不映射 | 环境效果，不产生伤口 |
| `Deterioration` | 腐化 | 物品腐化 | ❌ 不映射 | 仅用于物品，不适用于生物 |
| `Mining` | 挖掘 | 挖掘操作 | ❌ 不映射 | 工具伤害，不适用于战斗 |
| `Rotting` | 腐烂 | 尸体腐烂 | ❌ 不映射 | 仅用于尸体 |
| `ToxGas` | 毒气 | 毒气伤害（Biotech DLC） | ❌ 不映射 | ⚠️ 未来可能需要重新评估：如果毒气用于战斗，可能需要映射 |
| `MiningBomb` | 挖掘炸弹 | 挖掘炸弹造成（Odyssey DLC） | ❌ 不映射 | 工具伤害 |

---

## 二、映射策略总结

### 当前阶段（Phase 1）：核心战斗伤害映射
**映射数量**: 19种
**包括**:
- 所有物理战斗伤害（8种）
- 所有特殊战斗伤害（9种）
- 处决切口（1种）
- 手术切口（1种，暂时映射，未来可能调整）

### 未来阶段：特殊伤害适配

#### 优先级1：控制效果伤害（4种）
- `Stun`, `EMP`, `NerveStun`, `MechBandShockwave`
- **设计方向**: 不产生伤口，直接影响Trion系统或战斗体状态
- **可能实现**:
  - EMP → 临时降低Trion恢复速率
  - Stun → 临时降低战斗体反应速度
  - 不通过伤口系统，而是通过专门的Hediff（如`BDP_TrionDisruption`）

#### 优先级2：心灵伤害（1种）
- `Psychic`
- **设计方向**: 可能直接伤害Trion池，而不产生物理伤口
- **可能实现**: 绕过影子HP系统，直接扣除Trion

#### 优先级3：手术伤害（1种）
- `SurgicalCut`
- **设计方向**: 战斗体无法接受原版手术（NR-024），需要专门的"战斗体维修"系统
- **可能实现**: 拦截手术操作，提示"战斗体需要专门的维修设施"

#### 优先级4：毒气伤害（1种）
- `ToxGas`
- **设计方向**: 如果毒气用于战斗，可能需要映射
- **待评估**: 观察实际游戏中的使用场景

---

## 三、HediffDef命名规范

### 格式
```
BDP_CombatWound_{DamageDefName}
```

### 示例
- `Cut` → `BDP_CombatWound_Cut`
- `Bullet` → `BDP_CombatWound_Bullet`
- `ElectricalBurn` → `BDP_CombatWound_ElectricalBurn`

### 特殊情况
- 对于未来的"控制效果"Hediff，使用不同的前缀：
  - `BDP_TrionDisruption_EMP`
  - `BDP_TrionDisruption_Stun`

---

## 四、实现检查清单

### Phase 1: 核心战斗伤害（19种）
- [ ] Cut - 切割伤
- [ ] Crush - 压碎伤
- [ ] Blunt - 钝击伤
- [ ] Stab - 刺伤
- [ ] Bullet - 枪伤
- [ ] Bomb - 爆炸伤
- [ ] Scratch - 抓伤
- [ ] Bite - 咬伤
- [ ] ScratchToxic - 毒性抓伤
- [ ] TornadoScratch - 龙卷风抓伤
- [ ] Flame - 火焰伤
- [ ] Burn - 烧伤
- [ ] AcidBurn - 酸烧伤
- [ ] ElectricalBurn - 电烧伤
- [ ] Psychic - 心灵伤害
- [ ] Frostbite - 冻伤
- [ ] VacuumBurn - 真空烧伤
- [ ] SurgicalCut - 手术切口
- [ ] ExecutionCut - 处决切口

### Phase 2: 特殊伤害适配（待设计）
- [ ] Stun - 眩晕效果
- [ ] EMP - 电磁脉冲效果
- [ ] NerveStun - 神经眩晕效果
- [ ] MechBandShockwave - 机械波冲击效果
- [ ] ToxGas - 毒气效果（待评估）

---

## 五、参考信息

### 源代码位置
- **DamageDefOf类**: `RimWorld.DamageDefOf` (DamageDefOf.cs)
- **游戏程序集**: `C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed`

### 相关需求
- **NR-042**: 伤口Hediff规范
- **NR-020**: 战斗体无法自愈
- **NR-024**: 战斗体无法使用原版治疗

---

**文档维护者**: AI (Claude Sonnet 4.6)
**最后更新**: 2026-03-04
