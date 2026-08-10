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

function Get-ChipBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<ThingDef\s+ParentName=""ResourceBase"">\s*<defName>$DefName</defName>.*?</ThingDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-XmlValue {
    param(
        [string]$Text,
        [string]$ElementName
    )

    $match = [regex]::Match($Text, "(?s)<$ElementName>\s*(.*?)\s*</$ElementName>")
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups[1].Value.Trim()
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'

$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8

$pathLatchBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipPathLatch'
$volleyBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipRangedVolley'
$pathLatchVolleyBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipPathLatchVolley'

Assert-True ($pathLatchBlock -ne $null) 'DevHarness must keep the baseline PathLatch chip sample.'
Assert-True ($volleyBlock -ne $null) 'DevHarness must keep the baseline ranged volley chip sample.'
Assert-True ($pathLatchVolleyBlock -ne $null) 'DevHarness must add BDP_TestChipPathLatchVolley as the PathLatch volley variant.'

$volleyOriginSpread = Get-XmlValue $volleyBlock 'OriginSpread'

Assert-True (
    ($pathLatchVolleyBlock -match '<Execution>\s*<Rhythm>Simultaneous</Rhythm>\s*<OriginSpread>\s*<LateralMin>-0\.3</LateralMin>\s*<LateralMax>0\.3</LateralMax>\s*<ForwardMin>0</ForwardMin>\s*<ForwardMax>0\.105</ForwardMax>\s*</OriginSpread>\s*</Execution>') -and
    (Get-XmlValue $pathLatchVolleyBlock 'OriginSpread') -eq $volleyOriginSpread
) 'PathLatch volley chip must use the same simultaneous rhythm and random origin-spread range as the existing volley chip.'

Assert-True (
    $pathLatchVolleyBlock -match '<burstShotCount>5</burstShotCount>'
) 'PathLatch volley chip must fire 5 projectiles.'

Assert-True (
    ($pathLatchVolleyBlock -match '<DirectTargetLineOfSight>NotRequired</DirectTargetLineOfSight>') -and
    ($pathLatchVolleyBlock -match '<moduleDef>BDP_TestRangedPathLatchModule</moduleDef>') -and
    ($pathLatchVolleyBlock -match '<li>PathLatchChip</li>')
) 'PathLatch volley chip must keep the PathLatch targeting module and non-direct-LOS semantics.'

Assert-True (
    ($pathLatchVolleyBlock -match '<range>24</range>') -and
    ($pathLatchVolleyBlock -match '<warmupTime>0\.9</warmupTime>') -and
    ($pathLatchVolleyBlock -match '<ticksBetweenBurstShots>10</ticksBetweenBurstShots>') -and
    ($pathLatchVolleyBlock -match '<soundCast>Shot_Revolver</soundCast>') -and
    ($pathLatchVolleyBlock -match '<defaultCooldownTime>0\.9</defaultCooldownTime>')
) 'PathLatch volley chip must keep the baseline PathLatch combat parameters except rhythm, spread, and shot count.'

Write-Output 'DevHarnessPathLatchVolleyChipSmokeTests PASS'
