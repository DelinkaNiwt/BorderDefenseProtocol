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

$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$typedKeyPath = Join-Path $trionRoot 'TrionDrainKey.cs'
$trionFiles = Get-ChildItem -LiteralPath $trionRoot -Filter '*.cs' -Recurse
$trionText = ($trionFiles | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True (
    Test-Path -LiteralPath $typedKeyPath
) 'Task 2 requires a Trion-owned typed drain key file.'

Assert-True (
    $trionText -notmatch 'using\s+BDP\.Core\.Trigger'
) 'Trion namespace files must not import BDP.Core.Trigger.'

Assert-True (
    $trionText -notmatch '\bTriggerSide\b'
) 'Trion namespace files must not reference TriggerSide.'

Assert-True (
    $trionText -notmatch 'RegisterDrain\s*\(\s*string\s+key'
) 'Trion drain registration APIs must not keep string key signatures.'

Assert-True (
    $trionText -notmatch 'UnregisterDrain\s*\(\s*string\s+key'
) 'Trion drain unregistration APIs must not keep string key signatures.'

Assert-True (
    -not (Test-Path -LiteralPath (Join-Path $trionRoot 'TrionDrainKeys.cs'))
) 'Legacy TrionDrainKeys.cs must be removed completely.'

Write-Output 'TrionDrainKeyBoundarySmokeTests PASS'
