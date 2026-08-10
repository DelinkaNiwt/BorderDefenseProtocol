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

# 事项08退役边界：人工校验芯片退役，正式形态底层检查继续保留。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$devHarnessDefsRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness\1.6\Defs'
$probeDefName = 'BDP_ChipModeRuntimeProbe'

$activeXmlFiles = Get-ChildItem -LiteralPath $devHarnessDefsRoot -Recurse -File -Filter '*.xml'
$remainingReferences = @(
    $activeXmlFiles |
        Select-String -SimpleMatch $probeDefName |
        ForEach-Object { $_.Path }
)

Assert-True ($remainingReferences.Count -eq 0) (
    'The retired chip-mode runtime probe must not remain in active DevHarness defs. Found: ' +
    ($remainingReferences -join ', '))

$formalModeTests = @(
    'ChipExpressionModeDefinitionBoundarySmokeTests.ps1'
    'ChipExpressionModeResolutionSmokeTests.ps1'
    'TriggerChipModeRuntimeSmokeTests.ps1'
    'ChipModeGizmoContentSmokeTests.ps1'
)

foreach ($formalModeTest in $formalModeTests) {
    Assert-True (Test-Path -LiteralPath (Join-Path $PSScriptRoot $formalModeTest) -PathType Leaf) `
        ('The formal chip-mode regression test must remain: ' + $formalModeTest)
}

Write-Output 'DevHarnessChipModeProbeRetirementSmokeTests PASS'
