# RimWorld 同一模组多加载目录语言文件遮蔽回归测试。

$ErrorActionPreference = "Stop"

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$loadFoldersPath = Join-Path $modRoot "LoadFolders.xml"
$mainCommandsPath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\Commands.xml"

# 原版按相对于各加载目录的路径去重；重复路径会使一个完整语言文件被静默跳过。
[xml]$loadFolders = Get-Content -Raw -LiteralPath $loadFoldersPath -Encoding UTF8
$loadedFolderNames = @($loadFolders.loadFolders.'v1.6'.li | ForEach-Object { [string]$_ })
$languageRelativePaths = @{}
foreach ($loadedFolderName in $loadedFolderNames)
{
    $loadedFolderPath = if ($loadedFolderName -eq "/")
    {
        $modRoot
    }
    else
    {
        Join-Path $modRoot $loadedFolderName
    }

    $loadedLanguageRoot = Join-Path $loadedFolderPath "Languages"
    if (-not (Test-Path -LiteralPath $loadedLanguageRoot)) { continue }

    foreach ($file in @(Get-ChildItem -LiteralPath $loadedLanguageRoot -Recurse -File))
    {
        $relativePath = $file.FullName.Substring($loadedFolderPath.Length).TrimStart('\')
        Assert-True (-not $languageRelativePaths.ContainsKey($relativePath)) (
            "加载目录间存在重复语言文件相对路径：$relativePath")
        $languageRelativePaths[$relativePath] = $true
    }
}

# 锁定本次玩家实测暴露的两个主命令，避免只消除重名却遗漏主文件内容。
[xml]$mainCommands = Get-Content -Raw -LiteralPath $mainCommandsPath -Encoding UTF8
Assert-True ($null -ne $mainCommands.LanguageData.BDP_Command_TriggerAssembly_Use) "主语言文件缺少触发器装配台命令。"
Assert-True ($null -ne $mainCommands.LanguageData.BDP_Command_CombatBody_Activate) "主语言文件缺少开启战斗体命令。"
Assert-True ($null -ne $mainCommands.LanguageData.BDP_Command_CombatBody_ActivateDesc) "主语言文件缺少开启战斗体说明。"

Write-Host "PASS: 各加载目录语言文件路径唯一，主命令语言文件不会被开发目录遮蔽。"
