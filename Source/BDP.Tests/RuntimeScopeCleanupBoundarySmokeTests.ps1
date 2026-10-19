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
$damagePatchText = Get-Content -LiteralPath $damagePatchPath -Raw -Encoding utf8

Assert-True (($semanticText -match 'disposed') -and ($semanticText -match 'scopeId')) 'SemanticRuntimeScope PopScope must be idempotent and token-bound.'
Assert-True (-not (Test-Path -LiteralPath $targetingPath)) '自有输入不得再使用线程作用域。'

Assert-True (-not (Test-Path -LiteralPath $targetPatchPath)) '原版目标选择器输入转运补丁应已移除。'
Assert-True ($damagePatchText -match 'HarmonyFinalizer') 'Explosion damage semantic patch must clean temporary scope in a Harmony finalizer.'

Write-Output 'RuntimeScopeCleanupBoundary PASS'
