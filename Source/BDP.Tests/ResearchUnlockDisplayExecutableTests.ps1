# 使用工作区程序集和原版分组器，验证实际配置的共同解锁显示。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'TestSupport/ChipManufacturingTestSupport.ps1')
$modRoot = Get-BdpModRoot
$managedRoot = 'C:/NiwtGames/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
foreach ($name in @('UnityEngine.CoreModule.dll','UnityEngine.dll','Assembly-CSharp.dll'))
{ [void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot $name)) }
[void][Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6/Assemblies/BDP.Core.dll'))
$content = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6/Assemblies/BDP.Content.dll'))
$displayType = $content.GetType('BDP.Content.Research.ResearchUnlockDisplay', $true)
$build = $displayType.GetMethod('Build')
$postfix = $content.GetType('BDP.Content.Patches.Patch_ResearchProjectDef_UnlockedDefs_ChipManufacturing', $true).GetMethod('Postfix')
$iconPrefix = $content.GetType('BDP.Content.Patches.Patch_Widgets_DefIcon_ChipManufacturing', $true).GetMethod('Prefix')
$functionType = $content.GetType('BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef', $true)
$formType = $content.GetType('BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef', $true)
$functions = [Activator]::CreateInstance([Collections.Generic.List``1].MakeGenericType($functionType))
$forms = [Activator]::CreateInstance([Collections.Generic.List``1].MakeGenericType($formType))
$projects = @{}
$tab = [RimWorld.ResearchTabDef]::new(); $tab.defName = 'BDP_ResearchTab'
# 原版研究坐标成员仅大小写不同，使用字段反射绕过PowerShell适配器的大小写冲突。
$nameField = [Verse.Def].GetField('defName')
$labelField = [Verse.Def].GetField('label')
$tabField = [Verse.ResearchProjectDef].GetField('tab')
# 原版Def按加载期生成的名称哈希判断相等；离线构造必须补上不同身份。
$hashField = [Verse.Def].GetField('defNameHash',[Reflection.BindingFlags]'Instance,Public,NonPublic')
$fixtureHash = 1
$defs = Join-Path $modRoot '1.6/Content/Defs'
$researchXml = [xml](Get-Utf8Text (Join-Path $defs 'ResearchProjectDef/BDP_Research.xml'))
foreach ($node in $researchXml.Defs.ResearchProjectDef)
{
    $project = [Verse.ResearchProjectDef]::new()
    $nameField.SetValue($project,[string]$node.defName)
    $hashField.SetValue($project,$fixtureHash++)
    $labelField.SetValue($project,[string]$node.defName); $tabField.SetValue($project,$tab)
    $projects[[string]$node.defName] = $project
    [Verse.DefDatabase[Verse.ResearchProjectDef]]::Add($project)
}
foreach ($entry in @(@('ChipManufacturingPresetDef/Presets.xml',$functionType,$functions), @('ChipArmamentFormDef/Presets.xml',$formType,$forms)))
{
    $xml = [xml](Get-Utf8Text (Join-Path $defs $entry[0]))
    foreach ($node in $xml.Defs.ChildNodes | Where-Object { $_.defName })
    {
        $item = [Activator]::CreateInstance($entry[1])
        $item.defName = [string]$node.defName; $item.label = [string]$node.label
        $hashField.SetValue($item,$fixtureHash++)
        $item.researchPrerequisites = [Collections.Generic.List[Verse.ResearchProjectDef]]::new()
        foreach ($name in $node.researchPrerequisites.li) { $item.researchPrerequisites.Add($projects[[string]$name]) }
        if ($node.showInManufacturing) { $item.showInManufacturing = [bool]::Parse($node.showInManufacturing) }
        if ($node.implicitDefault) { $item.implicitDefault = [bool]::Parse($node.implicitDefault) }
        $entry[2].Add($item)
        [Verse.DefDatabase``1].MakeGenericType($entry[1]).GetMethod('Add',[Type[]]@($entry[1])).Invoke($null,[object[]]@($item))
    }
}
# ThingDef构造会创建Unity贴图；这里只需要原版定义身份，不运行图形初始化。
$carrier = [Runtime.Serialization.FormatterServices]::GetUninitializedObject([Verse.ThingDef])
$nameField.SetValue($carrier,'BDP_Chip_Custom'); $labelField.SetValue($carrier,'carrier')
$hashField.SetValue($carrier,$fixtureHash++)
[Verse.DefDatabase[Verse.ThingDef]]::Add($carrier)
$building = [Runtime.Serialization.FormatterServices]::GetUninitializedObject([Verse.ThingDef])
$nameField.SetValue($building,'BDP_TestBuilding'); $labelField.SetValue($building,'building')
$hashField.SetValue($building,$fixtureHash++)
$original = [Collections.Generic.List[Verse.Def]]::new(); $original.Add($carrier); $original.Add($building); $original.Add($building)
$cache = [Verse.ResearchProjectDef].GetField('cachedUnlockedDefs',[Reflection.BindingFlags]'Instance,NonPublic')
foreach ($project in $projects.Values)
{
    $arguments = [object[]]@($project,$original)
    $postfix.Invoke($null,$arguments)
    $result = $arguments[1]
    Assert-True ($carrier -notin $result) '通用芯片载体不能冒充具体功能。'
    Assert-True (@($result | Where-Object { $_ -eq $building }).Count -eq 1) '保留并去重原版产物。'
    Assert-True ($original.Count -eq 3) '不得修改原版缓存列表。'
    Assert-True (-not ($result | Where-Object { $nameField.GetValue($_) -match 'Alektor|Fujin|TrionCube' })) '不得公开黑触发器或隐藏默认构型。'
    $cache.SetValue($project,$result)
}
# 原版依据各项目的实际解锁列表生成共同解锁分组，而非读取说明文本。
foreach ($name in @('LightSoul','Thruster','EnergyShield_Auto','EmergencyEscape','LeadShotModified'))
{
    $item = $functions | Where-Object { $_.defName -eq ('BDP_ChipFunction_' + $name) } | Select-Object -First 1
    foreach ($project in $item.researchPrerequisites)
    {
        $groups = [RimWorld.ResearchPrerequisitesUtility]::UnlockedDefsGroupedByPrerequisites($project)
        $matches = @($groups | Where-Object { $item -in $_.Second })
        Assert-True ($matches.Count -eq 1) "$name 必须只出现一次。"
        $actual = @($matches[0].First.unlockedBy | ForEach-Object { $nameField.GetValue($_) } | Sort-Object)
        $expected = @($item.researchPrerequisites | Where-Object { $_ -ne $project } | ForEach-Object { $nameField.GetValue($_) } | Sort-Object)
        Assert-True (($actual -join ',') -eq ($expected -join ',')) "$name 的原版共同解锁分组必须与制造条件一致；实际：$($actual -join ',')；预期：$($expected -join ',')。"
        $link = [Verse.Dialog_InfoCard+Hyperlink]::new([Verse.Def]$item)
        Assert-True ($link.def -eq $item -and $link.Label -eq $item.label) '信息卡链接必须指向功能本身。'
        $iconArguments = [object[]]@($item.PSObject.BaseObject); $iconPrefix.Invoke($null,$iconArguments)
        Assert-True ([object]::ReferenceEquals($iconArguments[0],$carrier)) '功能图标必须使用已有芯片图标。'
        Assert-True ([object]::ReferenceEquals($link.def,$item)) '提供芯片图标不得改变信息卡目标。'
    }
}
$native = [Verse.ResearchProjectDef]::new(); $nameField.SetValue($native,'NativeResearch')
$unchanged = $build.Invoke($null,[object[]]@($native,$original,$functions,$forms))
Assert-True ([object]::ReferenceEquals($unchanged,$original)) '非BDP研究不得改变。'
$iconArguments = [object[]]@($building); $iconPrefix.Invoke($null,$iconArguments)
Assert-True ([object]::ReferenceEquals($iconArguments[0],$building)) '其他定义的图标不得改变。'
Write-Host 'PASS：原版共同解锁分组与制造条件一致，条目去重且信息卡指向正确，非BDP研究不受影响。'
