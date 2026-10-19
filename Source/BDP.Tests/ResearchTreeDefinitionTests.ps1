# 检查研究图、布局、共同解锁、中文注入与原版入口配置。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'TestSupport/ChipManufacturingTestSupport.ps1')
$modRoot = Get-BdpModRoot
$defs = Join-Path $modRoot '1.6/Content/Defs'
$language = Join-Path $modRoot 'Languages/ChineseSimplified (简体中文)/DefInjected'
$researchXml = [xml](Get-Utf8Text (Join-Path $defs 'ResearchProjectDef/BDP_Research.xml'))
$research = @($researchXml.Defs.ResearchProjectDef)
Assert-True ($research.Count -eq 18) '必须保留18项研究，不能把共同产物变成独立研究。'
$byName = @{}
foreach ($project in $research) { $byName[[string]$project.defName] = $project }
$native = @('Electricity','MicroelectronicsBasics','Fabrication')
function ResearchName([string]$Suffix) { 'BDP_Research_' + $Suffix }
function Assert-Requirements($Node, [string[]]$Expected, [string]$Description)
{
    $actual = @($Node.researchPrerequisites.li | ForEach-Object { [string]$_ })
    Assert-True ((($actual | Sort-Object) -join ',') -eq (($Expected | Sort-Object) -join ',')) $Description
}
# 逐层移除可达节点，遇到错误引用或循环会失败。
$remaining = [Collections.Generic.HashSet[string]]::new([string[]]@($byName.Keys))
$finished = [Collections.Generic.HashSet[string]]::new([string[]]$native)
while ($remaining.Count -gt 0)
{
    $ready = @($remaining | Where-Object {
        $pending = @($byName[$_].prerequisites.li | Where-Object { -not $finished.Contains([string]$_) })
        $pending.Count -eq 0
    })
    Assert-True ($ready.Count -gt 0) '研究前置存在循环或错误引用。'
    foreach ($name in $ready) { [void]$remaining.Remove($name); [void]$finished.Add($name) }
}
Assert-True ('Electricity' -in $byName[(ResearchName 'TrionTheory')].prerequisites.li) '电力必须接01。'
Assert-True ('MicroelectronicsBasics' -in $byName[(ResearchName 'BasicTriggerEngineering')].prerequisites.li) '微电子基础必须接03。'
Assert-True ('MicroelectronicsBasics' -notin $byName[(ResearchName 'SupportFacilities')].prerequisites.li) '支援设施不再重复微电子基础直接前置。'
Assert-True ([string]$byName[(ResearchName 'PrototypeEngineering')].researchViewX -eq [string]$byName[(ResearchName 'PortableTalentDetection')].researchViewX) '02与04必须并列。'
$branches = @('ConventionalArmamentForms','MorphingTechnology','ExplosiveTechnology','MaterializationTechnology','BasicTacticalFunctions','DefensivePrinciples','TrajectoryControl','EnhancedOptionalFunctions','SpatialMobility','SupportFacilities')
$columns = @($branches | ForEach-Object { [string]$byName[(ResearchName $_)].researchViewX } | Select-Object -Unique)
Assert-True ($columns.Count -eq 1) '03后的主要分支必须同列。'
foreach ($project in $research)
{
    Assert-True ($project.tab -eq 'BDP_ResearchTab') 'BDP研究必须在独立原版分页。'
    Assert-True ([double]$project.researchViewY -ge 0 -and [double]$project.researchViewY -le 6.5) '坐标超出原版纵向范围。'
    foreach ($other in $research)
    {
        if ($project -eq $other) { continue }
        $dx = [Math]::Abs([double]$project.researchViewX - [double]$other.researchViewX)
        $dy = [Math]::Abs([double]$project.researchViewY - [double]$other.researchViewY)
        Assert-True ($dx -ge 0.5 -or $dy -ge 0.5) '研究节点可能重叠。'
    }
}
# 检查全部新增研究文案能由简体中文包注入。
$translation = [xml](Get-Utf8Text (Join-Path $language 'ResearchProjectDef/BDP_Research.xml'))
foreach ($project in $research)
{
    Assert-True ($null -eq $project.customUnlockTexts) '解锁条目由真实定义生成，不得重复写说明文本。'
    foreach ($field in @('label','description'))
    {
        $key = [string]$project.defName + '.' + $field
        Assert-True ($null -ne $translation.LanguageData.SelectSingleNode($key)) "缺少中文研究文案：$key"
    }
}
$presets = [xml](Get-Utf8Text (Join-Path $defs 'ChipManufacturingPresetDef/Presets.xml'))
function FunctionNode([string]$Name) { $presets.Defs.ChildNodes | Where-Object { $_.defName -eq ('BDP_ChipFunction_' + $Name) } | Select-Object -First 1 }
Assert-Requirements (FunctionNode 'Thruster') @((ResearchName 'EnhancedOptionalFunctions'),(ResearchName 'DefensivePrinciples'),(ResearchName 'MorphingTechnology'),(ResearchName 'SpatialMobility')) '推进器必须满足四项共同条件。'
Assert-Requirements (FunctionNode 'LeadShotModified') @((ResearchName 'ExperimentalEngineering'),(ResearchName 'ConventionalArmamentForms'),(ResearchName 'EnhancedOptionalFunctions')) '铅弹·改必须满足18＋05＋14，不能要求16。'
Assert-Requirements (FunctionNode 'LightSoul') @((ResearchName 'DefensivePrinciples'),(ResearchName 'MorphingTechnology')) '光魂必须满足防护＋形变。'
Assert-Requirements (FunctionNode 'EnergyShield_Auto') @((ResearchName 'SpecialTacticalFunctions'),(ResearchName 'DefensivePrinciples')) '自动护盾必须满足特殊＋防护。'
Assert-Requirements (FunctionNode 'EmergencyEscape') @((ResearchName 'SpecialTacticalFunctions'),(ResearchName 'SpatialMobility')) '脱离芯片必须满足特殊＋机动。'
foreach ($name in @('EnergyShield_Auto','EmergencyEscape')) { Assert-True ((FunctionNode $name).showInManufacturing -eq 'true') '已研究的特殊芯片必须可制造。' }
foreach ($name in @('Alektor','Fujin','FujinRangedStrike')) { Assert-True ((FunctionNode $name).showInManufacturing -eq 'false') '黑触发器专属功能不得开放制造。' }
$forms = [xml](Get-Utf8Text (Join-Path $defs 'ChipArmamentFormDef/Presets.xml'))
$shotgun = $forms.Defs.ChildNodes | Where-Object { $_.defName -eq 'BDP_ArmamentForm_Shotgun' } | Select-Object -First 1
Assert-Requirements $shotgun @((ResearchName 'ConventionalArmamentForms')) '霰弹必须归常规构型。'
$beacon = [xml](Get-Utf8Text (Join-Path $defs 'ThingDef/Buildings/CombatBody.xml'))
Assert-Requirements $beacon.Defs.ThingDef @('Electricity',(ResearchName 'SpecialTacticalFunctions'),(ResearchName 'SpatialMobility'),(ResearchName 'SupportFacilities')) '信标需要三项共同研究并保留电力。'
$core = [xml](Get-Utf8Text (Join-Path $defs 'ThingDef/Items/TrionDriveCore.xml'))
$analysis = $core.Defs.ThingDef.comps.li
Assert-True ($analysis.Class -eq 'CompProperties_CompAnalyzableUnlockResearch') '首次分析必须使用原版组件。'
Assert-True ($analysis.requiresMechanitor -eq 'false' -and $analysis.destroyedOnAnalyzed -eq 'false') '普通殖民者可以分析，分析后保留材料。'
Assert-True ($byName[(ResearchName 'TrionTheory')].requiredAnalyzed.li -eq 'BDP_TrionDriveCore') '01必须使用核心分析要求。'
# 所有制造条件都引用存在的研究定义，且排除未开放研究的绕过路径。
foreach ($file in Get-ChildItem -LiteralPath $defs -Recurse -Filter '*.xml')
{
    $xml = [xml](Get-Utf8Text $file.FullName)
    foreach ($node in $xml.SelectNodes('//researchPrerequisites/li'))
    {
        $name = $node.InnerText
        if ($name.StartsWith('BDP_')) { Assert-True ($byName.ContainsKey($name)) "错误研究引用：$name" }
    }
}
$source = Join-Path $modRoot 'Source/BDP.Content/Assembly/ChipManufacturing'
foreach ($file in @('UI/ChipBillQueueOperations.cs','Bill/Bill_ChipProduction.cs'))
{
    Assert-True ((Get-Utf8Text (Join-Path $source $file)) -match 'ChipManufacturingResearch.IsUnlocked') '提交和执行账单必须检查研究。'
}
foreach ($file in @('Resolution/ChipCombinationResolver.cs','Services/ChipProductCreationService.cs'))
{
    # 生成服务位置可能不同，按文件名定位现有正式代码。
    $name = Split-Path -Leaf $file
    $path = Get-ChildItem -LiteralPath $source -Recurse -Filter $name | Select-Object -First 1
    Assert-True ($null -ne $path) "缺少现有组合或生成服务：$name"
    Assert-True ((Get-Utf8Text $path.FullName) -notmatch 'ChipManufacturingResearch|ResearchFinished') '剧情生成与装备组合解析不得受玩家研究限制。'
}
Write-Host 'PASS：18项原版研究前置无环，布局并列，联动解锁与中文注入正确，剧情生成不受研究限制。'
