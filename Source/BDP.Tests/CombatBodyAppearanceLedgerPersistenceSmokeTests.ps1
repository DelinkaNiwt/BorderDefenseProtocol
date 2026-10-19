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
$templateEntryPath = Join-Path $appearanceRoot 'CombatBodyAppearanceTemplateEntry.cs'
$templatePath = Join-Path $appearanceRoot 'CombatBodyAppearanceTemplate.cs'
$profilePath = Join-Path $appearanceRoot 'CombatBodyAppearanceProfile.cs'
$ledgerPath = Join-Path $appearanceRoot 'GameComponent_CombatBodyAppearanceTemplates.cs'
$servicePath = Join-Path $appearanceRoot 'CombatBodyAppearanceLedgerService.cs'

foreach ($path in @($templateEntryPath, $templatePath, $profilePath, $ledgerPath, $servicePath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "个人模板持久化文件缺失：$path"
}

$allSource = ($templateEntryPath, $templatePath, $profilePath, $ledgerPath, $servicePath | ForEach-Object {
    Get-Content -LiteralPath $_ -Raw -Encoding utf8
}) -join "`n"

$ledger = Get-Content -LiteralPath $ledgerPath -Raw -Encoding utf8
$service = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8

Assert-True ($ledger -match 'class\s+GameComponent_CombatBodyAppearanceTemplates\s*:\s*GameComponent') '个人模板账本必须使用 GameComponent。'
Assert-True ($ledger -match 'Scribe_Collections\.Look') '个人模板账本必须持久化小人档案集合。'
Assert-True ($ledger -match 'LookMode\.Deep') '个人模板账本必须深度持久化模板内容。'
Assert-True ($allSource -match 'Scribe_References\.Look') '个人模板档案必须保存 Pawn 引用。'
Assert-True ($allSource -match 'Scribe_Defs\.Look') '模板条目必须保存服装、材料和风格 Def。'
Assert-True ($allSource -match 'Scribe_Values\.Look') '模板条目必须保存颜色与模式等值字段。'
Assert-True ($allSource -match 'PawnThingId|ThingID') '个人模板必须保留 Pawn 的稳定 ThingID。'
Assert-True ($service -match 'Current\.Game') '账本服务必须从当前游戏解析 GameComponent。'
Assert-True ($service -match 'GetComponent<GameComponent_CombatBodyAppearanceTemplates>') '账本服务必须读取游戏级组件。'
# GameComponent_CombatBodyAppearanceTemplates 是本测试要求的存档组件，不能被“包含 Comp”误判为 Pawn Comp。
Assert-True (-not ($allSource -match 'class\s+(?:Comp|ThingComp|HediffComp)\w*\b|class\s+\w*CompProperties\w*\b')) '个人模板不得通过 Pawn Comp 注入。'
Assert-True (-not ($allSource -match '\bMap\b\s+[A-Za-z_]')) '个人模板账本不得绑定单张地图。'

Write-Output 'CombatBodyAppearanceLedgerPersistenceSmokeTests PASS'
