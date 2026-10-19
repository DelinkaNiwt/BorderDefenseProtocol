$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-RequiredFile {
    param([string]$Path)
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空按钮文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$floatingRoot = Join-Path $sourceRoot 'BDP.Content\FloatingFlight'
$uiRoot = Join-Path $floatingRoot 'UI'
$commandText = Read-RequiredFile (Join-Path $uiRoot 'Command_FloatingFlightMode.cs')
$providerText = Read-RequiredFile (Join-Path $uiRoot 'FloatingFlightGizmoProvider.cs')
$modeResolverText = Read-RequiredFile (Join-Path $floatingRoot 'FloatingFlightModeResolver.cs')
$bootstrapText = Read-RequiredFile (Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs')

Assert-True ($commandText -match 'Command_Action') '浮空按钮应复用原版 Command_Action。'
Assert-True ($commandText -match 'RightClickFloatMenuOptions') '浮空按钮必须提供右键直接选模式。'
Assert-True ($providerText -match 'ITriggerExternalGizmoProvider') '浮空按钮必须接入正式外部按钮提供器。'
Assert-True ($providerText -match 'HasCapability|FloatingFlightCapabilityResolver') '按钮必须只对有效浮空能力显示。'
Assert-True ($providerText -match 'Drafted|IsPlayerControlled') '按钮必须服从玩家征召条件。'
Assert-True ($providerText -match 'groupable\s*=\s*true|groupKeyIgnoreContent') '多选按钮必须按模式和操作合法性聚合。'
Assert-True ($providerText -match 'ResolvePreferredMode|GetOrCreateRecord') '按钮必须显示 Pawn 个体偏好。'
Assert-True ($providerText -match 'CanSelectAlways|AlwaysBlocked') '按钮必须阻止屋顶下开启始终浮空。'
Assert-True ($providerText -match 'defaultLabel\s*=\s*"BDP_Command_FloatingFlight_Switch"\.Translate\(ShortModeLabel\(current\)\)') '浮空按钮标签必须只显示当前短模式。'
Assert-True ($providerText -match 'ShortModeLabel') '浮空按钮必须提供独立的短模式标签解析。'
Assert-True ($providerText -match 'defaultDesc\s*=\s*BuildDescription\(current,\s*next,\s*canAlways\)') '浮空按钮描述必须同时传入当前模式和下一模式。'
Assert-True ($providerText -match 'FloatingFlightMode current' -and $providerText -match 'FloatingFlightMode next' -and $providerText -match 'bool canAlways') '浮空按钮描述构建器必须接收两个模式参数。'
Assert-True ($providerText -match 'BDP_Command_FloatingFlight_Description"\.Translate\(\s*ModeLabel\(current\),\s*ModeLabel\(next\)') '浮空按钮描述本地化必须为两个占位符提供参数。'
Assert-True ($modeResolverText -match 'TrySetPreferredMode|CyclePreferredMode') '模式解析器必须提供正式写入入口。'
Assert-True ($bootstrapText -match 'FloatingFlightGizmoProvider') 'Content 启动器必须注册浮空按钮提供器。'

Write-Output 'FloatingFlightGizmoSmokeTests PASS'
