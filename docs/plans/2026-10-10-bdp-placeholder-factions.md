# BDP 占位派系与近界芯片实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 增加“近界宙域”和“边境联盟”正式派系，并为近界原型触发体配齐三枚不可制造的固定芯片。

**Architecture:** 派系、PawnKind 与芯片数据放在 Content 的 Def 中；奥德赛轨道抵达由原版 `MayRequire` 条件字段启用。仅对近界宙域生成的人物追加 Trion 腺体、原型触发体并走 BDP 正式战斗体激活入口。

**Tech Stack:** RimWorld 1.6 XML Def、BDP.Content C#、简体中文语言包。

---

### Task 1：定义派系和近界战斗 PawnKind

**Files:**
- Create: `1.6/Content/Defs/FactionDef/PlaceholderFactions.xml`
- Create: `1.6/Content/Defs/PawnKindDef/NearRealm.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/FactionDef/PlaceholderFactions.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/PawnKindDef/NearRealm.xml`

近界宙域永久敌对、无定居点，可生成地表袭击；奥德赛加载时额外允许轨道抵达。边境联盟隐藏、无定居点和战斗队伍，初始关系保持原版中立。

### Task 2：添加三枚固定芯片和小型护盾状态

**Files:**
- Create: `1.6/Content/Defs/ChipManufacturingPresetDef/NearRealm.xml`
- Create: `1.6/Content/Defs/HediffDef/EnergyShield_Soldier.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef/NearRealm.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/HediffDef/EnergyShield_Soldier.xml`
- Modify: `1.6/Content/Defs/ThingDef/Equipment/NeighborPrototype.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/ThingDef/BDP_TriggerBodyNeighborPrototype.xml`

按已确认参数定义小兵短剑、小兵弹丸、小兵护盾；三者均关闭玩家制造入口，占用 20、启动消耗 0、启停延迟 0，并固定装入原型触发体的主、副、特殊槽。

### Task 3：把近界派系接到任务和正式战斗体入口

**Files:**
- Modify: `Source/BDP.Content/Royal/Quest/QuestNode_Root_RoyalArrival.cs`
- Create: `Source/BDP.Content/Factions/Patch_PawnGenerator_GeneratePawn_NearRealmCombatBody.cs`

皇室加入任务使用固定的近界宙域作为追兵。只对该派系生成的人物补齐 Trion 腺体和原型触发体，并通过现有 BDP 命令接口激活。

### Task 4：核对改动、写工作日志并提交

- Review only the requested diff and repository status; preserve unrelated untracked files.
- Do not add or run tests. Report that game loading and in-game raids still require a later manual check.
- Write one newest-first entry to `日志/Agent工作日志` and commit only the exact task files.
