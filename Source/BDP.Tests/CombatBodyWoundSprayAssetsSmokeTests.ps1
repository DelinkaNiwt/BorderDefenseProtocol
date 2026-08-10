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

$fleckXmlPath = Join-Path $repoRoot '1.6\Content\Defs\Flecks\FleckDefs_WoundSpray.xml'
$texturePath = Join-Path $repoRoot '1.6\Textures\BDP\Effects\LeakParticle.png'

Assert-True (Test-Path -LiteralPath $fleckXmlPath) 'Wound spray FleckDefs XML must exist.'
Assert-True (Test-Path -LiteralPath $texturePath) 'LeakParticle.png must exist in the main mod.'

$xmlText = Get-Content -LiteralPath $fleckXmlPath -Raw -Encoding utf8
[xml]$xml = $xmlText
$defNames = @($xml.Defs.FleckDef | ForEach-Object { $_.defName })

Assert-True ($xmlText -match 'encoding="utf-8"') 'Wound spray FleckDefs XML must declare UTF-8.'
Assert-True ($defNames -contains 'BDP_Fleck_LeakCore') 'Core wound spray FleckDef must exist.'
Assert-True ($defNames -contains 'BDP_Fleck_LeakMid') 'Mid wound spray FleckDef must exist.'
Assert-True ($defNames -contains 'BDP_Fleck_LeakOuter') 'Outer wound spray FleckDef must exist.'
Assert-True (([regex]::Matches($xmlText, '<texPath>BDP/Effects/LeakParticle</texPath>')).Count -eq 3) 'All wound spray FleckDefs must use LeakParticle.'
Assert-True (([regex]::Matches($xmlText, '<!--[\s\S]*?-->')).Count -ge 3) 'Each wound spray Fleck entry should have a Chinese comment.'
Assert-True ($xmlText -match '[\u4e00-\u9fff]') 'Wound spray Fleck XML comments must include Chinese text.'

Write-Output 'CombatBodyWoundSprayAssetsSmokeTests PASS'
