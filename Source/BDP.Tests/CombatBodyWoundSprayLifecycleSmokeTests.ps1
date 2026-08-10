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
$runtimePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundRuntime.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'

Assert-True (Test-Path -LiteralPath $runtimePath) 'CombatBodyWoundRuntime.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'

$runtimeText = Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8

$rebuildStart = $runtimeText.IndexOf('internal void RebuildActiveWounds')
$rebuildEnd = $runtimeText.IndexOf('internal void NotifyWoundAddedOrChanged', $rebuildStart)
Assert-True (($rebuildStart -ge 0) -and ($rebuildEnd -gt $rebuildStart)) 'Wound runtime must keep RebuildActiveWounds before NotifyWoundAddedOrChanged.'
$rebuildText = $runtimeText.Substring($rebuildStart, $rebuildEnd - $rebuildStart)

$notifyStart = $runtimeText.IndexOf('internal void NotifyWoundAddedOrChanged')
$notifyEnd = $runtimeText.IndexOf('internal void NotifyWoundRemoved', $notifyStart)
Assert-True (($notifyStart -ge 0) -and ($notifyEnd -gt $notifyStart)) 'Wound runtime must keep NotifyWoundAddedOrChanged before NotifyWoundRemoved.'
$notifyText = $runtimeText.Substring($notifyStart, $notifyEnd - $notifyStart)

$tickStart = $runtimeText.IndexOf('internal void Tick(Pawn pawn)')
$tickEnd = $runtimeText.IndexOf('private void ScheduleNextCalibration', $tickStart)
Assert-True (($tickStart -ge 0) -and ($tickEnd -gt $tickStart)) 'Wound runtime must keep Tick before ScheduleNextCalibration.'
$tickText = $runtimeText.Substring($tickStart, $tickEnd - $tickStart)

$restoreStart = $runtimeText.IndexOf('internal void RestoreAfterLoad(Pawn pawn)')
$restoreEnd = $runtimeText.IndexOf('internal void Tick(Pawn pawn)', $restoreStart)
Assert-True (($restoreStart -ge 0) -and ($restoreEnd -gt $restoreStart)) 'Wound runtime must keep RestoreAfterLoad before Tick.'
$restoreText = $runtimeText.Substring($restoreStart, $restoreEnd - $restoreStart)

$updateIndex = $notifyText.IndexOf('trionBinding.UpdateWoundDrain')
$sprayAddedIndex = $notifyText.IndexOf('CombatBodyWoundPresentationRegistry.NotifyWoundAdded')
Assert-True ($runtimeText -match 'CombatBodyWoundPresentationRegistry') 'Wound runtime must publish neutral presentation events.'
Assert-True ($runtimeText -match 'using BDP\.Core\.CombatBody\.Wounds\.Presentation;') 'Wound runtime must import neutral presentation namespace.'
Assert-True ($runtimeText -match 'CombatBodyWoundPresentationRegistry\.ExposeData\(\)') 'Wound runtime must save presentation provider state.'
Assert-True (($updateIndex -ge 0) -and ($sprayAddedIndex -gt $updateIndex)) 'Wound spray must start only after successful drain registration.'
Assert-True ($notifyText -match 'expiryTick\s*<=\s*0[\s\S]*CombatBodyWoundPresentationRegistry\.NotifyWoundDrainExpired') 'Failed drain registration must stop any existing presentation.'
Assert-True ($runtimeText -match 'NotifyWoundRemoved\(Pawn pawn,\s*Hediff hediff\)[\s\S]*CombatBodyWoundPresentationRegistry\.NotifyWoundRemoved') 'Wound removal must notify presentation providers.'
Assert-True ($runtimeText -match 'ClearActiveRuntime\(Pawn pawn\)[\s\S]*CombatBodyWoundPresentationRegistry\.ClearAll\(\)') 'Clearing active wound runtime must clear presentation providers.'
Assert-True ($rebuildText -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(pawn\)') 'Rebuild must preserve wound runtime through Active and Collapsing phases.'
Assert-True ($notifyText -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(pawn\)') 'Wound changes must accept Active and Collapsing spray applicability.'
Assert-True ($bindingText -match 'ExpireIdleDrains\(Pawn pawn,\s*int currentTick,\s*List<int> expiredIdsOut\)') 'Drain expiry must report expired Hediff load IDs.'
Assert-True ($tickText -match 'trionBinding\.ExpireIdleDrains\(pawn,\s*ticksGame,\s*expiredDrainIds\)') 'Wound runtime tick must request expired drain IDs.'
Assert-True ($tickText -match 'CombatBodyWoundPresentationRegistry\.NotifyWoundDrainExpired') 'Wound runtime tick must stop expired presentations.'
Assert-True ($tickText -match 'CombatBodyWoundPresentationRegistry\.Tick\(pawn\)') 'Wound runtime tick must advance active presentations.'
Assert-True ($tickText -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(pawn\)') 'Spray tick must continue during Active and Collapsing phases.'
Assert-True ($tickText.IndexOf('trionBinding.ExpireIdleDrains') -lt $tickText.IndexOf('CombatBodyWoundPresentationRegistry.Tick')) 'Drain expiry must run before presentation tick.'
Assert-True ($restoreText -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(pawn\)') 'RestoreAfterLoad must preserve spray runtime during Active and Collapsing phases.'
Assert-True ($restoreText -match 'CombatBodyWoundPresentationRegistry\.RebuildFromActiveDrains\([\s\r\n]*pawn,\s*trionBinding\.GetActiveHediffLoadIds\(\)\)') 'RestoreAfterLoad must rebuild presentations from active drain ids.'

Write-Output 'CombatBodyWoundSprayLifecycleSmokeTests PASS'
