# 项目规则

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
- 修复代码要避免盲目补丁，禁止一层层补丁叠加
- 修复问题必须遵从正确的需求逻辑，禁止从结果弥补掩盖逻辑错误
- 不要擅自生成文档

## RimWorld说明
1. 最新版本：1.6.4633
2. DLC列表（5个）：
3. 游戏根目录路径：“C:\NiwtGames\Steam\steamapps\common\RimWorld”
4. 游戏程序集路径：“C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed”
5. C# 7.3 语法
6. 编译命令：cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal