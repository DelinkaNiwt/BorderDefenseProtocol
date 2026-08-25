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
$patchPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Protection\Patch_Pawn_HealthTracker_CombatBodyTriggerProtection.cs'

Assert-True (Test-Path -LiteralPath $patchPath) 'Combat body trigger protection patch must exist.'

$patchText = Get-Content -LiteralPath $patchPath -Raw -Encoding utf8

Assert-True ($patchText -match 'CombatBodyPhase\.Active') 'Protection must cover Active phase.'
Assert-True ($patchText -match 'CombatBodyPhase\.Collapsing') 'Protection must cover Collapsing phase.'
Assert-True ($patchText -match 'CompTriggerBody') 'Protection must be limited to BDP TriggerBody.'
Assert-True ($patchText -match 'TryDropEquipment') 'Protection must guard the original equipment drop boundary.'
Assert-True ($patchText -match 'MakeDowned') 'Protection must cover the original downed cleanup path.'
Assert-True ($patchText -match 'CheckForStateChange') 'Protection must cover manipulation-loss cleanup.'
Assert-True ($patchText -match 'Notify_PawnSpawned') 'Protection must cover downed spawn compensation.'
Assert-True ($patchText -match 'TriggerCollapse\("PawnDowned"\)') 'Active downed transition must request PawnDowned collapse.'
Assert-True ($patchText -match 'CombatBodySurfaceAccess\.ResolveCommands') 'Collapse must use the formal combat body command surface.'
Assert-True ($patchText -match 'ShouldCollapseFromHeartMissing\(Pawn pawn\)') 'Active heart loss must have a dedicated collapse predicate.'
Assert-True ($patchText -match 'ShouldCollapseFromHeartMissing\(Pawn pawn\)[\s\S]*IsActivePhase\(pawn\)[\s\S]*HasCurrentPrimaryTrigger\(pawn\)') 'Heart loss collapse must be limited to active BDP trigger-body sessions.'
Assert-True ($patchText -match 'BodyPartDefOf\.Heart[\s\S]*PartIsMissing') 'Heart loss predicate must inspect the original Heart body part.'
Assert-True ($patchText -match 'TriggerCollapse\("HeartMissing"\)') 'Active heart loss must request HeartMissing collapse.'

Write-Output 'CombatBodyTriggerDropAndDownedCollapseSmokeTests PASS'
