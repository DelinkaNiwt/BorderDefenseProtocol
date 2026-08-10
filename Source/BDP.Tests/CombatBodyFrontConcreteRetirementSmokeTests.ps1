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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$frontModePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontMode.cs'
$frontPresetDefPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontPresetDef.cs'
$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$hostPropsPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompProperties_CombatBodyHost.cs'
$combatBodyConfigXmlPath = Join-Path $repoRoot '1.6\Defs\Pawn\CombatBody\CombatBodyConfigDefs.xml'
$armorXmlPath = Join-Path $repoRoot '1.6\Defs\Things\CombatBody\ThingDefs_CombatBodyFrontApparel.xml'

Assert-True -Condition (Test-Path -LiteralPath $frontModePath) -Message '通用前台模式契约必须保留。'
Assert-True -Condition (Test-Path -LiteralPath $frontPresetDefPath) -Message '通用前台预设契约必须保留。'
Assert-True -Condition (Test-Path -LiteralPath $bridgePath) -Message '通用战斗体桥接必须保留。'
Assert-True -Condition (Test-Path -LiteralPath $combatBodyConfigXmlPath) -Message '战斗体配置文件必须保留。'
Assert-True -Condition (-not (Test-Path -LiteralPath $armorXmlPath)) -Message '未使用的具体前台护甲定义必须退役。'

$hostPropsText = Get-Content -LiteralPath $hostPropsPath -Raw -Encoding utf8
$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$combatBodyConfigXmlText = Get-Content -LiteralPath $combatBodyConfigXmlPath -Raw -Encoding utf8

Assert-True -Condition ($hostPropsText -match 'public CombatBodyFrontMode frontMode = CombatBodyFrontMode\.MirrorOriginal;') -Message '默认前台模式必须继续跟随原衣物。'
Assert-True -Condition ($hostPropsText -match 'public string frontPresetDefName = null;') -Message '默认宿主配置不得再指向已退役的具体预设。'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontMode\.MirrorOriginal') -Message '桥接必须继续支持镜像原衣物。'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontMode\.Preset') -Message '桥接必须继续支持未来注册固定预设。'
Assert-True -Condition ($combatBodyConfigXmlText -notmatch 'BDP_DefaultCombatBodyFrontPreset|BDP_CombatBodyArmor') -Message '正式战斗体配置不得残留已退役的具体预设名称。'
Assert-True -Condition ($combatBodyConfigXmlText -match 'BDP_DefaultCombatBodySnapshotConfig') -Message '通用快照配置必须继续保留。'

Write-Output 'CombatBodyFrontConcreteRetirement PASS'
