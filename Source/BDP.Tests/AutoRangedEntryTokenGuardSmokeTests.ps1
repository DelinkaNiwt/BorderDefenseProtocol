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
$attackSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'

$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8

Assert-True (
    $attackSurfaceText -match 'residentSession\s*=\s*shootVerb\.HostModuleSession'
) 'Auto-ranged bridge must inspect the resident execution module session before staging auto entry state.'

Assert-True (
    $attackSurfaceText -match 'shootVerb\.StageEntryModuleSession\s*\(\s*stagedSession\s*\)'
) 'Auto-ranged bridge must still stage the auto entry module session for a fresh auto start.'

Assert-True (
    $attackSurfaceText -match 'if\s*\(\s*residentSession\s*==\s*null\s*\)\s*\{\s*shootVerb\.HostSessionToken\s*=\s*token\s*;\s*\}'
) 'Auto-ranged bridge must not overwrite HostSessionToken while a resident execution module session is alive.'

Write-Output 'AutoRangedEntryTokenGuard PASS'
