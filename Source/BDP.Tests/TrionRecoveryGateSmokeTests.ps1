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
$coreTrionRoot = Join-Path $sourceRoot 'BDP\Core\Trion'

$gatePath = Join-Path $coreTrionRoot 'ITrionRecoveryGate.cs'
$registryPath = Join-Path $coreTrionRoot 'TrionRecoveryGateRegistry.cs'
$compPath = Join-Path $coreTrionRoot 'CompTrion.cs'
$readerPath = Join-Path $coreTrionRoot 'ITrionReader.cs'
$servicePath = Join-Path $coreTrionRoot 'TrionService.cs'

Assert-True (Test-Path -LiteralPath $gatePath) 'Core must expose the neutral Trion recovery gate contract.'
Assert-True (Test-Path -LiteralPath $registryPath) 'Core must expose the Trion recovery gate registry.'
Assert-True (Test-Path -LiteralPath $readerPath) 'Core must expose the Trion reader contract.'
Assert-True (Test-Path -LiteralPath $servicePath) 'Core must expose the Trion reader service.'

$gateText = Get-Content -LiteralPath $gatePath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding utf8
$compText = Get-Content -LiteralPath $compPath -Raw -Encoding utf8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8

Assert-True (
    ($gateText -match 'interface\s+ITrionRecoveryGate') -and
    ($gateText -match 'float\s+GetRecoveryMultiplier\(Pawn\s+pawn\)')
) '中性恢复门控只暴露角色恢复倍率查询。'

Assert-True (
    ($registryText -match 'ITrionRecoveryGate') -and
    ($registryText -match 'GetRecoveryMultiplier\(Pawn\s+pawn\)') -and
    ($registryText -match 'return\s+1f')
) '未注册提供器时，恢复注册表必须保持基准倍率。'

Assert-True (
    ($compText -match 'TrionRecoveryGateRegistry\.GetRecoveryMultiplier\(parent\s+as\s+Pawn\)') -and
    ($compText -match 'return\s+RecoveryPerDay\s*\*\s*multiplier') -and
    ($compText -match 'EffectiveRecoveryPerDay') -and
    ($compText -match 'if\s*\(currentTick\s*>=\s*recoveryTick\)') -and
    ($compText -match 'recoveryTick\s*=\s*currentTick\s*\+')
) '角色必须到恢复间隔才查询许可，停运或冻结时也推进下一次结算时间。'

Assert-True (
    ($readerText -match 'float\s+EffectiveRecoveryPerDay\s*\{\s*get;\s*\}') -and
    ($serviceText -match 'public\s+float\s+EffectiveRecoveryPerDay') -and
    ($serviceText -match 'owner\.EffectiveRecoveryPerDay')
) 'The formal Trion reader must expose the effective recovery rate after permission and frozen-state checks.'

Assert-True (
    $compText -notmatch 'frozen\s*=.*GetRecoveryMultiplier|Frozen.*GetRecoveryMultiplier'
) 'The recovery gate must not overwrite CompTrion Frozen state.'

Write-Output 'TrionRecoveryGate PASS'
