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

# 事项08退役边界：教学骨架不得继续存在于活动内容、现行测试或现行说明。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$mainModRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainModRoot
$devHarnessRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness'

$checkedPaths = @(
    Join-Path $devHarnessRoot '1.6\Defs'
    Join-Path $devHarnessRoot 'Source\BDP.DevHarness'
    Join-Path $PSScriptRoot 'RangedModuleComboMountSmokeTests.ps1'
    Join-Path $PSScriptRoot 'RangedModuleSampleModulesSmokeTests.ps1'
    Join-Path $mainModRoot 'docs\plans\2026-04-09-BDP远程攻击业务模块编写标准.md'
    Join-Path $mainModRoot 'docs\需求说明\2026-04-10-BDP远程模块编写说明书-第一版.md'
)

$retiredTerms = @(
    'RangedTeachingSkeletonModule'
    'RangedTeachingSkeletonConfig'
    'BDP_TestRangedTeachingSkeletonModule'
    'BDP_TestCombo_RangedVolleyTeachingSkeleton'
    'test_combo_ranged_volley_teaching_skeleton'
)

$remainingReferences = [System.Collections.Generic.List[string]]::new()
foreach ($checkedPath in $checkedPaths) {
    $files = if (Test-Path -LiteralPath $checkedPath -PathType Container) {
        Get-ChildItem -LiteralPath $checkedPath -Recurse -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
    else {
        Get-Item -LiteralPath $checkedPath
    }

    foreach ($file in $files) {
        foreach ($term in $retiredTerms) {
            if (Select-String -LiteralPath $file.FullName -SimpleMatch $term -Quiet) {
                $remainingReferences.Add($file.FullName + ' -> ' + $term)
            }
        }
    }
}

Assert-True ($remainingReferences.Count -eq 0) (
    'The retired ranged teaching skeleton must not remain in active content or current guidance. Found: ' +
    ($remainingReferences -join ', '))

Write-Output 'DevHarnessRangedTeachingSkeletonRetirementSmokeTests PASS'
