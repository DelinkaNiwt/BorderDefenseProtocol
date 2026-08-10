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

$builderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs'
$resolvedVerbSpecPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ResolvedVerbSpec.cs'
$builderText = Get-Content -LiteralPath $builderPath -Raw -Encoding utf8
$resolvedVerbSpecText = if (Test-Path -LiteralPath $resolvedVerbSpecPath) {
    Get-Content -LiteralPath $resolvedVerbSpecPath -Raw -Encoding utf8
}
else {
    ''
}

$downstreamFiles = @(
    Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
    Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\MeleeAttackExecutionContext.cs'
    Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedAttackExecutionContext.cs'
    Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
)

$downstreamText = ($downstreamFiles | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object {
    Get-Content -LiteralPath $_ -Raw -Encoding utf8
}) -join "`n"

Assert-True (
    $builderText -notmatch 'MemberwiseClone'
) 'ExpressionSnapshotBuilder must not clone VerbProperties by MemberwiseClone.'

Assert-True (
    $builderText -notmatch 'forcedMissRadiusField'
) 'ExpressionSnapshotBuilder must not write forcedMissRadiusField through reflection.'

Assert-True (
    Test-Path -LiteralPath $resolvedVerbSpecPath
) 'Task 6 requires a typed ResolvedVerbSpec model.'

Assert-True (
    $resolvedVerbSpecText -match 'class\s+ResolvedVerbSpec|struct\s+ResolvedVerbSpec'
) 'ResolvedVerbSpec file must declare the typed resolved verb spec.'

Assert-True (
    $downstreamText -match '\bResolvedVerbSpec\b'
) 'ResolvedVerbSpec must be consumed downstream by runtime execution or verb hosts.'

Write-Output 'ExpressionResolvedVerbSpecBoundarySmokeTests PASS'
