$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$contentRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $contentRoot
$languagePaths = @((Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\BlackTrigger.xml'),(Join-Path $modRoot '1.6\Development\Languages\ChineseSimplified (简体中文)\Keyed\BlackTriggerDevelopment.xml'))
$languageText = ($languagePaths | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object { Get-Content -LiteralPath $_ -Raw -Encoding utf8 }) -join "`n"
$sourceFiles = @((Get-ChildItem -LiteralPath (Join-Path $contentRoot 'BDP.Content\Trigger\Black\Formation') -Recurse -Filter '*.cs'),(Get-ChildItem -LiteralPath (Join-Path $contentRoot 'BDP.Development\Trigger\BlackFormation') -Recurse -Filter '*.cs'))
$keys = New-Object System.Collections.Generic.HashSet[string]
foreach ($file in $sourceFiles) { $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8; foreach ($match in [regex]::Matches($text, 'BDP_[A-Za-z0-9_]+')) { if ($match.Value -like 'BDP_BlackTrigger_*') { [void]$keys.Add($match.Value) } } }
$nonLanguageKeys = @('BDP_BlackTriggerFormationSettings','BDP_BlackTriggerLifeImprintExhausted')
foreach ($key in $keys) { if ($key -notin $nonLanguageKeys) { Assert-True ($languageText -match "<$key>") "语言包缺少黑触发器语言键：$key" } }
Assert-True ($languageText -notmatch '>[^<]*(献祭者|制造者|形成者|形成倾向|最终意志|献祭当前角色|未定型遗物)[^<]*<') '玩家语言包不得使用废弃概念。'
Assert-True ($languageText -notmatch '>[^<]*(Pawn|ThingDef|DefName|Tick|LoadID)[^<]*<') '玩家语言包不得显示技术标识。'
$formalSource = ($sourceFiles | Where-Object { $_.FullName -like '*BDP.Content*' } | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }) -join "`n"
Assert-True ($formalSource -notmatch '"[^"\r\n]*[\u4e00-\u9fff][^"\r\n]*"') '正式形成代码不得硬编码玩家可见中文。'
Write-Output 'BlackTriggerLanguageSmokeTests PASS'
exit 0
