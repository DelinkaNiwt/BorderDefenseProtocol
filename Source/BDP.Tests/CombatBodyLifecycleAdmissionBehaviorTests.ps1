$ErrorActionPreference = 'Stop'
# 直接编译真实阶段状态；只替换游戏时钟、数学和序列化这三个外部边界。
$taskSourceRoot = Split-Path -Parent $PSScriptRoot
$taskCoreRoot = Join-Path $taskSourceRoot 'BDP/Core/CombatBody'
$taskTemp = Join-Path ([IO.Path]::GetTempPath()) ('bdp-body-lifecycle-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $taskTemp
try {
    $taskShim = Join-Path $taskTemp 'EngineBoundary.cs'
    @'
namespace UnityEngine
{
    /// <summary>仅提供阶段状态所需的纯数学函数。</summary>
    public static class Mathf
    {
        /// <summary>返回较大的整数。</summary>
        public static int Max(int a, int b) { return System.Math.Max(a, b); }
        /// <summary>返回较大的浮点数。</summary>
        public static float Max(float a, float b) { return System.Math.Max(a, b); }
    }
}
namespace Verse
{
    /// <summary>阶段状态的原版序列化接口边界。</summary>
    public interface IExposable
    {
        /// <summary>原版存档回调。</summary>
        void ExposeData();
    }
    /// <summary>测试只使用非读档模式。</summary>
    public enum LoadSaveMode
    {
        /// <summary>未进行存读档。</summary>
        Inactive,
        /// <summary>读档初始化结束。</summary>
        PostLoadInit
    }
    /// <summary>提供序列化模式查询。</summary>
    public static class Scribe
    {
        /// <summary>当前序列化模式。</summary>
        public static LoadSaveMode mode;
    }
    /// <summary>运行态检查不执行序列化。</summary>
    public static class Scribe_Values
    {
        /// <summary>保留原状态值，不模拟序列化实现。</summary>
        public static void Look<T>(ref T value, string key, T fallback = default(T)) { }
    }
    /// <summary>可控的游戏时钟。</summary>
    public sealed class TickManager
    {
        /// <summary>当前游戏刻数。</summary>
        public int TicksGame;
    }
    /// <summary>仅隔离原版时钟入口。</summary>
    public static class Find
    {
        /// <summary>当前测试使用的唯一时钟。</summary>
        public static TickManager TickManager = new TickManager();
    }
}
'@ | Set-Content -LiteralPath $taskShim -Encoding utf8
    $taskExe = Join-Path $taskTemp 'CombatBodyLifecycle.exe'
    $taskFiles = @(
        $taskShim,
        (Join-Path $taskCoreRoot 'State/CombatBodyPhase.cs'),
        (Join-Path $taskCoreRoot 'Access/Contracts/ICombatBodyReader.cs'),
        (Join-Path $taskCoreRoot 'State/ICombatBodyPhaseState.cs'),
        (Join-Path $taskCoreRoot 'State/CombatBodyState.cs'),
        (Join-Path $PSScriptRoot 'CombatBodyLifecycleAdmissionBehaviorTests.cs')
    )
    & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /target:exe "/out:$taskExe" @taskFiles
    if ($LASTEXITCODE -ne 0) { throw '生命周期行为检查编译失败。' }
    & $taskExe
    if ($LASTEXITCODE -ne 0) { throw '生命周期行为检查失败。' }
} finally {
    # 路径是本次创建的临时目录；删除前验证没有偏离临时根目录。
    $taskResolved = [IO.Path]::GetFullPath($taskTemp)
    $taskTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($taskResolved.StartsWith($taskTempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $taskResolved -Recurse -Force
    }
}
