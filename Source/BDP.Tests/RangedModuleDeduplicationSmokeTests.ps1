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
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Runtime\RangedAttackModuleResolver.cs'
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8

Assert-True (
    $resolverText -match 'HashSet<string>\s+seenModuleKeys\s*=\s*new HashSet<string>\(StringComparer\.Ordinal\)'
) 'RangedModuleResolver must keep a per-session ordinal module-definition key set.'

Assert-True (
    $resolverText -match 'string\s+moduleKey\s*=\s*mount\.moduleDef\.defName'
) 'RangedModuleResolver must identify duplicate modules by moduleDef.defName.'

Assert-True (
    $resolverText -match 'seenModuleKeys\.Contains\(moduleKey\)[\s\S]{0,120}continue;'
) 'RangedModuleResolver must skip a module definition already resolved in the current source lane.'

Assert-True (
    $resolverText -notmatch 'seenModuleKeys\.Contains\(mount\.moduleDef\.runtimeClass\)'
) 'RangedModuleResolver must not deduplicate by runtimeClass.'

$slotAddIndex = $resolverText.IndexOf('slots.Add(')
$keyAddIndex = $resolverText.IndexOf('seenModuleKeys.Add(moduleKey)')
Assert-True (
    $slotAddIndex -ge 0 -and $keyAddIndex -gt $slotAddIndex
) 'RangedModuleResolver must mark a module key only after runtime slot creation succeeds.'

Write-Output 'RangedModuleDeduplication PASS'
