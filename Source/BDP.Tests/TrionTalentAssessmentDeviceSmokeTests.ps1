$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$buildingPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Building_TrionDetector.cs'
$portablePath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Thing_TrionPortableDetector.cs'
$utilityPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\TrionTalentAssessmentFloatMenuUtility.cs'
$buildingDefPath = Join-Path $root '1.6\Content\Defs\Buildings\Trion\ThingDefs_TrionDetector.xml'
$portableDefPath = Join-Path $root '1.6\Content\Defs\Things\Trion\ThingDefs_TrionPortableDetector.xml'
$assessmentPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\CompTrionTalentAssessment.cs'
foreach ($path in @($buildingPath,$portablePath,$utilityPath,$buildingDefPath,$portableDefPath)) {
    Assert-True (Test-Path $path) "缺少检测设备文件：$path"
}
$building = Get-Content -Raw $buildingPath
$portable = Get-Content -Raw $portablePath
$utility = Get-Content -Raw $utilityPath
$assessment = Get-Content -Raw $assessmentPath
$buildingDef = Get-Content -Raw $buildingDefPath
$portableDef = Get-Content -Raw $portableDefPath
Assert-True ($building -match 'Building_Enterable') '固定设备必须使用原版可进入建筑流程。'
Assert-True ($building -notmatch 'TrionTalentAssessmentFloatMenuUtility') '固定设备不得继续调用旧操作员右键入口。'
Assert-True ($portable -match 'TrionTalentAssessmentFloatMenuUtility') '便携设备必须保留现有右键入口。'
Assert-True ($buildingDef -match 'CompPowerTrader') '固定设备必须使用原版供电组件。'
Assert-True ($portableDef -match '<stackLimit>') '便携设备必须是可堆叠的一次性物品。'
Assert-True ($buildingDef -match '<texPath>Things/Building/Misc/GrowthVat/GrowthVat</texPath>') '固定检测仪必须使用原版培育仓贴图路径。'
Assert-True ($buildingDef -match '<size>\(1,2\)</size>') '固定检测仪定义尺寸必须与原版培育仓一致。'
Assert-True ($buildingDef -match '<drawSize>\(1,2\)</drawSize>') '固定检测仪绘制尺寸必须与原版培育仓一致。'
Assert-True ($buildingDef -match '<defaultPlacingRot>East</defaultPlacingRot>') '固定检测仪必须像原版培育仓一样默认朝东，呈横向 2×1。'
Assert-True ($portableDef -match '<texPath>Things/Item/Resource/ComponentSpacer</texPath>') '便携器占位图必须使用原版高级部件的真实贴图路径。'
Assert-True ($portableDef -match '<graphicClass>Graphic_StackCount</graphicClass>') '原版高级部件路径必须配套使用 Graphic_StackCount，不能按不存在的单张根贴图读取。'
Assert-True ($utility -match 'BeginTargeting') '入口必须进入受检者目标选择。'
Assert-True ($assessment -match 'CompTrionTalentAssessment') '检测完成状态必须由 Content 侧 Pawn Comp 持有。'
Assert-True (($building + $portable + $utility) -notmatch 'CompTrionTalentAssessment') '检测设备本身不得持有检测完成状态。'
Write-Host 'PASS: 固定式使用原版入舱流程，便携式保留现有右键入口。'
