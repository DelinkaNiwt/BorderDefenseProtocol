$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$appearanceRoot = Join-Path $sourceRoot 'BDP.Content\CombatBody\Appearance'
$buildingPath = Join-Path $appearanceRoot 'Building_CombatBodyEditor.cs'
$driverPath = Join-Path $appearanceRoot 'JobDriver_UseCombatBodyEditor.cs'
$defsRoot = Join-Path $sourceRoot '..\1.6\Content\Defs'
$thingDefPath = Join-Path $defsRoot 'ThingDef\Buildings\CombatBodyAppearanceEditor.xml'
$jobDefPath = Join-Path $defsRoot 'JobDef\CombatBodyAppearanceEditor.xml'
$thingLanguagePath = Join-Path $sourceRoot '..\Languages\ChineseSimplified (简体中文)\DefInjected\ThingDef\BDP_BuildingsCombatBodyAppearanceEditor.xml'
$jobLanguagePath = Join-Path $sourceRoot '..\Languages\ChineseSimplified (简体中文)\DefInjected\JobDef\BDP_JobsCombatBodyAppearanceEditor.xml'

foreach ($path in @($buildingPath, $driverPath, $thingDefPath, $jobDefPath, $thingLanguagePath, $jobLanguagePath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "战斗体编辑台入口文件缺失：$path"
}

$building = Get-Content -LiteralPath $buildingPath -Raw -Encoding utf8
$driver = Get-Content -LiteralPath $driverPath -Raw -Encoding utf8
$thingDef = Get-Content -LiteralPath $thingDefPath -Raw -Encoding utf8
$jobDef = Get-Content -LiteralPath $jobDefPath -Raw -Encoding utf8
$thingLanguage = Get-Content -LiteralPath $thingLanguagePath -Raw -Encoding utf8
$jobLanguage = Get-Content -LiteralPath $jobLanguagePath -Raw -Encoding utf8

Assert-True ($building -match 'class\s+Building_CombatBodyEditor\s*:\s*Building') '编辑台必须使用 Content Building 实例类型。'
Assert-True ($building -match 'GetFloatMenuOptions') '编辑台必须提供 Pawn 右键入口。'
Assert-True ($building -match 'CompPowerTrader|PowerOn') '编辑台入口必须检查原版通电状态。'
Assert-True ($building -match 'PathEndMode\.InteractionCell') '编辑台入口必须使用交互格。'
Assert-True ($building -match 'CombatBodyPhase\.Active|CombatBodyPhase\.Collapsing') '战斗体激活或崩解时必须拒绝打开编辑台。'
Assert-True ($driver -match 'class\s+JobDriver_UseCombatBodyEditor\s*:\s*JobDriver') '编辑台必须使用原版 JobDriver。'
Assert-True ($driver -match 'TryMakePreToilReservations|Reserve') '编辑台 Job 必须预留目标。'
Assert-True ($driver -match 'Toils_Goto\.GotoThing.*InteractionCell') '编辑台 Job 必须走到交互格。'
Assert-True ($driver -match 'Find\.WindowStack\.Add') '编辑台 Job 到达后必须打开窗口。'
Assert-True (-not ($building -match 'CombatBodyAppearanceTemplate|LedgerService')) '建筑入口不得持有模板账本业务数据。'
Assert-True ($thingDef -match '<thingClass>BDP\.Content\.CombatBody\.Appearance\.Building_CombatBodyEditor</thingClass>') 'ThingDef 必须绑定编辑台 Building 类型。'
Assert-True ($thingDef -match '<hasInteractionCell>true</hasInteractionCell>') 'ThingDef 必须启用交互格。'
Assert-True ($thingDef -match 'CompProperties_Power|CompPowerTrader') 'ThingDef 必须声明原版通电组件。'
Assert-True ($jobDef -match '<driverClass>BDP\.Content\.CombatBody\.Appearance\.JobDriver_UseCombatBodyEditor</driverClass>') 'JobDef 必须绑定编辑台 JobDriver。'
Assert-True ($thingLanguage -match 'BDP_CombatBodyAppearanceEditor\.label|BDP_CombatBodyAppearanceEditor\.description') '编辑台标签和说明必须进入 DefInjected 语言包。'
Assert-True ($jobLanguage -match 'BDP_UseCombatBodyAppearanceEditor\.reportString') '编辑台工作报告必须进入 DefInjected 语言包。'

Write-Output 'CombatBodyAppearanceEditorDefinitionSmokeTests PASS'
