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
$coreRoot = Join-Path $sourceRoot 'BDP\Core'
$interpreterPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionContractInterpreter.cs'
$entryContractPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionEntryContract.cs'
$readerPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerLoadoutReader.cs'

foreach ($path in @($interpreterPath, $entryContractPath, $readerPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('姿态解析设施缺少文件：' + $path)
}

$interpreterText = Get-Content -LiteralPath $interpreterPath -Raw -Encoding UTF8
$entryContractText = Get-Content -LiteralPath $entryContractPath -Raw -Encoding UTF8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding UTF8

Assert-True (
    $readerText -match 'string\s+GetChipStanceKey\(Thing chip\)'
) '表达解释读取面必须能读取芯片当前姿态。'

Assert-True (
    $interpreterText -match 'ResolveUncached\(\s*ChipExpressionConfig config,\s*string currentModeKey,\s*string currentStanceKey\s*\)' -and
    $interpreterText -match 'ResolveEffectiveStanceKey' -and
    $interpreterText -match 'mode\.ActiveEntryIds' -and
    $interpreterText -match 'stance\.ActiveEntryIds'
) '解释器必须按当前形态和姿态解析，并合并形态公共条目与姿态条目。'

Assert-True (
    ($entryContractText -match 'string\s+ModeKey') -and
    ($entryContractText -match 'string\s+StanceKey')
) '最终表达条目必须同时标记所属形态和姿态。'

Assert-True (
    ($interpreterText -match 'currentStanceKey') -and
    ($interpreterText -match 'DefaultStanceKey') -and
    ($interpreterText -match '已回退默认姿态')
) '空白或无效姿态必须回退到当前形态自己的默认姿态，并保留诊断。'

Write-Output 'ChipNestedStanceResolutionSmokeTests PASS'
