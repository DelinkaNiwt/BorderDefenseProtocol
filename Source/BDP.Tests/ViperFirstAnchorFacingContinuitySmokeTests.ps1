$ErrorActionPreference = 'Stop'

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
$verbPath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_Shoot.cs'
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8

$tryStartMatch = [regex]::Match(
    $verbText,
    '(?s)public override bool TryStartCastOn\(.*?public override void WarmupComplete')
Assert-True $tryStartMatch.Success `
    'BdpVerb_Shoot.TryStartCastOn and WarmupComplete boundaries must remain readable.'

$tryStartText = $tryStartMatch.Value
Assert-True (
    ($tryStartText -match 'LocalTargetInfo\s+baseCastTarget\s*=\s*ResolveBaseVerbStartTarget') -and
    ($tryStartText -match 'base\.TryStartCastOn\(baseCastTarget')
) 'The original warmup/cooldown focus must continue to start from the first launch target.'
Assert-True (
    $tryStartText -notmatch '(?m)^\s*currentTarget\s*=\s*target;\s*$'
) 'TryStartCastOn must not replace the first-anchor stance focus with the requested semantic target.'

$battleLogMatch = [regex]::Match(
    $verbText,
    '(?s)private void LogWarmupBattleEntry\(\).*?private ThingDef ResolveVanillaBattleLogWeaponDef')
Assert-True $battleLogMatch.Success `
    'LogWarmupBattleEntry boundary must remain readable.'
Assert-True (
    ($battleLogMatch.Value -match 'LocalTargetInfo\s+semanticTarget\s*=\s*sessionTarget\.IsValid\s*\?\s*sessionTarget\s*:\s*currentTarget') -and
    ($battleLogMatch.Value -match 'semanticTarget\.HasThing\s*\?\s*semanticTarget\.Thing\s*:\s*null')
) 'Battle log target semantics must not depend on the first-anchor stance focus.'

$experienceMatch = [regex]::Match(
    $verbText,
    '(?s)private void GrantShootingExperience\(\).*?private new IntVec3 GetForcedMissTarget')
Assert-True $experienceMatch.Success `
    'GrantShootingExperience boundary must remain readable.'
Assert-True (
    ($experienceMatch.Value -match 'LocalTargetInfo\s+semanticTarget\s*=\s*sessionTarget\.IsValid\s*\?\s*sessionTarget\s*:\s*currentTarget') -and
    ($experienceMatch.Value -match 'Pawn\s+targetPawn\s*=\s*semanticTarget\.Thing\s+as\s+Pawn')
) 'Shooting experience target semantics must not depend on the first-anchor stance focus.'

Write-Output 'ViperFirstAnchorFacingContinuitySmokeTests PASS'
