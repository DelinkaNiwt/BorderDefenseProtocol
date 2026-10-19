using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using RimWorld;
using Verse;

/// <summary>验证真实健康页补丁与已发布泄露登记，不启动游戏界面。</summary>
internal static class CombatBodyTrionLeakIconTests
{
    /// <summary>实际依赖目录。</summary>
    private static string[] paths;
    /// <summary>实例字段查询范围。</summary>
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary>当前核心程序集。</summary>
    private static Assembly core;
    /// <summary>待测显示补丁。</summary>
    private static Type patch;
    /// <summary>离线夹具提供原版计算完成后的部位生命值。</summary>
    private static float partHealth;
    /// <summary>测试伤口的唯一编号。</summary>
    private static int nextWoundId = 100;

    /// <summary>先配置依赖，再执行验证。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>只使用当前真实游戏、补丁库与待测模组文件。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string file = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(file)) return Assembly.LoadFrom(file);
        }
        return null;
    }

    /// <summary>验证实际补丁能够生成，且包扎、原版生理流血和其他界面逻辑保留。</summary>
    private static void Run()
    {
        core = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Core.dll"));
        patch = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll")).GetType(
            "BDP.Content.CombatBody.Wounds.Visuals.Patch_HealthCardUtility_DrawHediffRow_TrionLeak");
        Check(patch != null, "健康页尚未接入 BDP 泄露图标，零流血伤口无法显示颗粒图标。");
        MethodInfo row = AccessTools.Method(typeof(HealthCardUtility), "DrawHediffRow");
        List<CodeInstruction> before = PatchProcessor.GetOriginalInstructions(row).ToList();
        new Harmony("bdp.tests.trion.leak.icon").CreateClassProcessor(patch).Patch();
        List<CodeInstruction> after = PatchProcessor.GetCurrentInstructions(row).ToList();
        Check(after.Count == before.Count + 3, "只应增加贴图选择的两条指令和尺寸累计的一条指令。");
        foreach (string name in new[] { "ShouldShowIcon", "ResolveIcon", "AccumulateIconSize" })
            Check(after.Count(code => code.operand is MethodInfo && ((MethodInfo)code.operand).DeclaringType == patch
                && ((MethodInfo)code.operand).Name == name) == 1, "健康页必须完整接入：" + name);
        Check(after.Count(code => code.Calls(AccessTools.PropertyGetter(typeof(Hediff), "StateIcon"))) ==
            before.Count(code => code.Calls(AccessTools.PropertyGetter(typeof(Hediff), "StateIcon"))), "不得替换包扎状态图标。");
        Check(after.Count(code => code.Calls(AccessTools.PropertyGetter(typeof(Hediff), "BleedRate"))) == 1,
            "保留原版流血率读取，只改变此界面的视觉累计。");

        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("tickManager", Fields).SetValue(Current.Game,
            FormatterServices.GetUninitializedObject(typeof(TickManager)));
        TestIndependentWounds();
        TestMergeAndReplacement();
        TestBleedingEligibility();
        ProbeWound ordinary = MakeWound(null, 0.25f);
        Check(Show(ordinary), "普通流血伤口继续显示血滴。");
        Check(Size(0.2f, 0.25f, ordinary) == 0.45f, "普通伤口保留原版尺寸累计。");
        Check(Call("ResolveIcon", null, ordinary) == null, "普通伤口返回原版贴图。");
        ordinary.Rate = 0f;
        Check(!Show(ordinary), "普通已止血伤口不显示。");

        foreach (string phase in new[] { "Active", "Collapsing" })
        {
            ProbeWound wound = MakeWound(phase, 0f);
            Check(!Show(wound), "未登记泄露的战斗体伤口不能凭伤势显示。");
            SetDrain(wound, true);
            Check(Show(wound), "原版流血为零时仍应显示有效泄露。");
            Check(wound.BleedRate == 0f, "显示不能更改真实流血率。");
            float size = Size(0f, 0f, wound);
            Check(Math.Abs(size - 0.36f) < 0.0001f, "15点／秒应显示16像素，不能固定占满20像素。");
            Check(Math.Abs(Size(size, 0f, wound) - 0.6f) < 0.0001f, "组内30点／秒达到20像素上限。");
            Check(Size(0.6f, 0f, wound) == 0.6f, "更多泄露也不得撑出20像素槽位。");
            SetDrain(wound, true, 1f);
            Check(Math.Abs(Size(0f, 0f, wound) - 0.136f) < 0.0001f, "1点／秒应显示约12.3像素。");
            SetDrain(wound, false);
            Check(!Show(wound), "注销来源后即使伤口还在也必须隐藏。");
            wound.Rate = 0.25f;
            Check(!Show(wound), "战斗体伤口不能回退到生理流血显示。");
            SetDrain(wound, true);
            wound.pawn.health.hediffSet.hediffs.Clear();
            Check(!Show(wound), "已经移除的伤口实例不能继续显示。");
        }
        foreach (string phase in new[] { "Inactive", "Cooldown" })
        {
            ProbeWound wound = MakeWound(phase, 0.25f);
            Check(Show(wound), "退出战斗体后恢复原版血滴判定。");
            Check(Size(0.2f, 0.25f, wound) == 0.45f, "原身尺寸累计不受影响。");
        }
        Console.WriteLine("通过：真实健康页与受伤补丁生成；独立伤口、错开到期、恢复不续期、原版合并、停止后再受伤、零损伤、400点限额、断口替代及下级去重；动态尺寸、包扎槽及普通人物边界。地图效果需游戏内复测。");
    }

    /// <summary>建立真实宿主、部位、伤口组件与健康集合，跳过地图生成。</summary>
    private static ProbeWound MakeWound(string phase, float bleedRate)
    {
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "TestPawn";
        pawn.health = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("pawn", Fields).SetValue(pawn.health, pawn);
        pawn.health.hediffSet = new HediffSet(pawn);
        var comps = new List<ThingComp>();
        // 原版无组件对象使用 null；当前 GetComp（取组件）短路径不接受手造空列表。
        if (phase != null) typeof(ThingWithComps).GetField("comps", Fields).SetValue(pawn, comps);
        if (phase != null)
        {
            Type hostType = core.GetType("BDP.Core.CombatBody.CompCombatBodyHost", true);
            ThingComp host = (ThingComp)Activator.CreateInstance(hostType);
            host.parent = pawn;
            host.props = (CompProperties)Activator.CreateInstance(core.GetType("BDP.Core.CombatBody.CompProperties_CombatBodyHost", true));
            comps.Add(host);
            core.GetType("BDP.Core.CombatBody.CombatBodySurfaceAccess").GetMethod("ResolveReader").Invoke(null, new object[] { pawn });
            object state = hostType.GetField("state", Fields).GetValue(host);
            FieldInfo phaseField = state.GetType().GetField("phase", Fields);
            phaseField.SetValue(state, Enum.Parse(phaseField.FieldType, phase));
            if (phase == "Cooldown") state.GetType().GetField("cooldownEndTick", Fields).SetValue(state, int.MaxValue);
            ThingComp trion = (ThingComp)Activator.CreateInstance(core.GetType("BDP.Core.Trion.CompTrion", true));
            trion.parent = pawn;
            comps.Add(trion);
        }
        BodyDef body = new BodyDef();
        BodyPartRecord part = new BodyPartRecord { body = body, def = new BodyPartDef(), customLabel = "测试部位" };
        body.corePart = part;
        pawn.def.race = new RaceProperties { body = body };
        var wound = new ProbeWound { pawn = pawn, Part = part, def = BleedingDef(), Rate = bleedRate, Severity = 1f, loadID = nextWoundId++ };
        AddInfo(wound);
        pawn.health.hediffSet.hediffs.Add(wound);
        return wound;
    }

    /// <summary>给夹具伤口挂真实信息组件，等同原版定义加载后生成的组件。</summary>
    private static void AddInfo(HediffWithComps wound)
    {
        HediffComp info = (HediffComp)Activator.CreateInstance(core.GetType("BDP.Core.CombatBody.Wounds.CombatBodyWoundTrionInfoHediffComp", true));
        info.parent = wound;
        wound.comps = new List<HediffComp> { info };
    }

    /// <summary>在真实前后置接线及账本上验证同部位两伤错开到期，不复写扣费算法。</summary>
    private static void TestIndependentWounds()
    {
        var boundary = new Harmony("bdp.tests.wound.health.boundary");
        boundary.Patch(AccessTools.Method(typeof(HediffSet), "GetPartHealth"),
            prefix: new HarmonyMethod(typeof(CombatBodyTrionLeakIconTests).GetMethod("PartHealth", BindingFlags.Static | BindingFlags.NonPublic)));
        boundary.Patch(AccessTools.Method(typeof(BodyPartDef), "GetMaxHealth"),
            prefix: new HarmonyMethod(typeof(CombatBodyTrionLeakIconTests).GetMethod("MaxHealth", BindingFlags.Static | BindingFlags.NonPublic)));
        new Harmony("bdp.tests.wound.addition").CreateClassProcessor(core.GetType(
            "BDP.Patches.Patch_HediffSet_AddDirect_CombatBodyWounds", true)).Patch();
        object policy = core.GetType("BDP.Core.CombatBody.Wounds.CombatBodyWoundPolicy")
            .GetField("fallbackPolicy", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        policy.GetType().GetField("trionDrainEnabled").SetValue(policy, true);
        policy.GetType().GetField("trionDrainPerDamagePerSecond").SetValue(policy, 1f);

        ProbeWound first = MakeWound("Active", 0f);
        Pawn pawn = first.pawn;
        first.Severity = 6f;
        pawn.health.hediffSet.hediffs.Clear();
        SetTick(0);
        ApplyAddition(first, pawn, 30f, 24f);
        var second = new ProbeWound { pawn = pawn, Part = first.Part, def = first.def, Severity = 4f, loadID = nextWoundId++ };
        AddInfo(second);
        SetTick(240);
        ApplyAddition(second, pawn, 24f, 20f);
        Check(Ledger(pawn).Count == 2, "同部位两条真实伤口必须各有一条登记，不能共用部位来源。");
        Check(Ledger(pawn).Values.Cast<float>().Sum() == 10f, "两伤应合计6+4，不能重复套用部位总损伤。");
        Check(Math.Abs(Size(Size(0f, 0f, first), 0f, second) - 0.28f) < 0.0001f,
            "同组两条真实伤口按6+4合计显示约14.7像素。");
        object runtime = pawn.AllComps.First(comp => comp.GetType().Name == "CompCombatBodyHost")
            .GetType().GetProperty("WoundRuntime", Fields).GetValue(pawn.AllComps.First(comp => comp.GetType().Name == "CompCombatBodyHost"), null);
        object binding = runtime.GetType().GetField("trionBinding", Fields).GetValue(runtime);
        Check(((IEnumerable<int>)binding.GetType().GetMethod("GetActiveHediffLoadIds", Fields)
            .Invoke(binding, new object[] { pawn })).Count() == 1, "同部位地图喷射应只保留一个有效代表。");
        var records = (System.Collections.IList)binding.GetType().GetField("woundDrains", Fields).GetValue(binding);
        Check((int)records[0].GetType().GetField("expiryTick").GetValue(records[0]) == 600
            && (int)records[1].GetType().GetField("expiryTick").GetValue(records[1]) == 840,
            "第二伤在第240刻出现，不得为第一伤续期。");
        // 模拟原版已完成引用恢复后的重发：只有账本清空，保存的伤口引用、速率与期限保留。
        ThingComp trion = pawn.AllComps.First(comp => comp.GetType().Name == "CompTrion");
        ((System.Collections.IDictionary)trion.GetType().GetField("drainRegistry", Fields).GetValue(trion)).Clear();
        first.Severity = 2f;
        object restored = binding.GetType().GetMethod("RestoreAfterLoad", Fields).Invoke(binding, new object[] { pawn, 500 });
        Check((int)restored == 600 && Ledger(pawn).Values.Cast<float>().Sum() == 10f,
            "恢复必须保持保存速率和原期限，不能按愈合后严重度重算。");
        var expired = new List<int>();
        object next = binding.GetType().GetMethod("ExpireIdleDrains", Fields).Invoke(binding, new object[] { pawn, 600, expired });
        Check((int)next == 840 && Ledger(pawn).Count == 1 && Ledger(pawn).Values.Cast<float>().Single() == 4f,
            "第一条到期必须只停止6点，第二条继续4点／秒。");
        Check(!Show(first) && Show(second) && Math.Abs(Size(0f, 0f, second) - 0.184f) < 0.0001f,
            "第一伤到期后图标应按第二伤的4点／秒缩小。");
        Check(((IEnumerable<int>)binding.GetType().GetMethod("GetActiveHediffLoadIds", Fields)
            .Invoke(binding, new object[] { pawn })).Single() == second.loadID, "过期代表必须切换到仍有效的第二伤口。");
        binding.GetType().GetMethod("ExpireIdleDrains", Fields).Invoke(binding, new object[] { pawn, 840, expired });
        Check(Ledger(pawn).Count == 0, "第二条到期后必须全部停止。");
        binding.GetType().GetMethod("RestoreAfterLoad", Fields).Invoke(binding, new object[] { pawn, 900 });
        Check(Ledger(pawn).Count == 0, "过期旧伤恢复时不得重新启动。");
        SetTick(0);
    }

    /// <summary>覆盖真实原版合并、停止后再受伤、零损伤、过量损伤与断口替代。</summary>
    private static void TestMergeAndReplacement()
    {
        ProbeWound first = MakeWound("Active", 0f);
        Pawn pawn = first.pawn;
        first.def = new HediffDef { injuryProps = new InjuryProps { canMerge = true, bleedRate = 0.06f } };
        first.Severity = 6f;
        pawn.health.hediffSet.hediffs.Clear();
        ApplyAddition(first, pawn, 30f, 24f);
        Type addition = core.GetType("BDP.Patches.Patch_HediffSet_AddDirect_CombatBodyWounds", true);
        var incoming = new ProbeWound { pawn = pawn, Part = first.Part, def = first.def, Severity = 3f, loadID = nextWoundId++ };
        partHealth = 24f;
        object[] state = { incoming, pawn, null };
        addition.GetMethod("Prefix").Invoke(null, state);
        Check(first.TryMergeWith(incoming), "夹具应运行原版真实伤口合并。");
        partHealth = 21f;
        SetTick(100);
        addition.GetMethod("Postfix").Invoke(null, new object[] { incoming, pawn, state[2] });
        Check(Ledger(pawn).Count == 1 && Ledger(pawn).Values.Cast<float>().Single() == 9f,
            "合并只加本次3点，不能注册消失的临时实例或重复算9点。");
        object binding = Binding(pawn);
        var records = (System.Collections.IList)binding.GetType().GetField("woundDrains", Fields).GetValue(binding);
        Check(ReferenceEquals(records[0].GetType().GetField("wound").GetValue(records[0]), first), "合并记录必须引用最终伤口。");
        Check((int)records[0].GetType().GetField("expiryTick").GetValue(records[0]) == 700, "合并加重只续期该伤口。");
        partHealth = 21f;
        state = new object[] { incoming, pawn, null };
        addition.GetMethod("Prefix").Invoke(null, state);
        SetTick(400);
        addition.GetMethod("Postfix").Invoke(null, new object[] { incoming, pawn, state[2] });
        Check((int)records[0].GetType().GetField("expiryTick").GetValue(records[0]) == 700, "零实际损伤不得续期。");
        binding.GetType().GetMethod("ExpireIdleDrains", Fields).Invoke(binding, new object[] { pawn, 700, new List<int>() });
        partHealth = 21f;
        incoming.Severity = 2f;
        state = new object[] { incoming, pawn, null };
        addition.GetMethod("Prefix").Invoke(null, state);
        Check(first.TryMergeWith(incoming), "过期旧伤仍由原版决定是否合并。");
        partHealth = 19f;
        SetTick(710);
        addition.GetMethod("Postfix").Invoke(null, new object[] { incoming, pawn, state[2] });
        Check(Ledger(pawn).Values.Cast<float>().Single() == 2f, "过期伤口再次受伤只登记新增2点，不复活旧9点。");

        first = MakeWound("Active", 0f); pawn = first.pawn;
        first.Severity = 400f;
        pawn.health.hediffSet.hediffs.Clear();
        ApplyAddition(first, pawn, 30f, 0f);
        Check(Ledger(pawn).Values.Cast<float>().Single() == 30f, "400点攻击不得超过实际30点损伤。");
        // 原版断肢结束后的真实列表：原伤口已被断口替代。
        partHealth = 1f;
        state = new object[] { first, pawn, null };
        addition.GetMethod("Prefix").Invoke(null, state);
        pawn.health.hediffSet.hediffs.Clear();
        var stump = new Hediff_MissingPart { pawn = pawn, Part = first.Part, def = BleedingDef(), loadID = nextWoundId++ };
        pawn.health.hediffSet.hediffs.Add(stump);
        partHealth = 0f;
        addition.GetMethod("Postfix").Invoke(null, new object[] { first, pawn, state[2] });
        addition.GetMethod("Postfix").Invoke(null, new object[] { first, pawn, state[2] });
        Check(Ledger(pawn).Count == 1 && Ledger(pawn).Values.Cast<float>().Single() == 30f,
            "断口必须替代旧伤；递归重复通知也只登记一次全额。");
        var childPart = new BodyPartRecord { body = first.Part.body, def = new BodyPartDef(), parent = first.Part, customLabel = "下级部位" };
        var child = new ProbeWound { pawn = pawn, Part = childPart, def = first.def, loadID = nextWoundId++, Severity = 4f };
        ApplyAddition(child, pawn, 4f, 0f);
        Check(Ledger(pawn).Count == 1, "缺失祖先下的递归伤口不得额外计费。");
        pawn.health.hediffSet.hediffs.Clear();
        binding = Binding(pawn);
        binding.GetType().GetMethod("ReconcileActiveWounds", Fields).Invoke(binding, new object[] { pawn });
        Check(Ledger(pawn).Count == 0, "部位恢复清理列表后必须注销断口。");
        SetTick(0);
    }

    /// <summary>验证原版不流血的伤口及部位不泄露；钝击最终产生的有效断口仍按断口处理。</summary>
    private static void TestBleedingEligibility()
    {
        ProbeWound bruise = MakeWound("Active", 0f);
        Pawn pawn = bruise.pawn;
        bruise.def.injuryProps.bleedRate = 0f;
        pawn.health.hediffSet.hediffs.Clear();
        ApplyAddition(bruise, pawn, 30f, 24f);
        Check(Ledger(pawn).Count == 0 && !Show(bruise), "瘀伤的原版流血系数为零，真实损伤也不得登记泄露或显示图标。");

        ProbeWound cut = MakeWound("Active", 0f);
        pawn = cut.pawn;
        cut.Part.def.bleedRate = 0f;
        pawn.health.hediffSet.hediffs.Clear();
        ApplyAddition(cut, pawn, 30f, 24f);
        Check(Ledger(pawn).Count == 0, "部位原版流血系数为零时，割伤也不得泄露。");
        cut.Part.def.bleedRate = 1f;
        pawn.health.hediffSet.hediffs.Clear();
        ApplyAddition(cut, pawn, 24f, 20f);
        Check(Ledger(pawn).Values.Cast<float>().Single() == 4f && Show(cut), "战斗体真实流血已压为零，原版可流血割伤仍须按新增损伤泄露。");
        // 模拟旧版保存了不应泄露的伤口；读档前资源账本已按既有生命周期清空。
        cut.def.injuryProps.bleedRate = 0f;
        Check(!Show(cut), "不合资格的旧登记不能继续显示颗粒图标。");
        ThingComp trion = pawn.AllComps.First(comp => comp.GetType().Name == "CompTrion");
        ((System.Collections.IDictionary)trion.GetType().GetField("drainRegistry", Fields).GetValue(trion)).Clear();
        object binding = Binding(pawn);
        binding.GetType().GetMethod("RestoreAfterLoad", Fields).Invoke(binding, new object[] { pawn, 0 });
        Check(Ledger(pawn).Count == 0 && ((System.Collections.IList)binding.GetType().GetField("woundDrains", Fields).GetValue(binding)).Count == 0,
            "读档必须移除旧版误记的非流血伤口，不能重新发布。");

        pawn = bruise.pawn;
        Type addition = core.GetType("BDP.Patches.Patch_HediffSet_AddDirect_CombatBodyWounds", true);
        partHealth = 6f;
        object[] state = { bruise, pawn, null };
        addition.GetMethod("Prefix").Invoke(null, state);
        pawn.health.hediffSet.hediffs.Clear();
        var stump = new Hediff_MissingPart { pawn = pawn, Part = bruise.Part, def = BleedingDef(), loadID = nextWoundId++ };
        pawn.health.hediffSet.hediffs.Add(stump);
        partHealth = 0f;
        addition.GetMethod("Postfix").Invoke(null, new object[] { bruise, pawn, state[2] });
        Check(Ledger(pawn).Values.Cast<float>().Single() == 30f, "钝击最终形成会流血断口时，必须根据最终断口登记一次全额。");
        stump.Part.def.bleedRate = 0f;
        Binding(pawn).GetType().GetMethod("ReconcileActiveWounds", Fields).Invoke(Binding(pawn), new object[] { pawn });
        Check(Ledger(pawn).Count == 0, "断口同样受部位流血资格约束，校准必须注销失效来源。");
        Console.WriteLine("通过：瘀伤不泄露、不显示；无流血部位排除；压制流血的割伤仍泄露；读档清理错误旧登记；钝击断肢按最终断口处理。");
    }

    /// <summary>夹具使用原版割伤的正流血系数，避免把生理流血被压制与定义不流血混为一谈。</summary>
    private static HediffDef BleedingDef() { return new HediffDef { injuryProps = new InjuryProps { bleedRate = 0.06f } }; }

    /// <summary>从真实宿主取得当前登记层。</summary>
    private static object Binding(Pawn pawn)
    {
        ThingComp host = pawn.AllComps.First(comp => comp.GetType().Name == "CompCombatBodyHost");
        object runtime = host.GetType().GetProperty("WoundRuntime", Fields).GetValue(host, null);
        return runtime.GetType().GetField("trionBinding", Fields).GetValue(runtime);
    }
    /// <summary>设置夹具游戏时刻，不运行地图。</summary>
    private static void SetTick(int tick) { typeof(TickManager).GetField("ticksGameInt", Fields).SetValue(Find.TickManager, tick); }

    /// <summary>调用正式添加前后置，中间只提供原版最终保留的伤口和健康结果。</summary>
    private static void ApplyAddition(ProbeWound wound, Pawn pawn, float before, float after)
    {
        Type addition = core.GetType("BDP.Patches.Patch_HediffSet_AddDirect_CombatBodyWounds", true);
        partHealth = before;
        object[] args = { wound, pawn, null };
        addition.GetMethod("Prefix").Invoke(null, args);
        wound.pawn = pawn;
        pawn.health.hediffSet.hediffs.Add(wound);
        partHealth = after;
        addition.GetMethod("Postfix").Invoke(null, new object[] { wound, pawn, args[2] });
    }

    /// <summary>读取真实资源登记表快照。</summary>
    private static System.Collections.IDictionary Ledger(Pawn pawn)
    {
        ThingComp trion = pawn.AllComps.First(comp => comp.GetType().Name == "CompTrion");
        return (System.Collections.IDictionary)trion.GetType().GetMethod("GetDrainSnapshot", Fields).Invoke(trion, null);
    }
    /// <summary>游戏外只替代已经计算完成的部位健康边界。</summary>
    private static bool PartHealth(ref float __result) { __result = partHealth; return false; }
    /// <summary>离线固定部位上限，避免进入 Unity（游戏引擎）人物属性计算。</summary>
    private static bool MaxHealth(ref float __result) { __result = 30f; return false; }

    /// <summary>通过真实资源组件登记／注销与伤口查询一致的部位来源。</summary>
    private static void SetDrain(Hediff wound, bool enabled, float perSecond = 15f)
    {
        Type utility = core.GetType("BDP.Core.CombatBody.Wounds.CombatBodyWoundTrionDrainUtility", true);
        object key = utility.GetMethod("BuildDrainKey", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { wound });
        ThingComp trion = wound.pawn.AllComps.First(comp => comp.GetType().Name == "CompTrion");
        if (enabled)
        {
            object kind = Enum.Parse(core.GetType("BDP.Core.Trion.TrionDrainKind"), "WoundLeak");
            trion.GetType().GetMethod("RegisterDrain", Fields).Invoke(trion, new object[] { key, perSecond, kind });
        }
        else trion.GetType().GetMethod("UnregisterDrain", Fields).Invoke(trion, new[] { key });
    }

    /// <summary>调用正式显示方法。</summary>
    private static object Call(string name, params object[] args)
    {
        return patch.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }
    /// <summary>读取真实显示决定。</summary>
    private static bool Show(Hediff wound) { return (bool)Call("ShouldShowIcon", wound); }
    /// <summary>读取真实视觉累计。</summary>
    private static float Size(float current, float rate, Hediff wound) { return (float)Call("AccumulateIconSize", current, rate, wound); }
    /// <summary>输出明确的行为失败。</summary>
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    /// <summary>仅替代流血率读取，避免离线测试进入 Unity 属性计算；其余使用原版伤口。</summary>
    private sealed class ProbeWound : Hediff_Injury
    {
        /// <summary>测试输入的真实生理流血率。</summary>
        internal float Rate;
        /// <summary>允许独立验证零流血时的泄露图标。</summary>
        public override float BleedRate { get { return Rate; } }
        /// <summary>夹具输入最终伤势，避免赋值时触发地图健康通知。</summary>
        public override float Severity { get; set; }
    }
}
