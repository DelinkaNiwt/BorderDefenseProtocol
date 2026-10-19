$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$oldType = 'Chip' + 'FunctionDef'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing'
$presetSource = Join-Path $contentRoot 'Defs\ChipManufacturingPresetDef.cs'
$oldSource = Join-Path $contentRoot ("Defs\$oldType.cs")
$presetXml = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$oldPresetXml = Join-Path $repoRoot ("1.6\Content\Defs\$oldType\Presets.xml")
$languageXml = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef\Presets.xml'
$oldLanguageXml = Join-Path $repoRoot ("Languages\ChineseSimplified (简体中文)\DefInjected\$oldType\Presets.xml")

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True (Test-Path -LiteralPath $presetSource) '制造预设 C# 类型文件必须存在。'
Assert-True (-not (Test-Path -LiteralPath $oldSource)) '旧 ChipFunctionDef C# 类型不得继续存在。'
Assert-True (Test-Path -LiteralPath $presetXml) '制造预设 XML 必须迁移到 ChipManufacturingPresetDef 目录。'
Assert-True (-not (Test-Path -LiteralPath $oldPresetXml)) '旧 ChipFunctionDef XML 目录不得继续承载正式预设。'
Assert-True (Test-Path -LiteralPath $languageXml) '制造预设语言文件必须迁移到新目录。'
Assert-True (-not (Test-Path -LiteralPath $oldLanguageXml)) '旧 ChipFunctionDef 语言目录不得继续承载正式预设。'

$sourceText = (Get-ChildItem -LiteralPath $contentRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw
}) -join "`n"
Assert-True ($sourceText -match 'ChipManufacturingPresetDef') '制造代码必须使用明确的制造预设类型。'
Assert-True ($sourceText -notmatch ('\b' + [regex]::Escape($oldType) + '\b')) '制造代码不得继续定义或使用 ChipFunctionDef 类型。'

$xmlText = Get-Content -LiteralPath $presetXml -Raw
Assert-True ($xmlText -notmatch ('ChipManufacturing\.Defs\.' + [regex]::Escape($oldType))) '正式预设 XML 不得继续使用旧 C# 类型节点。'
Assert-True ($xmlText -match 'ChipManufacturing\.Defs\.ChipManufacturingPresetDef') '正式预设 XML 必须使用新 C# 类型节点。'

Write-Host 'ChipManufacturingPresetRename PASS'
