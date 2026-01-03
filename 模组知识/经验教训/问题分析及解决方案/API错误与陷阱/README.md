# API 错误与陷阱

RimWorld API 使用中的常见错误。

## 内容格式

每个 API 一个文件或目录，记录：
- **问题**：调用该 API 时遇到什么错误？
- **症状**：游戏中表现为什么现象？
- **根因**：RimWorld 源码中的原因是什么？
- **解决方案**：如何正确使用？

## 例子

- `Pawn.Kill_内存泄漏.md` - 调用 Kill 后资源未清理
- `JobDriver_死循环风险.md` - CheckForGetOpportunityJob 陷阱

## 说明

**不存储 API 文档本身**（用 RiMCP 查询）。只记录实践中发现的错误和陷阱。
