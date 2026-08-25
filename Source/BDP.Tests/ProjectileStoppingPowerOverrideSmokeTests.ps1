$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$sourceRoot = Join-Path $modRoot "Source"
$defsPath = Join-Path $modRoot "1.6\Content\Defs\ChipArmamentFormDef\Presets.xml"
$overridePath = Join-Path $sourceRoot "BDP\Core\Expressions\Config\ProjectileOverrides.cs"
$fireEmitPath = Join-Path $sourceRoot "BDP\Core\AttackExecution\RangedProtocol\Model\FireEmitRecord.cs"
$firePath = Join-Path $sourceRoot "BDP\Core\AttackExecution\RangedProtocol\Fire\FireStageService.cs"
$planPath = Join-Path $sourceRoot "BDP\Core\AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs"
$planStagePath = Join-Path $sourceRoot "BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs"
$projectilePath = Join-Path $sourceRoot "BDP\Core\Projectiles\BdpProjectile.cs"
$comboEntryPath = Join-Path $sourceRoot "BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs"
$comboClonePath = Join-Path $sourceRoot "BDP\Core\Combos\Config\ComboExpressionEntryCloneService.cs"
$comboMapPath = Join-Path $sourceRoot "BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs"
$comboServicePath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs"
$comboModifierPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormComboExpressionModifier.cs"

$defsText = Get-Utf8Text $defsPath
$overrideText = Get-Utf8Text $overridePath
$fireEmitText = Get-Utf8Text $fireEmitPath
$fireText = Get-Utf8Text $firePath
$planText = Get-Utf8Text $planPath
$planStageText = Get-Utf8Text $planStagePath
$projectileText = Get-Utf8Text $projectilePath
$comboEntryText = Get-Utf8Text $comboEntryPath
$comboCloneText = Get-Utf8Text $comboClonePath
$comboMapText = Get-Utf8Text $comboMapPath
$comboServiceText = Get-Utf8Text $comboServicePath
$comboModifierText = Get-Utf8Text $comboModifierPath

Assert-True ($defsText -match '<stoppingPowerMultiplier>2\.5</stoppingPowerMultiplier>') `
    'Revolver armament form must declare its stopping power multiplier.'
Assert-True ($defsText -match '<stoppingPowerMultiplier>1</stoppingPowerMultiplier>') `
    'Assault rifle armament form must declare its stopping power multiplier.'
Assert-True ($defsText -match '<stoppingPowerMultiplier>3</stoppingPowerMultiplier>') `
    'Shotgun armament form must declare its stopping power multiplier.'
Assert-True ($overrideText -match 'stoppingPowerMultiplier') `
    'ProjectileOverrides must expose stopping power multiplier.'
Assert-True ($fireEmitText -match 'StoppingPowerFactor') `
    'FireEmitRecord must carry the stopping power factor per emission.'
Assert-True ($fireText -match 'stoppingPowerMultiplier') `
    'FireStageService must consume the stopping power multiplier.'
Assert-True ($fireText -match 'StoppingPowerFactor') `
    'FireStageService must freeze stopping power factor into each emission.'
Assert-True ($planText -match 'InitialStoppingPowerFactor') `
    'ProjectileInitPlan must carry the frozen stopping power factor.'
Assert-True ($planText -match 'initialStoppingPowerFactor') `
    'ProjectileInitPlan must persist the frozen stopping power factor.'
Assert-True ($planStageText -match 'InitialStoppingPowerFactor\s*=') `
    'ProjectileInitStageService must copy stopping power factor into each plan.'
Assert-True ($projectileText -match 'launchPlan\.InitialStoppingPowerFactor') `
    'BdpProjectile must apply the plan stopping power factor at launch.'
Assert-True ($comboEntryText -match 'ProjectileOverrides') `
    'Combo expression entries must carry projectile overrides.'
Assert-True ($comboCloneText -match 'ProjectileOverrides') `
    'Combo expression entry cloning must preserve projectile overrides.'
Assert-True ($comboMapText -match 'ProjectileOverrides') `
    'Combo expression entry mapping must pass projectile overrides to the interpreter.'
Assert-True ($comboServiceText -match 'ComboExpressionEntryConfig[\s\S]*ApplyProjectileOverrides') `
    'Combo armament application must apply projectile overrides to combo entries.'
Assert-True ($comboModifierText -match 'projectileOverrides') `
    'Combo armament modifier must not discard projectile-only armament forms.'

Write-Output 'ProjectileStoppingPowerOverrideSmokeTests PASS'
