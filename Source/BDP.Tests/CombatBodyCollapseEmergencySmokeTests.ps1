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

$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$propsPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompProperties_CombatBodyHost.cs'
$combatBodySessionServicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$battleExitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$combatBodySessionBindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionTrionBinding.cs'
$combatBodyHediffDefsPath = Join-Path $repoRoot '1.6\Defs\HediffDef\CombatBody.xml'
$collapsePendingHediffPath = Join-Path $repoRoot 'Source\BDP\Core\Hediffs\Hediff_BdpCombatBodyCollapsePending.cs'
$collapseReasonPresenterPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\External\CombatBodyCollapseReasonPresenter.cs'
$combatBodyTrionGizmoExtensionProviderPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\External\CombatBodyTrionGizmoExtensionProvider.cs'
$hostStatePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\HostState.cs'
$messagesLanguagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Messages.xml'

$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$propsText = Get-Content -LiteralPath $propsPath -Raw -Encoding utf8
$combatBodySessionServiceText = Get-Content -LiteralPath $combatBodySessionServicePath -Raw -Encoding utf8
$battleExitTransactionText = Get-Content -LiteralPath $battleExitTransactionPath -Raw -Encoding utf8
$combatBodySessionBindingText = Get-Content -LiteralPath $combatBodySessionBindingPath -Raw -Encoding utf8
$combatBodyHediffDefsText = Get-Content -LiteralPath $combatBodyHediffDefsPath -Raw -Encoding utf8
$hostStateText = Get-Content -LiteralPath $hostStatePath -Raw -Encoding utf8
$messagesLanguageText = Get-Content -LiteralPath $messagesLanguagePath -Raw -Encoding utf8
$collapsePendingHediffText = ''
if (Test-Path -LiteralPath $collapsePendingHediffPath) {
    $collapsePendingHediffText = Get-Content -LiteralPath $collapsePendingHediffPath -Raw -Encoding utf8
}
$collapseReasonPresenterText = ''
if (Test-Path -LiteralPath $collapseReasonPresenterPath) {
    $collapseReasonPresenterText = Get-Content -LiteralPath $collapseReasonPresenterPath -Raw -Encoding utf8
}
$combatBodyTrionGizmoExtensionProviderText = ''
if (Test-Path -LiteralPath $combatBodyTrionGizmoExtensionProviderPath) {
    $combatBodyTrionGizmoExtensionProviderText = Get-Content -LiteralPath $combatBodyTrionGizmoExtensionProviderPath -Raw -Encoding utf8
}

Assert-True (
    $propsText -match 'public float maintenanceDrainPerSecond = 0f;'
) 'CompProperties_CombatBodyHost must expose maintenanceDrainPerSecond.'

Assert-True (
    $hostText -match 'public override void CompTick\(\)[\s\S]*Service\.Phase == CombatBodyPhase\.Collapsing[\s\S]*Service\.GetCollapseRemaining\(\) <= 0[\s\S]*Service\.FinalizeCollapse\(\);'
) 'CompCombatBodyHost.CompTick() 必须在崩解表现结束后通过 FinalizeCollapse() 进入统一收尾。'

Assert-True (
    $combatBodySessionServiceText -match 'internal void RestoreAfterLoad\(\)'
) 'CombatBodySessionService must expose RestoreAfterLoad() for post-load runtime recovery.'

Assert-True (
    $hostText -match 'PostLoadInit[\s\S]*combatBodySessionService\?\.RestoreAfterLoad\(\);'
) 'CompCombatBodyHost.PostExposeData() must restore battle-session runtime subscriptions after load.'

Assert-True (
    $combatBodySessionServiceText -match 'public void TriggerCollapse\(string reason\)[\s\S]*rawCombatBodyService\.EnterCollapsing\(reason\)[\s\S]*EndCurrentJob\(JobCondition\.InterruptForced'
) 'TriggerCollapse must enter Collapsing and interrupt the current pawn job.'

Assert-True (
    $combatBodySessionServiceText -match 'public void TriggerCollapse\(string reason\)[\s\S]*CombatBodyCollapseExtensionRegistry\.Prepare\(OwnerPawn\)[\s\S]*rawCombatBodyService\.EnterCollapsing\(reason\)[\s\S]*SetCombatBodyUnavailableDisabled\(true\)[\s\S]*ApplyCollapsePendingHediff\(OwnerPawn\)'
) 'TriggerCollapse must prepare neutral collapse extensions first, then enter Collapsing, immediately disable trigger slots, and finally attach the collapse-pending hediff.'

Assert-True (
    $combatBodySessionServiceText -match 'public void FinalizeCollapse\(\)'
) 'CombatBodySessionService 必须提供 FinalizeCollapse()，用于崩解表现结束后的正式收尾。'

Assert-True (
    $hostStateText -notmatch 'CachedCollapseEmergencyEscape|CombatBodyEmergencyEscapeResolution'
) 'HostState must not persist a business-specific collapse resolution.'

Assert-True (
    $battleExitTransactionText -match 'public void Execute\(Pawn ownerPawn, CombatBodySessionExitMode exitMode\)[\s\S]*trionCommands\.Release\(rawCombatBodyService\.AllocatedTrion\)'
) 'CombatBodyExitTransaction 退出收尾仍必须先释放已锁定 Trion。'

Assert-True (
    $battleExitTransactionText -match 'CombatBodySessionExitMode\.Collapse[\s\S]*TrySetCurrent\(0f\)'
) 'CombatBodyExitTransaction 被动崩解收尾必须把 Trion 当前值清 0。'

Assert-True (
    ($battleExitTransactionText -match 'public void Execute\(Pawn ownerPawn, CombatBodySessionExitMode exitMode\)[\s\S]*if \(exitMode == CombatBodySessionExitMode\.Collapse\)[\s\S]*RemoveCollapsePendingHediff\(ownerPawn\)[\s\S]*CombatBodyCollapseExtensionRegistry\.Execute\(ownerPawn\)[\s\S]*if \(exitMode == CombatBodySessionExitMode\.Release\)[\s\S]*DeactivateAllSlots\(triggerLoadoutCommands\)[\s\S]*EnterCooldown\(ResolveCooldownTicks\(exitMode\), ResolveExitReason\(exitMode\)\)[\s\S]*if \(exitMode == CombatBodySessionExitMode\.Collapse\)[\s\S]*TrySetCurrent\(0f\)[\s\S]*ApplyCollapseAftereffect\(ownerPawn\)[\s\S]*SetCombatBodyUnavailableDisabled\(false\)') -and
    ($battleExitTransactionText -notmatch 'CombatBodyEmergencyEscape|emergencyEscapeResolver\.Resolve\(ownerPawn\)')
) 'CombatBodyExitTransaction 必须通过中性崩解扩展入口执行业务，不得直接持有紧急脱离。'

Assert-True (
    $battleExitTransactionText -notmatch 'CombatBodySessionExitMode\.Emergency'
) 'CombatBodyExitTransaction 不应再把 Emergency 当作并列退出模式。'

Assert-True (
    $combatBodySessionBindingText -match 'TryRegisterCombatBodyMaintenanceDrain\(ITrionCommands trionCommands\)[\s\S]*owner\.MaintenanceDrainPerSecond[\s\S]*RegisterDrain\(combatBodyMaintenanceDrainKey'
) 'CombatBodySessionTrionBinding must register combat-body maintenance drain from host config.'

Assert-True (
    $combatBodyHediffDefsText -match '<defName>BDP_CombatBodyCollapseAftereffect</defName>'
) 'CombatBody collapse hediff xml must define BDP_CombatBodyCollapseAftereffect.'

Assert-True (
    $combatBodyHediffDefsText -match '<defName>BDP_CombatBodyCollapseAftereffect</defName>[\s\S]*<hediffClass>HediffWithComps</hediffClass>'
) 'CombatBody collapse hediff xml must declare HediffWithComps when using comps.'

Assert-True (
    $combatBodyHediffDefsText -match '<defName>BDP_CombatBodyCollapsePending</defName>'
) 'CombatBody hediff xml must define BDP_CombatBodyCollapsePending.'

Assert-True (
    $combatBodyHediffDefsText -match '<defName>BDP_CombatBodyCollapsePending</defName>[\s\S]*<hediffClass>BDP\.Core\.Hediffs\.Hediff_BdpCombatBodyCollapsePending</hediffClass>'
) 'CombatBody collapse pending hediff xml must bind to Hediff_BdpCombatBodyCollapsePending.'

Assert-True (
    Test-Path -LiteralPath $collapsePendingHediffPath
) 'Collapse pending hediff class must exist.'

Assert-True (
    $collapsePendingHediffText -match 'public override string LabelBase'
) 'Collapse pending hediff must override LabelBase for live countdown.'

Assert-True (
    $collapsePendingHediffText -match 'public override string TipStringExtra'
) 'Collapse pending hediff must override TipStringExtra for collapse reason tooltip.'

Assert-True (
    $collapsePendingHediffText -match 'GetCollapseRemaining\(\)'
) 'Collapse pending hediff must read live collapse remaining ticks from CombatBody.'

Assert-True (
    $collapsePendingHediffText -match 'CollapseReason'
) 'Collapse pending hediff tip must read direct collapse reason from CombatBody.'

Assert-True (
    Test-Path -LiteralPath $collapseReasonPresenterPath
) 'CombatBody collapse reason presenter must exist for player-facing text.'

Assert-True (
    ($collapseReasonPresenterText -match 'TrionAvailableDepleted') -and
    ($collapseReasonPresenterText -match 'BDP_Message_CombatBody_CollapseReasonTrionDepleted"\.Translate\(\)') -and
    ($messagesLanguageText -match '<BDP_Message_CombatBody_CollapseReasonTrionDepleted>[^<]*[\u4e00-\u9fff][^<]*</BDP_Message_CombatBody_CollapseReasonTrionDepleted>')
) 'CombatBody collapse reason presenter must map TrionAvailableDepleted through a readable Chinese language key.'

Assert-True (
    $collapsePendingHediffText -match 'CombatBodyCollapseReasonPresenter\.Describe'
) 'Collapse pending hediff must convert collapse reason to player-facing text through CombatBodyCollapseReasonPresenter.'

Assert-True (
    $combatBodyTrionGizmoExtensionProviderText -match 'CombatBodyCollapseReasonPresenter\.Describe'
) 'CombatBody Trion gizmo collapsing tooltip must convert collapse reason to player-facing text through CombatBodyCollapseReasonPresenter.'

Write-Output 'CombatBodyCollapseEmergency PASS'
