# 对照原版 IncidentDef.ConfigErrors：事件引用的任务随机权重必须为零。
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$defs = Join-Path $modRoot '1.6/Content/Defs'
$questXml = [xml](Get-Content -LiteralPath (Join-Path $defs 'QuestScriptDef/RoyalArrival.xml') -Raw -Encoding UTF8)
$incidentXml = [xml](Get-Content -LiteralPath (Join-Path $defs 'IncidentDef/RoyalArrivalOpening.xml') -Raw -Encoding UTF8)
function Get-InheritedField($Node, [string]$Field)
{
    $value = $Node.SelectSingleNode($Field)
    if ($null -ne $value) { return $value.InnerText }
    if ($Node.ParentName)
    {
        $parent = $questXml.SelectSingleNode("/Defs/QuestScriptDef[@Name='$($Node.ParentName)']")
        if ($null -eq $parent) { throw '任务继承父定义不存在。' }
        return Get-InheritedField $parent $Field
    }
    return '0'
}
$target = [string]$incidentXml.Defs.IncidentDef.questScriptDef
$opening = $questXml.SelectSingleNode("/Defs/QuestScriptDef[defName='$target']")
if ($null -eq $opening) { throw '事件引用的首发任务定义不存在。' }
if ([float](Get-InheritedField $opening 'rootSelectionWeight') -ne 0) { throw '原版配置冲突：事件任务同时具有非零随机抽取权重。' }
$random = $questXml.SelectSingleNode('/Defs/QuestScriptDef[defName="BDP_RoyalArrival"]')
if ([float](Get-InheritedField $random 'rootSelectionWeight') -le 0) { throw '修复不得关闭后续随机再接触。' }
if ($opening -eq $random) { throw '首发与随机投放必须使用各自的任务入口定义。' }
if ([float](Get-InheritedField $random 'rootEarliestDay') -ne 8) { throw '随机再接触的第八天条件不得改变。' }
if ([float]((Get-InheritedField $opening 'expireDaysRange').Split('~')[0]) -le 0) { throw '任务邀请期限必须保留。' }
Write-Host 'PASS：首发任务不进入随机池，随机再接触保留，阶段与邀请条件保持。'
