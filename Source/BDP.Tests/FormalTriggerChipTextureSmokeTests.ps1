$ErrorActionPreference = 'Stop'

# 统一断言入口，失败时直接指出正式模组资源契约。
function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 主模组的正式芯片 Def 与贴图必须在同一可加载内容树中。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$chipDefFiles = @(
    (Join-Path $modRoot '1.6\Content\Defs\Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml'),
    (Join-Path $modRoot '1.6\Content\Defs\Things\Items\Chips\Shield\ThingDefs_Chip_EnergyShield.xml')
)
$triggerChipTexturePath = Join-Path $modRoot '1.6\Content\Textures\Things\Trigger\Chip\BDP_TriggerChip.png'
$expectedTextureDefPath = 'Things/Trigger/Chip/BDP_TriggerChip'
$expectedTextureHash = '46D747674E99BF4ED5BFD409162FF442B9E0C99802EA560876E86840689B544A'
$formalChipDefNames = @('BDP_Chip_Kogetsu', 'BDP_Chip_Senku', 'BDP_Chip_EnergyShield')

# 三枚正式芯片均须指向主模组拥有的共用贴图。
$chipDefinitionText = ($chipDefFiles | ForEach-Object {
    Get-Content -LiteralPath $_ -Raw -Encoding utf8
}) -join [Environment]::NewLine
foreach ($defName in $formalChipDefNames) {
    $chipMatch = [regex]::Match(
        $chipDefinitionText,
        "(?s)<ThingDef.*?<defName>$defName</defName>(.*?)</ThingDef>"
    )
    Assert-True $chipMatch.Success "$defName must exist in the formal mod."
    Assert-True (
        $chipMatch.Groups[1].Value -match "<texPath>$expectedTextureDefPath</texPath>"
    ) "$defName must use the formal unified trigger chip texture."
}

# 图片必须随主模组发布，并保持用户确认的源图字节不变。
Assert-True (Test-Path -LiteralPath $triggerChipTexturePath -PathType Leaf) 'The formal unified trigger chip texture file must exist.'
$actualTextureHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $triggerChipTexturePath).Hash
Assert-True ($actualTextureHash -eq $expectedTextureHash) 'The formal unified trigger chip texture must match the approved source PNG exactly.'

Write-Output 'FormalTriggerChipTexture PASS'
