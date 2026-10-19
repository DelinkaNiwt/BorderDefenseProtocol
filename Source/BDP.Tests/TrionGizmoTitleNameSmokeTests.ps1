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
$repoRoot = Split-Path -Parent $sourceRoot
$gizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'
$languagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\StatsReport.xml'

Assert-True -Condition (Test-Path -LiteralPath $gizmoPath) -Message 'Gizmo_TrionStatus.cs must exist.'
Assert-True -Condition (Test-Path -LiteralPath $languagePath) -Message 'StatsReport.xml must exist.'

$gizmoText = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding utf8
$titleStart = $gizmoText.IndexOf('private void DrawTitleRow')
$titleEnd = $gizmoText.IndexOf('private void DrawBottomRow', $titleStart)
Assert-True -Condition (($titleStart -ge 0) -and ($titleEnd -gt $titleStart)) -Message 'Gizmo_TrionStatus must keep a focused DrawTitleRow method.'

$titleText = $gizmoText.Substring($titleStart, $titleEnd - $titleStart)
$titleUsesShortOwnerName =
    ($titleText -match 'string ownerLabel = owner != null \? owner\.LabelShortCap : string\.Empty;') -and
    ($titleText -match 'BDP_Trion_Status_Title"\.Translate\(ownerLabel\)')
Assert-True -Condition $titleUsesShortOwnerName -Message 'Trion Gizmo title must pass owner.LabelShortCap into the localized title.'

$languageHasOwnerPlaceholder = $languageText.Contains('<BDP_Trion_Status_Title>Trion储量（{0}）</BDP_Trion_Status_Title>')
Assert-True -Condition $languageHasOwnerPlaceholder -Message 'Trion Gizmo title localization must include the owner-name placeholder.'

Write-Output 'TrionGizmoTitleNameSmokeTests PASS'
