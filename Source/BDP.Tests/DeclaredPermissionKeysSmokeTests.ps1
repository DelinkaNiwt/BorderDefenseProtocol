$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$requiredFiles = @(
    'BDP\Core\Expressions\Config\ChipExpressionEntryConfig.cs',
    'BDP\Core\Expressions\Contract\ChipExpressionEntryContract.cs',
    'BDP\Core\Expressions\Model\ExpressionSourceDeclaration.cs',
    'BDP\Core\Expressions\Model\ExpressionSourceMaterial.cs',
    'BDP\Core\Expressions\Model\FormalExpressionResult.cs',
    'BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs',
    'BDP\Core\Combos\Config\ComboExpressionEntryCloneService.cs',
    'BDP\Core\Expressions\Contract\ChipExpressionContractInterpreter.cs',
    'BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs',
    'BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs',
    'BDP\Core\Expressions\Pipeline\DefaultExpressionSourceDeclarationProvider.cs',
    'BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs',
    'BDP\Core\Expressions\Pipeline\ExpressionSourceCollector.cs',
    'BDP\Core\Expressions\Pipeline\SingleSideExpressionBuilder.cs',
    'BDP\Core\AttackExecution\RangedModules\Runtime\RangedAttackModuleResolver.cs',
    'BDP\Core\AttackExecution\RangedModules\Runtime\RangedAttackModuleRuntimeHost.cs',
    'BDP\Core\Permissions\MapPermissionContext.cs'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $sourceRoot $relativePath
    Assert-True (Test-Path $path) "缺少许可声明传播文件：$path"
    $text = Get-Content -Raw $path
    Assert-True ($text -match 'DeclaredPermissionKeys') "文件必须使用 DeclaredPermissionKeys：$relativePath"
    if ($relativePath -notlike '*MapPermissionContext.cs') {
        Assert-True ($text -notmatch '\bPermissionKeys\b') "表达传播文件不得保留旧字段 PermissionKeys：$relativePath"
    }
}

$clonePath = Join-Path $sourceRoot 'BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs'
$cloneText = Get-Content -Raw $clonePath
Assert-True ($cloneText -match 'DeclaredPermissionKeys\s*=\s*source\.DeclaredPermissionKeys\s*!=\s*null\s*\?\s*new List<string>\(source\.DeclaredPermissionKeys\)\s*:\s*null') '制造芯片表达克隆必须保留条目声明的许可键列表。'

$presetsPath = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$presets = Get-Content -Raw $presetsPath
$fujinBlock = [regex]::Match($presets, '(?s)<defName>BDP_ChipFunction_FujinRangedStrike</defName>.*?</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>')
$entityBladeBlock = [regex]::Match($presets, '(?s)<defName>BDP_ChipFunction_Fujin</defName>.*?</BDP\.Content\.Assembly\.ChipManufacturing\.Defs\.ChipManufacturingPresetDef>')
Assert-True $fujinBlock.Success '找不到风刃芯片 Def。'
Assert-True $entityBladeBlock.Success '找不到风刃·实体刀芯片 Def。'
Assert-True ($fujinBlock.Value -match '<DeclaredPermissionKeys>\s*<li>BDP\.OperatorConsole\.Active</li>\s*</DeclaredPermissionKeys>') '风刃必须显式声明操作员控制台许可键。'
Assert-True ($entityBladeBlock.Value -notmatch '<DeclaredPermissionKeys>') '风刃·实体刀不得隐式接入操作员控制台许可。'

Write-Host 'DeclaredPermissionKeys PASS'
