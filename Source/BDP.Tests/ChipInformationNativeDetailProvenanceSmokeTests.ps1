$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$publisherPath = Join-Path $repoRoot '模组工程\BorderDefenseProtocol\Source\BDP.Content\Information\ManufacturedChipInfoCardPublisher.cs'
$languagePath = Join-Path $repoRoot '模组工程\BorderDefenseProtocol\Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml'

$publisherText = Get-Content -LiteralPath $publisherPath -Raw -Encoding UTF8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8

if ($publisherText -notmatch 'BuildFieldReport\(') {
    throw '原版信息条目的详情报告必须经过统一的字段详情构建器。'
}
if ($publisherText -notmatch 'field\.Provenance') {
    throw '字段详情构建器必须读取基础值、构型贡献等来源链。'
}
if ($publisherText -notmatch 'TryFormatProvenanceValue') {
    throw '字段来源详情必须沿用统一值格式化，而不是直接拼接原始数字。'
}
if ($languageText -notmatch 'BDP_Info_Report_FieldFinalValue') {
    throw '字段详情必须有最终值语言键。'
}
if ($languageText -notmatch 'BDP_Info_Report_FieldProvenance') {
    throw '字段详情必须有来源明细语言键。'
}
if ($languageText -notmatch 'BDP_Info_Report_Contribution_') {
    throw '字段详情必须有基础值、构型修正等贡献类型语言键。'
}

Write-Output 'ChipInformationNativeDetailProvenanceSmokeTests PASS'
