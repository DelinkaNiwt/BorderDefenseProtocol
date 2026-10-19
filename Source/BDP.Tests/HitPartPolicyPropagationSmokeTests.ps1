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

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'

$paths = @(
    (Join-Path $coreRoot 'Expressions\Contract\ChipExpressionContractInterpreter.cs'),
    (Join-Path $coreRoot 'Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs'),
    (Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'),
    (Join-Path $coreRoot 'Expressions\Pipeline\SingleSideExpressionBuilder.cs'),
    (Join-Path $coreRoot 'Expressions\Pipeline\CompositeExpressionResolver.cs'),
    (Join-Path $coreRoot 'Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'),
    (Join-Path $coreRoot 'Combos\Config\ComboExpressionEntryConfig.cs'),
    (Join-Path $coreRoot 'Combos\Config\ComboExpressionEntryCloneService.cs'),
    (Join-Path $contentRoot 'Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs')
)

foreach ($path in $paths) {
    Assert-True (Test-Path -LiteralPath $path) "Propagation file is missing: $path"
    $text = Get-Content -LiteralPath $path -Raw -Encoding utf8
    Assert-True ($text -match 'HitPartPolicy') "Propagation file must carry or clone HitPartPolicy: $path"
}

$compositeText = Get-Content -LiteralPath (Join-Path $coreRoot 'Expressions\Pipeline\CompositeExpressionResolver.cs') -Raw -Encoding utf8
Assert-True (($compositeText -match 'BuildDualHitPartPolicy') -or ($compositeText -match 'HitPartPolicy =')) 'Composite results must make the hit-part policy merge explicit.'

$comboText = Get-Content -LiteralPath (Join-Path $coreRoot 'Expressions\Pipeline\ComboFormalExpressionResultFactory.cs') -Raw -Encoding utf8
Assert-True (($comboText -match 'HitPartPolicy') -and ($comboText -match 'CloneHitPartPolicy')) 'Combo results must clone the optional hit-part policy.'

Write-Output 'HitPartPolicyPropagationSmokeTests PASS'
