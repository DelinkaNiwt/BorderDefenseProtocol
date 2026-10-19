# 旋空被动类别修正设计

## 已确认事实

- 正式旋空模板位于 `1.6/Content/Defs/ChipFunctionDef/Presets.xml`。
- `BDP_ChipFunction_Senku` 的表达条目已经声明 `<Kind>Passive</Kind>`。
- 该模板的正式芯片画像目前声明为 `<Category>BDP_ChipCategory_Ability</Category>`。
- 当前被动芯片判定读取正式芯片画像的 `Profile.Category`，不是读取表达条目的 `Kind`。

## 设计

只把 `BDP_ChipFunction_Senku` 的 `Profile/Category` 改为 `BDP_ChipCategory_Passive`。

保留旋空现有的表达类型、被动键、主副槽配置、激活条件、Trion 参数、制造参数、标签、组合技来源声明和独立表达。此次不修改 C#、组合技匹配逻辑或其它芯片。

## 结果边界

修改后，旋空会被现有被动分类提供器识别为被动芯片，从而使用已经完成的手动激活/关闭与同侧默认互斥豁免逻辑。战斗体状态、手部存在性、禁用状态、激活条件、显式互斥组和特殊槽位隔离继续由现有逻辑负责。

## 验证

- 正式 Def 烟测断言 `BDP_ChipFunction_Senku` 使用被动类别，并保留 `<Kind>Passive</Kind>` 和组合所需字段。
- 对修改后的 XML 做结构解析。
- 构建 `BDP.Content` Release 工程。
- 检查 `git diff --check`。
