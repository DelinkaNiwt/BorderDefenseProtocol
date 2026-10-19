$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mergeRoot = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution'
$configMerge = Get-Content -LiteralPath (Join-Path $mergeRoot 'ChipConfigurationMergeService.cs') -Raw -Encoding UTF8
$featureMerge = Get-Content -LiteralPath (Join-Path $mergeRoot 'ChipFeatureMergeService.cs') -Raw -Encoding UTF8
$context = Get-Content -LiteralPath (Join-Path $mergeRoot 'ChipFeatureCloneContext.cs') -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($configMerge -match 'Features\s*=\s*ChipFeatureMergeService\.CloneFeatures') '单预设克隆必须复制 Features。'
Assert-True ($featureMerge -match 'Sources\s*=\s*CloneSources') '单预设克隆必须复制来源列表。'
Assert-True ($featureMerge -match 'Children\s*=\s*CloneFeatures') '单预设克隆必须递归复制子功能。'
Assert-True ($featureMerge -match 'CloneForRemap\(') '来源配置必须通过统一入口复制并重映射条目。'
Assert-True ($featureMerge -match 'Groups\s*=\s*CloneGroups') '单预设克隆必须递归复制信息组。'
Assert-True ($featureMerge -match 'Subgroups\s*=\s*CloneSubgroups') '单预设克隆必须复制信息组的一级子组。'
Assert-True ($context -match 'RegisterEntry' -and $context -match 'RegisterFeature' -and $context -match 'RegisterSource') '复制上下文必须集中维护三类 Id 映射。'

Write-Host 'ChipFeatureManufacturingClone PASS'
