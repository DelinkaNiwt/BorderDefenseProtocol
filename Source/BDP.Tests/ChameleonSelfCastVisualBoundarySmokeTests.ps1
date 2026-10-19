$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$selfVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\BdpVerb_CastSelfAbility.cs'
$selfStancePath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\BdpStance_SelfCastWarmup.cs'
$generalVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\BdpVerb_CastAbility.cs'
$abilityPath = Join-Path $repoRoot '1.6\Content\Defs\AbilityDef\Chameleon.xml'

Assert-True (Test-Path -LiteralPath $selfVerbPath) 'Generic self-cast Ability Verb must exist.'
Assert-True (Test-Path -LiteralPath $selfStancePath) 'Generic self-cast warmup stance must exist.'
Assert-True (Test-Path -LiteralPath $generalVerbPath) 'General Ability Verb must exist.'
Assert-True (Test-Path -LiteralPath $abilityPath) 'Chameleon Ability XML must exist.'

$selfVerbText = Get-Content -LiteralPath $selfVerbPath -Raw -Encoding utf8
$selfStanceText = Get-Content -LiteralPath $selfStancePath -Raw -Encoding utf8
$generalVerbText = Get-Content -LiteralPath $generalVerbPath -Raw -Encoding utf8
$abilityText = Get-Content -LiteralPath $abilityPath -Raw -Encoding utf8
$selfVerbExecutableText = [regex]::Replace($selfVerbText, '(?m)//.*$', '')

Assert-True ($selfVerbText -match 'class\s+BdpVerb_CastSelfAbility\s*:\s*BdpVerb_CastAbility') `
    'Self-cast Verb must reuse the general BDP Ability Verb.'
Assert-True ($selfVerbText -match 'override\s+bool\s+TryStartCastOn') `
    'Self-cast Verb must own its cast-start path.'
Assert-True ($selfVerbText -match 'new\s+BdpStance_SelfCastWarmup') `
    'Self-cast Verb must use the generic self-cast warmup stance.'
Assert-True ($selfVerbExecutableText -notmatch 'Notify_WarmingCastAlongLine|TryFindShootLineFromTo') `
    'Self-cast Verb must not enter the ranged aiming preparation path.'
Assert-True ($selfVerbText -match 'TriggerCastJitter') `
    'Self-cast Verb must explicitly suppress inherited cast jitter.'
Assert-True ($selfVerbText -notmatch 'Chameleon|变色龙') `
    'Generic self-cast Verb must not contain Chameleon-specific coupling.'
Assert-True ($selfStanceText -match 'class\s+BdpStance_SelfCastWarmup\s*:\s*Stance_Warmup') `
    'Self-cast warmup stance must reuse vanilla warmup timing and lifecycle.'
Assert-True ($selfStanceText -match 'neverAimWeapon\s*=\s*true') `
    'Self-cast warmup stance must disable vanilla weapon aiming.'
Assert-True ($selfStanceText -match 'override\s+void\s+StanceDraw') `
    'Self-cast warmup stance must own its visual indicator.'
Assert-True ($selfStanceText -notmatch 'DrawAimPie') `
    'Self-cast warmup stance must not draw the vanilla aim pie.'
Assert-True ($selfStanceText -match 'DrawCooldownCircle') `
    'Self-cast warmup stance must reuse the vanilla circular cooldown indicator.'
Assert-True ($selfStanceText -notmatch 'DrawLineBetween|DrawCircleOutline') `
    'Self-cast warmup stance must not replace the vanilla circular indicator with a custom line ring.'
Assert-True ($selfStanceText -notmatch 'Chameleon|变色龙') `
    'Generic self-cast warmup stance must not contain Chameleon-specific coupling.'
Assert-True ($generalVerbText -match 'TriggerCastJitter') `
    'General Ability Verb must retain its existing ranged cast jitter hook.'
Assert-True ($abilityText -match '<verbClass>BDP\.Core\.Abilities\.BdpVerb_CastSelfAbility</verbClass>') `
    'Chameleon must select the generic self-cast Verb.'

Write-Output 'ChameleonSelfCastVisualBoundarySmokeTests PASS'
