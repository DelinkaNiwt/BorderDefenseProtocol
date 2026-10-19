using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>直接运行正式攻击一致性与宿主同步代码，验证条件刷新不清空未变会话。</summary>
internal static class AttackConditionRefreshTests
{
    /// <summary>真实程序集与依赖目录。</summary>
    private static Assembly core;
    /// <summary>依赖搜索目录。</summary>
    private static string[] directories;
    /// <summary>反射范围。</summary>
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>运行来源、攻击定义和运行态保留边界。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            core = Assembly.LoadFrom(Path.Combine(args[2], "BDP.Core.dll"));
            object before = Projection(10, Result("weapon", 25f));
            object after = Projection(11, Result("weapon", 25f));
            Check(Unchanged(before, after).Contains("weapon"), "参数副本相同可续接");
            Set(ResultOf(after, "weapon"), "DisplayLabel", "只改显示名称");
            Check(Unchanged(before, after).Contains("weapon"), "显示信息不影响攻击身份");
            Set(Get(ResultOf(after, "weapon"), "ResolvedVerbSpec"), "Range", 30f);
            Check(!Unchanged(before, after).Contains("weapon"), "攻击射程改变不可续接");
            after = Projection(11, Result("weapon", 25f));
            Set(Get(ResultOf(after, "weapon"), "SourceReference"), "ChipThingId", "another_chip");
            Check(!Unchanged(before, after).Contains("weapon"), "替换来源实例不可续接");
            after = Projection(11, Result("weapon", 25f));
            ((IDictionary)Get(after, "ResultIdToFormalSlot"))["weapon"] = Enum.Parse(T("BDP.Core.VerbHosting.BdpFormalVerbHostSlot"), "SubPrimary");
            Check(!Unchanged(before, after).Contains("weapon"), "固定槽位改变不可续接");
            Check(!Unchanged(before, Projection(11)).Contains("weapon"), "消失攻击必须失效");
            after = Projection(11, Result("weapon", 25f));
            Set(ResultOf(after, "weapon"), "CanProject", false);
            Check(!Unchanged(before, after).Contains("weapon"), "不能投影的结果不可救活旧会话");
            after = Projection(11, Result("weapon", 25f));
            object requirements = New("BDP.Core.Requirements.PawnRequirementCheckResult");
            Set(requirements, "Satisfied", false); Set(ResultOf(after, "weapon"), "UseRequirementCheck", requirements);
            Check(!Unchanged(before, after).Contains("weapon"), "使用条件受阻的组合攻击不可续接");
            Set(requirements, "Satisfied", true);
            Check(Unchanged(before, after).Contains("weapon"), "满足条件的检查副本不改变攻击定义");
            TestConfigurationChanges();
            object oldToken = Token(9, "attack_123");
            MethodInfo migrate = T("BDP.Core.AttackExecution.AttackSessionContinuity").GetMethod("CanMigrateToken", BindingFlags.Static | BindingFlags.NonPublic);
            Check(!(bool)migrate.Invoke(null, new object[] { oldToken, 10, Token(11, null), new HashSet<string> { "weapon" } }), "更早失效的令牌不得迁移");
            object oldDual = Projection(10, Result("dual", 25f), Result("main", 25f), Result("sub", 25f));
            object newDual = Projection(11, Result("dual", 25f), Result("main", 25f), Result("sub", 26f));
            AddComposite(oldDual); AddComposite(newDual);
            Check(!Unchanged(oldDual, newDual).Contains("dual"), "复合副侧改变不可继续旧计划");
            TestShell("BDP.Core.Verbs.BdpVerb_FormalHostShoot", false);
            TestShell("BDP.Core.Verbs.BdpVerb_FormalHostMelee", true);
            TestCleanupOwnership();
            TestVisualContinuity();
            Console.WriteLine("AttackConditionRefreshTests PASS（真实条件刷新行为通过）");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>迁移同一攻击的作业清理归属，并保留跨攻击实例隔离。</summary>
    private static void TestCleanupOwnership()
    {
        object verb = New("BDP.Core.Verbs.BdpVerb_FormalHostShoot");
        object driver = New("BDP.Core.AttackExecution.JobDriver_BdpRangedAttackExecution");
        object job = Activator.CreateInstance(Type.GetType("Verse.AI.Job, Assembly-CSharp", true));
        Field(job, "verbToUse").SetValue(job, verb); Field(driver, "job").SetValue(driver, job);
        Set(verb, "HostSessionToken", Token(10, "attack_123"));
        Invoke(driver, "CaptureCleanupOwnedSessionIfNeeded", verb);
        Check((bool)Invoke(driver, "ShouldCleanupCurrentVerbSession", verb), "旧版作业原本拥有本会话");
        Set(verb, "HostSessionToken", Token(11, "attack_123"));
        Check(!(bool)Invoke(driver, "ShouldCleanupCurrentVerbSession", verb), "版本变更前不得盲目清理");
        Invoke(driver, "MigrateCleanupOwnedSession", verb, Token(10, "attack_123"), 11);
        Check((bool)Invoke(driver, "ShouldCleanupCurrentVerbSession", verb), "同一攻击迁移后仍能退出清理");
        Set(verb, "HostSessionToken", Token(12, "another_attack"));
        Invoke(driver, "MigrateCleanupOwnedSession", verb, Token(11, "attack_123"), 12);
        Check(!(bool)Invoke(driver, "ShouldCleanupCurrentVerbSession", verb), "旧作业不得迁移到另一攻击实例");
    }

    /// <summary>保留同一攻击的枪口焦点，失效或其它实例不能恢复旧执行态。</summary>
    private static void TestVisualContinuity()
    {
        object owner = New("BDP.Core.Trigger.Runtime.TriggerVisualRuntimeStateOwner");
        Invoke(owner, "PublishExecutionState", 10, "attack_123", "weapon",
            new List<string> { "weapon" }, new List<string> { "weapon" }, new List<string> { "weapon" });
        object previous = Get(owner, "PublishedState");
        Invoke(owner, "ResetForPublishedProjection", 11);
        Invoke(owner, "RestoreContinuedExecutionState", previous, Token(11, "attack_123"), 10);
        object current = Get(owner, "PublishedState");
        Check((int)Get(current, "ProjectionVersion") == 11 && (string)Get(current, "AttackInstanceId") == "attack_123", "动态执行态同步新版");
        Check((bool)Invoke(current, "ContainsActiveEmitSourceResult", "weapon"), "本轮剩余连射仍有枪口焦点");
        Check(Get(current, "EquipmentPoseSample") == null, "旧装备姿态样本不随执行态迁移");
        Invoke(owner, "ResetForPublishedProjection", 12);
        Invoke(owner, "RestoreContinuedExecutionState", previous, Token(12, "another_attack"), 10);
        Check(!(bool)Get(Get(owner, "PublishedState"), "HasExecutionState"), "另一攻击不能恢复旧视觉执行态");
    }

    /// <summary>建立真实正式结果，配置对象独立生成。</summary>
    private static object Result(string id, float range)
    {
        object result = New("BDP.Core.Expressions.FormalExpressionResult");
        Set(result, "Id", id); Set(result, "IsAvailable", true); Set(result, "CanProject", true);
        Set(result, "OutputChannel", Enum.Parse(T("BDP.Core.Expressions.ExpressionOutputChannel"), "Verb"));
        object source = New("BDP.Core.Expressions.ExpressionSourceReference");
        Set(source, "ChipThingId", "chip_" + id); Set(source, "ChipDefName", "test");
        Set(result, "SourceReference", source);
        object spec = New("BDP.Core.Expressions.ResolvedVerbSpec");
        Set(spec, "Range", range); Set(result, "ResolvedVerbSpec", spec);
        return result;
    }

    /// <summary>配置深复制相同可续接，节奏、费用和模块挂载变化应使旧计划失效。</summary>
    private static void TestConfigurationChanges()
    {
        object before = Projection(10, Result("weapon", 25f));
        object after = Projection(11, Result("weapon", 25f));
        foreach (object projection in new object[] { before, after })
        {
            object result = ResultOf(projection, "weapon");
            object style = New("BDP.Core.CombatModel.AttackExecutionStyle");
            object single = New("BDP.Core.CombatModel.SingleAttackExecutionStyle");
            Field(single, "meleeHitCount").SetValue(single, 2);
            Field(style, "Single").SetValue(style, single); Set(result, "ExecutionStyle", style);
            object trion = New("BDP.Core.Expressions.ExpressionSourceTrionConfig");
            Field(trion, "UseCost").SetValue(trion, 3f); Set(result, "Trion", trion);
            IList modules = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(T("BDP.Core.AttackExecution.RangedModuleMountConfig")));
            modules.Add(New("BDP.Core.AttackExecution.RangedModuleMountConfig")); Set(result, "RangedModules", modules);
        }
        Check(Unchanged(before, after).Contains("weapon"), "节奏与模块副本相同可续接");
        object next = ResultOf(after, "weapon");
        object nextSingle = Field(Get(next, "ExecutionStyle"), "Single").GetValue(Get(next, "ExecutionStyle"));
        Field(nextSingle, "meleeHitCount").SetValue(nextSingle, 3);
        Check(!Unchanged(before, after).Contains("weapon"), "连击数变化不可续接");
        Field(nextSingle, "meleeHitCount").SetValue(nextSingle, 2);
        Field(Get(next, "Trion"), "UseCost").SetValue(Get(next, "Trion"), 4f);
        Check(!Unchanged(before, after).Contains("weapon"), "使用费变化不可续接");
        Field(Get(next, "Trion"), "UseCost").SetValue(Get(next, "Trion"), 3f);
        object mount = ((IList)Get(next, "RangedModules"))[0];
        Field(mount, "enabled").SetValue(mount, false);
        Check(!Unchanged(before, after).Contains("weapon"), "模块挂载变化不可续接");
    }

    /// <summary>建立真实投影索引。</summary>
    private static object Projection(int version, params object[] results)
    {
        object projection = New("BDP.Core.Trigger.Runtime.TriggerCombatProjectionState");
        Set(projection, "ProjectionVersion", version);
        IDictionary index = Dictionary(T("BDP.Core.Expressions.FormalExpressionResult"));
        IDictionary slots = Dictionary(T("BDP.Core.VerbHosting.BdpFormalVerbHostSlot"));
        foreach (object result in results)
        {
            string id = (string)Get(result, "Id");
            index.Add(id, result);
            slots.Add(id, Enum.Parse(T("BDP.Core.VerbHosting.BdpFormalVerbHostSlot"), "MainPrimary"));
        }
        Set(projection, "ResultIndex", index); Set(projection, "ResultIdToFormalSlot", slots);
        Set(projection, "CompositeReferenceIndex", Dictionary(T("BDP.Core.Expressions.CompositeExpressionReference")));
        return projection;
    }

    /// <summary>添加双侧来源，覆盖聚合编号相同而副侧定义改变的场景。</summary>
    private static void AddComposite(object projection)
    {
        object reference = New("BDP.Core.Expressions.CompositeExpressionReference");
        Set(reference, "CompositeId", "dual"); Set(reference, "SourceResultIds", new List<string> { "main", "sub" });
        Set(reference, "MainSourceResultId", "main"); Set(reference, "SubSourceResultId", "sub");
        ((IDictionary)Get(projection, "CompositeReferenceIndex")).Add("dual", reference);
    }

    /// <summary>验证正式壳同步只迁移版本，不重置动作状态或当前近战表面。</summary>
    private static void TestShell(string name, bool melee)
    {
        object verb = New(name);
        object props = Activator.CreateInstance(Type.GetType("Verse.VerbProperties, Assembly-CSharp", true));
        object binding = New("BDP.Core.VerbHosting.BdpFormalVerbBindingState");
        Set(binding, "IsAvailable", true); Set(binding, "ResultId", "weapon"); Set(binding, "VerbProps", props);
        Set(binding, "SessionToken", Token(10, null));
        Invoke(verb, "SyncFormalBinding", binding, props, false);
        Set(verb, "HostSessionToken", Token(10, "attack_123"));
        Set(verb, "AttackInstanceId", "attack_123");
        Field(verb, "burstShotsLeft").SetValue(verb, 3);
        object stepSurface = Activator.CreateInstance(props.GetType());
        Field(verb, "verbProps").SetValue(verb, stepSurface);
        object moduleSession = melee ? null : New("BDP.Core.AttackExecution.RangedAttackModuleSession");
        if (!melee) Set(verb, "HostModuleSession", moduleSession);
        if (melee) { Set(verb, "PlanSessionToken", Token(10, "attack_123")); Set(verb, "NextRuntimeStepIndex", 4); }
        object nextProps = Activator.CreateInstance(props.GetType());
        Set(binding, "VerbProps", nextProps); Set(binding, "SessionToken", Token(11, null));
        Invoke(verb, "SyncFormalBinding", binding, props, true);
        object token = Get(verb, "HostSessionToken");
        Check((int)Get(token, "ProjectionVersion") == 11 && (string)Get(token, "AttackInstanceId") == "attack_123", "宿主迁移版本且保留攻击实例");
        Check((int)Field(verb, "burstShotsLeft").GetValue(verb) == 3, "保留剩余连射/连击");
        Check(Object.ReferenceEquals(Field(verb, "verbProps").GetValue(verb), stepSurface), "保留当前冻结攻击表面");
        if (!melee) Check(Object.ReferenceEquals(Get(verb, "HostModuleSession"), moduleSession), "不重新创建冻结模块会话");
        if (melee)
        {
            Check((int)Get(Get(verb, "PlanSessionToken"), "ProjectionVersion") == 11, "近战后续计划同时迁移版本");
            Check((int)Get(verb, "NextRuntimeStepIndex") == 4, "不得回退近战动作游标");
        }
        Set(binding, "IsAvailable", false);
        Invoke(verb, "SyncFormalBinding", binding, props, false);
        Check((int)Field(verb, "burstShotsLeft").GetValue(verb) == 0, "破盾盾击等失效攻击正常复位");
        object rejected = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(Type.GetType("Verse.Verb, Assembly-CSharp", true)));
        rejected.GetType().GetMethod("Add").Invoke(rejected, new object[] { verb });
        object state = T("BDP.Core.AttackExecution.AttackExecutionPostLoadRecovery")
            .GetMethod("ValidateCurrentAttackSession", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { verb, false, rejected });
        Check(state.ToString() == "Invalid", "明确拒绝的旧作业不能被新绑定令牌救活");
    }

    /// <summary>构造不依赖游戏地图的真实令牌。</summary>
    private static object Token(int version, string attackId)
    {
        object token = New("BDP.Core.AttackExecution.AttackSessionToken");
        Set(token, "ResultId", "weapon"); Set(token, "OwnerPawnThingId", "pawn_1");
        Set(token, "ProjectionVersion", version); Set(token, "AttackInstanceId", attackId); return token;
    }

    /// <summary>调用实际续接集合判断。</summary>
    private static ISet<string> Unchanged(object before, object after)
    {
        return (ISet<string>)T("BDP.Core.AttackExecution.AttackSessionContinuity").GetMethod("GetUnchangedResultIds", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { before, after });
    }

    /// <summary>小型反射辅助，只访问真实生产类。</summary>
    private static Type T(string name) { return core.GetType(name, true); }
    /// <summary>创建真实生产对象。</summary>
    private static object New(string name) { return Activator.CreateInstance(T(name), true); }
    /// <summary>创建实际类型的结果字典。</summary>
    private static IDictionary Dictionary(Type type) { return (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), type)); }
    /// <summary>读取结果索引。</summary>
    private static object ResultOf(object projection, string id) { return ((IDictionary)Get(projection, "ResultIndex"))[id]; }
    /// <summary>读取真实属性。</summary>
    private static object Get(object value, string name) { return value.GetType().GetProperty(name, Flags).GetValue(value, null); }
    /// <summary>设置真实属性。</summary>
    private static void Set(object value, string name, object data) { value.GetType().GetProperty(name, Flags).SetValue(value, data, null); }
    /// <summary>调用真实行为方法。</summary>
    private static object Invoke(object value, string name, params object[] data) { return value.GetType().GetMethod(name, Flags).Invoke(value, data); }
    /// <summary>沿继承链查找真实字段。</summary>
    private static FieldInfo Field(object value, string name)
    {
        for (Type type = value.GetType(); type != null; type = type.BaseType)
        { FieldInfo field = type.GetField(name, Flags | BindingFlags.DeclaredOnly); if (field != null) return field; }
        throw new MissingFieldException(name);
    }
    /// <summary>行为断言。</summary>
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); }

    /// <summary>按实际构建环境装载依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories)
        { string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
}
