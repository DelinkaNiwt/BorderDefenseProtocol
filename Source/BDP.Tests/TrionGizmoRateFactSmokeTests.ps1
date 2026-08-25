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
$repoRoot = Split-Path -Parent $sourceRoot
$gizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'

Assert-True (Test-Path -LiteralPath $gizmoPath) 'Gizmo_TrionStatus.cs must exist.'

$gizmoText = Get-Content -LiteralPath $gizmoPath -Raw -Encoding utf8
$rateStart = $gizmoText.IndexOf('private string BuildRateText()')
$rateEnd = $gizmoText.IndexOf('private void DrawBar', $rateStart)
Assert-True (($rateStart -ge 0) -and ($rateEnd -gt $rateStart)) 'Gizmo_TrionStatus must keep BuildRateText before DrawBar.'
$rateText = $gizmoText.Substring($rateStart, $rateEnd - $rateStart)

Assert-True ($rateText -match 'reader\.TotalDrainPerSecond\s*>\s*0f') 'Rate text must display drain from the formal aggregate drain fact, including empty-chip combat bodies.'
Assert-True ($rateText -notmatch 'reader\.Allocated') 'Rate text must not infer activity or drain from allocated capacity.'

Write-Output 'TrionGizmoRateFactSmokeTests PASS'
