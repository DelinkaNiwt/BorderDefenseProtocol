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

function Read-RequiredFile {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "缺少浮空功能文件：$Path"
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$contentRoot = Join-Path $modRoot '1.6\Content'
$languageRoot = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)'

$chipDefPath = Join-Path $contentRoot 'Defs\ChipManufacturingPresetDef\Presets.xml'
$hediffDefPath = Join-Path $contentRoot 'Defs\HediffDef\FloatingFlight.xml'
$pathGridDefPath = Join-Path $contentRoot 'Defs\PathGridDef\FloatingFlight.xml'
$chipLanguagePath = Join-Path $languageRoot 'DefInjected\BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef\Presets.xml'
$hediffLanguagePath = Join-Path $languageRoot 'DefInjected\HediffDef\FloatingFlight.xml'
$commandsLanguagePath = Join-Path $languageRoot 'Keyed\Commands.xml'
$gameplayLanguagePath = Join-Path $languageRoot 'Keyed\Gameplay.xml'
$settingsPath = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\FloatingFlightSettingsExtension.cs'
$defOfPath = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\FloatingFlightDefOf.cs'
$hediffClassPath = Join-Path $sourceRoot 'BDP.Content\FloatingFlight\Hediff_FloatingFlight.cs'

$chipText = Read-RequiredFile $chipDefPath
$hediffText = Read-RequiredFile $hediffDefPath
$pathGridText = Read-RequiredFile $pathGridDefPath
$chipLanguageText = Read-RequiredFile $chipLanguagePath
$hediffLanguageText = Read-RequiredFile $hediffLanguagePath
$commandsLanguageText = Read-RequiredFile $commandsLanguagePath
$gameplayLanguageText = Read-RequiredFile $gameplayLanguagePath
$settingsText = Read-RequiredFile $settingsPath
$defOfText = Read-RequiredFile $defOfPath
$hediffClassText = Read-RequiredFile $hediffClassPath

# 精确选择浮空条目，防止从前面的其它功能误匹配到材料或槽位字段。
$chipXml = [xml]$chipText
$chipNode = $chipXml.SelectSingleNode('/Defs/*[defName="BDP_ChipFunction_Floating"]')
Assert-True ($null -ne $chipNode) '缺少 BDP_ChipFunction_Floating 芯片功能定义。'
$chipBlock = @{ Value = $chipNode.OuterXml }
Assert-True ($chipBlock.Value -match '<Category>BDP_ChipCategory_GeneralSkill</Category>') '浮空芯片必须属于通用技能。'
Assert-True ($chipBlock.Value -match '<SlotRegion>Special</SlotRegion>') '浮空芯片必须占用特殊槽。'
Assert-True ($chipBlock.Value -match '<SlotOccupancy>Single</SlotOccupancy>') '浮空芯片必须单槽占用。'
Assert-True ($chipBlock.Value -match '<ActivationDelayTicks>0</ActivationDelayTicks>') '浮空芯片启用延迟必须为 0。'
Assert-True ($chipBlock.Value -match '<DeactivationDelayTicks>0</DeactivationDelayTicks>') '浮空芯片停用延迟必须为 0。'
Assert-True ($chipNode.showInManufacturing -eq 'false') '浮空芯片不得开放玩家制造。'
Assert-True ($null -eq $chipNode.costList -and $null -eq $chipNode.additionalWorkAmount) '不可制造的浮空芯片不得保留制造材料和工作量。'
Assert-True ($null -eq $chipNode.researchPrerequisites) '浮空不得继续作为研究制造解锁项。'
Assert-True ($chipBlock.Value -match '<CapacityCost>100</CapacityCost>') '浮空芯片容量占用必须为 100。'
Assert-True ($chipBlock.Value -match '<ActivationCost>0</ActivationCost>') '浮空芯片启用消耗必须为 0。'
Assert-True ($chipBlock.Value -match '<Minimum>1</Minimum>') '浮空芯片最低 Trion 强度必须为 1。'
Assert-True ($chipBlock.Value -match '<Id>floating_flight_passive</Id>') '浮空芯片必须声明固定被动条目 ID。'
Assert-True ($chipBlock.Value -match '<EntryType>Passive</EntryType>') '浮空芯片必须通过 Passive 发布能力。'
Assert-True ($chipBlock.Value -match '<PassiveIdentityKey>BDP_FloatingFlight</PassiveIdentityKey>') '浮空能力身份键错误。'

Assert-True ($hediffText -match '<defName>BDP_Hediff_FloatingFlight</defName>') '缺少浮空 Hediff Def。'
Assert-True ($hediffText -match 'Hediff_FloatingFlight') '浮空 Hediff 必须使用正式 Hediff 类。'
Assert-True ($hediffText -match '<becomeVisible>true</becomeVisible>') '浮空 Hediff 必须显示在健康面板。'
Assert-True ($hediffText -match '<MoveSpeed>2(?:\.0)?</MoveSpeed>') '浮空 Hediff 必须提供 MoveSpeed 2 倍修正。'

Assert-True ($pathGridText -match 'BDP_PathGrid_FloatingHybrid') '缺少混合浮空路径网格 Def。'
Assert-True ($pathGridText -match 'BDP_PathGrid_FloatingAlways') '缺少始终浮空路径网格 Def。'
Assert-True ([regex]::Matches($pathGridText, '<flying>true</flying>').Count -eq 2) '两套浮空路径网格都必须声明 flying=true。'
Assert-True ([regex]::Matches($pathGridText, '<fencePassable>true</fencePassable>').Count -eq 2) '两套浮空路径网格都必须声明 fencePassable=true。'

Assert-True ($chipLanguageText -match 'BDP_ChipFunction_Floating\.label') '缺少浮空芯片名称语言键。'
Assert-True ($gameplayLanguageText -match '<BDP_ChipFunction_Floating>[^<]+</BDP_ChipFunction_Floating>') '缺少浮空芯片制造界面使用的 Keyed 名称语言键。'
Assert-True ($gameplayLanguageText -match 'BDP_ChipFunction_Floating_Description') '缺少浮空芯片说明语言键。'
Assert-True ($hediffLanguageText -match 'BDP_Hediff_FloatingFlight\.label') '缺少浮空 Hediff 名称语言键。'
Assert-True ($hediffLanguageText -match 'BDP_Hediff_FloatingFlight\.description') '缺少浮空 Hediff 说明语言键。'
Assert-True ($commandsLanguageText -match 'BDP_Command_FloatingFlight_') '缺少浮空按钮语言键。'
Assert-True ($gameplayLanguageText -match 'BDP_FloatingFlight_') '缺少浮空状态语言键。'
Assert-True ($gameplayLanguageText -match 'BDP_FloatingFlight_ModeShort_Off') '缺少关闭浮空短模式语言键。'
Assert-True ($gameplayLanguageText -match 'BDP_FloatingFlight_ModeShort_MoveOnly') '缺少移动浮空短模式语言键。'
Assert-True ($gameplayLanguageText -match 'BDP_FloatingFlight_ModeShort_Always') '缺少始终浮空短模式语言键。'

Assert-True ($settingsText -match 'class\s+FloatingFlightSettingsExtension\s*:\s*DefModExtension') '缺少浮空可配置 DefModExtension。'
Assert-True ($settingsText -match 'transitionTicks\s*=\s*30') '浮空默认过渡时间必须为 30 tick。'
Assert-True ($settingsText -match 'presentationHeight\s*=\s*0\.6f') '浮空默认表现高度必须为 0.6。'
Assert-True ($settingsText -match 'sustainTrionPerSecond\s*=\s*0f') '浮空默认持续消耗必须为 0。'
Assert-True ($defOfText -match 'BDP_Hediff_FloatingFlight') 'DefOf 必须声明浮空 Hediff。'
Assert-True ($hediffClassText -match 'class\s+Hediff_FloatingFlight\s*:\s*Hediff') '缺少浮空 Hediff 类。'

Write-Output 'FloatingFlightDefinitionSmokeTests PASS'
