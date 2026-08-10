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
$mainRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $mainRoot
$candidateRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness'

$mainBasePath = Join-Path $mainRoot '1.6\Content\Defs\Things\Items\Chips\ThingDefs_ChipBase.xml'
$candidateBasePath = Join-Path $candidateRoot '1.6\Defs\Things\Items\Chips\ThingDefs_ChipBase.xml'

Assert-True (Test-Path -LiteralPath $mainBasePath) 'BDP_ChipBase 必须由主模组正式拥有。'
Assert-True (-not (Test-Path -LiteralPath $candidateBasePath)) 'DevHarness 不得继续保留 BDP_ChipBase 副本。'

[xml]$baseDocument = Get-Content -LiteralPath $mainBasePath -Raw -Encoding utf8
$baseDef = $baseDocument.SelectSingleNode('/Defs/ThingDef[@Name="BDP_ChipBase"]')
Assert-True ($null -ne $baseDef) '主模组 BDP_ChipBase 定义缺失。'
Assert-True ($baseDef.ParentName -eq 'ResourceBase') 'BDP_ChipBase 必须继承原版 ResourceBase。'
Assert-True ($baseDef.Abstract -eq 'True') 'BDP_ChipBase 必须保持为抽象基类。'
Assert-True ([int]$baseDef.stackLimit -eq 1) 'BDP_ChipBase 必须保持不可堆叠。'

$formalChipPaths = @(
    (Join-Path $mainRoot '1.6\Content\Defs\Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml'),
    (Join-Path $mainRoot '1.6\Content\Defs\Things\Items\Chips\Shield\ThingDefs_Chip_EnergyShield.xml')
)

foreach ($chipPath in $formalChipPaths) {
    [xml]$chipDocument = Get-Content -LiteralPath $chipPath -Raw -Encoding utf8
    $children = @($chipDocument.SelectNodes('/Defs/ThingDef[@ParentName="BDP_ChipBase"]'))
    Assert-True ($children.Count -gt 0) "正式芯片必须继续继承 BDP_ChipBase：$chipPath"
}

Write-Output 'ChipBaseOwnershipBoundarySmokeTests PASS'
