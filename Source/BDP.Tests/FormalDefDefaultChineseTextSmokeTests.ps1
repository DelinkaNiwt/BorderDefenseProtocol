$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$defsRoot = Join-Path $modRoot '1.6\Content\Defs'

# 只检查会直接成为玩家文本的字段；稳定键、类型名、资源路径和枚举不属于翻译范围。
$translatableTags = @(
    'label',
    'description',
    'DisplayLabel',
    'jobString',
    'gerund',
    'reportString',
    'reportStringOverride',
    'verbLabel',
    'commandLabel',
    'inspectString',
    'baseInspectLine',
    'labelMale',
    'labelFemale',
    'labelShort',
    'labelNoun'
)
$englishDefaults = New-Object System.Collections.Generic.List[string]

foreach ($file in Get-ChildItem -LiteralPath $defsRoot -Filter '*.xml' -Recurse) {
    [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach ($tag in $translatableTags) {
        foreach ($node in $xml.SelectNodes('//' + $tag)) {
            $text = $node.InnerText.Trim()
            if ($text -match '[A-Za-z]' -and $text -notmatch '[\u4e00-\u9fff]') {
                $relativePath = $file.FullName.Substring($modRoot.Length + 1)
                $englishDefaults.Add($relativePath + ' <' + $tag + '> ' + $text)
            }
        }
    }
}

$failureMessage = "正式 Def 的可翻译字段仍含纯英文默认文本（共 $($englishDefaults.Count) 处）：`n" `
    + ($englishDefaults -join "`n")
Assert-True ($englishDefaults.Count -eq 0) $failureMessage

Write-Output 'FormalDefDefaultChineseTextSmokeTests PASS'
