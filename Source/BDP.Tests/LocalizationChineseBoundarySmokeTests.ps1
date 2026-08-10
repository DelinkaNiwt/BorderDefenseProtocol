$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 语言目录只提供简体中文；后续新增语言时，应在这里显式扩展检查范围。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$languageRoot = Join-Path $modRoot 'Languages'
$chineseRoot = Join-Path $languageRoot 'ChineseSimplified (简体中文)'

Assert-True (Test-Path -LiteralPath $chineseRoot) 'ChineseSimplified language directory is missing.'
$languageDirectories = @(Get-ChildItem -LiteralPath $languageRoot -Directory)
Assert-True ($languageDirectories.Count -eq 1 -and $languageDirectories[0].Name -eq 'ChineseSimplified (简体中文)') 'Only the approved Simplified Chinese language directory may be present.'

$keyedRoot = Join-Path $chineseRoot 'Keyed'
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $modRoot 'Source') -Recurse -Filter *.cs | Where-Object {
    $_.FullName -match 'Source\\BDP(\\|$)' -or $_.FullName -match 'Source\\BDP\.Content(\\|$)'
})
$sourceKeys = @{}
foreach ($file in $sourceFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach ($match in [regex]::Matches($text, '"(?<key>BDP_[A-Za-z0-9_]+)"\s*\.\s*Translate(?:Formatted)?\s*\(')) {
        $sourceKeys[$match.Groups['key'].Value] = $true
    }
}

$keyedKeys = @{}
foreach ($file in @(Get-ChildItem -LiteralPath $keyedRoot -Filter *.xml)) {
    $document = New-Object System.Xml.XmlDocument
    $document.Load($file.FullName)
    Assert-True ($document.DocumentElement.Name -eq 'LanguageData') ($file.Name + ' must use the RimWorld LanguageData root.')
    foreach ($node in $document.DocumentElement.ChildNodes) {
        if ($node.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
        Assert-True (-not $keyedKeys.ContainsKey($node.Name)) ('Duplicate keyed localization key: ' + $node.Name)
        $keyedKeys[$node.Name] = $true
    }
}

$missingKeys = @($sourceKeys.Keys | Where-Object { -not $keyedKeys.ContainsKey($_) })
Assert-True ($missingKeys.Count -eq 0) ('Source translation keys missing from Keyed XML: ' + ($missingKeys -join ', '))

$defInjectedRoot = Join-Path $chineseRoot 'DefInjected'
$defInjectedKeys = @{}
foreach ($file in @(Get-ChildItem -LiteralPath $defInjectedRoot -Recurse -Filter *.xml)) {
    $document = New-Object System.Xml.XmlDocument
    $document.Load($file.FullName)
    Assert-True ($document.DocumentElement.Name -eq 'LanguageData') ($file.Name + ' must use the RimWorld LanguageData root.')
    foreach ($node in $document.DocumentElement.ChildNodes) {
        if ($node.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
        Assert-True (-not $defInjectedKeys.ContainsKey($node.Name)) ('Duplicate DefInjected key: ' + $node.Name)
        $defInjectedKeys[$node.Name] = $true
    }
}

Write-Output ('LocalizationChineseBoundarySmokeTests PASS (Keyed=' + $keyedKeys.Count + ', DefInjected=' + $defInjectedKeys.Count + ')')
