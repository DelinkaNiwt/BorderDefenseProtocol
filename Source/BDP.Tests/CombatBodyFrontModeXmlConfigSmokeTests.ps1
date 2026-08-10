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
$modRoot = Split-Path -Parent $sourceRoot
$hostConfigPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CombatBodyHostConfigDef.cs'
$compPropsPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CompProperties_CombatBodyHost.cs'
$bridgePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$hostPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$configXmlPath = Join-Path $modRoot '1.6\Defs\CombatBodyDef\Config.xml'

$hostConfigText = Get-Content -LiteralPath $hostConfigPath -Raw -Encoding utf8
$compPropsText = Get-Content -LiteralPath $compPropsPath -Raw -Encoding utf8
$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$configXmlText = Get-Content -LiteralPath $configXmlPath -Raw -Encoding utf8

Assert-True -Condition ($hostConfigText -match 'public CombatBodyFrontMode frontMode = CombatBodyFrontMode\.MirrorOriginal;') -Message '全局宿主配置必须提供安全回退为 MirrorOriginal 的前台模式。'
Assert-True -Condition ($hostConfigText -match 'public string frontPresetDefName = null;') -Message '全局宿主配置必须提供可选的前台预设名。'
Assert-True -Condition ($compPropsText -notmatch 'frontMode|frontPresetDefName') -Message '动态注入 CompProperties 不得继续硬编码前台模式。'
Assert-True -Condition ($bridgeText -match 'CombatBodyHostConfigDef config = CombatBodyHostConfigResolver\.Resolve\(\);') -Message '宿主桥必须从全局 Def 解析前台配置。'
Assert-True -Condition ($bridgeText -match 'switch \(config\.frontMode\)') -Message '前台替换必须按 XML 配置模式分流。'
Assert-True -Condition ($bridgeText -match 'ResolveFrontPresetDef\(CombatBodyHostConfigDef config\)') -Message '预设解析必须接收同一份全局配置。'
Assert-True -Condition ($bridgeText -match 'config\.frontPresetDefName') -Message '预设名必须来自全局 XML 配置。'
Assert-True -Condition ($hostText -notmatch 'new PawnCombatBodyBridge\(pawn, Props,') -Message '宿主桥不应再接收注入式 CompProperties 前台配置。'
Assert-True -Condition ($configXmlText -match '<frontMode>Preset</frontMode>') -Message '当前游戏观察配置必须暂时选择 Preset。'
Assert-True -Condition ($configXmlText -match '<frontPresetDefName>BDP_TemporaryCombatBodyObservationPreset</frontPresetDefName>') -Message '当前游戏观察配置必须指向临时套装预设。'
Assert-True -Condition ($bridgeText -match 'ApplyMirrorOriginalFrontReplacement\(frontState\)') -Message '镜像原身路径必须保持存在。'

Write-Output 'CombatBodyFrontModeXmlConfig PASS'
