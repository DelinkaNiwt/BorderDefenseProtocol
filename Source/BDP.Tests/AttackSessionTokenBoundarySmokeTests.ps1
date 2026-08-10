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

$tokenPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackSessionToken.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$meleeVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_MeleeAttackDamage.cs'
$postLoadRecoveryPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionPostLoadRecovery.cs'
$executionEntryPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\DefaultAttackExecutionEntry.cs'

$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8
$postLoadRecoveryText = Get-Content -LiteralPath $postLoadRecoveryPath -Raw -Encoding utf8
$executionEntryText = Get-Content -LiteralPath $executionEntryPath -Raw -Encoding utf8
$tokenText = if (Test-Path -LiteralPath $tokenPath) {
    Get-Content -LiteralPath $tokenPath -Raw -Encoding utf8
}
else {
    ''
}

Assert-True (
    Test-Path -LiteralPath $tokenPath
) 'Task 4 requires a single AttackSessionToken type.'

Assert-True (
    $tokenText -match 'class\s+AttackSessionToken|struct\s+AttackSessionToken'
) 'AttackSessionToken file must declare the AttackSessionToken type.'

Assert-True (
    $shootVerbText -notmatch '\bHostProjectionVersion\b'
) 'BdpVerb_Shoot must stop storing HostProjectionVersion directly.'

Assert-True (
    $meleeVerbText -notmatch '\bHostProjectionVersion\b'
) 'BdpVerb_MeleeAttackDamage must stop storing HostProjectionVersion directly.'

Assert-True (
    (($postLoadRecoveryText -match '\bAttackSessionToken\b') -or
     ($postLoadRecoveryText -match '\bHostSessionToken\b')) -and
    ($postLoadRecoveryText -notmatch '\bHostProjectionVersion\b')
) 'AttackExecutionPostLoadRecovery must validate against AttackSessionToken rather than detached projection-version fragments.'

Assert-True (
    ($executionEntryText -match '\bAttackSessionToken\b') -and
    ($executionEntryText -notmatch 'request\.ProjectionVersion')
) 'DefaultAttackExecutionEntry must validate request identity through AttackSessionToken rather than request.ProjectionVersion.'

Write-Output 'AttackSessionTokenBoundarySmokeTests PASS'
