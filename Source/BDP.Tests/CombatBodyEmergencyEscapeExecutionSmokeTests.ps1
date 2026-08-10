$ErrorActionPreference = "Stop"

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

function Normalize-Text {
    param([string]$Text)
    if ($null -eq $Text) {
        return ""
    }

    return [regex]::Replace($Text, "\s+", "")
}

$exitTransactionPath = Join-Path $repoRoot "Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs"
$hostStatePath = Join-Path $repoRoot "Source\BDP\Core\CombatBody\Bridge\HostState.cs"
$escapeServicePath = Join-Path $repoRoot "Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeService.cs"
$escapeEffectsPath = Join-Path $repoRoot "Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeEffects.cs"
$resolutionPath = Join-Path $repoRoot "Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeResolution.cs"
$resolverPath = Join-Path $repoRoot "Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeResolver.cs"
$sourceReferencePath = Join-Path $repoRoot "Source\BDP\Core\Expressions\Model\ExpressionPublishedSourceReference.cs"
$triggerCommandsPath = Join-Path $repoRoot "Source\BDP\Core\Trigger\Access\Contracts\ITriggerLoadoutCommands.cs"
$formalProjectionPath = Join-Path $repoRoot "Source\BDP\Core\Expressions\Model\ExpressionPublishedProjectionSnapshot.cs"
$formalResultPath = Join-Path $repoRoot "Source\BDP\Core\Expressions\Model\ExpressionPublishedResultSnapshot.cs"
$singleSideBuilderPath = Join-Path $repoRoot "Source\BDP\Core\Expressions\Pipeline\SingleSideExpressionBuilder.cs"
$loadoutSurfacePath = Join-Path $repoRoot "Source\BDP\Core\Trigger\Access\Surfaces\TriggerFormalSurfaces.cs"

foreach ($path in @($escapeServicePath, $escapeEffectsPath, $sourceReferencePath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing required Content/Core source: $path"
    }
}

$exitNormalized = Normalize-Text (Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8)
$hostStateNormalized = Normalize-Text (Get-Content -LiteralPath $hostStatePath -Raw -Encoding utf8)
$escapeNormalized = Normalize-Text (Get-Content -LiteralPath $escapeServicePath -Raw -Encoding utf8)
$escapeEffectsNormalized = Normalize-Text (Get-Content -LiteralPath $escapeEffectsPath -Raw -Encoding utf8)
$resolutionNormalized = Normalize-Text (Get-Content -LiteralPath $resolutionPath -Raw -Encoding utf8)
$resolverNormalized = Normalize-Text (Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8)
$sourceReferenceNormalized = Normalize-Text (Get-Content -LiteralPath $sourceReferencePath -Raw -Encoding utf8)
$triggerCommandsNormalized = Normalize-Text (Get-Content -LiteralPath $triggerCommandsPath -Raw -Encoding utf8)
$projectionNormalized = Normalize-Text (Get-Content -LiteralPath $formalProjectionPath -Raw -Encoding utf8)
$formalResultNormalized = Normalize-Text (Get-Content -LiteralPath $formalResultPath -Raw -Encoding utf8)
$singleSideBuilderNormalized = Normalize-Text (Get-Content -LiteralPath $singleSideBuilderPath -Raw -Encoding utf8)
$loadoutSurfaceNormalized = Normalize-Text (Get-Content -LiteralPath $loadoutSurfacePath -Raw -Encoding utf8)

if ($exitNormalized.IndexOf("CombatBodyCollapseExtensionRegistry.Execute(ownerPawn)") -lt 0) { throw "Collapse branch must invoke the neutral collapse extension registry." }
if ($exitNormalized.IndexOf("CombatBodyCollapseExtensionRegistry.Clear(ownerPawn)") -lt 0) { throw "Exit transaction must clear collapse extension state." }
if ($exitNormalized.IndexOf("CombatBodyEmergencyEscape") -ge 0 -or $exitNormalized.IndexOf("emergencyEscape") -ge 0) { throw "Core exit transaction must not directly own emergency escape business." }
if ($hostStateNormalized.IndexOf("CachedCollapseEmergencyEscape") -ge 0) { throw "HostState must not persist a business-specific emergency escape cache." }

$executeIndex = $exitNormalized.IndexOf("CombatBodyCollapseExtensionRegistry.Execute(ownerPawn)")
$cooldownIndex = $exitNormalized.IndexOf("EnterCooldown(ResolveCooldownTicks(exitMode),ResolveExitReason(exitMode));")
$clearCurIndex = $exitNormalized.IndexOf("trionCommands?.TrySetCurrent(0f);")
$aftereffectIndex = $exitNormalized.IndexOf("ApplyCollapseAftereffect(ownerPawn);")
if (-not ($executeIndex -lt $cooldownIndex -and $cooldownIndex -lt $clearCurIndex -and $clearCurIndex -lt $aftereffectIndex)) { throw "Collapse extension must execute before cooldown and later collapse cleanup." }

if ($resolutionNormalized.IndexOf("publicList<ExpressionPublishedSourceReference>SourceReferences;") -lt 0) { throw "Resolution must retain all public source references." }
if ($resolverNormalized.IndexOf("ExpressionSurfaceAccess.ResolvePublishedProjection(pawn)") -lt 0) { throw "Resolver must consume the public published projection." }
if ($resolverNormalized.IndexOf("TryGetCompositeReference") -lt 0) { throw "Resolver must retain composite source reconstruction." }

if ($sourceReferenceNormalized.IndexOf("publicstringChipThingId{get;internalset;}") -lt 0) { throw "Published source reference must expose chip thing id." }
if ($sourceReferenceNormalized.IndexOf("publicTriggerSideSide{get;internalset;}") -lt 0) { throw "Published source reference must expose source side." }
if ($sourceReferenceNormalized.IndexOf("publicintSlotIndex{get;internalset;}") -lt 0) { throw "Published source reference must expose slot index." }

if ($projectionNormalized.IndexOf("CompositeReferenceIndex") -lt 0) { throw "Public projection must expose composite source references." }
if ($formalResultNormalized.IndexOf("SourceReference") -lt 0) { throw "Public result snapshot must expose its source reference." }
if ($singleSideBuilderNormalized.IndexOf("SourceReference=material.SourceReference") -lt 0) { throw "SingleSideExpressionBuilder must still carry internal source references into results." }

if ($triggerCommandsNormalized.IndexOf("boolTryDestroyLoadedChip(TriggerSideside,intslotIndex,stringexpectedThingId);") -lt 0) { throw "Trigger formal commands must expose TryDestroyLoadedChip." }
if ($loadoutSurfaceNormalized.IndexOf("publicboolTryDestroyLoadedChip(TriggerSideside,intslotIndex,stringexpectedThingId)") -lt 0) { throw "Trigger formal command surface must forward TryDestroyLoadedChip." }

if ($escapeNormalized.IndexOf("TryConsumeSourceChips(pawn,resolution.SourceReferences);") -lt 0) { throw "Emergency escape execution must consume all source chips through the cached source list." }
if ($escapeNormalized.IndexOf("TryConsumeSourceChip(pawn,sourceReferences[i]);") -lt 0) { throw "Emergency escape execution must iterate source chips one by one." }
if ($escapeNormalized.IndexOf("CombatBodyEmergencyEscapeRouter.FindEscapeDestination(") -lt 0) { throw "Emergency escape execution must go through the Content router." }
if ($escapeNormalized.IndexOf("CombatBodyEmergencyEscapeEffects.PlayEntryEffects(") -lt 0) { throw "Emergency escape execution must play entry effects." }
if ($escapeNormalized.IndexOf("CombatBodyEmergencyEscapeEffects.PlayExitEffects(") -lt 0) { throw "Emergency escape execution must play exit effects." }

if ($escapeEffectsNormalized.IndexOf("FleckDefOf.PsycastSkipFlashEntry") -lt 0) { throw "Emergency escape entry effects must preserve the existing flash." }
if ($escapeEffectsNormalized.IndexOf("FleckDefOf.PsycastSkipInnerExit") -lt 0) { throw "Emergency escape exit effects must preserve the existing inner fleck." }
if ($escapeEffectsNormalized.IndexOf("FleckDefOf.PsycastSkipOuterRingExit") -lt 0) { throw "Emergency escape exit effects must preserve the existing outer fleck." }

Write-Output "CombatBodyEmergencyEscapeExecution PASS"
