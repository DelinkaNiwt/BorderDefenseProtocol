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
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboSourceBinding.cs'
$readResultPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboDefinitionReadResult.cs'
$indexPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'
$resolutionPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResolution.cs'
$factoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$collectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSourceCollector.cs'
$sourceResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboSourceFieldResolver.cs'

Assert-True (Test-Path -LiteralPath $bindingPath) 'Combo source binding model must exist.'
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8
$readResultText = Get-Content -LiteralPath $readResultPath -Raw -Encoding utf8
$indexText = Get-Content -LiteralPath $indexPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$resolutionText = Get-Content -LiteralPath $resolutionPath -Raw -Encoding utf8
$factoryText = Get-Content -LiteralPath $factoryPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8
$sourceResolverText = Get-Content -LiteralPath $sourceResolverPath -Raw -Encoding utf8

Assert-True ($bindingText -match 'FirstSourceChip') 'Binding must carry the actual ComboDef first source chip.'
Assert-True ($bindingText -match 'SecondSourceChip') 'Binding must carry the actual ComboDef second source chip.'
Assert-True ($readResultText -match 'SourceBinding') 'Read result must carry one runtime-only source binding.'
Assert-True ($indexText -match 'forwardAdmissionPassed[\s\S]*FirstSourceChip\s*=\s*forwardAdmissionPassed') 'Forward admission must preserve incoming first/second order.'
Assert-True ($indexText -match 'FirstSourceChip\s*=\s*forwardAdmissionPassed[\s\S]*secondSourceChip') 'Reverse admission must swap the concrete source order.'
Assert-True ($indexText -match 'FirstSourceChip[\s\S]*SecondSourceChip') 'A successful runtime match must return both concrete source identities.'

Assert-True ($resolverText -match 'normalMain') 'Resolver must separate ordinary Main candidates.'
Assert-True ($resolverText -match 'normalSub') 'Resolver must separate ordinary Sub candidates.'
Assert-True ($resolverText -match 'passiveMain') 'Resolver must enumerate passive Main candidates.'
Assert-True ($resolverText -match 'passiveSub') 'Resolver must enumerate passive Sub candidates.'
Assert-True ($resolverText -match 'normalMain\[i\][\s\S]*normalSub\[j\]') 'Resolver must allow ordinary Main + ordinary Sub.'
Assert-True ($resolverText -match 'normalMain\[i\][\s\S]*passiveMain\[j\]') 'Resolver must allow ordinary Main + same-side passive Main.'
Assert-True ($resolverText -match 'normalSub\[i\][\s\S]*passiveSub\[j\]') 'Resolver must allow ordinary Sub + same-side passive Sub.'
Assert-True ($resolverText -notmatch 'passiveMain\[i\][\s\S]*passiveSub\[j\]') 'Resolver must reject passive + passive.'
Assert-True ($resolverText -notmatch 'AppendComboResultsForPair\(pawn, triggerLoadoutReader, normalMain\[i\], passiveSub') 'Resolver must reject cross-side ordinary Main + passive Sub.'
Assert-True ($resolverText -notmatch 'AppendComboResultsForPair\(pawn, triggerLoadoutReader, normalSub\[i\], passiveMain') 'Resolver must reject cross-side ordinary Sub + passive Main.'
Assert-True (($resolverText -match 'TriggerSide\.Special') -or ($resolverText -match 'slot\.Side\s*!=\s*TriggerSide\.Main[\s\S]*slot\.Side\s*!=\s*TriggerSide\.Sub')) 'Resolver must exclude Special from candidate collection.'
Assert-True ($resolverText -match 'comboReadResult\.SourceBinding') 'Resolver must follow ComboDef first/second source binding.'
Assert-True ($resolverText -match 'BuildComboSourceIds\(firstReference, secondReference\)') 'Combo references must preserve first/second source result order.'

Assert-True ($resolutionText -match 'FirstSourceMaterial') 'Combo resolution must use first-source semantics.'
Assert-True ($resolutionText -match 'SecondSourceMaterial') 'Combo resolution must use second-source semantics.'
Assert-True ($resolutionText -match 'FirstSourceResult') 'Combo resolution must retain the first source result.'
Assert-True ($resolutionText -match 'SecondSourceResult') 'Combo resolution must retain the second source result.'
Assert-True ($factoryText -match 'BuildSourceIdentity') 'Combo result IDs must include concrete source identity.'
Assert-True ($factoryText -match 'ThingID') 'Combo result IDs must use stable ThingID source identity.'
Assert-True ($factoryText -match 'FirstSourceMaterial[\s\S]*SecondSourceMaterial') 'Combo factory must bind result fields to first/second materials.'
Assert-True ($factoryText -match 'FirstSourceResult[\s\S]*SecondSourceResult') 'Combo factory must bind fallback fields to first/second results.'
Assert-True ($sourceResolverText -match 'FollowFirstSource') 'FollowFirstSource must remain available.'
Assert-True ($sourceResolverText -match 'FollowSecondSource') 'FollowSecondSource must remain available.'
Assert-True ($sourceResolverText -match 'Average|Max|Min') 'Combo field math must remain available.'

# 独立表达仍由全部激活来源单独收集，不由组合解析器替换。
Assert-True ($collectorText -match 'GetActiveSlots\(\)') 'Independent expression collection must keep all active slots.'

Write-Output 'ComboSourceBindingSmokeTests PASS'
