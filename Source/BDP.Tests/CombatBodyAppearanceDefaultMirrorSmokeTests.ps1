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
$repoRoot = Split-Path -Parent $sourceRoot
$configPath = Join-Path $repoRoot '1.6\Defs\CombatBodyDef\Config.xml'
$temporaryPresetPath = Join-Path $repoRoot '1.6\Content\Defs\CombatBody\TemporaryFrontPreset.xml'
$hostConfigPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyHostConfigDef.cs'
$frontPresetDefPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CombatBodyFrontPresetDef.cs'

Assert-True (Test-Path -LiteralPath $configPath -PathType Leaf) '战斗体默认配置缺失。'
Assert-True (Test-Path -LiteralPath $hostConfigPath -PathType Leaf) 'Core 宿主配置类型缺失。'
Assert-True (Test-Path -LiteralPath $frontPresetDefPath -PathType Leaf) 'Core 通用前台预设类型必须保留。'

$config = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$hostConfig = Get-Content -LiteralPath $hostConfigPath -Raw -Encoding utf8

Assert-True ($config -match '<frontMode>MirrorOriginal</frontMode>') 'BDP 默认前台模式必须统一为镜像。'
Assert-True ($config -notmatch '<frontPresetDefName>') 'BDP 默认配置不得继续引用固定前台预设。'
Assert-True (-not (Test-Path -LiteralPath $temporaryPresetPath)) 'BDP 临时固定前台预设 XML 必须退役。'
Assert-True ($hostConfig -match 'frontMode\s*=\s*CombatBodyFrontMode\.MirrorOriginal') 'Core 宿主配置必须安全默认镜像。'
Assert-True ($hostConfig -match 'frontPresetDefName\s*=\s*null') 'Core 通用预设名必须保持可选。'

Write-Output 'CombatBodyAppearanceDefaultMirrorSmokeTests PASS'
