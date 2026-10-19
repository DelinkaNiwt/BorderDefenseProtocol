$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 测试脚本位于 Source\BDP.Tests，向上定位模组工程根目录。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$chipPresetPath = Join-Path $repoRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$triggerDefPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Equipment\Trigger.xml'

foreach ($path in @($chipPresetPath, $triggerDefPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('Required Fujin definition file is missing: ' + $path)
}

# 读取正式芯片功能预设与正式触发体定义。
$chipDocument = [xml](Get-Content -LiteralPath $chipPresetPath -Raw -Encoding UTF8)
$triggerDocument = [xml](Get-Content -LiteralPath $triggerDefPath -Raw -Encoding UTF8)
$chipDef = $chipDocument.Defs.SelectSingleNode("*[defName='BDP_ChipFunction_FujinRangedStrike']")
$entityChipDef = $chipDocument.Defs.SelectSingleNode("*[defName='BDP_ChipFunction_Fujin']")
$triggerDef = $triggerDocument.Defs.SelectSingleNode("ThingDef[defName='BDP_TriggerBody_Fujin']")

Assert-True ($null -ne $chipDef) 'The Fujin ranged strike chip function definition must exist.'
Assert-True ($null -ne $entityChipDef) 'The Fujin chip function definition must exist.'
Assert-True ($null -ne $triggerDef) 'The Fujin trigger body definition must exist.'

# 风刃是能力类芯片，且只能装入特殊槽；保持单槽占用，不引入额外槽位语义。
Assert-True ($chipDef.config.Profile.Category -eq 'BDP_ChipCategory_GeneralSkill') `
    'Fujin must use the GeneralSkill chip category.'
Assert-True ($chipDef.config.Loadout.SlotRegion -eq 'Special') `
    'Fujin must declare the Special slot region.'
Assert-True ($chipDef.config.Loadout.SlotOccupancy -eq 'Single') `
    'Fujin must occupy one physical slot.'
Assert-True ([int]$chipDef.config.Loadout.ActivationDelayTicks -eq 0) `
    'Fujin ranged strike activation delay must be zero.'
Assert-True ([int]$chipDef.config.Loadout.DeactivationDelayTicks -eq 0) `
    'Fujin ranged strike deactivation delay must be zero.'
Assert-True ([int]$entityChipDef.config.Loadout.ActivationDelayTicks -eq 0) `
    'Fujin activation delay must be zero.'
Assert-True ([int]$entityChipDef.config.Loadout.DeactivationDelayTicks -eq 0) `
    'Fujin deactivation delay must be zero.'

# 风刃仍必须通过现有独立 Ability 入口提供远程能力表现。
$abilityEntry = $chipDef.config.Expression.Entries.SelectSingleNode("li[EntryType='Ability' and AbilityDefName='BDP_Ability_FujinRangedStrike']")
Assert-True ($null -ne $abilityEntry) 'Fujin must retain its existing Ability entry.'

# 风刃触发体在特殊槽1、2预载两个动态芯片，并由现有初始化键解析功能。
$preloadEntries = @($triggerDef.SelectNodes("comps/li/fixedLoadout/li[side='Special']"))
Assert-True ($preloadEntries.Count -eq 2) 'Fujin trigger must preload exactly two Special-slot chips.'
Assert-True ($preloadEntries[0].slotNumber -eq '1' -and $preloadEntries[0].chipDef -eq 'BDP_Chip_Custom' -and
    $preloadEntries[0].initializationKey -eq 'BDP_ChipFunction_FujinRangedStrike') `
    'Fujin trigger Special slot 1 must resolve the ranged strike chip function.'
Assert-True ($preloadEntries[1].slotNumber -eq '2' -and $preloadEntries[1].chipDef -eq 'BDP_Chip_Custom' -and
    $preloadEntries[1].initializationKey -eq 'BDP_ChipFunction_Fujin') `
    'Fujin trigger Special slot 2 must resolve the entity blade chip function.'
Assert-True (@($triggerDef.SelectNodes("comps/li/fixedLoadout/li[initializationKey='BDP_ChipFunction_EnergyShield_Auto' or initializationKey='BDP_ChipFunction_EmergencyEscape']")).Count -eq 0) `
    'Fujin trigger must not preload the removed automatic shield or emergency escape chips.'

Write-Output 'FujinSpecialSlotPreloadSmokeTests PASS'
