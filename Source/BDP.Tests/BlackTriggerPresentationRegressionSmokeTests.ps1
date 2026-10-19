$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }

$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$emphasesPath = Join-Path $root '1.6\Content\Defs\BlackTrigger\Emphases.xml'
$emphasesXml = [xml](Get-Content -LiteralPath $emphasesPath -Raw -Encoding utf8)
$emphasisNodes = @($emphasesXml.Defs.ChildNodes | Where-Object { $_.NodeType -eq [System.Xml.XmlNodeType]::Element })
Assert-True ($emphasisNodes.Count -gt 0) '至少需要一个战斗侧重 Def。'
foreach ($node in $emphasisNodes) {
    Assert-True ($node.label -and -not [string]::IsNullOrWhiteSpace([string]$node.label)) "战斗侧重缺少基础 label：$($node.defName)"
}

$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$formatter = Get-Content -LiteralPath (Join-Path $formation 'Presentation\BlackTriggerImprintFormatter.cs') -Raw -Encoding utf8
$window = Get-Content -LiteralPath (Join-Path $formation 'Presentation\Window_BlackTriggerFormationDecision.cs') -Raw -Encoding utf8
$worldService = Get-Content -LiteralPath (Join-Path $formation 'World\BlackTriggerFormationWorldService.cs') -Raw -Encoding utf8
$language = Get-Content -LiteralPath (Join-Path $root 'Languages\ChineseSimplified (简体中文)\Keyed\BlackTrigger.xml') -Raw -Encoding utf8
Assert-True ($formatter -match 'TipString') '特性说明必须复用原版 TipString 进行人物语法解析。'
Assert-True ($formatter -match 'FormatTraitEffects') '特性同行信息必须保留原版机械效果摘要。'
Assert-True ($formatter -match 'NullOrEmpty') '生命印记统计值必须在边界处拒绝空文本。'
Assert-True ($window -match 'Text\.CalcHeight') '形成选项高度必须按实际文本高度计算。'
Assert-True ($window -match 'DrawEmphasisSelectable') '战斗侧重必须使用同行简介绘制。'
Assert-True ($window -match 'DrawTraitSelectable') '继承特性必须使用名称与机械效果同行绘制。'
Assert-True ($window -notmatch 'footerStatusRect|BuildMissingDecisionStatus|ResolveEmphasisEvidence') '窗口不得绘制匹配过程或底栏未决红色提示。'
Assert-True ($window -notmatch 'FormatTraitOption|Formation_MatchedFacts|Formation_OtherSkills') '窗口不得显示继承特性说明或匹配/其它资料分组。'
Assert-True ($worldService -match 'HeirLoadId\s*=\s*NoneSelection') '新形成事件必须默认选择不指定传承人。'
Assert-True ($language -match '<BDP_BlackTrigger_Formation_HeirTitle>选择传承人</BDP_BlackTrigger_Formation_HeirTitle>' `
    -and $language -match '<BDP_BlackTrigger_Formation_TraitTitle>选择继承特性</BDP_BlackTrigger_Formation_TraitTitle>') '标题不得继续显示可选括号提示。'
Assert-True ($window -match 'UI\.screenWidth|UI\.screenHeight') '形成窗口尺寸必须考虑实际屏幕空间。'

Write-Output 'BlackTriggerPresentationRegressionSmokeTests PASS'
exit 0
