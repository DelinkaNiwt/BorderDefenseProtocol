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
$presetPath = Join-Path $modRoot '1.6\Content\Defs\CombatBody\TemporaryFrontPreset.xml'
$configPath = Join-Path $modRoot '1.6\Defs\CombatBodyDef\Config.xml'
$frontPresetDefPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CombatBodyFrontPresetDef.cs'

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$frontPresetDefText = Get-Content -LiteralPath $frontPresetDefPath -Raw -Encoding utf8

Assert-True -Condition (-not (Test-Path -LiteralPath $presetPath)) -Message '临时战斗体服装观察预设必须退役。'
Assert-True -Condition ($configText -match '<frontMode>MirrorOriginal</frontMode>') -Message '全局配置默认必须回到镜像模式。'
Assert-True -Condition ($configText -notmatch '<frontPresetDefName>') -Message '全局配置不得继续引用临时观察预设。'
Assert-True -Condition ($frontPresetDefText -match 'class CombatBodyFrontPresetDef\s*:\s*Def') -Message 'Core 通用前台预设类型必须保留。'

Write-Output 'CombatBodyTemporaryFrontPresetRetirement PASS'
