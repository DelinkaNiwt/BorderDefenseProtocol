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
$emitterPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Wounds\Visuals\CombatBodyWoundSprayEmitter.cs'

Assert-True (Test-Path -LiteralPath $emitterPath) 'CombatBodyWoundSprayEmitter.cs must exist.'

$text = Get-Content -LiteralPath $emitterPath -Raw -Encoding utf8

Assert-True ($text -match 'internal\s+sealed\s+class\s+CombatBodyWoundSprayEmitter') 'Emitter must be internal sealed.'
Assert-True ($text -match 'internal\s+int\s+HediffLoadId') 'Emitter must expose HediffLoadId.'
Assert-True ($text -match 'internal\s+void\s+Tick\(Pawn pawn\)') 'Emitter must expose Tick(Pawn pawn).'
Assert-True ($text -match 'internal\s+void\s+NotifyBurst\(\)') 'Emitter must expose NotifyBurst().'
Assert-True ($text -match 'internal\s+float\s+CutTilt') 'Emitter must expose CutTilt for runtime save-load.'
Assert-True ($text -match 'WoundSprayFleckDefs\.LeakCore') 'Emitter must emit core leak layer.'
Assert-True ($text -match 'WoundSprayFleckDefs\.LeakMid') 'Emitter must emit mid leak layer.'
Assert-True ($text -match 'WoundSprayFleckDefs\.LeakOuter') 'Emitter must emit outer leak layer.'
Assert-True ($text -match 'EmitInterval\s*=\s*3') 'Compromise wound spray cadence must stay at one emission per three ticks.'
Assert-True ($text -match 'EmitBurst\s*=\s*3') 'Compromise wound spray density must emit three particles per layer.'
Assert-True ($text -match 'BaseConeHalfAngle\s*=\s*6f') 'Compromise wound spray cone must stay narrow enough to read as a seam.'
Assert-True ($text -match 'Find\.CameraDriver\.CurrentViewRect\.ExpandedBy') 'Emitter must use camera culling.'
Assert-True ($text -match 'PawnDrawUtility\.FindAnchors') 'Emitter must use vanilla wound anchor lookup.'
Assert-True ($text -match 'PawnDrawUtility\.AnchorUsable') 'Emitter must use vanilla anchor usability checks.'
Assert-True ($text -match 'PawnDrawUtility\.CalcAnchorData') 'Emitter must use vanilla anchor offset calculation.'
Assert-True ($text -match 'PawnOverlayDrawer\.OverlayLayer') 'Emitter must filter or handle vanilla overlay layers.'
Assert-True ($text -notmatch ':\s*HediffComp') 'Emitter must not be a HediffComp.'
Assert-True ($text -match '/// <summary>') 'Emitter members must be documented.'

Write-Output 'CombatBodyWoundSprayEmitterSmokeTests PASS'
