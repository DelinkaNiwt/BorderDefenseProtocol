$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$specPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ResolvedVerbSpec.cs'
$factoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ResolvedVerbSpecFactory.cs'
$planPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$stagePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$shootPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'

$specText = Get-Content -Raw -Encoding utf8 -LiteralPath $specPath
$factoryText = Get-Content -Raw -Encoding utf8 -LiteralPath $factoryPath
$planText = Get-Content -Raw -Encoding utf8 -LiteralPath $planPath
$stageText = Get-Content -Raw -Encoding utf8 -LiteralPath $stagePath
$shootText = Get-Content -Raw -Encoding utf8 -LiteralPath $shootPath

Assert-True (
    ($specText -match 'public float MuzzleFlashScale \{ get; set; \}') -and
    ($factoryText -match 'MuzzleFlashScale = verbProps\.muzzleFlashScale') -and
    ($factoryText -match 'MuzzleFlashScale = baseSpec\.MuzzleFlashScale')
) '正式 Verb 规格必须保存并在组合覆盖中保留来源武器的枪口闪光尺寸。'

Assert-True (
    $factoryText -match 'muzzleFlashScale = 0f'
) 'BDP 宿主表面必须把原版中心枪口闪光尺寸设为零，避免重复闪光。'

Assert-True (
    ($planText -match 'public float MuzzleFlashScale \{ get; set; \}') -and
    ($planText -match 'Scribe_Values\.Look\(ref muzzleFlashScale, "muzzleFlashScale", 0f\)') -and
    ($stageText -match 'MuzzleFlashScale = ResolveMuzzleFlashScale\(emit, entry\)')
) '每个投射物计划必须冻结自己来源武器的枪口闪光尺寸并参与存档。'

$tryEmitMethod = [regex]::Match(
    $shootText,
    '(?s)internal bool TryEmitPlan\(ProjectileInitPlan plan\).*?\r?\n        \}\r?\n\r?\n        protected override bool TryCastShot').Value
Assert-True (-not [string]::IsNullOrWhiteSpace($tryEmitMethod)) '必须能定位发射计划执行成员。'

$launchIndex = $tryEmitMethod.IndexOf('bool emitted = TryLaunchSinglePlan')
$flashIndex = $tryEmitMethod.IndexOf('FleckMaker.Static(')
Assert-True (
    ($launchIndex -ge 0) -and
    ($flashIndex -gt $launchIndex) -and
    ($tryEmitMethod -match 'if \(emitted && plan\.MuzzleFlashScale > 0\.01f\)') -and
    ($tryEmitMethod -match 'FleckMaker\.Static\(\s*rootOrigin,\s*caster\.Map,\s*FleckDefOf\.ShotFlash,\s*plan\.MuzzleFlashScale\s*\)')
) '只有实际发射成功后，才允许在该计划的枪口根坐标调用原版 ShotFlash。'

Assert-True (
    $tryEmitMethod -notmatch 'FleckMaker\.Static\(\s*caster\.Position'
) 'BDP 每发枪焰不得回退到小人所在格中心。'

Write-Output 'RangedMuzzleFlashOwnershipSmokeTests PASS'
