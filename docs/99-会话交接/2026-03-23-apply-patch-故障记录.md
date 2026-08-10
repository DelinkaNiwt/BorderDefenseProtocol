# 2026-03-23 apply_patch 故障记录

## 现象

- 在 `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol` 下使用 `apply_patch`
- 即使只是最小新建文件
- 也会直接报：

`windows sandbox: setup refresh failed with status exit code: 1`

## 排查结果

已做的最小对照：

- 工作区根目录可正常 `apply_patch`
- `模组工程` 父目录可正常 `apply_patch`
- `BorderDefenseProtocol.Legacy` 可正常 `apply_patch`
- 新建同级普通目录 `PatchPlainTest` 可正常 `apply_patch`
- 新建同级目录 `BorderDefenseProtocol.Clean` 可正常 `apply_patch`
- `bdp_ascii` 结点路径同样失败，说明不是中文路径或结点路径本身的问题

结论：

- 问题不在中文路径
- 问题不在 `apply_patch` 工具整体失效
- 问题落在原 `BorderDefenseProtocol` 根目录自身

进一步证据：

- `模组工程` 父目录 ACL 与正常可写目录相比，多一条沙箱访问项
- 原 `BorderDefenseProtocol` 根目录没有正确继承这条访问项
- `apply_patch` 失败发生在 sandbox refresh 阶段，符合“沙箱身份进不去目录”的表现

## 本次采取的稳妥处理

没有继续硬修坏 ACL，而是：

1. 新建同级干净目录 `BorderDefenseProtocol.Clean`
2. 先做最小 `apply_patch` 验证，确认正常
3. 将原项目内容迁移到干净目录
4. 将坏根目录改名备份为 `BorderDefenseProtocol.acl-broken-20260323`
5. 将干净目录改名为正式目录 `BorderDefenseProtocol`
6. 再做一次最小 `apply_patch` 验证，确认恢复正常

## 后续规则

- 新建项目根目录后，第一件事不是继续写内容，而是先做最小 `apply_patch` 验证
- 如果失败，不要继续在该根目录里堆文档和代码
- 优先重建同级干净目录并迁移内容
- 只有在必须保留原目录安全信息时，才考虑继续追修 ACL
