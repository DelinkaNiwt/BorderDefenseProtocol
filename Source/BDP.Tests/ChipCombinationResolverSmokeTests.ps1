# 芯片组合统一解析与字段合成冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$resolutionRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution"

$requiredFiles = @(
    "ChipCombinationResolver.cs",
    "ChipCombinationCompatibilityService.cs",
    "ChipConfigurationMergeService.cs",
    "ChipExpressionMergeService.cs",
    "IChipRequirementMergeRule.cs",
    "ChipRequirementMergeRegistry.cs",
    "IChipExtensionMergeRule.cs",
    "ChipExtensionMergeRegistry.cs",
    "ChipGunShellApplicationService.cs"
)
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $resolutionRoot $fileName)) "缺少组合解析组件：$fileName"
}

$resolverText = Get-Utf8Text (Join-Path $resolutionRoot "ChipCombinationResolver.cs")
$languageText = Get-Utf8Text (Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml")
$compatibilityText = Get-Utf8Text (Join-Path $resolutionRoot "ChipCombinationCompatibilityService.cs")
$mergeText = Get-Utf8Text (Join-Path $resolutionRoot "ChipConfigurationMergeService.cs")
$expressionText = Get-Utf8Text (Join-Path $resolutionRoot "ChipExpressionMergeService.cs")
$requirementText = Get-Utf8Text (Join-Path $resolutionRoot "ChipRequirementMergeRegistry.cs")
$extensionText = Get-Utf8Text (Join-Path $resolutionRoot "ChipExtensionMergeRegistry.cs")
$gunShellText = Get-Utf8Text (Join-Path $resolutionRoot "ChipGunShellApplicationService.cs")

Assert-True ($resolverText -match 'ChipCombinationResolution\s+Resolve\s*\(ChipCombinationRecord\s+record\)') "解析器必须公开唯一 Resolve(record) 主入口。"
Assert-True ($resolverText -match 'MissingSource') "来源缺失必须独立返回 MissingSource。"
Assert-True ($resolverText -match 'LastResolvedLabel') "成功解析必须回写最后成功名称。"
Assert-True ($resolverText -match 'Invalid') "来源齐全但规则不满足时必须返回 Invalid。"
Assert-True ($resolverText -match 'string\.Join\s*\(\s*"/"') "双动作成品名称必须使用无空格斜线连接动作名称。"
Assert-True ($resolverText -match 'BDP_ChipManufacturing_ProductLabel') "组合解析器必须通过语言键生成完整触发器芯片名称。"
Assert-True ($resolverText -match 'BDP_ChipManufacturing_ProductLabelWithGunShell') "有枪型时组合解析器必须通过独立语言键追加方括号枪型。"
Assert-True ($languageText -match '<BDP_ChipManufacturing_ProductLabel>触发器芯片:\{0\}</BDP_ChipManufacturing_ProductLabel>') "语言包缺少英文冒号无枪型成品名称规范。"
Assert-True ($languageText -match '<BDP_ChipManufacturing_ProductLabelWithGunShell>触发器芯片:\{0\}\[\{1\}型\]</BDP_ChipManufacturing_ProductLabelWithGunShell>') "语言包缺少英文冒号带方括号枪型的成品名称规范。"
Assert-True ($resolverText -notmatch 'label\s*\+\s*" / "') "成品名称不得保留带空格的旧动作分隔格式。"

Assert-True ($compatibilityText -match 'SlotRegion') "双动作必须检查槽位区域。"
Assert-True ($compatibilityText -match 'SlotOccupancy') "双动作必须检查槽位占用。"
Assert-True ($compatibilityText -match 'HasIntrinsicMultipleModes') "双动作必须拒绝预设自身多形态。"
Assert-True ($compatibilityText -match 'CanMerge') "双动作必须先检查条件与扩展是否可合并。"
Assert-True ($compatibilityText -match 'RequirementMergeRuleMissing') "未知条件类型必须返回稳定原因代码。"
Assert-True ($compatibilityText -match 'ExtensionMergeRuleMissing') "未知扩展类型必须返回稳定原因代码。"

Assert-True ($mergeText -match 'CapacityCost\s*=\s*[^;]*\+') "容量消耗必须相加。"
Assert-True ($mergeText -match 'ActivationCost\s*=\s*[^;]*\+') "激活消耗必须相加。"
Assert-True ($mergeText -match 'Math\.Max\s*\([^;]*ActivationDelayTicks') "激活延迟必须取大值。"
Assert-True ($mergeText -match 'Math\.Max\s*\([^;]*DeactivationDelayTicks') "关闭延迟必须取大值。"
Assert-True ($mergeText -match 'ActivationExclusionGroups') "激活互斥组必须并集去重。"
Assert-True ($mergeText -match 'Tags') "普通标签必须并集去重。"

Assert-True ($requirementText -match 'TrionIntensityRequirement') "条件注册表必须支持 Trion 释放力要求。"
Assert-True ($requirementText -match 'SkillLevelRequirement') "条件注册表必须支持技能等级要求。"
Assert-True ($requirementText -match 'Math\.Max') "同类条件门槛必须取大值。"
Assert-True ($extensionText -match 'rules\.Count\s*==\s*0|Rules\.Count\s*==\s*0') "空扩展注册表必须明确拒绝双动作扩展。"

Assert-True ($expressionText -match 'DefaultModeKey\s*=\s*firstAction\.defName') "动作顺序一必须成为默认形态。"
Assert-True ($expressionText -match 'UseCost') "表达合并必须保留各形态自己的使用消耗。"
Assert-True ($expressionText -match 'MinimumRequired') "表达合并必须保留各形态自己的最低需求。"
Assert-True ($gunShellText -match 'Apply') "枪壳覆盖必须集中在单一应用服务。"

function Assert-Equal
{
    param($Actual, $Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message 实际=$Actual，预期=$Expected" }
}

function New-TypedList
{
    param([Type]$ElementType, [object[]]$Items = @())
    $openListType = [System.Collections.Generic.List``1]
    $listType = $openListType.MakeGenericType([Type[]]@($ElementType))
    $list = [Activator]::CreateInstance($listType)
    foreach ($item in $Items) { [void]$list.Add($item) }
    return ,$list
}

function New-ReflectedInstance
{
    param([Reflection.Assembly]$Assembly, [string]$TypeName)
    return [Activator]::CreateInstance($Assembly.GetType($TypeName, $true))
}

# 加载正式程序集，实际构造两份配置并调用合并器，不只检查源码文本。
$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))

$category = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipCategoryDef"
$tagA = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipTagDef"
$tagB = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipTagDef"
$groupA = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipExclusionGroupDef"
$groupB = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipExclusionGroupDef"
$tagType = $tagA.GetType()
$groupType = $groupA.GetType()
$requirementType = $coreAssembly.GetType("BDP.Core.Requirements.PawnRequirement", $true)
$extensionType = $coreAssembly.GetType("BDP.Core.Chips.ChipExtensionConfig", $true)

function New-TestConfig
{
    param(
        [float]$Capacity,
        [float]$Activation,
        [int]$ActivationDelay,
        [int]$DeactivationDelay,
        [object[]]$Tags,
        [object[]]$Groups,
        [float]$TrionMinimum,
        [float]$UseCost,
        [float]$EntryMinimum,
        [string]$EntryId
    )

    $profile = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipProfileConfig"
    $profile.Category = $category
    $profile.Tags = New-TypedList $tagType $Tags

    $loadout = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipLoadoutConfig"
    $loadout.SlotRegion = [Enum]::Parse($loadout.GetType().GetField("SlotRegion").FieldType, "MainSub")
    $loadout.SlotOccupancy = [Enum]::Parse($loadout.GetType().GetField("SlotOccupancy").FieldType, "Single")
    $loadout.ActivationDelayTicks = $ActivationDelay
    $loadout.DeactivationDelayTicks = $DeactivationDelay
    $loadout.ActivationExclusionGroups = New-TypedList $groupType $Groups

    $trion = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipTrionConfig"
    $trion.CapacityCost = $Capacity
    $trion.ActivationCost = $Activation

    $requirement = New-ReflectedInstance $coreAssembly "BDP.Core.Requirements.TrionIntensityRequirement"
    $requirement.Minimum = $TrionMinimum

    $entryTrion = New-ReflectedInstance $coreAssembly "BDP.Core.Expressions.ExpressionSourceTrionConfig"
    $entryTrion.UseCost = $UseCost
    $entryTrion.MinimumRequired = $EntryMinimum
    $entry = New-ReflectedInstance $coreAssembly "BDP.Core.Expressions.ChipExpressionEntryConfig"
    $entry.Id = $EntryId
    $entry.Trion = $entryTrion
    $entryType = $entry.GetType()

    $expression = New-ReflectedInstance $coreAssembly "BDP.Core.Expressions.ChipExpressionConfig"
    $expression.Entries = New-TypedList $entryType @($entry)

    $config = New-ReflectedInstance $coreAssembly "BDP.Core.Chips.ChipDefinitionConfig"
    $config.Profile = $profile
    $config.Loadout = $loadout
    $config.Trion = $trion
    $config.Expression = $expression
    $config.ActivationRequirements = New-TypedList $requirementType @($requirement)
    $config.Extensions = New-TypedList $extensionType
    return $config
}

$configA = New-TestConfig 10 4 60 20 @($tagA) @($groupA) 2 3 5 "entry_a"
$configB = New-TestConfig 20 8 30 45 @($tagA, $tagB) @($groupA, $groupB) 4 7 9 "entry_b"
$shootingSkill = [Activator]::CreateInstance($gameAssembly.GetType("RimWorld.SkillDef", $true))
$skillRequirementA = New-ReflectedInstance $coreAssembly "BDP.Core.Requirements.SkillLevelRequirement"
$skillRequirementA.Skill = $shootingSkill
$skillRequirementA.MinimumLevel = 6
$skillRequirementB = New-ReflectedInstance $coreAssembly "BDP.Core.Requirements.SkillLevelRequirement"
$skillRequirementB.Skill = $shootingSkill
$skillRequirementB.MinimumLevel = 10
$configA.ActivationRequirements.Add($skillRequirementA)
$configB.ActivationRequirements.Add($skillRequirementB)
$childEntry = New-ReflectedInstance $coreAssembly "BDP.Core.Expressions.ChipExpressionEntryConfig"
$childEntry.Id = "entry_child"
$childEntry.ParentEntryId = "entry_a"
$configA.Expression.Entries.Add($childEntry)
$modeType = $coreAssembly.GetType("BDP.Core.Expressions.ChipExpressionModeConfig", $true)
$sourceMode = [Activator]::CreateInstance($modeType)
$sourceMode.ModeKey = "source_mode"
$sourceMode.ActiveEntryIds = New-TypedList ([string]) @("entry_a")
$configA.Expression.Modes = New-TypedList $modeType @($sourceMode)
$actionA = New-ReflectedInstance $contentAssembly "BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef"
$actionA.defName = "ActionA"
$actionA.label = "动作甲"
$actionA.config = $configA
$actionB = New-ReflectedInstance $contentAssembly "BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef"
$actionB.defName = "ActionB"
$actionB.label = "动作乙"
$actionB.config = $configB

$mergeType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Resolution.ChipConfigurationMergeService", $true)
$merged = $mergeType.GetMethod("MergeDual").Invoke($null, @($actionA, $actionB))
Assert-Equal $merged.Trion.CapacityCost 30 "容量消耗应相加。"
Assert-Equal $merged.Trion.ActivationCost 12 "激活消耗应相加。"
Assert-Equal $merged.Loadout.ActivationDelayTicks 60 "激活延迟应取大值。"
Assert-Equal $merged.Loadout.DeactivationDelayTicks 45 "关闭延迟应取大值。"
Assert-Equal $merged.Profile.Tags.Count 2 "普通标签应并集去重。"
Assert-True ([object]::ReferenceEquals($merged.Profile.Tags[0], $tagA)) "标签并集必须保持首次出现顺序。"
Assert-True ([object]::ReferenceEquals($merged.Profile.Tags[1], $tagB)) "第二个新标签应稳定追加。"
Assert-Equal $merged.Loadout.ActivationExclusionGroups.Count 2 "激活排斥组应并集去重。"
Assert-Equal $merged.ActivationRequirements.Count 2 "Trion 与技能条件应分别保留。"
Assert-Equal $merged.ActivationRequirements[0].Minimum 4 "同类 Trion 条件应取大值。"
Assert-Equal $merged.ActivationRequirements[1].MinimumLevel 10 "同技能条件应取大值。"
Assert-Equal $merged.Expression.DefaultModeKey "ActionA" "动作顺序一应成为默认形态。"
Assert-Equal $merged.Expression.Modes.Count 2 "双动作应生成两个形态。"
Assert-Equal $merged.Expression.Entries[0].Trion.UseCost 3 "形态一使用消耗应独立保留。"
Assert-Equal $merged.Expression.Entries[2].Trion.UseCost 7 "形态二使用消耗应独立保留。"
Assert-Equal $merged.Expression.Entries[0].Trion.MinimumRequired 5 "形态一最低需求应独立保留。"
Assert-Equal $merged.Expression.Entries[2].Trion.MinimumRequired 9 "形态二最低需求应独立保留。"
Assert-Equal $merged.Expression.Modes[0].ActiveEntryIds.Count 1 "原动作单形态的启用条目范围必须保留。"
Assert-Equal $merged.Expression.Modes[0].ActiveEntryIds[0] "mfg_0_entry_a" "形态条目 Id 必须同步增加前缀。"
Assert-Equal $merged.Expression.Entries[1].ParentEntryId "mfg_0_entry_a" "父子条目引用必须同步增加前缀。"

# 直接调用兼容判定器，验证槽位与内置多形态都产生稳定非法原因。
$professionType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipProfessionDef", $true)
$shooter = [Activator]::CreateInstance($professionType)
$shooter.defName = "BDP_ChipProfession_Shooter"
$gunner = [Activator]::CreateInstance($professionType)
$gunner.defName = "BDP_ChipProfession_Gunner"
$gunner.acceptedActionProfessions = New-TypedList $professionType @($shooter)
$actionA.profession = $shooter
$actionB.profession = $shooter
$actionType = $actionA.GetType()
$actions = New-TypedList $actionType @($actionA, $actionB)
$compatibilityType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Resolution.ChipCombinationCompatibilityService", $true)

$configB.Loadout.SlotRegion = [Enum]::Parse($configB.Loadout.GetType().GetField("SlotRegion").FieldType, "Special")
$slotFailures = $compatibilityType.GetMethod("Validate").Invoke($null, @($category, $gunner, $actions, $null))
Assert-True (($slotFailures | Where-Object { $_.Code -eq "SlotRegionMismatch" }).Count -eq 1) "槽位区域不同时必须返回稳定非法原因。"
$configB.Loadout.SlotRegion = [Enum]::Parse($configB.Loadout.GetType().GetField("SlotRegion").FieldType, "MainSub")

$secondSourceMode = [Activator]::CreateInstance($modeType)
$secondSourceMode.ModeKey = "source_mode_2"
$secondSourceMode.ActiveEntryIds = New-TypedList ([string]) @("entry_b")
$configB.Expression.Modes = New-TypedList $modeType @($sourceMode, $secondSourceMode)
$modeFailures = $compatibilityType.GetMethod("Validate").Invoke($null, @($category, $gunner, $actions, $null))
Assert-True (($modeFailures | Where-Object { $_.Code -eq "IntrinsicMultiMode" }).Count -eq 1) "预设自身多形态参与双动作时必须返回稳定非法原因。"

# 未知 DefName 只应进入 MissingSource（来源缺失），不能被误判为 Invalid（非法组合）。
$record = New-ReflectedInstance $contentAssembly "BDP.Content.Assembly.ChipManufacturing.Model.ChipCombinationRecord"
$record.CategoryDefName = "BDP_Test_MissingCategory"
$record.OrderedActionPresetDefNames.Add("BDP_Test_MissingAction")
$resolver = New-ReflectedInstance $contentAssembly "BDP.Content.Assembly.ChipManufacturing.Resolution.ChipCombinationResolver"
$resolution = $resolver.Resolve($record)
Assert-Equal $resolution.Status.ToString() "MissingSource" "来源 Def 不存在时必须返回 MissingSource。"

# 记录自身缺少必要键时不可伪装成可恢复的来源缺失。
$malformedRecord = New-ReflectedInstance $contentAssembly "BDP.Content.Assembly.ChipManufacturing.Model.ChipCombinationRecord"
$malformedResolution = $resolver.Resolve($malformedRecord)
Assert-Equal $malformedResolution.Status.ToString() "Invalid" "空分类或空动作记录必须返回 Invalid。"

Write-Host "PASS: 组合解析器区分来源缺失与非法组合，并按既定规则合成配置、形态、条件和枪壳。"
