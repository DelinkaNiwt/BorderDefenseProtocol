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

$genePath = Join-Path $repoRoot 'Source\BDP\Core\Genes\Gene_TrionGland.cs'
$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\Genes\TrionGeneGizmoBridge.cs'
$gizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'
$extensionProviderPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\External\ITrionGizmoExtensionProvider.cs'
$extensionContextPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\External\TrionGizmoExtensionContext.cs'
$extensionBadgePath = Join-Path $repoRoot 'Source\BDP\Core\Trion\External\TrionGizmoExtensionBadge.cs'
$extensionRegistryPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\External\TrionGizmoExtensionRegistry.cs'
$compTrionPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\CompTrion.cs'
$commandsPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\ITrionCommands.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\Trion\TrionService.cs'
$writeResultPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\TrionCurrentWriteResult.cs'
$combatBodyProviderPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\External\CombatBodyTrionGizmoExtensionProvider.cs'
$trionBootstrapPath = Join-Path $repoRoot 'Source\BDP\Core\Bootstrap\TrionGizmoBootstrap.cs'

Assert-True -Condition (Test-Path -LiteralPath $bridgePath) -Message 'Trion gene-carried GUI requires TrionGeneGizmoBridge.cs.'
Assert-True -Condition (Test-Path -LiteralPath $gizmoPath) -Message 'Trion gene-carried GUI requires Gizmo_TrionStatus.cs.'
Assert-True -Condition (Test-Path -LiteralPath $extensionProviderPath) -Message 'Trion gizmo extension zone requires ITrionGizmoExtensionProvider.cs.'
Assert-True -Condition (Test-Path -LiteralPath $extensionContextPath) -Message 'Trion gizmo extension zone requires TrionGizmoExtensionContext.cs.'
Assert-True -Condition (Test-Path -LiteralPath $extensionBadgePath) -Message 'Trion gizmo extension zone requires TrionGizmoExtensionBadge.cs.'
Assert-True -Condition (Test-Path -LiteralPath $extensionRegistryPath) -Message 'Trion gizmo extension zone requires TrionGizmoExtensionRegistry.cs.'
Assert-True -Condition (Test-Path -LiteralPath $combatBodyProviderPath) -Message 'Main mod must provide a formal CombatBody -> Trion gizmo extension provider.'
Assert-True -Condition (Test-Path -LiteralPath $trionBootstrapPath) -Message 'Main mod must provide a bootstrap entry that registers formal Trion gizmo extension providers.'
Assert-True -Condition (Test-Path -LiteralPath $commandsPath) -Message 'Trion debug writes require ITrionCommands.cs.'
Assert-True -Condition (Test-Path -LiteralPath $servicePath) -Message 'Trion debug writes require TrionService.cs.'
Assert-True -Condition (Test-Path -LiteralPath $writeResultPath) -Message 'Trion debug writes require TrionCurrentWriteResult.cs.'

$geneText = Get-Content -LiteralPath $genePath -Raw -Encoding utf8
$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$gizmoText = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8
$compTrionText = Get-Content -LiteralPath $compTrionPath -Raw -Encoding utf8
$commandsText = Get-Content -LiteralPath $commandsPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$combatBodyProviderText = Get-Content -LiteralPath $combatBodyProviderPath -Raw -Encoding utf8
$trionBootstrapText = Get-Content -LiteralPath $trionBootstrapPath -Raw -Encoding utf8

$cardWidthMatch = [regex]::Match($gizmoText, 'private const float CardWidth = ([0-9]+(?:\.[0-9]+)?)f;')
Assert-True -Condition $cardWidthMatch.Success -Message 'Gizmo_TrionStatus must declare an explicit CardWidth constant.'
$cardWidth = [float]$cardWidthMatch.Groups[1].Value

$cardHeightMatch = [regex]::Match($gizmoText, 'private const float CardHeight = ([0-9]+(?:\.[0-9]+)?)f;')
Assert-True -Condition $cardHeightMatch.Success -Message 'Gizmo_TrionStatus must declare an explicit CardHeight constant.'
$cardHeight = [float]$cardHeightMatch.Groups[1].Value

$geneUsesBridge = (($geneText -match 'TrionGeneGizmoBridge\.') -and ($geneText -match 'GetGizmos\(\)'))
Assert-True -Condition $geneUsesBridge -Message 'Gene_TrionGland.GetGizmos() must build the Trion gizmo through TrionGeneGizmoBridge.'

$bridgeUsesSurface = ($bridgeText -match 'TrionSurfaceAccess\.ResolveReader\(pawn\)')
Assert-True -Condition $bridgeUsesSurface -Message 'TrionGeneGizmoBridge must resolve the gizmo data source through TrionSurfaceAccess.ResolveReader(pawn).'

$bridgePublishesDebugButtons =
    ($bridgeText -match 'DebugSettings\.godMode') -and
    ($bridgeText -notmatch 'Prefs\.DevMode') -and
    ($bridgeText -match 'Command_Action') -and
    ($bridgeText -match '\+50') -and
    ($bridgeText -match '-50') -and
    ($bridgeText -match 'MAX') -and
    ($bridgeText -match 'TrySetCurrent|AdjustCurrent')
Assert-True -Condition $bridgePublishesDebugButtons -Message 'TrionGeneGizmoBridge must add +50 / -50 / MAX / 0 debug buttons only in god mode through the formal Trion commands surface.'

$gizmoStaysIndependent = (($gizmoText -match 'ITrionReader') -and ($gizmoText -notmatch 'CombatBodySessionService|CompTriggerBody|CompCombatBodyHost'))
Assert-True -Condition $gizmoStaysIndependent -Message 'Gizmo_TrionStatus must read only ITrionReader and must not couple to CombatBodySession, Trigger, or CombatBody internals.'

$gizmoUsesNewTitle = ($gizmoText -match 'Trion能量')
Assert-True -Condition $gizmoUsesNewTitle -Message 'Gizmo_TrionStatus title must use the exact copy Trion能量.'

$gizmoDropsOldTopHeader =
    ($gizmoText -notmatch 'LeftLabelWidth') -and
    ($gizmoText -notmatch 'HeaderSpacing') -and
    ($gizmoText -notmatch 'DrawTopRateBlock')
Assert-True -Condition $gizmoDropsOldTopHeader -Message 'Gizmo_TrionStatus must drop the old dual-column top header layout.'

$gizmoBuildsBottomInfo =
    ($gizmoText -match 'DrawBottomRow') -and
    ($gizmoText -match 'BuildBottomInfoText') -and
    ($gizmoText -match '可用:') -and
    ($gizmoText -notmatch 'BuildBottomInfoText\(\).*恢复') -and
    ($gizmoText -notmatch 'BuildBottomInfoText\(\).*消耗')
Assert-True -Condition $gizmoBuildsBottomInfo -Message 'Gizmo_TrionStatus bottom info row must focus on the available text while the rate text lives in the same bottom information band.'

$gizmoHasBottomRateGroup =
    ($gizmoText -match 'BuildRateText') -and
    ($gizmoText -match 'DrawRateText') -and
    ($gizmoText -match 'DrawBottomRow') -and
    ($gizmoText -match 'MiddleRight') -and
    ($gizmoText -match '恢复 ') -and
    ($gizmoText -match '消耗 ')
Assert-True -Condition $gizmoHasBottomRateGroup -Message 'Gizmo_TrionStatus must render 恢复/消耗 in the bottom right-aligned info group.'

$gizmoUsesReservedBoundaryFromLeft =
    ($gizmoText -match 'reader\.Reserved / max') -and
    ($gizmoText -notmatch 'reader\.Cur - reader\.Reserved')
Assert-True -Condition $gizmoUsesReservedBoundaryFromLeft -Message 'Inactive predicted divider must preview reserved lock width from the left boundary, not remaining available width.'

$gizmoWidthIsApproved = (($cardWidth -gt 160.0) -and ($cardWidth -le 240.0) -and ($gizmoText -match 'return CardWidth;') -and ($gizmoText -notmatch 'return maxWidth;'))
Assert-True -Condition $gizmoWidthIsApproved -Message 'Gizmo_TrionStatus width must stay within the approved compact range (>160 and <=240) instead of stretching to the whole remaining gizmo row.'

$gizmoHeightMatchesVanilla = (($cardHeight -eq 75.0) -and ($gizmoText -match 'new Rect\(topLeft\.x, topLeft\.y, GetWidth\(maxWidth\), CardHeight\)'))
Assert-True -Condition $gizmoHeightMatchesVanilla -Message 'Gizmo_TrionStatus card height must stay aligned with the vanilla gizmo height of 75.'

$usesOneDecimalFormatting =
    ($gizmoText -match 'ToString\("F1"\)') -and
    ($gizmoText -notmatch 'ToString\("F2"\)') -and
    ($gizmoText -notmatch 'F0')
Assert-True -Condition $usesOneDecimalFormatting -Message 'Gizmo_TrionStatus visible numeric copy must use one decimal place consistently.'

$limitsBadgeSlots =
    ($gizmoText -match 'private const int MaxVisibleBadges = 4;') -and
    ($gizmoText -match 'Take\(MaxVisibleBadges\)|Mathf\.Min\(badges\.Count, MaxVisibleBadges\)')
Assert-True -Condition $limitsBadgeSlots -Message 'Gizmo_TrionStatus must hard-cap the bottom-right status area to four badges.'

$usesTopRightBadgeArea = ($gizmoText -match 'DrawTitleRow') -and ($gizmoText -match 'DrawBadgeRow') -and ($gizmoText -notmatch 'DrawExtensionsBelowBar')
Assert-True -Condition $usesTopRightBadgeArea -Message 'Gizmo_TrionStatus must render extension badges back in the top-right area instead of a row below the bar.'

$frozenBadgeAlwaysVisible =
    ($gizmoText -match 'CreateFrozenBadge') -and
    ($gizmoText -match 'reader\.Frozen \? FrozenBadgeColor : FrozenBadgeDimColor') -and
    ($gizmoText -match 'DrawBadge\(frozenRect, CreateFrozenBadge\(\), -1\)')
Assert-True -Condition $frozenBadgeAlwaysVisible -Message 'Frozen indicator must stay visible in the same group as the recovery/drain text and switch between highlight and dim tints.'

$usesSplitDivider =
    ($gizmoText -match 'DrawSoftDivider') -and
    ($gizmoText -notmatch 'Widgets\.DrawLineVertical\(dividerX, fillRect\.y, fillRect\.height\)')
Assert-True -Condition $usesSplitDivider -Message 'Gizmo_TrionStatus divider must use a softened subtle divider rather than one full-height bright line.'

$keepsExtensionRegistry = (($gizmoText -match 'TrionGizmoExtensionRegistry') -and ($gizmoText -match 'DrawTexture'))
Assert-True -Condition $keepsExtensionRegistry -Message 'Gizmo_TrionStatus must keep the extension registry path for external badges.'

$compHasNoDirectGizmoEntry = ($compTrionText -notmatch 'CompGetGizmosExtra|GetGizmos\(')
Assert-True -Condition $compHasNoDirectGizmoEntry -Message 'CompTrion must not directly carry the Pawn-side Trion gizmo entry.'

$formalWriteSurfaceExists =
    ($commandsText -match 'TrionCurrentWriteResult') -and
    ($commandsText -match 'AdjustCurrent\(') -and
    ($commandsText -match 'TrySetCurrent\(') -and
    ($serviceText -match 'AdjustCurrent\(') -and
    ($serviceText -match 'TrySetCurrent\(')
Assert-True -Condition $formalWriteSurfaceExists -Message 'ITrionCommands and TrionService must expose formal current-value debug write entry points.'

$compTrionEnforcesAllocatedFloor =
    ($compTrionText -match 'Mathf\.Max\(allocated') -and
    ($compTrionText -match 'TrySetCurrent') -and
    ($compTrionText -match 'AdjustCurrent') -and
    ($compTrionText -match 'allocated')
Assert-True -Condition $compTrionEnforcesAllocatedFloor -Message 'CompTrion debug writes must enforce the allocated floor and keep current value from dropping under formal lock.'

$combatBodyProviderUsesFormalSurface =
    ($combatBodyProviderText -match 'CombatBodySurfaceAccess\.ResolveReader\(') -and
    ($combatBodyProviderText -notmatch 'CombatBodySessionService|CompCombatBodyHost|GetComp<')
Assert-True -Condition $combatBodyProviderUsesFormalSurface -Message 'CombatBody Trion gizmo provider must read state only through CombatBodySurfaceAccess and must not reach into CombatBodySession or host comps directly.'

$combatBodyProviderPublishesStatus =
    ($combatBodyProviderText -match 'CombatBodyPhase\.Active') -and
    ($combatBodyProviderText -match 'CombatBodyPhase\.Inactive') -and
    ($combatBodyProviderText -match '战斗体') -and
    ($combatBodyProviderText -match 'TrionGizmoExtensionBadge')
Assert-True -Condition $combatBodyProviderPublishesStatus -Message 'CombatBody Trion gizmo provider must publish a formal battle-body status badge for active and inactive states instead of showing and hiding by state.'

$bootstrapRegistersProvider =
    ($trionBootstrapText -match 'StaticConstructorOnStartup') -and
    ($trionBootstrapText -match 'TrionGizmoExtensionRegistry\.Register\(new CombatBodyTrionGizmoExtensionProvider\(\)\)')
Assert-True -Condition $bootstrapRegistersProvider -Message 'Main mod bootstrap must register CombatBodyTrionGizmoExtensionProvider into TrionGizmoExtensionRegistry.'

Write-Output 'TrionGeneGizmoSmokeTests PASS'

