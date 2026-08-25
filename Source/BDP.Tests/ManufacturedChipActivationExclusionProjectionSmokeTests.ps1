# 动态成品芯片互斥组投影回归测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$readerPath = Join-Path $modRoot "Source\BDP\Core\Chips\Access\ChipDefinitionReaderSurface.cs"
$readerText = Get-Utf8Text $readerPath

Assert-True (
    $readerText -match 'ActivationExclusionGroups\s*=\s*loadout\s*!=\s*null[\s\S]*?loadout\.ActivationExclusionGroups[\s\S]*?new\s+List<ChipExclusionGroupDef>'
) '动态成品配置必须把 Loadout.ActivationExclusionGroups 投影到运行时契约。'

Assert-True (
    $readerText -match 'ActivationExclusionGroups\s*=\s*loadout\s*!=\s*null'
) '动态成品契约必须显式拥有激活互斥组投影字段。'

Write-Host 'PASS: 动态成品芯片的激活互斥组投影已固定。'
