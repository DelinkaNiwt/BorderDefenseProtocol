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

$snapshotBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs'
$compositeResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'
$comboFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$comboResolutionPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResolution.cs'
$primarySelectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultPrimaryExpressionSelector.cs'
$attackExecutionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'
$formalExpressionResultPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\FormalExpressionResult.cs'
$compositeSetPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\CompositeExpressionSet.cs'
$compositeReferencePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\CompositeExpressionReference.cs'

$comboDefPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Defs\ComboDef.cs'
$comboConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboDefinitionConfig.cs'
$comboExpressionConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboExpressionConfig.cs'
$comboExpressionEntryConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs'
$comboResolveModePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboValueResolveMode.cs'
$comboContractPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboDefinitionContract.cs'
$comboExpressionHandlePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboExpressionContractHandle.cs'
$comboFieldValuePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboResolvedFieldValue.cs'
$comboResolvedVerbPropsPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboResolvedVerbProps.cs'
$comboResolvedExecutionPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboResolvedExecution.cs'
$comboSourceResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboSourceFieldResolver.cs'
$comboResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Contract\ComboDefinitionContractResolver.cs'
$comboReaderInterfacePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\IComboDefinitionReader.cs'
$comboReadResultPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboDefinitionReadResult.cs'
$comboReaderSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboDefinitionReaderSurface.cs'
$comboSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboSurfaceAccess.cs'
$comboValidatorPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Validation\ComboDefinitionValidator.cs'
$comboValidationResultPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Validation\ComboDefinitionValidationResult.cs'
$comboValidationMessagePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Validation\ComboDefinitionValidationMessage.cs'
$devHarnessComboDefsPath = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Combos\Test\ComboDefs_TestCombos.xml'

$snapshotBuilderText = Get-Content -LiteralPath $snapshotBuilderPath -Raw -Encoding utf8
$compositeResolverText = Get-Content -LiteralPath $compositeResolverPath -Raw -Encoding utf8
$comboFactoryText = if (Test-Path -LiteralPath $comboFactoryPath) { Get-Content -LiteralPath $comboFactoryPath -Raw -Encoding utf8 } else { '' }
$comboResolutionText = if (Test-Path -LiteralPath $comboResolutionPath) { Get-Content -LiteralPath $comboResolutionPath -Raw -Encoding utf8 } else { '' }
$primarySelectorText = Get-Content -LiteralPath $primarySelectorPath -Raw -Encoding utf8
$attackExecutionText = Get-Content -LiteralPath $attackExecutionPath -Raw -Encoding utf8
$formalExpressionResultText = Get-Content -LiteralPath $formalExpressionResultPath -Raw -Encoding utf8
$compositeSetText = Get-Content -LiteralPath $compositeSetPath -Raw -Encoding utf8
$compositeReferenceText = Get-Content -LiteralPath $compositeReferencePath -Raw -Encoding utf8

$comboDefExists = Test-Path -LiteralPath $comboDefPath
$comboConfigExists = Test-Path -LiteralPath $comboConfigPath
$comboExpressionConfigExists = Test-Path -LiteralPath $comboExpressionConfigPath
$comboExpressionEntryConfigExists = Test-Path -LiteralPath $comboExpressionEntryConfigPath
$comboResolveModeExists = Test-Path -LiteralPath $comboResolveModePath
$comboContractExists = Test-Path -LiteralPath $comboContractPath
$comboExpressionHandleExists = Test-Path -LiteralPath $comboExpressionHandlePath
$comboFieldValueExists = Test-Path -LiteralPath $comboFieldValuePath
$comboResolvedVerbPropsExists = Test-Path -LiteralPath $comboResolvedVerbPropsPath
$comboResolvedExecutionExists = Test-Path -LiteralPath $comboResolvedExecutionPath
$comboSourceResolverExists = Test-Path -LiteralPath $comboSourceResolverPath
$comboResolverExists = Test-Path -LiteralPath $comboResolverPath
$comboReaderInterfaceExists = Test-Path -LiteralPath $comboReaderInterfacePath
$comboReadResultExists = Test-Path -LiteralPath $comboReadResultPath
$comboReaderSurfaceExists = Test-Path -LiteralPath $comboReaderSurfacePath
$comboSurfaceExists = Test-Path -LiteralPath $comboSurfacePath
$comboValidatorExists = Test-Path -LiteralPath $comboValidatorPath
$comboValidationResultExists = Test-Path -LiteralPath $comboValidationResultPath
$comboValidationMessageExists = Test-Path -LiteralPath $comboValidationMessagePath

$comboDefText = if ($comboDefExists) { Get-Content -LiteralPath $comboDefPath -Raw -Encoding utf8 } else { '' }
$comboConfigText = if ($comboConfigExists) { Get-Content -LiteralPath $comboConfigPath -Raw -Encoding utf8 } else { '' }
$comboExpressionConfigText = if ($comboExpressionConfigExists) { Get-Content -LiteralPath $comboExpressionConfigPath -Raw -Encoding utf8 } else { '' }
$comboExpressionEntryConfigText = if ($comboExpressionEntryConfigExists) { Get-Content -LiteralPath $comboExpressionEntryConfigPath -Raw -Encoding utf8 } else { '' }
$comboResolveModeText = if ($comboResolveModeExists) { Get-Content -LiteralPath $comboResolveModePath -Raw -Encoding utf8 } else { '' }
$comboContractText = if ($comboContractExists) { Get-Content -LiteralPath $comboContractPath -Raw -Encoding utf8 } else { '' }
$comboExpressionHandleText = if ($comboExpressionHandleExists) { Get-Content -LiteralPath $comboExpressionHandlePath -Raw -Encoding utf8 } else { '' }
$comboFieldValueText = if ($comboFieldValueExists) { Get-Content -LiteralPath $comboFieldValuePath -Raw -Encoding utf8 } else { '' }
$comboResolvedVerbPropsText = if ($comboResolvedVerbPropsExists) { Get-Content -LiteralPath $comboResolvedVerbPropsPath -Raw -Encoding utf8 } else { '' }
$comboResolvedExecutionText = if ($comboResolvedExecutionExists) { Get-Content -LiteralPath $comboResolvedExecutionPath -Raw -Encoding utf8 } else { '' }
$comboSourceResolverText = if ($comboSourceResolverExists) { Get-Content -LiteralPath $comboSourceResolverPath -Raw -Encoding utf8 } else { '' }
$comboResolverText = if ($comboResolverExists) { Get-Content -LiteralPath $comboResolverPath -Raw -Encoding utf8 } else { '' }
$comboReaderInterfaceText = if ($comboReaderInterfaceExists) { Get-Content -LiteralPath $comboReaderInterfacePath -Raw -Encoding utf8 } else { '' }
$comboReadResultText = if ($comboReadResultExists) { Get-Content -LiteralPath $comboReadResultPath -Raw -Encoding utf8 } else { '' }
$comboReaderSurfaceText = if ($comboReaderSurfaceExists) { Get-Content -LiteralPath $comboReaderSurfacePath -Raw -Encoding utf8 } else { '' }
$comboSurfaceText = if ($comboSurfaceExists) { Get-Content -LiteralPath $comboSurfacePath -Raw -Encoding utf8 } else { '' }
$comboValidatorText = if ($comboValidatorExists) { Get-Content -LiteralPath $comboValidatorPath -Raw -Encoding utf8 } else { '' }
$comboValidationResultText = if ($comboValidationResultExists) { Get-Content -LiteralPath $comboValidationResultPath -Raw -Encoding utf8 } else { '' }
$comboValidationMessageText = if ($comboValidationMessageExists) { Get-Content -LiteralPath $comboValidationMessagePath -Raw -Encoding utf8 } else { '' }
$devHarnessComboDefsText = if (Test-Path -LiteralPath $devHarnessComboDefsPath) { Get-Content -LiteralPath $devHarnessComboDefsPath -Raw -Encoding utf8 } else { '' }

# Combo integration rules:
# 1. Combo is matched in expression composite stage, not manually injected by callers
# 2. ComboResult must be consumed by the normal single-result path
# 3. Combo must not become default primary attack
# 4. Combo-specific business names must never appear in architecture code

Assert-True ($snapshotBuilderText -match 'ComboResults') 'ExpressionSnapshotBuilder must own combo result assembly.'
Assert-True ($primarySelectorText -notmatch 'CompositeKind\s*==\s*CompositeExpressionKind\.Combo') 'Combo must not become default primary selection.'
Assert-True ($attackExecutionText -notmatch 'BuildComboCasts') 'Attack execution must not grow a combo-only cast path.'
Assert-True ($attackExecutionText -notmatch 'ComboAttackExecutor') 'Attack execution must not grow a combo-only executor.'

Assert-True $comboDefExists 'ComboDef must exist.'
Assert-True ($comboDefText -match 'class\s+ComboDef\s*:\s*Def') 'ComboDef must be a formal Def type.'

Assert-True $comboConfigExists 'Combo definition config must exist.'
Assert-True $comboExpressionConfigExists 'Combo expression config must exist.'
Assert-True $comboExpressionEntryConfigExists 'Combo expression entry config must exist.'
Assert-True $comboResolveModeExists 'Combo resolve mode must exist.'
Assert-True $comboContractExists 'Combo definition contract must exist.'
Assert-True $comboExpressionHandleExists 'Combo expression contract handle must exist.'

Assert-True ($comboConfigText -match 'chipA') 'Combo definition config must declare chipA.'
Assert-True ($comboConfigText -match 'chipB') 'Combo definition config must declare chipB.'
Assert-True ($comboConfigText -match 'Expression') 'Combo definition config must declare Expression block.'
Assert-True ($comboExpressionConfigText -match 'Entries') 'Combo expression config must declare Entries.'
Assert-True ($comboExpressionConfigText -match 'List<ComboExpressionEntryConfig>\s+Entries') 'Combo expression config entries must use combo-specific entry config.'
Assert-True ($comboExpressionConfigText -notmatch 'ComboVerbPropsResolutionConfig\s+VerbProps') 'Combo expression config must not keep top-level verb-props resolve block.'
Assert-True ($comboExpressionConfigText -notmatch 'ComboExecutionResolutionConfig\s+Execution') 'Combo expression config must not keep top-level execution resolve block.'
Assert-True ($comboExpressionEntryConfigText -match 'class\s+ComboExpressionEntryConfig') 'Combo expression entry config must define a dedicated entry model.'
Assert-True ($comboExpressionEntryConfigText -match 'ComboVerbPropsResolutionConfig\s+VerbPropsResolve') 'Combo expression entry config must hold entry-level verb-props resolve rules.'
Assert-True ($comboExpressionEntryConfigText -match 'ComboExecutionResolutionConfig\s+ExecutionResolve') 'Combo expression entry config must hold entry-level execution resolve rules.'
Assert-True ($comboResolveModeText -match 'FollowChipMain') 'Combo resolve mode must support FollowChipMain.'
Assert-True ($comboResolveModeText -match 'FollowChipSub') 'Combo resolve mode must support FollowChipSub.'
Assert-True ($comboResolveModeText -match 'Average') 'Combo resolve mode must support Average.'
Assert-True ($comboResolveModeText -match 'Max') 'Combo resolve mode must support Max.'
Assert-True ($comboResolveModeText -match 'Min') 'Combo resolve mode must support Min.'

Assert-True $comboReaderInterfaceExists 'Combo definition reader interface must exist.'
Assert-True $comboReadResultExists 'Combo definition read result must exist.'
Assert-True $comboReaderSurfaceExists 'Combo definition reader surface must exist.'
Assert-True $comboSurfaceExists 'Combo surface access must exist.'
Assert-True $comboResolverExists 'Combo definition contract resolver must exist.'
Assert-True $comboValidatorExists 'Combo definition validator must exist.'
Assert-True $comboValidationResultExists 'Combo definition validation result must exist.'
Assert-True $comboValidationMessageExists 'Combo definition validation message must exist.'

Assert-True ($comboReaderInterfaceText -match 'Read\s*\(') 'Combo definition reader must expose Read entry.'
Assert-True ($comboReaderSurfaceText -match 'Read\s*\(') 'Combo definition reader surface must expose Read entry.'
Assert-True ($comboResolverText -match 'Resolve\s*\(') 'Combo definition resolver must exist.'
Assert-True ($comboValidatorText -match 'Validate\s*\(') 'Combo definition validator must exist.'
Assert-True ($comboSurfaceText -match 'ResolveDefinitionReader') 'Combo surface access must expose definition reader.'
Assert-True ($comboValidatorText -match 'chipA') 'Combo validator must validate chipA.'
Assert-True ($comboValidatorText -match 'chipB') 'Combo validator must validate chipB.'
Assert-True ($comboValidatorText -match 'Expression') 'Combo validator must validate Expression.'

Assert-True ($formalExpressionResultText -match 'ComboDefName') 'FormalExpressionResult must expose ComboDefName.'
Assert-True (Test-Path -LiteralPath $comboFactoryPath) 'Combo formal result factory must exist.'
Assert-True (Test-Path -LiteralPath $comboResolutionPath) 'Combo formal result resolution model must exist.'
Assert-True ($snapshotBuilderText -notmatch 'ComboResults\s*=\s*new List<FormalExpressionResult>\s*\(\s*\)') 'ComboResults must not remain a hard-coded empty list.'
Assert-True ($compositeResolverText -match 'CompositeKind\s*=\s*CompositeExpressionKind\.Combo') 'Composite resolver must create combo composite results.'
Assert-True ($compositeResolverText -match 'ComboDefName') 'Composite resolver must set ComboDefName on combo results.'
Assert-True ($compositeResolverText -match 'ComboFormalExpressionResultFactory') 'Composite resolver must delegate combo result construction.'
Assert-True ($compositeSetText -match 'ComboResults') 'CompositeExpressionSet must expose ComboResults.'
Assert-True ($compositeReferenceText -match 'SourceResultIds') 'CompositeExpressionReference must keep source mapping.'

Assert-True ($attackExecutionText -match 'BuildSingleResultCasts') 'Attack execution must keep consuming normal single results.'
Assert-True ($attackExecutionText -match 'CompositeExpressionKind\.DualWeapon') 'DualWeapon special handling must remain explicit.'

Assert-True $comboFieldValueExists 'Combo resolved field value contract must exist.'
Assert-True $comboResolvedVerbPropsExists 'Combo resolved verb props contract must exist.'
Assert-True $comboResolvedExecutionExists 'Combo resolved execution contract must exist.'
Assert-True $comboSourceResolverExists 'Combo source field resolver must exist.'
Assert-True ($comboResolutionText -match 'ComboDefinitionReadResult') 'Combo formal result resolution must retain combo read result.'
Assert-True ($comboResolutionText -match 'ComboExpressionEntryConfig') 'Combo formal result resolution must retain combo entry config.'
Assert-True ($comboResolutionText -match 'ChipExpressionEntryContract') 'Combo formal result resolution must retain interpreted entry contract.'
Assert-True ($comboResolutionText -match 'MainSourceMaterial') 'Combo formal result resolution must retain main source material.'
Assert-True ($comboResolutionText -match 'SubSourceMaterial') 'Combo formal result resolution must retain sub source material.'
Assert-True ($comboSourceResolverText -match 'FollowChipMain') 'Combo source resolver must support FollowChipMain.'
Assert-True ($comboSourceResolverText -match 'Average') 'Combo source resolver must support Average.'
Assert-True ($comboResolverText -match 'ResolveVerbProps') 'Combo resolver must own verb prop resolution.'
Assert-True ($comboResolverText -match 'ComboSourceFieldResolver') 'Combo resolver must delegate shared field math.'
Assert-True ($comboFieldValueText -match 'ResolveMode') 'Combo resolved field value must retain resolve mode.'
Assert-True ($comboResolvedVerbPropsText -match 'Range') 'Combo resolved verb props must expose Range.'
Assert-True ($comboResolvedExecutionText -match 'HitCount') 'Combo resolved execution must expose HitCount.'
Assert-True ($comboResolverText -match 'ResolveVerbProps\s*\(\s*ComboExpressionEntryConfig') 'Combo resolver must resolve verb props at entry level.'
Assert-True ($comboResolverText -match 'ResolveExecution\s*\(\s*ComboExpressionEntryConfig') 'Combo resolver must resolve execution at entry level.'
Assert-True ($comboValidatorText -match 'entry\.VerbPropsResolve') 'Combo validator must inspect entry-level verb-props resolve rules.'
Assert-True ($comboValidatorText -match 'entry\.ExecutionResolve') 'Combo validator must inspect entry-level execution resolve rules.'
Assert-True ($compositeResolverText -match 'BuildComboInterpreterEntries') 'Composite resolver must map combo entry configs into normal interpreter input.'
Assert-True ($comboSourceResolverText -match 'if\s*\(!mode\.HasValue\)') 'Combo source resolver must keep fields untouched when no resolve mode is explicitly declared.'
Assert-True ($comboResolverText -notmatch 'declaredMode\s*\?\?\s*defaultMode') 'Combo resolver must not apply implicit default resolve modes.'
Assert-True ($comboResolverText -match 'DefaultProjectile = null') 'Combo resolver must not implicitly inherit default projectile from source chips.'
Assert-True ($comboFactoryText -match 'ExpressionResultKind\.Verb') 'Combo factory must support Verb.'
Assert-True ($comboFactoryText -match 'ExpressionResultKind\.Ability') 'Combo factory must support Ability.'
Assert-True ($comboFactoryText -match 'ExpressionResultKind\.Hediff') 'Combo factory must support Hediff.'
Assert-True ($comboFactoryText -match 'ExpressionResultKind\.Passive') 'Combo factory must support Passive.'
Assert-True ($comboFactoryText -match 'AbilityDefName') 'Combo factory must carry AbilityDefName.'
Assert-True ($comboFactoryText -match 'HediffDefName') 'Combo factory must carry HediffDefName.'
Assert-True ($comboFactoryText -match 'PassiveKey') 'Combo factory must carry PassiveKey.'
Assert-True ($devHarnessComboDefsText -match '<Kind>Ability</Kind>') 'DevHarness must include combo ability sample.'
Assert-True ($devHarnessComboDefsText -match '<Kind>Hediff</Kind>') 'DevHarness must include combo hediff sample.'
Assert-True ($devHarnessComboDefsText -match '<Kind>Passive</Kind>') 'DevHarness must include combo passive sample.'

Write-Output 'ComboDefinitionBoundary PASS'
