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
$defsRoot = Join-Path $repoRoot '1.6'
$xmlFiles = Get-ChildItem -LiteralPath $defsRoot -Recurse -Filter '*.xml' -File
$blackThingDefs = @()

foreach ($xmlFile in $xmlFiles) {
    [xml]$xml = Get-Content -LiteralPath $xmlFile.FullName -Raw -Encoding utf8
    foreach ($thingDef in $xml.SelectNodes('//ThingDef')) {
        $blackCategory = $thingDef.SelectSingleNode('.//triggerCategory[text()="BDP_TriggerCategory_Black"]')
        if ($blackCategory -ne $null) {
            $blackThingDefs += [pscustomobject]@{
                Path = $xmlFile.FullName
                Def = $thingDef
            }
        }
    }
}

foreach ($entry in $blackThingDefs) {
    $trionComp = $entry.Def.SelectSingleNode('.//comps/li[@Class="BDP.Core.Trion.CompProperties_Trion"]')
    Assert-True ($trionComp -ne $null) "黑触发体 Def 缺少 CompProperties_Trion：$($entry.Path)"

    $baseMax = $trionComp.SelectSingleNode('./baseMax')
    Assert-True ($baseMax -ne $null) "黑触发体 Def 缺少 baseMax：$($entry.Path)"
    $parsedBaseMax = 0.0
    Assert-True ([double]::TryParse($baseMax.InnerText, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsedBaseMax)) "黑触发体 baseMax 不是有效数字：$($entry.Path)"
    Assert-True ($parsedBaseMax -ge 0) "黑触发体 baseMax 不能为负数：$($entry.Path)"

    $startPercent = $trionComp.SelectSingleNode('./startPercent')
    Assert-True ($startPercent -eq $null -or $startPercent.InnerText -eq '1') "黑触发体 startPercent 必须为 1：$($entry.Path)"

    $recoveryPerDay = $trionComp.SelectSingleNode('./recoveryPerDay')
    Assert-True ($recoveryPerDay -eq $null -or $recoveryPerDay.InnerText -eq '0') "黑触发体未装备时不得独立自然恢复：$($entry.Path)"
}

Write-Output "TriggerBodyDefinitionBoundarySmokeTests PASS（黑触发体数量：$($blackThingDefs.Count)）"
