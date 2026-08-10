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

# 事项08退役边界：故意非法芯片不得继续存在于游戏读取目录或候选运行源码。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$devHarnessRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness'
$retiredDefName = 'BDP_TestChipInvalidDefinition'

$activeFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $devHarnessRoot '1.6\Defs') -Recurse -File -Filter '*.xml'
    Get-ChildItem -LiteralPath (Join-Path $devHarnessRoot 'Source\BDP.DevHarness') -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
)

$remainingReferences = @(
    $activeFiles |
        Select-String -SimpleMatch $retiredDefName |
        ForEach-Object { $_.Path }
)

Assert-True ($remainingReferences.Count -eq 0) (
    'The retired invalid chip must not remain in active DevHarness definitions or runtime source. Found: ' +
    ($remainingReferences -join ', '))

Write-Output 'DevHarnessInvalidChipRetirementSmokeTests PASS'
