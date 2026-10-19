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
$factoryPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CombatBodyFrontApparelFactory.cs'
$bridgePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$frontStatePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\CombatBodyFrontState.cs'

Assert-True (Test-Path -LiteralPath $factoryPath -PathType Leaf) 'Core 必须提供统一的前台服装创建工厂。'
Assert-True (Test-Path -LiteralPath $bridgePath -PathType Leaf) '战斗体宿主桥必须存在。'
Assert-True (Test-Path -LiteralPath $frontStatePath -PathType Leaf) '战斗体前台状态必须存在。'

$factory = Get-Content -LiteralPath $factoryPath -Raw -Encoding utf8
$bridge = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$frontState = Get-Content -LiteralPath $frontStatePath -Raw -Encoding utf8

Assert-True ($factory -match 'class\s+CombatBodyFrontApparelFactory') '必须存在统一的前台服装创建工厂。'
Assert-True ($factory -match 'ThingMaker\.MakeThing') '前台服装工厂必须使用原版 ThingMaker 创建独立副本。'
Assert-True ($factory -match 'StuffDef|stuff') '前台服装工厂必须接收实际材料。'
Assert-True ($factory -match 'HasColor|Color') '前台服装工厂必须复制颜色。'
Assert-True ($factory -match 'StyleDef') '前台服装工厂必须复制风格。'

Assert-True ($bridge -match 'CombatBodyFrontAppearanceRegistry\.TryResolve') '战斗体宿主桥必须先查询个人前台外观提供器。'
Assert-True ($bridge -match 'CombatBodyFrontAppearancePlan') '战斗体宿主桥必须消费 Core 外观解析计划。'
Assert-True ($bridge -match 'ClearAndDestroyContents') '动态预设应用前必须清理旧动态服装容器。'
Assert-True ($bridge -match 'CombatBodyFrontApparelFactory') '镜像与动态预设必须共用前台服装工厂。'
Assert-True ($bridge -match 'Entries') '动态预设必须按解析计划条目创建服装。'
Assert-True ($bridge -match 'IsMirrorOriginal') '退出战斗体时仍必须区分镜像与预设服装。'

Assert-True ($frontState -match 'ActiveApparelThingIds') '前台状态必须继续记录激活服装 ID。'

Write-Output 'CombatBodyDynamicPresetReplacementSmokeTests PASS'
