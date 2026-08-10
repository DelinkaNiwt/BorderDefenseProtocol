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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$diagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionDiagnostics.cs'
$surfacePath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$shootVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_Shoot.cs'

$diagnosticsText = Read-Source $diagnosticsPath
$surfaceText = Read-Source $surfacePath
$shootVerbText = Read-Source $shootVerbPath

Assert-True (
    ($diagnosticsText -notmatch 'LogEntryModuleSessionStaged\(') -and
    ($diagnosticsText -notmatch 'event=entry_module_session_staged') -and
    ($surfaceText -match 'previousToken\s*=\s*shootVerb\.HostSessionToken') -and
    ($surfaceText -match 'residentSession\s*=\s*shootVerb\.HostModuleSession') -and
    ($surfaceText -notmatch 'LogEntryModuleSessionStaged\(')
) 'Auto ranged staging must not keep the temporary per-query staged-session trace log in the hot path.'

Assert-True (
    ($diagnosticsText -match 'LogEntryModuleSessionResolution\(') -and
    ($diagnosticsText -match 'event=entry_module_session_resolution') -and
    ($diagnosticsText -match 'AttackExecutionThrottled\(') -and
    ($shootVerbText -match 'HostModuleSession\s*!=\s*null\s*&&\s*stagedEntryModuleSession\s*!=\s*null') -and
    ($shootVerbText -match 'resident_over_staged_conflict')
) 'Entry session resolution must log the suspicious resident-over-staged conflict without changing selection order.'

Assert-True (
    ($diagnosticsText -match 'LogEntryModuleSessionCleared\(') -and
    ($diagnosticsText -match 'event=entry_module_session_cleared') -and
    ($shootVerbText -match 'LogEntryModuleSessionCleared\(')
) 'Entry staging cleanup must log when a staged module session is discarded.'

Assert-True (
    $shootVerbText -notmatch '\u9366|\u7487|\u59AF|\u6D7C|\u93C6|\u7039'
) 'BdpVerb_Shoot.cs contains mojibake Chinese comments. Read and write this source as UTF-8.'

Write-Output 'RangedEntrySessionDiagnosticsSmokeTests PASS'
