$ErrorActionPreference = "Stop"

# 统一断言入口，失败时直接指出具体芯片或资源契约。
function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 定位 DevHarness 的芯片定义与目标贴图。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$modsRoot = Split-Path -Parent $modRoot
$devHarnessRoot = Join-Path $modsRoot 'BorderDefenseProtocol.DevHarness'
$chipDefsRoot = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips'
$triggerChipTexturePath = Join-Path $devHarnessRoot '1.6\Textures\Things\Trigger\Chip\BDP_TriggerChip.png'
$expectedTextureDefPath = 'Things/Trigger/Chip/BDP_TriggerChip'
$expectedTextureHash = '46D747674E99BF4ED5BFD409162FF442B9E0C99802EA560876E86840689B544A'

# 收集全部直接继承芯片基类的物品定义。
$chipBlocks = @()
$chipDefFiles = Get-ChildItem -LiteralPath $chipDefsRoot -Recurse -File -Filter '*.xml'
foreach ($chipDefFile in $chipDefFiles) {
    $chipDefText = Get-Content -LiteralPath $chipDefFile.FullName -Raw -Encoding utf8
    $chipBlocks += [regex]::Matches(
        $chipDefText,
        '(?s)<ThingDef\s+ParentName="BDP_ChipBase"[^>]*>.*?</ThingDef>'
    )
}

Assert-True ($chipBlocks.Count -eq 16) "DevHarness must keep exactly 16 direct BDP_ChipBase chip items; actual: $($chipBlocks.Count)."

# 每个芯片必须显式指向同一个语义清晰的物品贴图。
foreach ($chipBlock in $chipBlocks) {
    $defNameMatch = [regex]::Match($chipBlock.Value, '<defName>([^<]+)</defName>')
    Assert-True $defNameMatch.Success 'Every DevHarness chip item must declare a defName.'

    $defName = $defNameMatch.Groups[1].Value
    Assert-True (
        $chipBlock.Value -match "<texPath>$expectedTextureDefPath</texPath>"
    ) "$defName must use the unified trigger chip texture."
}

# 目标图片必须存在，并与用户确认的源图逐字节一致。
Assert-True (Test-Path -LiteralPath $triggerChipTexturePath -PathType Leaf) 'The unified trigger chip texture file must exist.'
$actualTextureHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $triggerChipTexturePath).Hash
Assert-True ($actualTextureHash -eq $expectedTextureHash) 'The unified trigger chip texture must match the approved source PNG exactly.'

Write-Output 'DevHarnessTriggerChipTexture PASS'
