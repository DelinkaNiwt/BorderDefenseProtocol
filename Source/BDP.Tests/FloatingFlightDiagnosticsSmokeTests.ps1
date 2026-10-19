$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-RequiredFile {
    param([string]$Path)
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空诊断文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$diagnosticRoot = Join-Path $sourceRoot 'BDP.Development\FloatingFlight'
$providerText = Read-RequiredFile (Join-Path $diagnosticRoot 'FloatingFlightNonPlayerModeProvider.cs')
$debugText = Read-RequiredFile (Join-Path $diagnosticRoot 'FloatingFlightDebugActions.cs')
$bootstrapText = Read-RequiredFile (Join-Path $sourceRoot 'BDP.Development\DevelopmentBootstrap.cs')

Assert-True ($providerText -match 'IFloatingFlightNonPlayerModeProvider') 'Development 必须通过非玩家模式提供器接入 AI 模式。'
Assert-True ($providerText -match 'Dictionary|ThingID') 'Development 模式覆盖必须是临时 Pawn 个体记录。'
Assert-True ($providerText -match 'TryResolveMode') '非玩家模式提供器必须实现正式查询入口。'
Assert-True ($debugText -match 'DebugAction') 'Development 必须提供开发者调试入口。'
Assert-True ($debugText -match 'Always|MoveOnly|Off') '调试入口必须覆盖三态模式和清除。'
Assert-True ($debugText -match 'Log|Message') '调试入口必须能输出浮空状态快照。'
Assert-True ($bootstrapText -match 'FloatingFlightNonPlayerModeProvider|Register') 'Development 启动器必须注册浮空非玩家模式提供器。'
Assert-True ($bootstrapText -match 'IsDeveloperModeEnabled') '浮空诊断设施必须只在开发者模式下注册。'

Write-Output 'FloatingFlightDiagnosticsSmokeTests PASS'
