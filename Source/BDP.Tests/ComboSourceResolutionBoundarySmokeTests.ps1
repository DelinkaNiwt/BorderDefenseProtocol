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
Assert-True ($comboSourceResolverText -match 'FollowChipMain') 'Resolver must support FollowChipMain.'
Assert-True ($comboSourceResolverText -match 'FollowChipSub') 'Resolver must support FollowChipSub.'
Assert-True ($comboSourceResolverText -match 'Average') 'Resolver must support Average.'
Assert-True ($comboSourceResolverText -match 'Max') 'Resolver must support Max.'
Assert-True ($comboSourceResolverText -match 'Min') 'Resolver must support Min.'
Assert-True ($comboResolverText -match 'ComboSourceFieldResolver') 'ComboDefinitionContractResolver must delegate shared field math.'
Assert-True ($compositeResolverText -match 'ComboFormalExpressionResultFactory') 'CompositeExpressionResolver must delegate combo result construction.'
Assert-True ($compositeResolverText -notmatch 'Trion = mainPrimary != null \? mainPrimary\.Trion') 'Combo result construction must not hard-code Trion from main/sub result.'
Assert-True ($comboFactoryText -notmatch 'sourceResult\.RuntimePayload') 'Combo factory must not read payload back from formal source results.'
Assert-True ($compositeResolverText -match 'ResolveSourceMaterial\(materialIndex,\s*mainSet\)') 'Composite resolver must resolve main source material from the side result set.'
Assert-True ($compositeResolverText -match 'ResolveSourceMaterial\(materialIndex,\s*subSet\)') 'Composite resolver must resolve sub source material from the side result set.'
Assert-True ($comboFactoryText -match 'MainSourceMaterial') 'Combo factory must read main source material for source-side context.'
Assert-True ($comboFactoryText -match 'SubSourceMaterial') 'Combo factory must read sub source material for source-side context.'

Write-Output 'ComboSourceResolutionBoundarySmokeTests PASS'
