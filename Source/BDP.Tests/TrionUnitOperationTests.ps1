# 检查真实业务程序集中的运行规则；不以源码文本或测试替身代替实际决策。
[CmdletBinding()]
param([Parameter(Mandatory = $true)][ValidateSet('1.6','1.7')][string]$RimWorldVersion,
    [string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
[xml]$props = Get-Content -Raw -LiteralPath (Join-Path $modRoot 'Source/Build/Bdp.Local.props')
$gameRoot = if ($RimWorldVersion -eq '1.7') { $props.Project.PropertyGroup.RimWorld17ReferenceDir } else { $props.Project.PropertyGroup.RimWorld16ReferenceDir }
$searchPaths = @((Join-Path $modRoot "$RimWorldVersion/Assemblies"), $gameRoot)
# 在真实版本引用中解析依赖，防止 GetType 因找不到原版基类误报业务类型不存在。
$resolver = [ResolveEventHandler]{ param($sender,$eventArgs)
    $name = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name + '.dll'
    foreach ($directory in $searchPaths) {
        $candidate = Join-Path $directory $name
        if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
if (-not $AssemblyPath) { $AssemblyPath = Join-Path $modRoot "$RimWorldVersion/Assemblies/BDP.Content.dll" }
$assembly = [Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($AssemblyPath))
$rules = $assembly.GetType('BDP.Content.Trion.Units.TrionUnitOperationConditions', $false)
if ($null -eq $rules) { throw '运行条件入口尚未实现：不能验证许可与归属带宽的独立限制。' }
$method = $rules.GetMethod('EvaluateFacts')
if ($null -eq $method) { throw '缺少正式运行条件求值入口。' }
# 玩家任一独立条件失效都应停机；非玩家不受玩家前置约束。
$cases = @(
    @{ Facts = @($true,$true,$true,$true,$true); Expected = 'None' },
    @{ Facts = @($true,$false,$true,$true,$true); Expected = 'OffMap' },
    @{ Facts = @($true,$true,$false,$true,$true); Expected = 'Permission' },
    @{ Facts = @($true,$true,$true,$false,$true); Expected = 'Ownership' },
    @{ Facts = @($true,$true,$true,$true,$false); Expected = 'Bandwidth' },
    @{ Facts = @($false,$false,$false,$false,$false); Expected = 'None' }
)
foreach ($case in $cases) {
    $actual = $method.Invoke($null, [object[]]$case.Facts).ToString()
    if ($actual -ne $case.Expected) { throw "运行条件错误：预期 $($case.Expected)，实际 $actual。" }
}
Write-Output "通过：$RimWorldVersion 的玩家独立前置与非玩家免前置行为（6例）。"

# 真类型容器只验证访问链；游戏动作、伤口时钟及读档时序交由开发样本实测。
Add-Type -TypeDefinition (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'TrionUnitSurfaceTests.cs'))
$game = [Reflection.Assembly]::LoadFrom((Join-Path $gameRoot 'Assembly-CSharp.dll'))
$core = [Reflection.Assembly]::LoadFrom((Join-Path $searchPaths[0] 'BDP.Core.dll'))
[TrionUnitSurfaceTests]::Run($game,$core,$assembly)
[TrionUnitSurfaceTests]::CheckPlayerOrderQueue($game,(Join-Path $modRoot 'Common/Content/Defs/TrionUnits/ThinkTreeDefs_TrionUnit.xml'))
# 验证定义真正挂载了可抢占的节点，不能只验证一个从未被定义引用的正确类型。
[xml]$thinkTreeDefinitions = Get-Content -Raw -LiteralPath (Join-Path $modRoot 'Common/Content/Defs/TrionUnits/ThinkTreeDefs_TrionUnit.xml')
$combatNode = $thinkTreeDefinitions.SelectSingleNode('//ThinkTreeDef[defName="BDP_TrionUnitAutoCombat"]/thinkRoot/subNodes/li')
if ($null -eq $combatNode) { throw '缺少独立自动战斗子树。' }
$combatType = $assembly.GetType([string]$combatNode.Class, $true)
$combatGiver = [Activator]::CreateInstance($combatType)
$priorityArguments = [object[]]@($null,[single]0)
if (-not $combatType.GetMethod('TryGetPriority').Invoke($combatGiver,$priorityArguments) -or $priorityArguments[1] -lt 1) {
    throw '自动战斗定义实际挂载的节点不具备抢占资格。'
}
Write-Output "通过：$RimWorldVersion 的身体身份、人类会话隔离、初始化/运行/停机功能准入与正式投影发布入口。"
Write-Output "通过：$RimWorldVersion 的自动战斗开关、控制资格限制及独立作业来源识别。"
Write-Output "通过：$RimWorldVersion 的玩家／非玩家兵芯片资格豁免、普通人物与通用条件保留、玩家运行／停机共用资源条。"
Write-Output "通过：$RimWorldVersion 的实际定义顺序及原版队列取命令，两条命令依次返回后才征召待命。"
