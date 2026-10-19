# 远程模块增强作用范围边界测试。

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
$coreRoot = Join-Path $sourceRoot 'BDP\Core'
$configPath = Join-Path $coreRoot 'Expressions\Config\RangedModuleAugmentationConfig.cs'
$resolverPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionAugmentationResolver.cs'

foreach ($path in @($configPath, $resolverPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) ('缺少源码文件：' + $path)
}

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding UTF8

$scopeEnumMatch = [regex]::Match(
    $configText,
    '(?s)enum\s+RangedModuleAugmentationScope\s*\{(?<body>.*?)\}')
Assert-True $scopeEnumMatch.Success '必须声明远程模块增强作用范围枚举。'
Assert-True ($scopeEnumMatch.Groups['body'].Value -match '\bAllSides\b') `
    '作用范围枚举必须包含 AllSides。'
Assert-True ($scopeEnumMatch.Groups['body'].Value -match '\bOppositeSide\b') `
    '作用范围枚举必须包含 OppositeSide。'

Assert-True ($configText -match `
    'RangedModuleAugmentationScope\s+Scope\s*=\s*RangedModuleAugmentationScope\.AllSides') `
    '作用范围字段默认值必须是 AllSides。'
Assert-True ($configText -match 'Scope\s*=\s*Scope') `
    '增强配置 Clone 必须复制 Scope。'

Assert-True ($resolverText -match 'RangedModuleAugmentationScope') `
    '增强解析器必须消费通用作用范围类型。'
Assert-True ($resolverText -match 'OppositeSide') `
    '增强解析器必须实现 OppositeSide 分支。'
Assert-True ($resolverText -match 'SourceReference\.Side') `
    '增强解析器必须根据来源结果和目标结果的 SourceReference.Side 判断侧别。'
Assert-True ($resolverText -match 'TriggerSide\.Main[\s\S]*TriggerSide\.Sub|TriggerSide\.Sub[\s\S]*TriggerSide\.Main') `
    '对侧判断必须明确覆盖 Main 与 Sub 的相反关系。'
Assert-True ($resolverText -notmatch 'LeadShot|LeadShotModified|LeadShotOppositeSide') `
    'Core 远程增强解析器不得硬编码铅弹业务标识。'

Write-Output 'RangedModuleAugmentationScopeBoundarySmokeTests PASS'
