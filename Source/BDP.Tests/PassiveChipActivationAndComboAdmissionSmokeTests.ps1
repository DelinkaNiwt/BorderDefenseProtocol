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

$classificationInterfacePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\External\ITriggerChipActivationControlProvider.cs'
$classificationRegistryPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\External\TriggerChipActivationControlRegistry.cs'
$contentProviderPath = Join-Path $repoRoot 'Source\BDP.Content\Trigger\Classification\AccessoryActivationControlProvider.cs'
$bootstrapPath = Join-Path $repoRoot 'Source\BDP.Content\ContentBootstrap.cs'
$chipFunctionPresetsPath = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$activationTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$interactionInterpreterPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Interaction\TriggerInteractionInterpreter.cs'
$loadoutPanelProviderPath = Join-Path $repoRoot 'Source\BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs'
$switchServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchService.cs'
$switchTransitionServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'
$disableSyncPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerDisableSync.cs'
$requirementMonitorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerActivationRequirementMonitor.cs'
$compositeResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'
$comboBindingPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboSourceBinding.cs'
$comboReadResultPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboDefinitionReadResult.cs'
$comboIndexPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs'
$comboResolutionPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResolution.cs'
$comboFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$primarySourceCollectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSourceCollector.cs'
$loadRejectionPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Building\Building_TriggerAssembler.cs'

Assert-True (Test-Path -LiteralPath $classificationInterfacePath) 'Core must expose a neutral chip classification extension point.'
Assert-True (Test-Path -LiteralPath $classificationRegistryPath) 'Core must expose a chip classification registry.'
Assert-True (Test-Path -LiteralPath $contentProviderPath) 'Content must provide the passive category classifier.'

$classificationRegistryText = Get-Content -LiteralPath $classificationRegistryPath -Raw -Encoding utf8
$contentProviderText = Get-Content -LiteralPath $contentProviderPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8
$chipFunctionPresetsText = Get-Content -LiteralPath $chipFunctionPresetsPath -Raw -Encoding utf8
$activationText = Get-Content -LiteralPath $activationTransactionPath -Raw -Encoding utf8
$interactionText = Get-Content -LiteralPath $interactionInterpreterPath -Raw -Encoding utf8
$loadoutPanelProviderText = Get-Content -LiteralPath $loadoutPanelProviderPath -Raw -Encoding utf8
$switchText = Get-Content -LiteralPath $switchServicePath -Raw -Encoding utf8
$switchTransitionText = Get-Content -LiteralPath $switchTransitionServicePath -Raw -Encoding utf8
$disableSyncText = Get-Content -LiteralPath $disableSyncPath -Raw -Encoding utf8
$requirementText = Get-Content -LiteralPath $requirementMonitorPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $compositeResolverPath -Raw -Encoding utf8
$bindingText = if (Test-Path -LiteralPath $comboBindingPath) { Get-Content -LiteralPath $comboBindingPath -Raw -Encoding utf8 } else { '' }
$readResultText = Get-Content -LiteralPath $comboReadResultPath -Raw -Encoding utf8
$indexText = Get-Content -LiteralPath $comboIndexPath -Raw -Encoding utf8
$resolutionText = Get-Content -LiteralPath $comboResolutionPath -Raw -Encoding utf8
$factoryText = Get-Content -LiteralPath $comboFactoryPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $primarySourceCollectorPath -Raw -Encoding utf8
$loadRejectionText = Get-Content -LiteralPath $loadRejectionPath -Raw -Encoding utf8

# Core 只依赖中性分类，不得认识 Content 的具体类别 DefName。
Assert-True ($classificationRegistryText -notmatch 'BDP_ChipCategory_Accessory') 'Core chip classification registry must not hard-code the passive category DefName.'
Assert-True ($contentProviderText -match 'ChipSnapshotAccess') 'Content passive policy must read the formal public chip surface.'
Assert-True ($contentProviderText -match 'Category') 'Content passive policy must classify by the chip profile Category.'
Assert-True ($contentProviderText -match 'BDP_ChipCategory_Accessory') 'Content passive policy must compare the existing passive category DefName.'
Assert-True ($bootstrapText -match 'TriggerChipActivationControlRegistry') 'Content bootstrap must register the chip classification provider.'

# 正式旋空模板的画像类别必须与其被动表达类型一致。
$senkuFunctionMatch = [regex]::Match(
    $chipFunctionPresetsText,
    '(?s)<BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>.*?<defName>BDP_ChipFunction_Senku</defName>(.*?)</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>')
Assert-True $senkuFunctionMatch.Success 'Formal Senku chip function must exist.'
Assert-True (
    ($senkuFunctionMatch.Groups[1].Value -match '<Category>BDP_ChipCategory_Accessory</Category>') -and
    ($senkuFunctionMatch.Groups[1].Value -match '<SlotRegion>MainSub</SlotRegion>') -and
    ($senkuFunctionMatch.Groups[1].Value -match '<EntryType>Passive</EntryType>') -and
    ($senkuFunctionMatch.Groups[1].Value -match '<PassiveIdentityKey>bdp\.senku\.passive</PassiveIdentityKey>')
) 'Formal Senku must use the passive category while retaining its passive expression and Main/Sub loadout.'

# 被动类别只用于分类，不得在战斗体激活时自动打开。
Assert-True ($activationText -match 'TryAutoActivatePrimarySlots') 'Combat-body activation must retain the existing ordinary Main/Sub activation path.'
Assert-True ($activationText -notmatch 'TryAutoActivatePassiveSlots') 'Combat-body activation must not auto-activate passive Main/Sub chips.'
Assert-True ($activationText -notmatch 'RequestAutomaticActivate') 'Passive chips must not have a dedicated automatic activation request.'

# 被动槽必须走普通玩家 Activate/Deactivate。
Assert-True ($interactionText -notmatch 'isAutomaticallyActivated') 'Interaction interpretation must not disable player operations for passive chips.'
Assert-True ($interactionText -match 'TriggerInteractionOperationKind\.Activate') 'Interaction interpretation must retain normal Activate operation handling.'
Assert-True ($interactionText -match 'TriggerInteractionOperationKind\.Deactivate') 'Interaction interpretation must retain normal Deactivate operation handling.'
Assert-True ($loadoutPanelProviderText -match 'OperationKind == TriggerInteractionOperationKind\.Deactivate[\s\S]*RequestDeactivate\(interaction\.ControlSide,\s*interaction\.ControlSlotIndex\)') 'Slot deactivation must submit the exact control side and slot index.'
Assert-True ($switchText -match 'HasIndependentActivationControl\(nextSlot\.LoadedChip\)[\s\S]*RequestSlotActivate') 'Passive manual activation must use the formal slot transition path.'
Assert-True ($switchText -match 'HasIndependentActivationControl\(targetSlot\.LoadedChip\)[\s\S]*RequestSlotDeactivate') 'Passive manual deactivation must use the formal exact-slot transition path.'
Assert-True ($switchText -match 'blocker => RequestDeactivate\(context, blocker\.Side, blocker\.Index\)') 'Activation must close the exact blocker slot, not the side-level representative slot.'
Assert-True ($switchTransitionText -match 'requestBlockerDeactivation') 'Transition settlement must preserve exact blocker deactivation routing.'

# 同侧策略：普通仍互斥，被动只跳过默认同侧范围，显式互斥组仍继续检查。
Assert-True ($switchText -match 'HasIndependentActivationControl') 'Switching must consult the passive chip classifier.'
Assert-True ($switchText -match 'ActivationExclusionGroups|explicitExclusion|ExplicitExclusion') 'Explicit activation exclusion groups must remain enforced.'
Assert-True ($disableSyncText -notmatch 'getAutomaticSwitchContext|setAutomaticSwitchContext|AutomaticSwitchContext') 'Disable synchronization must not carry passive automatic-transition state.'
Assert-True ($requirementText -match 'slot|GetActiveSlots') 'Activation requirement monitoring must process the exact active slot.'

# 组合只枚举合法候选对，并使用每次匹配的第一/第二来源绑定。
Assert-True ($resolverText -match 'GetActiveSlots\(\)') 'Combo resolution must enumerate all active slots.'
Assert-True ($resolverText -match 'Passive|IsPassive|ActivationPolicy') 'Combo resolution must distinguish ordinary and passive activation.'
Assert-True ($resolverText -match 'Main.*Sub|Sub.*Main') 'Combo resolution must retain the ordinary Main/Sub candidate.'
Assert-True ($resolverText -match 'same-side|SameSide|sameSide|TriggerSide') 'Combo resolution must enforce same-side passive pairing.'
Assert-True ($resolverText -match 'Special') 'Combo resolution must explicitly exclude the Special side.'
Assert-True (Test-Path -LiteralPath $comboBindingPath) 'Runtime combo matching must expose a concrete source binding.'
Assert-True ($bindingText -match 'FirstSourceChip') 'Combo source binding must expose the ComboDef first source chip.'
Assert-True ($bindingText -match 'SecondSourceChip') 'Combo source binding must expose the ComboDef second source chip.'
Assert-True ($readResultText -match 'SourceBinding|Binding') 'Combo read result must retain a runtime source binding.'
Assert-True ($indexText -match 'SourceBinding|Binding') 'Combo runtime index must return the concrete source binding.'
Assert-True ($indexText -match 'forward|Forward') 'Combo runtime index must preserve forward source order.'
Assert-True ($indexText -match 'reverse|Reverse|swap|Swap') 'Combo runtime index must preserve reverse source order.'
Assert-True ($resolutionText -match 'FirstSourceMaterial') 'Combo resolution must bind first source material precisely.'
Assert-True ($resolutionText -match 'SecondSourceMaterial') 'Combo resolution must bind second source material precisely.'
Assert-True ($factoryText -match 'FirstSourceResult|SecondSourceResult') 'Combo result construction must bind first/second source results precisely.'
Assert-True ($factoryText -match 'ThingID|SourceResultIds') 'Combo result identity must distinguish concrete source pairs.'

# 独立表达与 Active 装卸边界保持现况。
Assert-True ($collectorText -match 'GetActiveSlots\(\)') 'Independent expression collection must keep all active source slots.'
Assert-True ($loadRejectionText -match 'Active|CombatBodyPhase') 'The normal Active-phase load rejection entry must remain unchanged.'

Write-Output 'PassiveChipActivationAndComboAdmissionSmokeTests PASS'
