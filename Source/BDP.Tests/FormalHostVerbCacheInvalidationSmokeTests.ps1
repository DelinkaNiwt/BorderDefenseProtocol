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
$formalHostShootPath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_FormalHostShoot.cs'
$formalHostShootText = Get-Content -LiteralPath $formalHostShootPath -Raw -Encoding UTF8

# 原版 Verb 私有缓存依赖当前 verbProps；正式宿主切换绑定时必须显式清理两项缓存。
Assert-True (
    $formalHostShootText -match 'cachedTicksBetweenBurstShots'
) 'Formal host must address the vanilla cached burst interval when its binding surface changes.'

Assert-True (
    $formalHostShootText -match 'cachedBurstShotCount'
) 'Formal host must address the vanilla cached burst count when its binding surface changes.'

Assert-True (
    $formalHostShootText -match 'InvalidateVanillaVerbPropertyCaches\('
) 'Formal host must centralize vanilla Verb cache invalidation.'

$resetBoundary = [regex]::Match(
    $formalHostShootText,
    '(?s)if \(resetApplied\).*?verbProps = bindingState'
)
Assert-True $resetBoundary.Success 'Formal host binding sync must keep a reset boundary before assigning the new verbProps.'
Assert-True (
    $resetBoundary.Value -match '(?s)Reset\(\).*InvalidateVanillaVerbPropertyCaches\(' -or
    $resetBoundary.Value -match '(?s)InvalidateVanillaVerbPropertyCaches\(\).*Reset\('
) 'Formal host binding reset must invalidate vanilla Verb caches before the new binding surface is consumed.'

Write-Output 'FormalHostVerbCacheInvalidationSmokeTests PASS'
