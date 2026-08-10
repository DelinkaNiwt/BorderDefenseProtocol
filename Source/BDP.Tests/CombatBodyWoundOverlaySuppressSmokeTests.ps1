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
$patchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_PawnWoundDrawer_RenderPawnOverlay_CombatBodySuppress.cs'

Assert-True (Test-Path -LiteralPath $patchPath) 'CombatBody wound overlay suppress patch must exist.'

$text = Get-Content -LiteralPath $patchPath -Raw -Encoding utf8

Assert-True ($text -match 'HarmonyPatch\(typeof\(PawnOverlayDrawer\),\s*nameof\(PawnOverlayDrawer\.RenderPawnOverlay\)\)') 'Patch must target PawnOverlayDrawer.RenderPawnOverlay.'
Assert-True ($text -match 'public\s+static\s+bool\s+Prefix\(PawnOverlayDrawer __instance\)') 'Patch Prefix must return bool and receive PawnOverlayDrawer instance.'
Assert-True ($text -match '__instance\s+is\s+PawnWoundDrawer') 'Patch must only suppress PawnWoundDrawer.'
Assert-True ($text -match 'AccessTools\.FieldRefAccess<PawnOverlayDrawer,\s*Pawn>\("pawn"\)') 'Patch must resolve protected pawn field from PawnOverlayDrawer.'
Assert-True ($text -match 'CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(pawn\)') 'Patch must suppress when wound runtime visuals apply.'
Assert-True ($text -match 'return\s+!CombatBodyWoundPolicy\.IsCombatBodyWoundRuntimeApplicable\(pawn\);') 'Patch must return false during Active and Collapsing wound visual phases.'
Assert-True ($text -match '/// <summary>') 'Patch members must be documented.'

Write-Output 'CombatBodyWoundOverlaySuppressSmokeTests PASS'
