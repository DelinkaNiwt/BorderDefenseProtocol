# 远程模块增强武装构型白名单边界测试。

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

$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$configPath = Join-Path $modRoot 'Source\BDP\Core\Expressions\Config\RangedModuleAugmentationConfig.cs'
$resolverPath = Join-Path $modRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionAugmentationResolver.cs'
$presetPath = Join-Path $modRoot '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'

foreach ($path in @($configPath, $resolverPath, $presetPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('缺少测试目标文件：' + $path)
}

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding UTF8
$presetText = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8

Assert-True ($configText -match 'List<string>\s+AllowedSourceVariantKeys\s*=\s*new List<string>\(\)') `
    '远程增强配置必须声明默认空的来源变体白名单。'
Assert-True ($configText -match 'AllowedSourceVariantKeys\s*=\s*new List<string>\(\)') `
    '增强配置 Clone 必须初始化并复制来源变体白名单。'
Assert-True ($resolverText -match 'AllowedSourceVariantKeys') `
    '远程增强解析器必须消费来源变体白名单。'
Assert-True ($resolverText -match 'SourceVariantKey') `
    '远程增强解析器必须按目标 SourceVariantKey 筛选构型。'
Assert-True ($resolverText -notmatch 'BDP_ArmamentForm_') `
    'Core 解析器不得硬编码具体武装构型标识。'

function Get-DefBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    return [regex]::Match(
        $Text,
        '(?s)<defName>' + [regex]::Escape($DefName) + '</defName>.*?</BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef>'
    ).Value
}

$modifiedBlock = Get-DefBlock $presetText 'BDP_ChipFunction_LeadShotModified'
$oppositeBlock = Get-DefBlock $presetText 'BDP_ChipFunction_LeadShotOppositeSide'
Assert-True (-not [string]::IsNullOrWhiteSpace($modifiedBlock)) '铅弹·改 Def 必须存在。'
Assert-True (-not [string]::IsNullOrWhiteSpace($oppositeBlock)) '铅弹 Def 必须存在。'

$allowListMatch = [regex]::Match(
    $modifiedBlock,
    '(?s)<AllowedSourceVariantKeys>(?<body>.*?)</AllowedSourceVariantKeys>'
)
Assert-True $allowListMatch.Success '铅弹·改必须声明来源变体白名单。'
$allowListValues = @(
    [regex]::Matches($allowListMatch.Groups['body'].Value, '<li>\s*(?<key>[^<]+?)\s*</li>') |
        ForEach-Object { $_.Groups['key'].Value.Trim() }
)
Assert-True ($allowListValues.Count -eq 1 -and $allowListValues[0] -eq 'BDP_ArmamentForm_Revolver') `
    '铅弹·改当前必须只允许左轮手枪构型。'
Assert-True ($oppositeBlock -notmatch '<AllowedSourceVariantKeys>') `
    '新铅弹本次不得继承铅弹·改的构型白名单。'

Write-Output 'RangedModuleAugmentationVariantFilterBoundarySmokeTests PASS'
