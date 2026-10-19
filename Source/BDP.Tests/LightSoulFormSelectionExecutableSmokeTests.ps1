param([Parameter(Mandatory)][ValidateSet('1.6','1.7')][string]$RimWorldVersion)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'TestSupport/RangedVersionTestSupport.ps1')
Initialize-RangedVersionTest -RimWorldVersion $RimWorldVersion

# 必须检查真实解释器结果，避免 XML 看似互斥但因缺空选择表而无法启用进攻。
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 创建与正式配置字段类型完全一致的列表，不把缺失列表变成空列表。
function New-TypedList {
    param([type]$ItemType)
    $generic = [type]::GetType('System.Collections.Generic.List`1')
    return ,([Activator]::CreateInstance($generic.MakeGenericType($ItemType)))
}

# 按真实 XML 的存在性读取选择表：没有节点就保留空引用。
function Read-EntryIds {
    param([System.Xml.XmlElement]$Node)
    if ($null -eq $Node) { return $null }
    $list = [Collections.Generic.List[string]]::new()
    foreach ($id in $Node.SelectNodes('li')) { $list.Add($id.InnerText) }
    return ,$list
}

$core = $script:RangedCore
$configType = $core.GetType('BDP.Core.Expressions.ChipExpressionConfig', $true)
$entryType = $core.GetType('BDP.Core.Expressions.ChipExpressionEntryConfig', $true)
$modeType = $core.GetType('BDP.Core.Expressions.ChipExpressionModeConfig', $true)
$stanceType = $core.GetType('BDP.Core.Expressions.ChipExpressionStanceConfig', $true)
$validationType = $core.GetType('BDP.Core.Expressions.ChipExpressionContractValidationResult', $true)
[xml]$presets = Get-Content (Join-Path $script:RangedModRoot 'Common/Content/Defs/ChipManufacturingPresetDef/Presets.xml') -Raw -Encoding UTF8
$xmlConfig = $presets.SelectSingleNode('/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef[defName="BDP_ChipFunction_LightSoul"]/config/Expression')
$config = [Activator]::CreateInstance($configType)
$config.DefaultModeKey = [string]$xmlConfig.DefaultModeKey
$config.Entries = New-TypedList $entryType
foreach ($node in $xmlConfig.Entries.li) {
    $entry = [Activator]::CreateInstance($entryType)
    $entry.Id = [string]$node.Id
    $config.Entries.Add($entry)
}
$config.Modes = New-TypedList $modeType
foreach ($node in $xmlConfig.Modes.li) {
    $mode = [Activator]::CreateInstance($modeType)
    $mode.ModeKey = [string]$node.ModeKey
    $mode.DisplayLabelKey = [string]$node.DisplayLabelKey
    $mode.DefaultStanceKey = [string]$node.DefaultStanceKey
    $mode.ActiveEntryIds = Read-EntryIds $node.SelectSingleNode('ActiveEntryIds')
    if ($null -ne $node.Stances) {
        $mode.Stances = New-TypedList $stanceType
        foreach ($stanceNode in $node.Stances.li) {
            $stance = [Activator]::CreateInstance($stanceType)
            $stance.StanceKey = [string]$stanceNode.StanceKey
            $stance.DisplayLabelKey = [string]$stanceNode.DisplayLabelKey
            $stance.ActiveEntryIds = Read-EntryIds $stanceNode.SelectSingleNode('ActiveEntryIds')
            $mode.Stances.Add($stance)
        }
    }
    $config.Modes.Add($mode)
}

$interpreter = $core.GetType('BDP.Core.Expressions.ChipExpressionContractInterpreter', $true)
$flags = [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static
# 先经过正式结构校验，保证空公共表与姿态配置确实可以进入解释阶段。
$validator = $core.GetType('BDP.Core.Expressions.ChipExpressionStructureValidator', $true)
$structure = $validator.GetMethod('Validate', $flags).Invoke($null, @($config))
$instanceFlags = [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance
$structureValid = $structure.GetType().GetProperty('IsValid', $instanceFlags).GetValue($structure)
$structureErrors = $structure.GetType().GetField('Errors', $instanceFlags).GetValue($structure)
Assert-True $structureValid ("$RimWorldVersion 光魂形态配置未通过正式结构校验：" + ($structureErrors -join '；'))
$resolveMode = $interpreter.GetMethod('ResolveEffectiveModeKey', $flags)
$resolveStance = $interpreter.GetMethod('ResolveEffectiveStanceKey', $flags)
$select = $interpreter.GetMethod('SelectActiveEntryConfigs', $flags)
$cases = @(
    @{ Mode = $null; Stance = $null; Expected = 'light_soul_defense_shield|light_soul_defense_state' },
    @{ Mode = 'defense'; Stance = $null; Expected = 'light_soul_defense_shield|light_soul_defense_state' },
    @{ Mode = 'attack'; Stance = $null; Expected = 'light_soul_heavy_blade' },
    @{ Mode = 'attack'; Stance = 'heavy_blade'; Expected = 'light_soul_heavy_blade' },
    @{ Mode = 'attack'; Stance = 'punch'; Expected = 'light_soul_punch' })
foreach ($case in $cases) {
    $validation = [Activator]::CreateInstance($validationType)
    $validation.IsValid = $true
    $validation.Errors = [Collections.Generic.List[string]]::new()
    $validation.Warnings = [Collections.Generic.List[string]]::new()
    $mode = $resolveMode.Invoke($null, @($config, $case.Mode, $validation))
    $stance = $resolveStance.Invoke($null, @($config, $mode, $case.Stance, $validation))
    $selected = $select.Invoke($null, @($config, $mode, $stance, $validation))
    Assert-True ($validation.IsValid -and (($selected.Id -join '|') -eq $case.Expected)) ("$RimWorldVersion 真实解释器选择错误：$mode/$stance；" + ($validation.Errors -join '；'))
}

# XML 动态类型不能由 C# 编译器兜底，直接检查两版 Content 中的改名引用。
foreach ($path in @('HediffDef/LightSoul.xml', 'JobDef/LightSoul.xml', 'ChipManufacturingPresetDef/Presets.xml')) {
    [xml]$doc = Get-Content (Join-Path $script:RangedModRoot ('Common/Content/Defs/' + $path)) -Raw -Encoding UTF8
    foreach ($name in $doc.SelectNodes('//@Class|//verbClass|//driverClass')) {
        if ($name.InnerText -like 'BDP.Content.*LightSoul*') {
            Assert-True ($null -ne $script:RangedContent.GetType($name.InnerText, $false)) ('XML 引用缺少业务类型：' + $name.InnerText)
        }
    }
}
Write-Output "LightSoulFormSelectionExecutableSmokeTests $RimWorldVersion PASS"
