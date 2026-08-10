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

$contractPath = Join-Path $bdpSourceRoot 'Core\Expressions\Contract\ChipExpressionEntryContract.cs'
$declarationPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\ExpressionSourceDeclaration.cs'
$materialPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\ExpressionSourceMaterial.cs'
$resultPath = Join-Path $bdpSourceRoot 'Core\Expressions\Model\FormalExpressionResult.cs'
$interpreterPath = Join-Path $bdpSourceRoot 'Core\Expressions\Contract\DefaultChipExpressionContractInterpreter.cs'
$hostManagerPath = Join-Path $bdpSourceRoot 'Core\VerbHosting\TriggerBodyVerbHostManager.cs'

$contractText = Get-Content -LiteralPath $contractPath -Raw -Encoding utf8
$declarationText = Get-Content -LiteralPath $declarationPath -Raw -Encoding utf8
$materialText = Get-Content -LiteralPath $materialPath -Raw -Encoding utf8
$resultText = Get-Content -LiteralPath $resultPath -Raw -Encoding utf8
$interpreterText = Get-Content -LiteralPath $interpreterPath -Raw -Encoding utf8
$hostManagerText = Get-Content -LiteralPath $hostManagerPath -Raw -Encoding utf8

Assert-True (
    $contractText -match 'DeclaredTools'
) 'ChipExpressionEntryContract must preserve the full declared melee tool collection.'

Assert-True (
    $declarationText -match 'DeclaredTools'
) 'ExpressionSourceDeclaration must carry the full declared melee tool collection.'

Assert-True (
    $materialText -match 'DeclaredTools'
) 'ExpressionSourceMaterial must carry the full declared melee tool collection.'

Assert-True (
    $resultText -match 'DeclaredTools'
) 'FormalExpressionResult must preserve declared melee tools for runtime selection.'

Assert-True (
    $interpreterText -match 'ResolveDeclaredTools'
) 'DefaultChipExpressionContractInterpreter must expose an explicit declared-tool resolver instead of only resolving one tool.'

Assert-True (
    $interpreterText -notmatch 'return config\.tools != null && config\.tools\.Count > 0 \? config\.tools\[0\] : null;'
) 'DefaultChipExpressionContractInterpreter must not treat tools[0] as the only truth for melee entries.'

Assert-True (
    $hostManagerText -match 'DeclaredTools'
) 'TriggerBodyVerbHostManager binding path must retain declared melee tools instead of only binding a single tool.'

Write-Output 'MeleeMultiToolDeclaration PASS'
