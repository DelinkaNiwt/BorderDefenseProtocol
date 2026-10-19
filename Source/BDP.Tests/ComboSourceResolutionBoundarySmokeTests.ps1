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

$comboSourceResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboSourceFieldResolver.cs'
$comboResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboDefinitionContractResolver.cs'
$comboFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$comboResolutionPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResolution.cs'
$compositeResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'

Assert-True (Test-Path -LiteralPath $comboSourceResolverPath) 'ComboSourceFieldResolver must exist.'
Assert-True (Test-Path -LiteralPath $comboFactoryPath) 'Combo formal result factory must exist.'
Assert-True (Test-Path -LiteralPath $comboResolutionPath) 'Combo formal result resolution model must exist.'

$comboSourceResolverText = Get-Content -LiteralPath $comboSourceResolverPath -Raw -Encoding utf8
$comboResolverText = Get-Content -LiteralPath $comboResolverPath -Raw -Encoding utf8
$comboFactoryText = Get-Content -LiteralPath $comboFactoryPath -Raw -Encoding utf8
$compositeResolverText = Get-Content -LiteralPath $compositeResolverPath -Raw -Encoding utf8

Assert-True ($comboSourceResolverText -match 'ResolveFloat') 'Resolver must resolve float fields.'
Assert-True ($comboSourceResolverText -match 'ResolveInt') 'Resolver must resolve int fields.'
Assert-True ($comboSourceResolverText -match 'ResolveString') 'Resolver must resolve string fields.'
Assert-True ($comboSourceResolverText -match 'ResolveList') 'Resolver must resolve list fields.'
Assert-True ($comboSourceResolverText -match 'FollowFirstSource') 'Resolver must support FollowFirstSource.'
Assert-True ($comboSourceResolverText -match 'FollowSecondSource') 'Resolver must support FollowSecondSource.'
Assert-True ($comboSourceResolverText -match 'Average') 'Resolver must support Average.'
Assert-True ($comboSourceResolverText -match 'Max') 'Resolver must support Max.'
Assert-True ($comboSourceResolverText -match 'Min') 'Resolver must support Min.'
Assert-True ($comboResolverText -match 'ComboSourceFieldResolver') 'ComboDefinitionContractResolver must delegate shared field math.'
Assert-True ($compositeResolverText -match 'ComboFormalExpressionResultFactory') 'CompositeExpressionResolver must delegate combo result construction.'
Assert-True ($compositeResolverText -notmatch 'Trion = mainPrimary != null \? mainPrimary\.Trion') 'Combo result construction must not hard-code Trion from main/sub result.'
Assert-True ($comboFactoryText -notmatch 'sourceResult\.RuntimePayload') 'Combo factory must not read payload back from formal source results.'
Assert-True ($compositeResolverText -match 'ResolveSourceMaterialForSlot\(sourceMaterials,\s*slot\)') 'Composite resolver must resolve source material from the unfiltered source material set by exact slot.'
Assert-True ($compositeResolverText -match 'material\.Side\s*!=\s*slot\.Side' -and
    $compositeResolverText -match 'material\.SlotIndex\s*!=\s*slot\.Index') 'Composite resolver must keep side and slot identity when resolving source material.'
Assert-True ($comboFactoryText -match 'FirstSourceMaterial') 'Combo factory must read first source material for source-side context.'
Assert-True ($comboFactoryText -match 'SecondSourceMaterial') 'Combo factory must read second source material for source-side context.'

Write-Output 'ComboSourceResolutionBoundarySmokeTests PASS'
