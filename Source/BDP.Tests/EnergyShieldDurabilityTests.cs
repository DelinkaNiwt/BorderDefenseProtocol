using System;
using System.IO;
using System.Reflection;

/// <summary>直接运行正式状态类的边界测试，不复写护盾算法。</summary>
internal static class EnergyShieldDurabilityTests
{
    /// <summary>依赖搜索目录。</summary>
    private static string[] directories;
    /// <summary>状态实例类型。</summary>
    private static Type stateType;

    /// <summary>装载正式程序集并检验确定性的耐久与冷却行为。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            Assembly content = Assembly.LoadFrom(Path.Combine(args[2], "BDP.Content.dll"));
            stateType = content.GetType("BDP.Content.Shield.EnergyShieldDurabilityState", true);
            object state = Activator.CreateInstance(stateType);
            Equal(Current(state, 120f), 120f, "新盾满耐久");
            Check(!(bool)Call(state, "ApplyBlockedDamage", 30f, 120f, 1f, 500, 600), "未耗尽不破盾");
            Equal(Current(state, 120f), 90f, "成功格挡扣耐久");
            Equal(Current(state, 240f), 180f, "单双盾变化保留比例");
            Equal(Current(state, 120f), 90f, "反复切换不刷耐久");
            Check((bool)Call(state, "ApplyBlockedDamage", 1000f, 120f, 1f, 500, 600), "超额命中耗尽后破盾");
            Equal(Current(state, 120f), 0f, "破盾耐久为零");
            Check((int)Property(state, "RecoveryUntilTick") == 1100, "冷却使用绝对完成刻");
            Check(!(bool)Call(state, "TryBreak", 700, 600), "重复破盾不重置计时");
            Check((int)Property(state, "RecoveryUntilTick") == 1100, "完成刻保持不变");
            Check(!(bool)Call(state, "RecoverIfDue", 1099), "冷却未到期不得恢复");
            Check((bool)Call(state, "RecoverIfDue", 1100), "到期恢复");
            Equal(Current(state, 240f), 240f, "以当前上限满耐久恢复");
            Check(!(bool)Call(state, "RecoverIfDue", 1101), "恢复不重复触发");
            Call(state, "ApplyBlockedDamage", 10f, 120f, 2f, 1200, 600);
            Equal(Current(state, 120f), 100f, "耐久倍率独立计算");
            Call(state, "ApplyBlockedDamage", -10f, 120f, 1f, 1200, 600);
            Equal(Current(state, 120f), 100f, "负伤害不能回复耐久");
            object repeated = Activator.CreateInstance(stateType);
            for (int i = 0; i < 119; i++)
                Check(!(bool)Call(repeated, "ApplyBlockedDamage", 1f, 120f, 1f, 500, 600), "前 119 点伤害不能提前破盾");
            Check((bool)Call(repeated, "ApplyBlockedDamage", 1f, 120f, 1f, 500, 600), "120 次一伤恰好耗尽，不能被浮点残量多挡一击");
            TestFixedRecovery();
            TestNaturalRecovery();
            TestSavedRecovery();
            TestBypass(content);
            Console.WriteLine("EnergyShieldDurabilityTests PASS（真实状态行为通过）");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>当前默认零延迟固定恢复；改变容量不会把固定速度按比例放大。</summary>
    private static void TestFixedRecovery()
    {
        object state = Activator.CreateInstance(stateType);
        Call(state, "ApplyBlockedDamage", 60f, 120f, 1f, 500, 600);
        Call(state, "RecoverIfDue", 501);
        Equal(Current(state, 120f), 60.4f, "零延迟从下一游戏刻开始按固定点数恢复");
        Call(state, "RecoverIfDue", 560);
        Equal(Current(state, 120f), 84f, "默认固定每秒恢复 24 点");
        Set(state, "LastMaximumDurability", 240f);
        Equal(Current(state, 240f), 168f, "上限变更仍保留剩余比例");
        Call(state, "RecoverIfDue", 620);
        Equal(Current(state, 240f), 192f, "上限翻倍后固定恢复仍为每秒 24 点");
        Call(state, "RecoverIfDue", 620);
        Equal(Current(state, 240f), 192f, "同组同刻不重复恢复固定点数");
        state = RoundTrip(state);
        Call(state, "RecoverIfDue", 680);
        Equal(Current(state, 240f), 216f, "关闭与数值读档后保留固定恢复速度及最近上限");
        Set(state, "NaturalRecoveryPerSecond", 48f);
        Call(state, "ApplyBlockedDamage", 120f, 240f, 1f, 680, 600);
        state = RoundTrip(state);
        Call(state, "RecoverIfDue", 740);
        Equal(Current(state, 240f), 144f, "自定义固定恢复点数也能保存读取");
    }

    /// <summary>自然恢复只由共用状态推进，未启用防护宿主时也使用相同游戏时间。</summary>
    private static void TestNaturalRecovery()
    {
        object state = Activator.CreateInstance(stateType);
        Set(state, "NaturalRecoveryUsePercentage", true);
        Set(state, "NaturalRecoveryPerSecond", 0.1f);
        Set(state, "NaturalRecoveryDelayTicks", 120);
        Call(state, "ApplyBlockedDamage", 60f, 120f, 1f, 500, 600);
        Check(!(bool)Call(state, "RecoverIfDue", 619), "等待期间不发生破盾恢复转换");
        Equal(Current(state, 120f), 60f, "2 秒等待未满不恢复");
        Call(state, "RecoverIfDue", 620);
        Equal(Current(state, 120f), 60f, "延迟结束当刻尚未经过恢复时间");
        Check(!(bool)Call(state, "RecoverIfDue", 680), "自然恢复不能触发表达条件刷新");
        Equal(Current(state, 120f), 72f, "延迟后每秒恢复上限的 10%");
        Call(state, "RecoverIfDue", 680);
        Equal(Current(state, 120f), 72f, "同一游戏刻重复推进不重复恢复");
        Equal(Current(state, 240f), 144f, "来源相加上限保持正在恢复的比例");
        Call(state, "ApplyBlockedDamage", 12f, 120f, 1f, 680, 600);
        Call(state, "RecoverIfDue", 799);
        Equal(Current(state, 120f), 60f, "再次扣耐力重置等待时间");
        Call(state, "RecoverIfDue", 860);
        Equal(Current(state, 120f), 72f, "连续受击结束后按新时间恢复");
        Call(state, "ApplyBlockedDamage", -10f, 120f, 1f, 860, 600);
        Call(state, "ApplyBlockedDamage", 10f, 120f, 0f, 860, 600);
        Call(state, "RecoverIfDue", 920);
        Equal(Current(state, 120f), 84f, "未扣耐力的调用不推迟恢复");
        Call(state, "RecoverIfDue", 2000);
        Equal(Current(state, 120f), 120f, "无防护宿主的稀疏推进继续恢复且不超上限");

        object perTick = Activator.CreateInstance(stateType);
        Set(perTick, "NaturalRecoveryUsePercentage", true);
        Set(perTick, "NaturalRecoveryPerSecond", 0.1f);
        Set(perTick, "NaturalRecoveryDelayTicks", 120);
        Call(perTick, "ApplyBlockedDamage", 119f, 120f, 1f, 500, 600);
        for (int tick = 501; tick <= 1214; tick++) Call(perTick, "RecoverIfDue", tick);
        Equal(Current(perTick, 120f), 119.8f, "逐刻恢复不提前回满");
        Call(perTick, "RecoverIfDue", 1215);
        Equal(Current(perTick, 120f), 120f, "逐游戏刻累加能够在预计时点恢复满耐力");

        object broken = Activator.CreateInstance(stateType);
        Call(broken, "ApplyBlockedDamage", 120f, 120f, 1f, 500, 600);
        Call(broken, "RecoverIfDue", 900);
        Equal(Current(broken, 120f), 0f, "破盾冷却期间自然恢复暂停");
        Check((bool)Call(broken, "RecoverIfDue", 1100), "破盾到期只触发一次条件刷新");
        Equal(Current(broken, 120f), 120f, "破盾到期直接恢复满耐力");

        object disabled = Activator.CreateInstance(stateType);
        Set(disabled, "NaturalRecoveryPerSecond", 0f);
        Call(disabled, "ApplyBlockedDamage", 60f, 120f, 1f, 500, 600);
        Call(disabled, "RecoverIfDue", 2000);
        Equal(Current(disabled, 120f), 60f, "速度为零可关闭自然恢复");
        Set(disabled, "NaturalRecoveryUsePercentage", true);
        Set(disabled, "NaturalRecoveryPerSecond", 0.2f);
        Set(disabled, "NaturalRecoveryDelayTicks", 0);
        Call(disabled, "RecoverIfDue", 2060);
        Equal(Current(disabled, 120f), 84f, "延迟与速度可按护盾配置独立调整");
    }

    /// <summary>调用原版存档器验证实际序列化，加载后继续原来的延迟与结算时间。</summary>
    private static void TestSavedRecovery()
    {
        object state = Activator.CreateInstance(stateType);
        Set(state, "NaturalRecoveryUsePercentage", true);
        Set(state, "NaturalRecoveryPerSecond", 0.2f);
        Set(state, "NaturalRecoveryDelayTicks", 90);
        Call(state, "ApplyBlockedDamage", 60f, 120f, 1f, 1000, 600);
        Call(state, "RecoverIfDue", 1040);
        object loaded = RoundTrip(state);
        Call(loaded, "RecoverIfDue", 1089);
        Equal(Current(loaded, 120f), 60f, "读档不提前或重置恢复延迟");
        Call(loaded, "RecoverIfDue", 1120);
        Equal(Current(loaded, 120f), 72f, "读档保留来源恢复速度与受击时点");
        loaded = RoundTrip(loaded);
        Call(loaded, "RecoverIfDue", 1120);
        Equal(Current(loaded, 120f), 72f, "读档保留已结算刻，不重复恢复");
        Call(loaded, "RecoverIfDue", 1150);
        Equal(Current(loaded, 120f), 84f, "加载恢复中状态后继续增量恢复");
        Call(loaded, "TryBreak", 1200, 600);
        loaded = RoundTrip(loaded);
        Call(loaded, "RecoverIfDue", 1799);
        Equal(Current(loaded, 120f), 0f, "读档保持破盾期间自然恢复暂停");
        Check((bool)Call(loaded, "RecoverIfDue", 1800), "读档沿用破盾完成刻");
        Equal(Current(loaded, 120f), 120f, "读档后破盾冷却到期回满");
    }

    /// <summary>用原版 Scribe（存档器）验证数值保存与读取；游戏整体加载收尾需在引擎内实测。</summary>
    private static object RoundTrip(object state)
    {
        Type scribe = Type.GetType("Verse.Scribe, Assembly-CSharp", true);
        object saver = scribe.GetField("saver").GetValue(null);
        object loader = scribe.GetField("loader").GetValue(null);
        string path = Path.Combine(Path.GetTempPath(), "bdp-shield-recovery-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            saver.GetType().GetMethod("InitSaving").Invoke(saver, new object[] { path, "shieldState" });
            Call(state, "ExposeData");
            saver.GetType().GetMethod("FinalizeSaving").Invoke(saver, null);
            object loaded = Activator.CreateInstance(stateType);
            loader.GetType().GetMethod("InitLoading").Invoke(loader, new object[] { path });
            Call(loaded, "ExposeData");
            // 本记录只有数值，无交叉引用或加载后初始化；控制台不能执行 Unity 的加载收尾。
            // 此处结束原版数值读取阶段，由 finally 清理 Scribe，不模拟游戏级加载成功。
            return loaded;
        }
        finally
        {
            scribe.GetMethod("ForceStop").Invoke(null, null);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>查找游戏及正式模组依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories)
        {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>真实穿盾范围支持指定组、嵌套与异常退出，释放后不污染下一次伤害。</summary>
    private static void TestBypass(Assembly content)
    {
        Type api = content.GetType("BDP.Content.Shield.EnergyShieldApi", true);
        MethodInfo begin = api.GetMethod("BeginBypass");
        MethodInfo check = api.GetMethod("ShouldBypass");
        Func<string, bool> bypass = key => (bool)check.Invoke(null, new object[] { null, key });
        Check(!bypass("a"), "普通伤害不穿盾");
        using ((IDisposable)begin.Invoke(null, new object[] { null, "a" }))
        {
            Check(bypass("a") && !bypass("b"), "穿盾只影响指定组");
            using ((IDisposable)begin.Invoke(null, new object[] { null, "b" }))
                Check(bypass("a") && bypass("b"), "嵌套保留外层范围");
            Check(bypass("a") && !bypass("b"), "内层释放恢复外层");
        }
        Check(!bypass("a"), "外层释放后正常格挡");
        try { using ((IDisposable)begin.Invoke(null, new object[] { null, null })) { throw new InvalidOperationException(); } }
        catch (InvalidOperationException) { }
        Check(!bypass("a"), "异常退出不泄漏穿盾设置");
    }

    /// <summary>读取指定上限下的实际剩余耐久。</summary>
    private static float Current(object state, float maximum) { return (float)Call(state, "CurrentDurability", maximum); }
    /// <summary>调用真实状态方法。</summary>
    private static object Call(object state, string name, params object[] args) { return stateType.GetMethod(name).Invoke(state, args); }
    /// <summary>读取真实只读状态属性。</summary>
    private static object Property(object state, string name) { return stateType.GetProperty(name).GetValue(state, null); }
    /// <summary>设置正式盾组的来源配置。</summary>
    private static void Set(object state, string name, object value) { stateType.GetField(name).SetValue(state, value); }
    /// <summary>检查状态结论。</summary>
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    /// <summary>检查浮点耐久。</summary>
    private static void Equal(float actual, float expected, string message) { Check(Math.Abs(actual - expected) < 0.001f, message + ": " + actual); }
}
