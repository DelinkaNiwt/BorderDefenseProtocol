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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$stagePath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\WeaponVisualActionStage.cs'
$snapshotPath = Join-Path $bdpSourceRoot 'Core\Trigger\Visual\WeaponVisualStageSnapshot.cs'
$overridePath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ExpressionVisualStageOverrideConfig.cs'
$presetPath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ExpressionVisualPresetDef.cs'

Assert-True (Test-Path -LiteralPath $stagePath) 'WeaponVisualActionStage must exist.'
Assert-True (Test-Path -LiteralPath $snapshotPath) 'WeaponVisualStageSnapshot must exist.'
Assert-True (Test-Path -LiteralPath $overridePath) 'ExpressionVisualStageOverrideConfig must exist.'
Assert-True (Test-Path -LiteralPath $presetPath) 'ExpressionVisualPresetDef must exist.'

$stageText = Get-Content -LiteralPath $stagePath -Raw -Encoding utf8
$snapshotText = Get-Content -LiteralPath $snapshotPath -Raw -Encoding utf8
$overrideText = Get-Content -LiteralPath $overridePath -Raw -Encoding utf8
$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding utf8

Assert-True (
    ($stageText -match 'Idle') -and
    ($stageText -match 'Warmup') -and
    ($stageText -match 'Firing') -and
    ($stageText -match 'Cooldown')
) 'The neutral visual stage contract must contain idle, warmup, firing and final cooldown.'

Assert-True (
    ($snapshotText -match 'WeaponVisualActionStage\s+Stage') -and
    ($snapshotText -match 'float\s+Progress01') -and
    ($snapshotText -match 'int\s+StageTicksRemaining') -and
    ($snapshotText -match 'string\s+MatchedSourceResultId') -and
    ($snapshotText -match 'string\s+HostResultId') -and
    ($snapshotText -match 'string\s+AttackInstanceId') -and
    ($snapshotText -match 'int\s+ProjectionVersion')
) 'The stage snapshot must retain progress and formal-session diagnostic identity.'

Assert-True (
    ($overrideText -match 'WeaponVisualActionStage\s+Stage') -and
    ($overrideText -match 'bool\s+Visible\s*=\s*true') -and
    ($overrideText -match 'GraphicData\s+GraphicData')
) 'Each optional stage override must define its stage, visibility and optional graphic.'

Assert-True (
    ($presetText -match 'List<ExpressionVisualStageOverrideConfig>\s+StageVisuals') -and
    ($presetText -match 'ResolveStageOverride\(') -and
    ($presetText -match 'ResolveStageVisibility\(') -and
    ($presetText -match 'ResolveGraphic\(bool active, WeaponVisualActionStage stage, Thing sourceThing\)')
) 'Expression visual presets must expose optional stage overrides and stage-aware graphic resolution.'

Assert-True (
    ($presetText -match 'ResolveGraphic\(bool active, Thing sourceThing\)') -and
    ($presetText -match 'return ResolveGraphic\(active, sourceThing\)') -and
    ($presetText -match 'return true')
) 'Missing stage configuration must preserve the original graphic selection and default visibility.'

Assert-True (
    ($presetText -match 'override IEnumerable<string> ConfigErrors\(\)') -and
    ($presetText -match 'BDP_ConfigError_DuplicateWeaponVisualStage')
) 'Duplicate stage entries must produce an author-facing configuration error.'

Write-Output 'WeaponVisualStageConfigSmokeTests PASS'
