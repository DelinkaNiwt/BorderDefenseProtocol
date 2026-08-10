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
$fleckDefsPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Wounds\Visuals\WoundSprayFleckDefs.cs'

Assert-True (Test-Path -LiteralPath $fleckDefsPath) 'WoundSprayFleckDefs.cs must exist.'

$text = Get-Content -LiteralPath $fleckDefsPath -Raw -Encoding utf8

Assert-True ($text -match 'namespace\s+BDP\.Content\.CombatBody\.Wounds\.Visuals') 'WoundSprayFleckDefs must live in Content wound visuals namespace.'
Assert-True ($text -match '\[StaticConstructorOnStartup\]') 'WoundSprayFleckDefs must load FleckDef refs at static startup.'
Assert-True ($text -match 'internal\s+static\s+class\s+WoundSprayFleckDefs') 'WoundSprayFleckDefs must be an internal static class.'
Assert-True ($text -match 'BDP_Fleck_LeakCore') 'WoundSprayFleckDefs must reference core leak FleckDef.'
Assert-True ($text -match 'BDP_Fleck_LeakMid') 'WoundSprayFleckDefs must reference mid leak FleckDef.'
Assert-True ($text -match 'BDP_Fleck_LeakOuter') 'WoundSprayFleckDefs must reference outer leak FleckDef.'
Assert-True ($text -match '/// <summary>') 'WoundSprayFleckDefs members must be documented.'

Write-Output 'CombatBodyWoundSprayFleckDefSmokeTests PASS'
