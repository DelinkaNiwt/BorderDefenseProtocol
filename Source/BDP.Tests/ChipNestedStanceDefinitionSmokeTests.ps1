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
$coreRoot = Join-Path $sourceRoot 'BDP\Core'
$modeConfigPath = Join-Path $coreRoot 'Expressions\Config\ChipExpressionModeConfig.cs'
$stanceConfigPath = Join-Path $coreRoot 'Expressions\Config\ChipExpressionStanceConfig.cs'
$modeContractPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionModeContract.cs'
$stanceContractPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionStanceContract.cs'
$validationPath = Join-Path $coreRoot 'Expressions\Validation\ChipExpressionStructureValidation.cs'

foreach ($path in @($modeConfigPath, $stanceConfigPath, $modeContractPath, $stanceContractPath, $validationPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('嵌套姿态设施缺少文件：' + $path)
}

$modeConfigText = Get-Content -LiteralPath $modeConfigPath -Raw -Encoding UTF8
$stanceConfigText = Get-Content -LiteralPath $stanceConfigPath -Raw -Encoding UTF8
$modeContractText = Get-Content -LiteralPath $modeContractPath -Raw -Encoding UTF8
$stanceContractText = Get-Content -LiteralPath $stanceContractPath -Raw -Encoding UTF8
$validationText = Get-Content -LiteralPath $validationPath -Raw -Encoding UTF8

Assert-True (
    ($modeConfigText -match 'string\s+DefaultStanceKey') -and
    ($modeConfigText -match 'List<ChipExpressionStanceConfig>\s+Stances')
) '形态配置必须拥有默认姿态键和姿态列表。'

Assert-True (
    ($stanceConfigText -match 'string\s+StanceKey') -and
    ($stanceConfigText -match 'string\s+DisplayLabel') -and
    ($stanceConfigText -match 'string\s+DisplayLabelKey') -and
    ($stanceConfigText -match 'string\s+GizmoIconTexPath') -and
    ($stanceConfigText -match 'List<string>\s+ActiveEntryIds')
) '姿态配置必须声明稳定键、可本地化名称、按钮图标和附加表达条目。'

Assert-True (
    ($modeContractText -match 'string\s+DefaultStanceKey') -and
    ($modeContractText -match 'List<ChipExpressionStanceContract>\s+Stances') -and
    ($stanceContractText -match 'string\s+StanceKey') -and
    ($stanceContractText -match 'List<string>\s+ActiveEntryIds')
) '正式表达契约必须完整保留姿态选择表。'

Assert-True (
    ($validationText -match 'ValidateModeStances') -and
    ($validationText -match 'DefaultStanceKey') -and
    ($validationText -match 'StanceKey') -and
    ($validationText -match 'ValidateParentOrder')
) '统一结构校验必须检查姿态默认键、唯一键、引用和最终父子顺序。'

Write-Output 'ChipNestedStanceDefinitionSmokeTests PASS'
