$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$semanticPath = Join-Path $repoRoot 'Source\BDP\Core\Semantics\SemanticRuntimeScope.cs'
$targetingPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\TargetingProtocol\Interaction\TargetingInputRuntimeScope.cs'
$targetPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Targeter_OrderPawnForceTarget_TargetingInput.cs'
$damagePatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs'

$semanticText = Get-Content -LiteralPath $semanticPath -Raw -Encoding utf8
$targetingText = Get-Content -LiteralPath $targetingPath -Raw -Encoding utf8
$targetPatchText = Get-Content -LiteralPath $targetPatchPath -Raw -Encoding utf8
$damagePatchText = Get-Content -LiteralPath $damagePatchPath -Raw -Encoding utf8

Assert-True (($semanticText -match 'disposed') -and ($semanticText -match 'scopeId')) 'SemanticRuntimeScope PopScope must be idempotent and token-bound.'
Assert-True (($targetingText -match 'disposed') -and ($targetingText -match 'scopeId')) 'TargetingInputRuntimeScope PopScope must be idempotent and token-bound.'

Assert-True ($targetPatchText -match 'HarmonyFinalizer') 'Targeting input patch must clean temporary scope in a Harmony finalizer.'
Assert-True ($damagePatchText -match 'HarmonyFinalizer') 'Explosion damage semantic patch must clean temporary scope in a Harmony finalizer.'

Write-Output 'RuntimeScopeCleanupBoundary PASS'
