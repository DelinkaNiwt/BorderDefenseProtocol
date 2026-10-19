# 本脚本使用 UTF-8 BOM，确保 Windows PowerShell 正确读取中文断言。
$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

function Read-Xml {
    param([string]$Path)

    return [xml](Get-Content -LiteralPath $Path -Raw -Encoding utf8)
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$distanceRoot = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\DistanceRevector'
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$distanceText = Read-Source (Join-Path $distanceRoot 'DistanceRevectorModule.cs')
$planText = Read-Source (Join-Path $coreRoot 'AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs')
$contributionText = Read-Source (Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs')
$initServiceText = Read-Source (Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs')
$initContextText = Read-Source (Join-Path $coreRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageContext.cs')
$abilityShootText = Read-Source (Join-Path $coreRoot 'Abilities\BdpVerb_AbilityShoot.cs')
$fujinModuleXml = Read-Xml (Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\RangedModules.xml')
$routeText = Read-Source (Join-Path $repoRoot 'Source\BDP.Content\RangedModules\RoutePath\RouteSegmentResolver.cs')
$sharedSpreadText = Read-Source (Join-Path $repoRoot 'Source\BDP.Content\RangedModules\OrderedSpread\OrderedSpreadSampler.cs')

Assert-True (
    ($distanceText -match 'UseOrderedParallelLanes') -and
    ($distanceText -match 'ParallelLaneUnit') -and
    ($distanceText -match 'ParallelLaneDirection') -and
    ($distanceText -match 'ResolveParallelLane') -and
    ($distanceText -match 'ResolveParallelLaneEnvelope') -and
    ($distanceText -match 'OrderedLaneExpansionStartRatio') -and
    ($distanceText -match 'OrderedLaneTerminalEnvelope') -and
    ($distanceText -match 'private\s+const\s+float\s+OrderedLaneTerminalEnvelope\s*=\s*0\.60f') -and
    ($distanceText -match 'ResolveParallelLanePoint') -and
    ($distanceText -match 'Mathf\.Max\(OrderedLaneTerminalEnvelope,\s*envelope\)')
) '风刃距离重定向必须冻结并复用有序横向车道及共同收束曲线。'

Assert-True (
    ($distanceText -match 'HasOriginSpreadOffsetOverride') -and
    ($distanceText -match 'OriginSpreadOffsetOverrideWorld') -and
    ($distanceText -match 'TryGetEmitOriginSpread') -and
    ($distanceText -match 'NextCheckpointIndex\s*=\s*beginParallelLaneExpansionImmediately\s*\?\s*0\s*:\s*1') -and
    ($distanceText -match 'InitialSegmentTriggerRatio\s*=\s*beginParallelLaneExpansionImmediately') -and
    ($distanceText -match 'NextCheckpointIndex\s*=\s*nextCheckpointIndex\s*<\s*0\s*\?\s*0\s*:\s*nextCheckpointIndex')
) '风刃模块必须把现有发射点散布解析为与车道一致的计划偏移。'

Assert-True (
    ($contributionText -match 'HasOriginSpreadOffsetOverride') -and
    ($contributionText -match 'OriginSpreadOffsetOverrideWorld') -and
    ($planText -match 'HasOriginSpreadOffsetOverride') -and
    ($planText -match 'OriginSpreadOffsetOverrideWorld') -and
    ($initServiceText -match 'plan\.HasOriginSpreadOffsetOverride')
) 'Core 只能提供中性的初始化计划发射点偏移承载，不得写入风刃业务判断。'

Assert-True (
    ($initContextText -match 'TryGetEmitOriginSpread') -and
    ($abilityShootText -match 'HasOriginSpreadOffsetOverride') -and
    ($abilityShootText -match 'OriginSpreadOffsetOverrideWorld')
) 'Ability 远程宿主必须消费模块已解析的发射点偏移。'

Assert-True (
    ([string](@($fujinModuleXml.Defs.'BDP.Core.AttackExecution.BdpRangedAttackModuleDef' |
        Where-Object { $_.defName -eq 'BDP_RangedModule_DistanceRevector' })[0].defaultConfig.UseOrderedParallelLanes) -eq 'true')
) '风刃必须显式启用有序平行车道；该开关不得默认扩散到其它模块。'

Assert-True (
    ($routeText -match 'OrderedSpreadSampler\.SampleOffset') -and
    ($sharedSpreadText -match 'SampleOffset')
) '毒蛇路线引导和共享散布工具必须保留原有行为。'

Write-Output 'FujinParallelLaneSpreadSmokeTests PASS'
