$ErrorActionPreference = 'Stop'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) {
        throw "FAIL: $message"
    }
}

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$comboSourceFiles = @(
    (Join-Path $projectRoot 'Source\BDP\Core\Combos'),
    (Join-Path $projectRoot 'Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs'),
    (Join-Path $projectRoot 'Source\BDP\Core\Expressions\Model\ExpressionRuntimePayload.cs'),
    (Join-Path $projectRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'),
    (Join-Path $projectRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Validation\ChipManufacturingDefValidator.cs')
)

$texts = @()
foreach ($path in $comboSourceFiles) {
    if (Test-Path $path -PathType Container) {
        $texts += Get-ChildItem $path -Filter '*.cs' -File -Recurse | ForEach-Object {
            Get-Content -Raw $_.FullName
        }
    }
    else {
        $texts += Get-Content -Raw $path
    }
}

$activeComboXml = Get-ChildItem (Join-Path $projectRoot '1.6\Content\Defs\ComboDef') -Filter '*.xml' -File
foreach ($xml in $activeComboXml) {
    $texts += Get-Content -Raw $xml.FullName
}

$combined = $texts -join "`n"
Assert-True ($combined -notmatch '\bchipA\b|\bchipB\b|\bChipA\b|\bChipB\b|FollowChipA|FollowChipB') '本次组合技正式代码和 XML 不得继续使用字母占位式来源标识。'
Assert-True ($combined -match 'FirstSource|SecondSource|firstSource|secondSource') '组合技正式代码必须使用第一来源/第二来源语义标识。'
Assert-True (($activeComboXml | ForEach-Object { Get-Content -Raw $_.FullName } | Select-String -Pattern '<firstSource|<secondSource' -Quiet)) '组合技正式 XML 必须使用第一来源/第二来源字段。'

Write-Host 'PASS: 组合技正式来源标识已统一为第一来源/第二来源语义。'
