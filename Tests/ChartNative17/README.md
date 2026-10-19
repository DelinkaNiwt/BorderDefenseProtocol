# 1.7 原生图表验证

独立进程编译正式图表几何源码，并调用真实游戏原生库。验证颜色、透明度、坐标缩放、父子裁切、状态恢复、雷达凸边界、凹轮廓、扇心、不同角度扇区、虚线和非重绘事件。

先在 `Source/Build/Bdp.Local.props` 配置真实的 `RimWorld17ReferenceDir`（1.7 引用目录），然后从工作区根目录执行：

```powershell
dotnet run --project '模组工程/BorderDefenseProtocol/Tests/ChartNative17/ChartNative17.csproj' -c Release -- 'C:/NiwtGames/Steam/steamapps/common/RimWorld'
```

末尾参数应指向与编译引用相同的 1.7 游戏根目录；游戏库只引用，不复制到模组。缺失引用时停止，不借用 1.6。

程序使用最小的原版坐标／缩放替身，检查原生绘制输出；它不代替游戏窗口、滚动、实际缩放和叠层的画面验收。验证工程独立于正式 `Source`（源码）模块，不参与模组部署。
