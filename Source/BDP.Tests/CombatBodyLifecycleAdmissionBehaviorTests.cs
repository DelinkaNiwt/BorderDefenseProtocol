using System;
using System.Reflection;
using BDP.Core.CombatBody;

/// <summary>执行真实阶段状态源码；游戏时钟固定可控，不模拟待验证的准入逻辑。</summary>
internal static class CombatBodyLifecycleAdmissionBehaviorTests
{
    /// <summary>累计检查失败数。</summary>
    private static int failures;

    /// <summary>验证系统重初始化、玩家短时锁以及崩解冷却的区别。</summary>
    private static int Main()
    {
        Test("手动锁不能阻止系统激活", delegate
        {
            CombatBodyState state = NewState();
            state.BeginManualTransformLock(12);
            Check(state.CanActivate(), "系统初始化被手动切换锁阻止");
            state.EnterActive(10f);
            Check(state.Phase == CombatBodyPhase.Active && state.AllocatedTrion == 10f, "系统激活未完成");
        });
        Test("同刻退出后可系统重初始化且手动入口仍受锁约束", delegate
        {
            CombatBodyState state = NewState();
            state.EnterActive(10f);
            state.BeginManualTransformLock(12);
            state.EnterCooldown(0);
            Check(state.CanActivate(), "零冷却退出后系统仍不能激活");
            Check(!ManualActivate(state), "手动激活绕过了短时锁");
            state.EnterActive(20f);
            Check(!state.CanManualDeactivate(), "系统重初始化清除了尚未到期的手动锁");
        });
        Test("手动锁到期后恢复玩家操作", delegate
        {
            CombatBodyState state = NewState();
            state.BeginManualTransformLock(12);
            Check(!ManualActivate(state), "锁内允许手动激活");
            Verse.Find.TickManager.TicksGame = 112;
            Check(ManualActivate(state), "锁到期后仍拒绝手动激活");
            state.EnterActive(0f);
            Check(state.CanManualDeactivate(), "锁到期后仍拒绝手动解除");
        });
        Test("系统激活不能绕过崩解表现和真实冷却", delegate
        {
            CombatBodyState state = NewState();
            state.EnterActive(10f);
            Check(!state.CanActivate(), "系统重复激活活跃战斗体");
            state.EnterCollapsing("test");
            Check(!state.CanActivate() && !ManualActivate(state), "崩解阶段允许重新激活");
            state.EnterCooldown(20);
            Check(!state.CanActivate() && !ManualActivate(state), "系统绕过了崩解冷却");
            Verse.Find.TickManager.TicksGame = 120;
            Check(state.CanActivate() && ManualActivate(state), "崩解冷却到期后仍不能激活");
        });
        Console.WriteLine("战斗体生命周期准入检查：4 项，失败 " + failures + " 项。");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>每项检查重置可控游戏时钟。</summary>
    private static CombatBodyState NewState()
    {
        Verse.Find.TickManager.TicksGame = 100;
        return new CombatBodyState();
    }

    /// <summary>通过反射兼容修复前尚未提供手动激活查询的测试阶段。</summary>
    private static bool ManualActivate(CombatBodyState state)
    {
        MethodInfo method = typeof(CombatBodyState).GetMethod("CanManualActivate");
        Check(method != null, "缺少独立的手动激活准入");
        return (bool)method.Invoke(state, null);
    }

    /// <summary>逐项报告，单项失败不阻止其他回归场景执行。</summary>
    private static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("通过：" + name); }
        catch (Exception error) { failures++; Console.Error.WriteLine("失败：" + name + "：" + error.Message); }
    }

    /// <summary>断言玩家可见行为对应的阶段准入结果。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
