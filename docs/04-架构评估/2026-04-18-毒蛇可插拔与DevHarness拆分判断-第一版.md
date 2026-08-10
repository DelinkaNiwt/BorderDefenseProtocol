# 毒蛇可插拔与 DevHarness 拆分判断

## 结论先说

- 主模组单独运行：**可以**。删除 `BorderDefenseProtocol.DevHarness` 后，主模组本身按当前代码依赖关系看，不应因为缺少测试模组而产生运行时依赖错误。
- 毒蛇运行时链路：**基本可插拔**。路线引导模块本身是业务样本，主要依赖主模组公开骨架。
- 当前仓库整体：**还没做到完整可插拔**。如果你直接把毒蛇相关文件剪到 `v2`，而不补改菜单发现和测试脚本，最终表现**不会与当前完全一致**。

## 为什么主模组单独运行没问题

- `BorderDefenseProtocol.DevHarness` 的 `About.xml` 依赖主模组。
- 主模组 `About.xml` 没有反向依赖 `DevHarness`。
- 主模组源码里没有查到 `BDP.DevHarness`、`PathLatchModule`、`BDP_TestRangedPathLatchModule` 这类运行时反向引用。

## 但有两个例外

- 旧存档如果已经装了测试芯片、测试 Trigger、测试 Def，删掉测试模组后会出现“缺失内容”问题。这是内容缺失，不是主模组核心骨架反向依赖。
- 主模组仓库里的很多烟雾测试直接读 `DevHarness` 文件；删掉测试模组后，这些开发期测试会失效。

## 毒蛇为什么说“基本可插拔”

- 当前毒蛇样本主要由以下几块组成：
  - `PathLatchModule`
  - `PathLatchState`
  - `PathLatchSegmentResolver`
  - `PathLatchConfig`
  - `BDP_TestRangedPathLatchModule`
  - `BDP_TestChipPathLatch`
- 这些内容都在测试模组里，通过主模组的中性接口接入：
  - 确认阶段写 `Target / SemanticTarget`
  - 投射初始化写 `LaunchTarget / AimTarget / CurrentTarget`
  - 到达阶段写 `NextDestination / NextTarget`
  - 段合法性走主模组 `TargetingSegmentLegality`

## 为什么现在还不能直接无痛剪去 v2

- `DevHarness` 旧窗口的芯片发现逻辑明确只认 `niwt.bdp.devharness` 这个 `packageId`。
- 也就是说，当前旧窗口不会自动把别的测试模组包里的标准芯片当成“自己的测试芯片”。
- 仓库里还有烟雾测试专门锁这个行为，说明这不是偶然，而是当前已定下来的实现。

## 直接剪到 v2 后，哪些会不一致

- 游戏运行时：
  - **大概率正常**，前提是把毒蛇相关 `Def（定义）`、运行时类、配置类一起迁走，并改好新的类型路径。
- `DevHarness` 旧菜单：
  - **不会保持当前一致**，因为它不会自动发现 `v2` 包里的芯片。
- 主模组仓库烟雾测试：
  - **不会保持当前一致**，因为当前有一批测试脚本直接引用 `DevHarness` 的毒蛇样本和 XML 文件。

## 最果决的判断

- **删掉当前测试模组后，主模组本身不会因为反向依赖而跑挂。**
- **毒蛇样本从运行时骨架角度看已经接近可插拔。**
- **但按你描述的“直接剪到 v2，老 DevHarness 不改还要保持全部表现一致”——答案是否定的。当前做不到。**

## 如果以后真要做到“完整可插拔”

- 把 `DevHarness` 芯片发现逻辑改成：
  - 包无关的标准发现
  - 或者多包白名单发现
- 把毒蛇专项烟雾测试迁到 `v2`
- 主模组只保留中性边界测试，不再直接点名 `PathLatch`
