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
$lifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$fixedLoadoutPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.FixedLoadout.cs'
$loadoutServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Loadout\TriggerLoadoutService.cs'
$diagnosticsPath = Join-Path $repoRoot 'Source\BDP\Support\Diagnostics\BdpDiagnostics.cs'

foreach ($path in @($lifecyclePath, $fixedLoadoutPath, $loadoutServicePath, $diagnosticsPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('Required fixed-loadout initialization file is missing: ' + $path)
}

$lifecycleText = Get-Content -LiteralPath $lifecyclePath -Raw -Encoding UTF8
$fixedLoadoutText = Get-Content -LiteralPath $fixedLoadoutPath -Raw -Encoding UTF8
$loadoutServiceText = Get-Content -LiteralPath $loadoutServicePath -Raw -Encoding UTF8
$diagnosticsText = Get-Content -LiteralPath $diagnosticsPath -Raw -Encoding UTF8

Assert-True ($lifecycleText -match 'PostPostMake\s*\(\)') `
    'Fixed-loadout initialization must have a PostPostMake entry point.'
Assert-True ($lifecycleText -match 'PostPostMake\s*\(\)[\s\S]*TryInstallInitialFixedLoadout') `
    'PostPostMake must invoke the initial fixed-loadout installer.'
Assert-True ($lifecycleText -notmatch 'PostExposeData\s*\([\s\S]*TryInstallInitialFixedLoadout') `
    'PostExposeData must not replay the initial fixed-loadout installer during save/load.'

Assert-True (
    ($fixedLoadoutText -match 'ThingMaker\.MakeThing') -and
    ($fixedLoadoutText -match 'TriggerLoadoutService\.TryLoadChip') -and
    ($fixedLoadoutText -match 'slotNumber\s*-\s*1')
) 'The installer must create real Things and reuse the existing load service with one-based slot conversion.'

Assert-True (
    ($fixedLoadoutText -match 'PlayerConfigurable') -or
    ($fixedLoadoutText -match 'TryLoadChip\s*\(\s*BuildLoadoutContext')
) 'Initial fixed-loadout installation must bypass the player command permission guard.'

Assert-True (
    ($fixedLoadoutText -match 'rollback') -or
    ($fixedLoadoutText -match 'Rollback')
) 'The installer must contain an explicit all-or-nothing rollback path.'
Assert-True ($fixedLoadoutText -match 'DestroyMode\.Vanish') `
    'Rollback must destroy only the batch-created chip Things after removing them from the trigger.'
Assert-True ($fixedLoadoutText -match 'BdpDiagnostics') `
    'Unexpected initialization failures must be recorded through compact diagnostics.'
Assert-True ($fixedLoadoutText -notmatch 'Messages\.Message') `
    'Initial fixed-loadout failures must not show player messages.'
Assert-True ($fixedLoadoutText -notmatch 'RequestActivate') `
    'Initial fixed-loadout installation must not add a second activation policy.'

Assert-True ($loadoutServiceText -match 'TryLoadChip') `
    'The existing TriggerLoadoutService must remain the single slot mutation service.'
Assert-True ($diagnosticsText -match 'Once') `
    'The diagnostics surface must support one-time initialization failure logging.'

Write-Output 'TriggerFixedLoadoutInitializationSmokeTests PASS'
