$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$clonePath = Join-Path $sourceRoot 'BDP.Content\Assembly\ChipManufacturing\Resolution\ChipExpressionMergeService.cs'
$cloneText = Get-Content -LiteralPath $clonePath -Raw -Encoding UTF8

Assert-True (
    ($cloneText -match 'CloneStances') -and
    ($cloneText -match 'DefaultStanceKey\s*=\s*mode\.DefaultStanceKey') -and
    ($cloneText -match 'Stances\s*=\s*CloneStances\(mode\.Stances\)')
) '制造单动作克隆必须完整保留形态内的默认姿态和姿态列表。'

Assert-True (
    ($cloneText -match 'DisplayLabelKey\s*=\s*mode\.DisplayLabelKey') -and
    ($cloneText -match 'DisplayLabelKey\s*=\s*stance\.DisplayLabelKey')
) '制造克隆必须保留形态和姿态的语言包显示键。'

Write-Output 'ChipStanceManufacturingCloneSmokeTests PASS'
