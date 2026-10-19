$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$coreRoot = Join-Path $modRoot 'Source\BDP\Core\Information'
$contentRoot = Join-Path $modRoot 'Source\BDP.Content'
$parser = Get-Content (Join-Path $coreRoot 'ChipInformationParser.cs') -Raw -Encoding UTF8
$readContext = Get-Content (Join-Path $coreRoot 'Features\Sources\ChipFeatureReadContext.cs') -Raw -Encoding UTF8
$contextProjector = Get-Content (Join-Path $coreRoot 'Features\Resolution\ChipFeatureContextProjector.cs') -Raw -Encoding UTF8
$featureResolver = Get-Content (Join-Path $coreRoot 'Features\Resolution\ChipFeatureResolver.cs') -Raw -Encoding UTF8
$modeValidation = Get-Content (Join-Path $modRoot 'Source\BDP\Core\Expressions\Validation\ChipExpressionStructureValidation.cs') -Raw -Encoding UTF8
$comboValidation = Get-Content (Join-Path $modRoot 'Source\BDP\Core\Combos\Validation\ComboDefinitionValidator.cs') -Raw -Encoding UTF8
$sourceSupport = Get-Content (Join-Path $contentRoot 'Information\Features\Workers\ChipFeatureSourceWorkerSupport.cs') -Raw -Encoding UTF8
$shieldWorker = Get-Content (Join-Path $contentRoot 'Information\Features\Workers\ShieldFeatureSourceWorker.cs') -Raw -Encoding UTF8
$renderer = Get-Content (Join-Path $contentRoot 'Assembly\Window\ChipInformationDetailRenderer.cs') -Raw -Encoding UTF8
$modelBuilder = Get-Content (Join-Path $contentRoot 'Information\ManufacturedChipInformationModelBuilder.cs') -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($readContext -match '(?s)if \(activeEntryIds == null\).*?ActiveEntryIds = null;') `
    '未传入上下文与显式空活动条目集必须保留为两种不同状态。'
Assert-True ($contextProjector -match 'CollectEntryIds\(group\.Subgroups' -and $contextProjector -match 'CollectEntryIds\(declaration\.Subgroups') `
    '功能组和子组来源必须参与功能声明的唯一范围计算。'
Assert-True ($contextProjector -match 'CollectEntryIds' -and $contextProjector -match 'FindCommonScope') `
    '每项功能必须按其来源条目归属到最近共同范围，只投影一次。'
Assert-True ($parser -match 'projection\.RootFeatures' -and $parser -match 'hasModeScopes') `
    '有形态树时根级功能必须来自唯一范围投影；无形态树仍走根级解析。'
Assert-True ($featureResolver -match '(?s)if \(sources == null \|\| sources\.Count == 0\).*?return false;') `
    '无来源功能声明的可达性必须由其分组或子功能决定。'
Assert-True ($featureResolver -match '(?s)if \(activeEntryIds == null\).*?return true;') `
    '显式空活动条目集不得被当作无限制解析。'
Assert-True ($featureResolver -match '(?s)private static bool IsGroupDeclarationActive.*?if \(activeEntryIds == null\)\s*\{\s*return true;\s*\}' -and $featureResolver -notmatch 'activeEntryIds == null \|\| activeEntryIds\.Count == 0') `
    '空活动条目集不得保留仅由姿态专属子组支撑的空信息组。'
Assert-True ($sourceSupport -match '(?s)if \(context\?\.ActiveEntryIds == null\).*?return true;') `
    '标准来源读取器必须区分未提供上下文和空活动条目上下文。'
Assert-True ($shieldWorker -match '(?s)if \(activeEntryIds == null\).*?return true;') `
    '护盾聚合规则必须区分未提供上下文和空活动条目上下文。'
Assert-True ($renderer -match '(?s)activeContext != null\s*\? activeContext\.Features\s*:\s*model\.Features') `
    '选中的空形态/姿态必须保持为空，不得回退到根级聚合功能。'
Assert-True ($modelBuilder -match '(?s)runtime != null && runtime\.HasRuntimeState.*?ChipInformationRuntimeInputAdapter\.Create\(') `
    '成品信息模型必须保留调用方提供的当前形态/姿态运行时状态。'
Assert-True ($modeValidation -match "mode\.ModeKey\.IndexOf\('/'\)" -and $modeValidation -match "stance\.StanceKey\.IndexOf\('/'\)") `
    '形态键和姿态键不得含路径分隔符，避免与形态/姿态复合键冲突。'
Assert-True ($comboValidation -match "scope\.ModeKey\.IndexOf\('/'\)" -and $comboValidation -match "scope\.StanceKey\.IndexOf\('/'\)") `
    '组合技来源范围键也不得含路径分隔符，确保静态引用可无歧义定位。'

Write-Output 'ChipFeatureContextIsolation PASS'
