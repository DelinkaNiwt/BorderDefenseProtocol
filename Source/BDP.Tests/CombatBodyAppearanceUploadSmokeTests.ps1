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
$servicePath = Join-Path $appearanceRoot 'CombatBodyAppearanceTemplateService.cs'
$resultPath = Join-Path $appearanceRoot 'CombatBodyAppearanceUploadResult.cs'
$messagesPath = Join-Path $sourceRoot '..\Languages\ChineseSimplified (简体中文)\Keyed\Messages.xml'

foreach ($path in @($servicePath, $resultPath, $messagesPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "服装模板上传文件缺失：$path"
}

$service = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$result = Get-Content -LiteralPath $resultPath -Raw -Encoding utf8
$messages = Get-Content -LiteralPath $messagesPath -Raw -Encoding utf8

Assert-True ($service -match 'WornApparel') '上传服务必须读取 Pawn 当前穿着服装。'
Assert-True ($service -match 'ApparelDef|\.def') '上传模板必须保存服装定义。'
Assert-True ($service -match 'Stuff|\.Stuff') '上传模板必须保存实际材料。'
Assert-True ($service -match 'CompColorable|HasColor|\.Color') '上传模板必须保存染色状态和颜色。'
Assert-True ($service -match 'StyleDef') '上传模板必须保存服装风格。'
Assert-True ($service -match 'BDP_CombatBodyEditor_DefaultTemplateName.*Translate|Translate.*BDP_CombatBodyEditor_DefaultTemplateName') '自动模板名称必须来自语言键。'
Assert-True ($service -match 'inventory\.innerContainer|TryAdd') '上传服装必须转入 Pawn 背包。'
Assert-True ($service -match 'apparel\.Remove|pawn\.apparel\.Remove') '上传成功前必须从 Pawn 身上移除原服装。'
Assert-True ($service -match 'apparel\.Wear|pawn\.apparel\.Wear') '转移失败时必须恢复原服装。'
Assert-True ($service -match 'SelectTemplate') '上传成功后必须自动选中新模板。'
Assert-True (-not ($service -match 'SetMode')) '上传服务不得强制调用模式切换。'
Assert-True ($service -match 'TryAdd|TransferFailed|Rollback|rollback') '库存转移失败必须具备回滚路径。'
Assert-True (-not ($service -match '\.Destroy\s*\(')) '上传服务不得强制销毁原服装实体。'
Assert-True ($result -match 'class\s+CombatBodyAppearanceUploadResult') '上传服务必须返回结构化结果。'
Assert-True ($result -match 'FailureMessageKey|MessageKey') '上传失败结果必须返回语言键而不是硬编码文案。'
Assert-True ($messages -match 'BDP_CombatBodyEditor_DefaultTemplateName') '默认模板名称语言键必须存在。'

Write-Output 'CombatBodyAppearanceUploadSmokeTests PASS'
