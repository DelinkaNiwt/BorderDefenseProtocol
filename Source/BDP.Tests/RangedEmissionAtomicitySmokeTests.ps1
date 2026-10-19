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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$launchServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectileLaunchService.cs'
$shootVerbText = Read-Source $shootVerbPath
$launchServiceText = Read-Source $launchServicePath

$tryCastStart = $shootVerbText.IndexOf('protected override bool TryCastShot()')
$tryCastEnd = $shootVerbText.IndexOf('        /// <summary>', $tryCastStart + 1)
$tryCastBody = if (($tryCastStart -ge 0) -and ($tryCastEnd -gt $tryCastStart)) {
    $shootVerbText.Substring($tryCastStart, $tryCastEnd - $tryCastStart)
} else {
    ''
}

$tryLaunchStart = $shootVerbText.IndexOf('private bool TryLaunchSinglePlan(')
$tryLaunchEnd = $shootVerbText.IndexOf('        private static void FreezeLaunchResolution', $tryLaunchStart + 1)
$tryLaunchBody = if (($tryLaunchStart -ge 0) -and ($tryLaunchEnd -gt $tryLaunchStart)) {
    $shootVerbText.Substring($tryLaunchStart, $tryLaunchEnd - $tryLaunchStart)
} else {
    ''
}

Assert-True (
    ($tryCastBody -match 'TryValidateCurrentEmissionWindowTargets\(\)') -and
    ($tryCastBody.IndexOf('TryValidateCurrentEmissionWindowTargets()') -lt $tryCastBody.IndexOf('TryCommitRoundTrionBeforeFirstEmission()'))
) 'TryCastShot（尝试发射）必须在首发 Trion 提交前验证当前窗口的实体目标仍有效。'

Assert-True (
    ($tryLaunchBody -match 'ShotReport.HitReportFor') -and
    ($tryLaunchBody -match 'CreateProjectileForLaunch\(') -and
    ($launchServiceText -match 'GenSpawn.Spawn')
) 'TryLaunchSinglePlan（发射单个计划）必须先完成原版射击报告，并通过统一服务生成投射物。'

Assert-True (
    $launchServiceText -match 'CreateProjectileForLaunch\('
) '投射物生成与初始化必须集中在统一发射服务中，避免各个 Ability/Verb 宿主重复实现。'

Write-Output 'RangedEmissionAtomicitySmokeTests PASS'
