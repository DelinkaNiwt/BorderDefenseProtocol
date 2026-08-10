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

function Get-Section {
    param(
        [string]$Text,
        [string]$StartMarker,
        [string]$EndMarker
    )

    $start = $Text.IndexOf($StartMarker)
    $end = $Text.IndexOf($EndMarker, $start + $StartMarker.Length)
    Assert-True ($start -ge 0 -and $end -gt $start) "找不到代码区段：$StartMarker"
    return $Text.Substring($start, $end - $start)
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$providerPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyTransformScanPresentationProvider.cs'
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8

Assert-True ($providerText -match 'private static bool ShouldPresentTransform\(') '扫描提供器必须集中判断表现资格。'
$beginSection = Get-Section $providerText 'public void Begin(Pawn pawn, CombatBodyTransformDirection direction)' 'public void End(Pawn pawn, CombatBodyTransformDirection direction)'
$endSection = Get-Section $providerText 'public void End(Pawn pawn, CombatBodyTransformDirection direction)' 'private static bool ShouldPresentTransform('
$predicateSection = Get-Section $providerText 'private static bool ShouldPresentTransform(' 'private static bool CanPresent('

Assert-True ($providerText -match 'using BDP\.Core\.CombatBody;') '扫描提供器必须通过 Core 只读战斗体表面读取阶段。'
Assert-True ($predicateSection -match 'direction != CombatBodyTransformDirection\.Exit[\s\S]*return true;') '进入方向和非退出方向必须继续允许扫描。'
Assert-True ($predicateSection -match 'ICombatBodyReader combatBodyReader = CombatBodySurfaceAccess\.ResolveReader\(pawn\);') '退出方向必须读取正式战斗体阶段。'
Assert-True ($predicateSection -match 'combatBodyReader == null[\s\S]*combatBodyReader\.Phase != CombatBodyPhase\.Collapsing') '只有明确处于崩解阶段时才禁止扫描，缺少读取器时保持旧行为。'
Assert-True ($beginSection -match '!ShouldPresentTransform\(pawn, direction\)[\s\S]*return;[\s\S]*CombatBodyPawnVisualCapture\.Capture\(pawn\)') '崩解退出必须在捕获人物画面前返回。'
Assert-True ($endSection -match 'pending\.Direction != direction[\s\S]*!ShouldPresentTransform\(pawn, direction\)[\s\S]*CombatBodyPawnVisualCapture\.Release\(pending\.OutgoingSnapshot\)[\s\S]*return;') '结束端发现崩解时必须释放残留快照且不生成 Mote。'

Write-Output 'CombatBodyTransformCollapseSuppression PASS'
