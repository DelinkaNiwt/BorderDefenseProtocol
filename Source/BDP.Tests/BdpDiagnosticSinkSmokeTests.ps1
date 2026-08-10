# BDP 中性诊断接收器冒烟测试。
# 验证 Core 只负责安全转发，不直接承担 Verse 日志输出。

$ErrorActionPreference = "Stop"

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$coreRoot = Join-Path $modRoot "Source\BDP"
$coreAssemblyPath = Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"
$interfacePath = Join-Path $coreRoot "Support\Diagnostics\IBdpDiagnosticSink.cs"
$registryPath = Join-Path $coreRoot "Support\Diagnostics\BdpDiagnosticSinkRegistry.cs"
$diagnosticsPath = Join-Path $coreRoot "Support\Diagnostics\BdpDiagnostics.cs"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

Assert-True (Test-Path -LiteralPath $interfacePath) "Core 缺少中性诊断接收器接口。"
Assert-True (Test-Path -LiteralPath $registryPath) "Core 缺少诊断接收器注册口。"
Assert-True (Test-Path -LiteralPath $diagnosticsPath) "Core 缺少 BdpDiagnostics。"
Assert-True (Test-Path -LiteralPath $coreAssemblyPath) "缺少已编译的 BDP.Core.dll。"

$diagnosticsText = Get-Content -Raw -Encoding UTF8 -LiteralPath $diagnosticsPath
Assert-True ($diagnosticsText -notmatch '\bLog\.Message\s*\(') `
    "BdpDiagnostics 不得直接调用 Verse.Log.Message。"
Assert-True ($diagnosticsText -match 'BdpDiagnosticSinkRegistry\.Write\s*\(') `
    "BdpDiagnostics 必须把实际输出交给中性接收器注册表。"

$coreAssembly = [Reflection.Assembly]::LoadFrom($coreAssemblyPath)
$sinkInterface = $coreAssembly.GetType("BDP.Support.Diagnostics.IBdpDiagnosticSink", $false)
$registryType = $coreAssembly.GetType("BDP.Support.Diagnostics.BdpDiagnosticSinkRegistry", $false)

Assert-True ($null -ne $sinkInterface) "编译后的 Core 缺少中性诊断接收器接口。"
Assert-True ($null -ne $registryType) "编译后的 Core 缺少诊断接收器注册口。"

$testTypeSource = @"
using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;

public sealed class BdpRecordingDiagnosticSink : IBdpDiagnosticSink
{
    public readonly List<string> Messages = new List<string>();

    public void Write(string message)
    {
        Messages.Add(message);
    }
}

public sealed class BdpThrowingDiagnosticSink : IBdpDiagnosticSink
{
    public void Write(string message)
    {
        throw new InvalidOperationException("test sink failure");
    }
}
"@

Add-Type -TypeDefinition $testTypeSource -Language CSharp -ReferencedAssemblies $coreAssemblyPath

$registerMethod = $registryType.GetMethod("Register", [Reflection.BindingFlags] "Public,Static")
$unregisterMethod = $registryType.GetMethod("Unregister", [Reflection.BindingFlags] "Public,Static")
$writeMethod = $registryType.GetMethod("Write", [Reflection.BindingFlags] "NonPublic,Static")

Assert-True ($null -ne $registerMethod) "诊断接收器注册口缺少 Register。"
Assert-True ($null -ne $unregisterMethod) "诊断接收器注册口缺少 Unregister。"
Assert-True ($null -ne $writeMethod) "诊断接收器注册口缺少内部 Write。"

$recordingSink = [BdpRecordingDiagnosticSink]::new()
$null = $registerMethod.Invoke($null, @($recordingSink))
$null = $writeMethod.Invoke($null, @("recorded"))
Assert-True ($recordingSink.Messages.Count -eq 1 -and $recordingSink.Messages[0] -eq "recorded") `
    "注册后必须把消息交给当前接收器。"

$differentSink = [BdpRecordingDiagnosticSink]::new()
$null = $unregisterMethod.Invoke($null, @($differentSink))
$null = $writeMethod.Invoke($null, @("still registered"))
Assert-True ($recordingSink.Messages.Count -eq 2) "不同实例不得注销当前接收器。"

$throwingSink = [BdpThrowingDiagnosticSink]::new()
$null = $registerMethod.Invoke($null, @($throwingSink))
$null = $writeMethod.Invoke($null, @("contained"))

$null = $unregisterMethod.Invoke($null, @($throwingSink))
$null = $writeMethod.Invoke($null, @("ignored"))

Write-Host "PASS: Core 诊断接收器支持注册、静默、同实例注销与异常隔离。"
