# 芯片制造专项测试的中性辅助函数。
# 本文件只提供断言与路径解析，不承载任何制造业务判断。

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-BdpModRoot
{
    return Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
}

function Get-Utf8Text
{
    param([string]$Path)
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path
}
