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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessSourceRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$pathLatchPath = Join-Path $devHarnessSourceRoot 'RangedModules\Samples\PathLatchModule.cs'
$pathLatchText = Read-Source $pathLatchPath

$mojibakePattern = '[\uE000-\uF8FF]|\u20AC|\u59e3\u6395\u6ce7|\u8930\u64b3\u58a0|\u74ba|\u95bf|\u9286\?'

Assert-True (
    $pathLatchText -notmatch $mojibakePattern
) 'PathLatchModule.cs contains mojibake Chinese text. Read and write this source as UTF-8.'

Assert-True (
    $pathLatchText -match '\u6bd2\u86c7\uff1a\u5f53\u524d\u6bb5\u76ee\u6807\u65e0\u6548\u3002'
) 'PathLatchModule.cs should keep readable UTF-8 Chinese reject reasons.'

Write-Output 'PathLatchUtf8ChineseSmokeTests PASS'
