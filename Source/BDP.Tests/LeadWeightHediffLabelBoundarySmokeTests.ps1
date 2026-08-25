$ErrorActionPreference = 'Stop'

$modRoot = Join-Path $PSScriptRoot '..\..'
$sourcePath = Join-Path $modRoot 'Source\BDP.Content\RangedModules\Debuff\Hediff_LeadWeight.cs'
$hediffPath = Join-Path $modRoot '1.6\Content\Defs\HediffDef\RangedDebuff.xml'

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw '缺少铅块负重专用 Hediff 标签实现。'
}

if (-not (Test-Path -LiteralPath $hediffPath)) {
    throw '缺少铅块负重 Hediff XML。'
}

$sourceText = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
$hediffText = Get-Content -LiteralPath $hediffPath -Raw -Encoding UTF8

foreach ($required in @(
        'public sealed class Hediff_LeadWeight : HediffWithComps',
        'public override string LabelBase',
        'base.LabelBase',
        'Mathf.Clamp01(Severity)',
        'ToStringPercent("0")')) {
    if (-not $sourceText.Contains($required)) {
        throw ('铅块负重标签实现缺少约束：' + $required)
    }
}

if ($hediffText -notmatch '<hediffClass>BDP\.Content\.RangedModules\.Debuff\.Hediff_LeadWeight</hediffClass>') {
    throw '铅块负重 HediffDef 没有绑定专用 Hediff 类。'
}

if ($hediffText -notmatch '(?s)<li>\(0, 1\)</li>.*?<li>\(1, 0\)</li>') {
    throw '铅块负重的严重度与移动速度因子映射已偏离：严重度必须直接表示移动速度降低比例。'
}

Write-Output 'LeadWeightHediffLabelBoundarySmokeTests PASS'
