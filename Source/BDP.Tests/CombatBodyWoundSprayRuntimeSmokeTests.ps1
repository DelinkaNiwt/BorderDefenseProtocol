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
$runtimePath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Wounds\Visuals\CombatBodyWoundSprayRuntime.cs'
$providerPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Wounds\Visuals\CombatBodyWoundSprayPresentationProvider.cs'

Assert-True (Test-Path -LiteralPath $runtimePath) 'CombatBodyWoundSprayRuntime.cs must exist.'
Assert-True (Test-Path -LiteralPath $providerPath) 'CombatBodyWoundSprayPresentationProvider.cs must exist.'

$text = Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$rebuildStart = $text.IndexOf('internal void RebuildFromActiveDrains')
$rebuildEnd = $text.IndexOf('private static Hediff FindWoundByLoadId', $rebuildStart)
Assert-True (($rebuildStart -ge 0) -and ($rebuildEnd -gt $rebuildStart)) 'Spray runtime must define RebuildFromActiveDrains before FindWoundByLoadId.'
$rebuildText = $text.Substring($rebuildStart, $rebuildEnd - $rebuildStart)

Assert-True ($text -match 'internal\s+sealed\s+class\s+CombatBodyWoundSprayRuntime') 'Spray runtime must be internal sealed.'
Assert-True ($text -match 'Dictionary<int,\s*CombatBodyWoundSprayEmitter>') 'Spray runtime must own emitters by Hediff loadID.'
Assert-True ($text -match 'MaxActiveEmitters\s*=\s*12') 'Spray runtime must cap active emitters at 12.'
Assert-True ($text -match 'Dictionary<int,\s*float>\s+cutTiltByHediffLoadId') 'Spray runtime must save cut tilt by Hediff loadID.'
Assert-True ($text -match 'internal\s+void\s+ExposeData\(\)') 'Spray runtime must expose save-load data.'
Assert-True ($text -match 'Scribe_Collections\.Look') 'Spray runtime must scribe saved cut tilt data.'
Assert-True ($text -match 'internal\s+void\s+NotifyWoundAdded\(Pawn pawn,\s*Hediff hediff\)') 'Spray runtime must handle wound added events.'
Assert-True ($text -match 'internal\s+void\s+NotifyWoundDrainExpired\(int hediffLoadId\)') 'Spray runtime must handle drain expiry.'
Assert-True ($text -match 'internal\s+void\s+ClearAll\(\)') 'Spray runtime must clear all emitters.'
Assert-True ($text -match 'internal\s+void\s+Tick\(Pawn pawn\)') 'Spray runtime must tick emitters.'
Assert-True ($text -match 'internal\s+void\s+RebuildFromActiveDrains\(Pawn pawn,\s*IEnumerable<int> activeHediffLoadIds\)') 'Spray runtime must rebuild from active drains.'
Assert-True ($rebuildText -notmatch 'NotifyBurst') 'RebuildFromActiveDrains must not trigger burst.'
Assert-True ($text -match '/// <summary>') 'Spray runtime members must be documented.'
Assert-True ($providerText -match 'Dictionary<int,\s*CombatBodyWoundSprayRuntime>\s+runtimesByPawnId') 'Presentation provider must isolate spray runtime by Pawn identity.'
Assert-True ($providerText -notmatch 'private readonly CombatBodyWoundSprayRuntime runtime') 'Presentation provider must not keep one global spray runtime shared by all Pawns.'
Assert-True ($providerText -match 'pawn\.thingIDNumber') 'Presentation provider must resolve each runtime through the owning Pawn identity.'

Write-Output 'CombatBodyWoundSprayRuntimeSmokeTests PASS'
