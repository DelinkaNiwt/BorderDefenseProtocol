# 验证轨迹发射诊断只在发射与终止边界记录摘要，不把日志放进每 Tick 路径。
$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-Source {
    param([string]$Path)
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$projectileText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs')
$bootstrapText = Read-Source (Join-Path $repoRoot 'Source\BDP.Development\DevelopmentBootstrap.cs')
$sinkText = Read-Source (Join-Path $repoRoot 'Source\BDP.Development\Diagnostics\VerseLogDiagnosticSink.cs')

Assert-True (
    ($projectileText -match 'LogLaunchSnapshotApplied') -and
    ($projectileText -match 'LogImpactResolution') -and
    ($projectileText -match 'launchDisposition') -and
    ($projectileText -match 'coverOriginCell') -and
    ($projectileText -match 'coverCandidates') -and
    ($projectileText -match 'terminalHitTarget')
) '真实投射物必须在发射与终止边界记录首发分支、掩体来源、候选和终止命中摘要。'

Assert-True (
    ($projectileText -notmatch 'Tick\(\)[\s\S]{0,1200}BdpDiagnostics\.AttackExecution\(') -and
    ($projectileText -notmatch 'TickInterval\([\s\S]{0,1200}BdpDiagnostics\.AttackExecution\(')
) '轨迹诊断不得放进每 Tick 或每 TickInterval 推进路径。'

Assert-True (
    ($bootstrapText -match 'BdpDiagnosticSinkRegistry\.Register') -and
    ($bootstrapText -match 'BindAttackExecutionSwitchResolver')
) 'Development 必须以可插拔接收器和开发模式开关接入诊断。'

Assert-True (
    ($sinkText -match 'Log\.Message') -and
    ($sinkText -match 'IsNullOrWhiteSpace')
) 'Verse 日志接收器必须安全处理空消息。'

Write-Output 'TrajectoryLaunchRuntimeDiagnosticSmokeTests PASS'
