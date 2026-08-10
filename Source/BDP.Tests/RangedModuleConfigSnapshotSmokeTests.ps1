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
$configNodePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Config\RangedModuleConfigNode.cs'
$mountConfigPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Config\RangedModuleMountConfig.cs'
$runtimeHostPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Runtime\RangedAttackModuleRuntimeHost.cs'

$configNodeText = Get-Content -LiteralPath $configNodePath -Raw -Encoding utf8
$mountConfigText = Get-Content -LiteralPath $mountConfigPath -Raw -Encoding utf8
$runtimeHostText = Get-Content -LiteralPath $runtimeHostPath -Raw -Encoding utf8

Assert-True (
    ($configNodeText -notmatch 'MemberwiseClone\(') -and
    ($configNodeText -match 'Clone')
) 'RangedModuleConfigNode must not use MemberwiseClone as the protocol default snapshot rule.'

Assert-True (
    ($mountConfigText -match 'config\s*=\s*config\s*!=\s*null\s*\?\s*config\.Clone\(\)\s*:\s*null')
) 'RangedModuleMountConfig must still freeze config through the shared clone entry.'

Assert-True (
    $runtimeHostText -match 'CloneMounts'
) 'RangedAttackModuleRuntimeHost must keep freezing mount snapshots for runtime sessions.'

Write-Output 'RangedModuleConfigSnapshotSmokeTests PASS'
