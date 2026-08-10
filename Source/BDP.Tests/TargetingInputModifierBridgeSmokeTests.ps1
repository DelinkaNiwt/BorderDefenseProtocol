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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'
$patchRoot = Join-Path $repoRoot 'Source\BDP\Patches'

$inputFramePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputFrame.cs'
$inputButtonPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputButton.cs'
$inputModifiersPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputModifiers.cs'
$inputRuntimeFactsPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputRuntimeFacts.cs'
$inputRuntimeScopePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputRuntimeScope.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$targeterPatchPath = Join-Path $patchRoot 'Patch_Targeter_OrderPawnForceTarget_TargetingInput.cs'

$inputFrameText = if (Test-Path -LiteralPath $inputFramePath) { Get-Content -LiteralPath $inputFramePath -Raw -Encoding utf8 } else { '' }
$inputButtonText = if (Test-Path -LiteralPath $inputButtonPath) { Get-Content -LiteralPath $inputButtonPath -Raw -Encoding utf8 } else { '' }
$inputModifiersText = if (Test-Path -LiteralPath $inputModifiersPath) { Get-Content -LiteralPath $inputModifiersPath -Raw -Encoding utf8 } else { '' }
$inputRuntimeFactsText = if (Test-Path -LiteralPath $inputRuntimeFactsPath) { Get-Content -LiteralPath $inputRuntimeFactsPath -Raw -Encoding utf8 } else { '' }
$inputRuntimeScopeText = if (Test-Path -LiteralPath $inputRuntimeScopePath) { Get-Content -LiteralPath $inputRuntimeScopePath -Raw -Encoding utf8 } else { '' }
$targetingSourceText = if (Test-Path -LiteralPath $targetingSourcePath) { Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8 } else { '' }
$targeterPatchText = if (Test-Path -LiteralPath $targeterPatchPath) { Get-Content -LiteralPath $targeterPatchPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    Test-Path -LiteralPath $inputButtonPath
) 'TargetingInputButton.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $inputModifiersPath
) 'TargetingInputModifiers.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $inputRuntimeFactsPath
) 'TargetingInputRuntimeFacts.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $inputRuntimeScopePath
) 'TargetingInputRuntimeScope.cs must exist.'

Assert-True (
    Test-Path -LiteralPath $targeterPatchPath
) 'Patch_Targeter_OrderPawnForceTarget_TargetingInput.cs must exist.'

Assert-True (
    ($inputButtonText -match 'enum\s+TargetingInputButton') -and
    ($inputButtonText -match 'None') -and
    ($inputButtonText -match 'Left') -and
    ($inputButtonText -match 'Right')
) 'TargetingInputButton must expose neutral button facts.'

Assert-True (
    ($inputModifiersText -match 'enum\s+TargetingInputModifiers') -and
    ($inputModifiersText -match 'Shift') -and
    ($inputModifiersText -match 'Control') -and
    ($inputModifiersText -match 'Alt')
) 'TargetingInputModifiers must expose neutral modifier facts.'

Assert-True (
    ($inputRuntimeFactsText -match 'class\s+TargetingInputRuntimeFacts') -and
    ($inputRuntimeFactsText -match 'TargetingInputButton') -and
    ($inputRuntimeFactsText -match 'TargetingInputModifiers')
) 'TargetingInputRuntimeFacts must carry the runtime button and modifier snapshot.'

Assert-True (
    ($inputRuntimeScopeText -match 'class\s+TargetingInputRuntimeScope') -and
    ($inputRuntimeScopeText -match 'Current') -and
    ($inputRuntimeScopeText -match 'Push')
) 'TargetingInputRuntimeScope must expose a neutral temporary input capture scope.'

Assert-True (
    ($inputFrameText -match 'TargetingInputButton\s+PressedButton') -and
    ($inputFrameText -match 'TargetingInputModifiers\s+Modifiers')
) 'TargetingInputFrame must carry button and modifier facts.'

Assert-True (
    ($targeterPatchText -match 'HarmonyPatch\(typeof\(Targeter\),\s*nameof\(Targeter\.OrderPawnForceTarget\)\)') -and
    ($targeterPatchText -match 'TargetingInputRuntimeScope\.Push')
) 'Targeter OrderPawnForceTarget must push a neutral runtime input scope before delegating to targeting sources.'

Assert-True (
    ($targetingSourceText -match 'TargetingInputRuntimeScope\.Current') -and
    ($targetingSourceText -match 'PressedButton') -and
    ($targetingSourceText -match 'Modifiers')
) 'AttackExecutionTargetingSource must read the neutral runtime input scope when creating the per-round targeting input frame.'

Write-Output 'TargetingInputModifierBridgeSmokeTests PASS'
