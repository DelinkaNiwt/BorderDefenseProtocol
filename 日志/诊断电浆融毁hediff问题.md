# 诊断电浆融毁hediff残留问题

## 需要确认的信息

请在游戏中开启Dev Mode，然后：

1. **查看hediff的defName**：
   - 选中小人
   - 打开Health标签
   - 右键点击"电浆融毁"hediff
   - 查看它的defName（英文名称）

2. **查看hediff的C#类型**：
   - 在Dev Mode下，使用"Inspect"工具
   - 查看这个hediff的完整类型名称（例如：Verse.Hediff_Injury）

3. **确认时间点**：
   - 这个hediff是在战斗体激活之前就有的？
   - 还是在战斗体激活之后添加的？

4. **查看日志**：
   - 解除战斗体时，查看游戏日志（按 ~ 键打开）
   - 搜索 "[BDP]" 相关的日志
   - 看是否有"发现残留hediff"的警告

## 可能的原因

### 原因1：hediff在排除列表中
如果这个hediff继承自以下类，会被排除：
- Verse.Hediff_Psylink
- Verse.Hediff_Mechlink

### 原因2：hediff在快照中
如果这个hediff在战斗体激活之前就存在，它会被记录在快照中，解除时会被恢复。

### 原因3：hediff在清理之后被添加
如果某个系统在清理之后又添加了这个hediff，它就会残留。

### 原因4：清理逻辑bug
可能是我们的清理逻辑有遗漏。

## 临时解决方案

如果确认是bug，可以临时在排除列表中添加这个hediff的defName，让它不被快照系统管理。

## 下一步

请提供上述信息，我会根据具体情况修复问题。
