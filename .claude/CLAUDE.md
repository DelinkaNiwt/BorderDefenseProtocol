# 项目规则
- 思考讨论和调试可以接受曲折玩绕，但对问题的最终修复应用要遵循第一性原理，清除冗余垃圾，回退错误改动
- 所有修改务必遵循"最简原则"
- 注释丰富
- 中文回答我的问题，回答完留下签名（指当前AI模型名称）
- 生成文档必须开头增加元信息，结尾增加历史记录。
  - 模板：“C:\NiwtDatas\Projects\RimworldModStudio\系统文件\元信息及历史记录模板.md”
  - 模板中“[标签]”的释义："C:\NiwtDatas\Projects\RimworldModStudio\系统文件\标签类别及说明.md"
- 访问网页优先fetch。遇上反爬的网站可以用playwright
- 主动“解释原因”
- 分析代码前先分析相关架构，知晓设计模式。若修改代码有打破架构模式（比如模块化代码的修改严禁打破边界越权）的风险必须发出警告
- 修改代码优先考虑低耦合的模块化设计
- 修复bug的目标是修正逻辑而非修正结果，正确的结果应该是正确的逻辑的自然产物
- 应用修正之前必须先对自己的方案基于第一性原理分析逻辑是否自洽闭环
- 不要擅自生成文档
- 主动用Task规划任务
- 对置信度不足的bug修复任务，禁止依靠猜测改动，优先增加诊断日志让用户辅助测试反馈，基于事实分析。（诊断日志在关键环节，防止无限刷屏）。修复完成后清除诊断日志残留。
- 错误的修改应及时撤销
- 搜集资料、探索信息类工作积极分派子代理完成，有保护主会话上下文长度和纯净度的意识

## RimWorld说明
1. 最新版本：1.6.4633
2. DLC列表（5个）：
3. 游戏根目录路径：“C:\NiwtGames\Steam\steamapps\common\RimWorld”
4. 游戏程序集路径：“C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed”
5. C# 7.3 语法
6. 编译命令：cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal