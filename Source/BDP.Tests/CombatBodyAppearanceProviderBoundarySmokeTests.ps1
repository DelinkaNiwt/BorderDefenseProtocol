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
$appearanceRoot = Join-Path $sourceRoot 'BDP.Content\CombatBody\Appearance'
$providerPath = Join-Path $appearanceRoot 'CombatBodyAppearanceProvider.cs'
$resolverPath = Join-Path $appearanceRoot 'CombatBodyAppearanceTemplateResolver.cs'
$bootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'

foreach ($path in @($providerPath, $resolverPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "个人外观提供器文件缺失：$path"
}

$provider = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$resolver = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$bootstrap = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8

Assert-True ($provider -match 'ICombatBodyFrontAppearanceProvider') '个人外观提供器必须实现 Core 外部契约。'
Assert-True ($provider -match 'CombatBodyAppearanceLedgerService') '个人外观提供器必须通过账本服务读取当前 Pawn 档案。'
Assert-True ($provider -match 'TryResolve\s*\(\s*Pawn\s+pawn') '个人外观提供器必须按 Pawn 解析。'
Assert-True ($resolver -match 'CombatBodyFrontAppearancePlan') '个人模板解析器必须输出 Core 外观计划。'
Assert-True ($resolver -match 'Mode\s*=\s*CombatBodyFrontMode\.MirrorOriginal') '个人镜像档案必须输出镜像计划。'
Assert-True ($resolver -match 'CombatBodyFrontMode\.Preset') '个人预设档案必须输出预设计划。'
Assert-True ($resolver -match 'IsDynamic\s*=\s*true') '个人外观计划必须标记为动态计划。'
Assert-True ($resolver -match 'ApparelDef\s*==\s*null|StuffDef\s*==\s*null') '缺失服装或材料 Def 时必须跳过失效条目。'
Assert-True ($resolver -match 'BdpDiagnostics\.Once') '失效模板条目必须写入一次性诊断日志。'
Assert-True ($resolver -match 'Entries\.Count\s*==\s*0|Entries\s*==\s*null') '合法空模板必须保留为空预设计划。'
Assert-True ($resolver -match 'SelectedTemplateId\s*=\s*CombatBodyAppearanceProfile\.NoTemplateId') '全失效模板必须清除选中模板。'
Assert-True ($resolver -match 'Mode\s*=\s*CombatBodyFrontMode\.MirrorOriginal') '全失效模板必须回退镜像模式。'
Assert-True ($bootstrap -match 'CombatBodyFrontAppearanceRegistry\.Register') 'Content 启动入口必须注册个人外观提供器。'

Write-Output 'CombatBodyAppearanceProviderBoundarySmokeTests PASS'
