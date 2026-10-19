using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;

/// <summary>用实际版本程序集的最小角色容器验证接线；不模拟地图、伤害或游戏时钟。</summary>
public static class TrionUnitSurfaceTests
{
    /// <summary>统一访问真实实例字段，避免脚本对原版大小写同名成员的适配冲突。</summary>
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>创建尚未经历游戏初始化的真实类型，仅填本次访问确需的容器字段。</summary>
    private static object Empty(Assembly assembly, string name) =>
        RuntimeHelpers.GetUninitializedObject(assembly.GetType(name, true));

    /// <summary>写入夹具事实，不替换任何被测查询逻辑。</summary>
    private static void Set(object target, string field, object value)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo member = type.GetField(field, Fields);
            if (member == null) continue;
            member.SetValue(target, value);
            return;
        }
        throw new MissingFieldException(target.GetType().FullName, field);
    }

    /// <summary>调用正式访问口并读取真实查询结果。</summary>
    private static object Call(Type type, string method, object target, params object[] args) =>
        type.GetMethod(method, Fields | BindingFlags.Static).Invoke(target, args);

    /// <summary>拒绝错误边界并返回明确原因。</summary>
    private static void Check(bool value, string reason)
    {
        if (!value) throw new InvalidOperationException(reason);
    }

    /// <summary>验证身体和运行分离，以及投影发布入口没有继续依赖人类会话。</summary>
    public static void Run(Assembly game, Assembly core, Assembly content)
    {
        object pawn = Empty(game, "Verse.Pawn");
        object pawnDef = Empty(game, "Verse.ThingDef");
        Set(pawnDef, "defName", "TrionUnitTestPawn");
        Set(pawn, "def", pawnDef);
        object health = Empty(game, "Verse.Pawn_HealthTracker");
        Set(health, "healthState", Enum.Parse(game.GetType("Verse.PawnHealthState", true), "Mobile"));
        Set(pawn, "health", health);
        object unit = Activator.CreateInstance(content.GetType("BDP.Content.Trion.Units.CompTrionUnit", true));
        Set(unit, "parent", pawn);
        Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(game.GetType("Verse.ThingComp", true));
        IList pawnComps = (IList)Activator.CreateInstance(listType);
        pawnComps.Add(unit);
        Set(pawn, "comps", pawnComps);
        TestAutoCombat(game, content, unit);
        TestChipRequirementExemption(game, core, pawn);
        TestResourceGizmo(game, core, unit, pawn, pawnComps);
        Type material = core.GetType("BDP.Core.CombatBody.CombatBodyMaterialAccess", true);
        Type functions = core.GetType("BDP.Core.Trigger.TriggerFunctionAvailability", true);
        Check((bool)Call(functions, "Allows", null, Empty(game, "Verse.Pawn")), "普通人物被新增功能准入误拒绝。");
        Check((bool)Call(material, "IsActive", null, pawn), "永久身体没有被共用访问口识别。");
        Check(!(bool)Call(functions, "Allows", null, pawn), "初始化前错误放行功能。");
        Set(unit, "ready", true);
        Set(unit, "operational", true);
        Check((bool)Call(functions, "Allows", null, pawn), "运行兵没有放行功能。");
        Set(unit, "operational", false);
        Check(!(bool)Call(functions, "Allows", null, pawn), "停机兵仍放行功能。");
        Check((bool)Call(material, "IsActive", null, pawn), "停机撤销了永久身体身份。");
        Check(Call(core.GetType("BDP.Core.CombatBody.CombatBodySurfaceAccess", true),
            "ResolveReader", null, pawn) == null, "永久身体错误接入人类转换会话。");

        object equipment = Empty(game, "Verse.ThingWithComps");
        object definition = Empty(game, "Verse.ThingDef");
        Set(equipment, "def", definition);
        Set(definition, "equipmentType", Enum.Parse(game.GetType("Verse.EquipmentType", true), "Primary"));
        object trigger = Activator.CreateInstance(core.GetType("BDP.Core.Trigger.CompTriggerBody", true));
        Set(trigger, "parent", equipment);
        IList gearComps = (IList)Activator.CreateInstance(listType);
        gearComps.Add(trigger);
        Set(equipment, "comps", gearComps);
        object tracker = Activator.CreateInstance(game.GetType("Verse.Pawn_EquipmentTracker", true), pawn);
        Set(pawn, "equipment", tracker);
        object container = tracker.GetType().GetField("equipment", Fields).GetValue(tracker);
        ((IList)container.GetType().GetField("innerList", Fields).GetValue(container)).Add(equipment);
        Set(equipment, "holdingOwner", container);
        Set(unit, "internalEquipment", equipment);
        Type internalGear = content.GetType("BDP.Content.Trion.Units.Patches.TrionUnitInternalEquipment", true);
        Check(Call(internalGear, "PrimaryForHealthCleanup", null, tracker) == null,
            "原版失能清理仍能把内置承载当成可丢弃主手。");
        Call(internalGear, "DestroyDiscardableEquipment", null, tracker,
            Enum.Parse(game.GetType("Verse.DestroyMode", true), "Vanish"));
        Check(ReferenceEquals(tracker.GetType().GetProperty("Primary").GetValue(tracker), equipment),
            "活兵倒地清装备错误销毁了内置承载。");
        Set(unit, "internalEquipment", null);
        Check(ReferenceEquals(Call(internalGear, "PrimaryForHealthCleanup", null, tracker), equipment),
            "普通主手受到内置装配保护的误干预。");
        Type policy = core.GetType("BDP.Core.CombatBodySession.CombatBodySessionPolicy", true);
        Check((bool)Call(policy, "ShouldPublishCombatProjection", Activator.CreateInstance(policy, true), pawn, trigger),
            "正式投影发布入口仍把永久身体拒绝为非人类会话。");
    }

    /// <summary>按实际定义顺序运行原版队列和征召待命节点，验证命令不会被待命饿死；不模拟寻路。</summary>
    public static void CheckPlayerOrderQueue(Assembly game, string definitionPath)
    {
        var xml = new XmlDocument();
        xml.Load(definitionPath);
        object root = Activator.CreateInstance(game.GetType("Verse.AI.ThinkNode_Priority", true));
        IList nodes = (IList)root.GetType().GetField("subNodes", Fields).GetValue(root);
        foreach (XmlNode node in xml.SelectNodes("//ThinkTreeDef[defName='BDP_TrionUnit']/thinkRoot/subNodes/li"))
        {
            string type = node.Attributes["Class"].Value;
            if (type == "ThinkNode_QueuedJob")
                nodes.Add(Activator.CreateInstance(game.GetType("Verse.AI.ThinkNode_QueuedJob", true)));
            else if (node.SelectSingleNode(".//li[@Class='JobGiver_Orders']") != null)
                nodes.Add(Activator.CreateInstance(game.GetType("Verse.AI.JobGiver_Orders", true)));
        }
        Check(nodes.Count == 2, "队列或玩家征召待命节点缺失／重复。");
        object pawn = Empty(game, "Verse.Pawn");
        object health = Empty(game, "Verse.Pawn_HealthTracker");
        Set(health, "healthState", Enum.Parse(game.GetType("Verse.PawnHealthState"), "Mobile"));
        Set(pawn, "health", health);
        object drafter = Empty(game, "RimWorld.Pawn_DraftController");
        Set(drafter, "draftedInt", true);
        Set(pawn, "drafter", drafter);
        object tracker = Empty(game, "Verse.AI.Pawn_JobTracker");
        object queue = Activator.CreateInstance(game.GetType("Verse.AI.JobQueue", true));
        Set(tracker, "jobQueue", queue);
        Set(pawn, "jobs", tracker);
        object first = Empty(game, "Verse.AI.Job");
        object second = Empty(game, "Verse.AI.Job");
        Call(queue.GetType(), "EnqueueLast", queue, first, null);
        Call(queue.GetType(), "EnqueueLast", queue, second, null);
        object gameState = Empty(game, "Verse.Game");
        Set(gameState, "uniqueIDsManager", Activator.CreateInstance(gameState.GetType().GetField("uniqueIDsManager", Fields).FieldType));
        PropertyInfo currentGame = game.GetType("Verse.Current").GetProperty("Game", BindingFlags.Public | BindingFlags.Static);
        object previousGame = currentGame.GetValue(null);
        FieldInfo binding = game.GetType("RimWorld.DefOfHelper").GetField("bindingNow", BindingFlags.Static | BindingFlags.NonPublic);
        object previousBinding = binding.GetValue(null);
        binding.SetValue(null, true);
        FieldInfo wait = game.GetType("RimWorld.JobDefOf").GetField("Wait_Combat");
        object previousWait = wait.GetValue(null);
        object waitDefinition = Empty(game, "Verse.JobDef");
        wait.SetValue(null, waitDefinition);
        currentGame.SetValue(null, gameState);
        try
        {
            object issue = Activator.CreateInstance(game.GetType("Verse.AI.JobIssueParams", true));
            foreach (object expected in new[] { first, second })
            {
                object result = Call(root.GetType(), "TryIssueJobPackage", root, pawn, issue);
                Check(ReferenceEquals(result.GetType().GetProperty("Job").GetValue(result), expected),
                    "征召待命抢在队列前返回，玩家命令仍留在等待处理。");
                Check((bool)result.GetType().GetProperty("FromQueue").GetValue(result), "没有沿原版队列取出玩家命令。");
            }
            object idle = Call(root.GetType(), "TryIssueJobPackage", root, pawn, issue);
            object idleJob = idle.GetType().GetProperty("Job").GetValue(idle);
            Check(ReferenceEquals(idleJob.GetType().GetField("def").GetValue(idleJob), waitDefinition),
                "队列为空时没有回到原版征召待命。");
        }
        finally
        {
            wait.SetValue(null, previousWait);
            binding.SetValue(null, previousBinding);
            currentGame.SetValue(null, previousGame);
        }
    }

    /// <summary>提供同一份芯片配置，仅替代制造来源，不替代正式条件求值逻辑。</summary>
    private static object ChipDefinitionProvider(Assembly game, Assembly core, object definition)
    {
        Type contract = core.GetType("BDP.Core.Chips.IChipInstanceDefinitionProvider", true);
        Type configType = definition.GetType();
        TypeBuilder builder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("TrionUnitChipFixture"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Fixture").DefineType("DeclaredChipProvider", TypeAttributes.Public,
                game.GetType("Verse.ThingComp", true), new[] { contract });
        FieldBuilder config = builder.DefineField("Definition", configType, FieldAttributes.Public);
        MethodBuilder method = builder.DefineMethod("TryGetChipDefinition",
            MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), new[] { configType.MakeByRefType() });
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, config);
        il.Emit(OpCodes.Stind_Ref);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);
        builder.DefineMethodOverride(method, contract.GetMethod("TryGetChipDefinition"));
        object provider = Activator.CreateInstance(builder.CreateType());
        Set(provider, "Definition", definition);
        return provider;
    }

    /// <summary>同芯片只免兵的激活资格，普通人物、通用条件和原始声明均不被放宽。</summary>
    private static void TestChipRequirementExemption(Assembly game, Assembly core, object pawn)
    {
        InitializeFixtureLanguage(game);
        object skill = Activator.CreateInstance(game.GetType("RimWorld.SkillDef", true));
        Set(skill, "defName", "TrionFixtureSkill");
        Set(skill, "label", "测试技能");
        object requirement = Activator.CreateInstance(core.GetType("BDP.Core.Requirements.SkillLevelRequirement", true));
        Set(requirement, "Skill", skill);
        Set(requirement, "MinimumLevel", 10f);
        IList requirements = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(
            core.GetType("BDP.Core.Requirements.PawnRequirement", true)));
        requirements.Add(requirement);
        object definition = Activator.CreateInstance(core.GetType("BDP.Core.Chips.ChipDefinitionConfig", true));
        Set(definition, "ActivationRequirements", requirements);
        object chip = Empty(game, "Verse.ThingWithComps");
        IList comps = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(
            game.GetType("Verse.ThingComp", true)));
        comps.Add(ChipDefinitionProvider(game, core, definition));
        Set(chip, "comps", comps);
        Type serviceType = core.GetType("BDP.Core.Chips.ChipActivationRequirementService", true);
        object service = serviceType.GetField("Instance").GetValue(null);
        object ordinary = Empty(game, "Verse.Pawn");
        object ordinaryResult = Call(serviceType, "Evaluate", service, ordinary, chip);
        Check(!(bool)ordinaryResult.GetType().GetProperty("Satisfied").GetValue(ordinaryResult),
            "普通人物的技能激活条件被误豁免。");
        object result = Call(serviceType, "Evaluate", service, pawn, chip);
        Check((bool)result.GetType().GetProperty("Satisfied").GetValue(result),
            "Trion兵仍被芯片角色激活条件拒绝。");
        object faction = pawn.GetType().GetProperty("Faction").GetValue(pawn);
        object factionDef = faction.GetType().GetField("def").GetValue(faction);
        Set(factionDef, "isPlayer", false);
        result = Call(serviceType, "Evaluate", service, pawn, chip);
        Check((bool)result.GetType().GetProperty("Satisfied").GetValue(result),
            "非玩家Trion兵没有豁免芯片角色激活条件。");
        Set(factionDef, "isPlayer", true);
        Check(((IList)Call(serviceType, "Describe", service, chip)).Count == 1 && requirements.Count == 1,
            "兵的豁免删除了芯片原始条件或静态说明。");
        Type generalType = core.GetType("BDP.Core.Requirements.PawnRequirementEvaluator", true);
        object generalResult = Call(generalType, "Evaluate", generalType.GetField("Instance").GetValue(null), pawn, requirements);
        Check(!(bool)generalResult.GetType().GetProperty("Satisfied").GetValue(generalResult),
            "芯片豁免错误扩散到组合等通用角色条件。");
    }

    /// <summary>独立测试进程只装入本场景需要的译文，避免未加载语言时误调用游戏日志入口。</summary>
    private static void InitializeFixtureLanguage(Assembly game)
    {
        object language = Empty(game, "Verse.LoadedLanguage");
        Set(language, "dataIsLoaded", true);
        Set(language, "workerInt", Activator.CreateInstance(game.GetType("Verse.LanguageWorker_Default", true)));
        FieldInfo dictionaryField = language.GetType().GetField("keyedReplacements", Fields);
        IDictionary replacements = (IDictionary)Activator.CreateInstance(dictionaryField.FieldType);
        string[] keys = { "BDP_Unit_Level", "BDP_Requirement_SkillLevelFailure", "BDP_Requirement_SkillLevelMinimum" };
        string[] values = { "{0}级", "{0}：当前{1}，需要{2}", "至少{0}级" };
        for (int i = 0; i < keys.Length; i++)
        {
            object replacement = RuntimeHelpers.GetUninitializedObject(dictionaryField.FieldType.GetGenericArguments()[1]);
            Set(replacement, "value", values[i]);
            replacements.Add(keys[i], replacement);
        }
        Set(language, "keyedReplacements", replacements);
        game.GetType("Verse.LanguageDatabase", true).GetField("activeLanguage").SetValue(null, language);
    }

    /// <summary>运行和停机玩家兵消费同一资源容器的现成状态条，敌方不公开容量。</summary>
    private static void TestResourceGizmo(Assembly game, Assembly core, object unit, object pawn, IList comps)
    {
        object resource = Activator.CreateInstance(core.GetType("BDP.Core.Trion.CompTrion", true));
        Set(resource, "parent", pawn);
        Set(resource, "max", 1000f);
        Set(resource, "cur", 650f);
        comps.Add(resource);
        object expected = core.GetType("BDP.Core.Trion.TrionSurfaceAccess", true)
            .GetMethod("ResolveReader", new[] { game.GetType("Verse.Pawn", true) }).Invoke(null, new[] { pawn });
        foreach (bool operating in new[] { false, true })
        {
            Set(unit, "operational", operating);
            IEnumerator gizmos = ((IEnumerable)Call(unit.GetType(), "CompGetGizmosExtra", unit)).GetEnumerator();
            Check(gizmos.MoveNext() && gizmos.Current.GetType().FullName == "BDP.Core.Trion.Gizmo_TrionStatus",
                "玩家兵没有复用资源状态条，或停机后容量被隐藏。");
            Check(ReferenceEquals(gizmos.Current.GetType().GetField("reader", Fields).GetValue(gizmos.Current), expected),
                "状态条没有读取兵的同一资源容器。");
            (gizmos as IDisposable)?.Dispose();
        }
        object faction = pawn.GetType().GetProperty("Faction").GetValue(pawn);
        object factionDef = faction.GetType().GetField("def").GetValue(faction);
        Set(factionDef, "isPlayer", false);
        Check(!((IEnumerable)Call(unit.GetType(), "CompGetGizmosExtra", unit)).GetEnumerator().MoveNext(),
            "敌方兵公开了玩家资源状态条。");
        Set(factionDef, "isPlayer", true);
        Set(unit, "operational", false);
        comps.Remove(resource);
    }

    /// <summary>验证可保存意图与作业来源边界，不在夹具中模拟游戏行为树决策。</summary>
    private static void TestAutoCombat(Assembly game, Assembly content, object unit)
    {
        PropertyInfo enabled = unit.GetType().GetProperty("AutoCombatEnabled");
        Check(enabled != null, "缺少自动战斗开关读取入口。");
        Check(!(bool)enabled.GetValue(unit), "自动战斗必须默认关闭。");
        Call(unit.GetType(), "SetAutoCombatEnabled", unit, true);
        Check((bool)enabled.GetValue(unit), "按钮命令没有保存开启意图。");
        Call(unit.GetType(), "SetAutoCombatEnabled", unit, false);
        Check(!(bool)enabled.GetValue(unit), "按钮命令没有保存关闭意图。");

        object branch = Activator.CreateInstance(content.GetType(
            "BDP.Content.Trion.Units.ThinkNode_ConditionalTrionUnitAutoCombat", true));
        object middle = Activator.CreateInstance(game.GetType("Verse.AI.ThinkNode_Priority", true));
        object giver = Activator.CreateInstance(content.GetType("BDP.Content.Trion.Units.JobGiver_TrionUnitAutoCombat", true));
        object[] priorityArguments = { null, 0f };
        Check((bool)Call(giver.GetType(), "TryGetPriority", giver, priorityArguments),
            "自动战斗的声明优先级没有被原版识别，不能抢占日常作业。");
        Check((float)priorityArguments[1] >= 1f, "自动战斗没有达到最低抢占优先级。");
        object copiedGiver = Call(giver.GetType(), "DeepCopy", giver, false);
        Check((bool)Call(copiedGiver.GetType(), "TryGetPriority", copiedGiver, priorityArguments)
            && (float)priorityArguments[1] >= 1f, "原版复制子树后丢失自动战斗抢占资格。");
        Set(middle, "parent", branch);
        Set(giver, "parent", middle);
        object job = Empty(game, "Verse.AI.Job");
        Set(job, "jobGiver", giver);
        Check((bool)Call(unit.GetType(), "IsAutoCombatJob", null, job), "没有识别自动分支中的实际作业来源。");
        TestAutoCombatAttackExit(game, content, unit, job);
        // 退出已把旧作业归还原版池，后续来源检查使用新的独立作业。
        job = Empty(game, "Verse.AI.Job");
        Set(job, "jobGiver", giver);
        Set(middle, "parent", null);
        Check(!(bool)Call(unit.GetType(), "IsAutoCombatJob", null, job), "把普通战斗或工作误认成自动战斗作业。");
        Set(job, "jobGiver", null);
        Check(!(bool)Call(unit.GetType(), "IsAutoCombatJob", null, job), "把玩家直接命令误认成自动战斗作业。");
        TestAutoCombatEligibility(game, unit);
    }

    /// <summary>真实原版动作可复位；自动退出与征召接管只收口本分支，不能清玩家新动作。</summary>
    private static void TestAutoCombatAttackExit(Assembly game, Assembly content, object unit, object automaticJob)
    {
        object pawn = unit.GetType().GetField("parent", Fields).GetValue(unit);
        object gameState = Empty(game, "Verse.Game");
        Set(gameState, "maps", Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(game.GetType("Verse.Map"))));
        PropertyInfo currentGame = game.GetType("Verse.Current").GetProperty("Game", BindingFlags.Public | BindingFlags.Static);
        object previousGame = currentGame.GetValue(null);
        currentGame.SetValue(null, gameState);
        Set(pawn, "mapIndexOrState", (sbyte)-1);
        object tracker = Empty(game, "Verse.AI.Pawn_JobTracker");
        Set(tracker, "pawn", pawn);
        Set(tracker, "curJob", automaticJob);
        Set(pawn, "jobs", tracker);
        Set(pawn, "stances", Activator.CreateInstance(game.GetType("Verse.Pawn_StanceTracker", true), pawn));
        Set(pawn, "carryTracker", Activator.CreateInstance(game.GetType("Verse.Pawn_CarryTracker", true), pawn));
        object driver = Activator.CreateInstance(game.GetType("Verse.AI.JobDriver_Wait", true));
        Set(driver, "pawn", pawn);
        Set(tracker, "curDriver", driver);
        object giver = automaticJob.GetType().GetField("jobGiver").GetValue(automaticJob);
        object jobDef = Empty(game, "Verse.JobDef");
        Set(jobDef, "defName", "TrionUnitTestWait");
        Set(automaticJob, "def", jobDef);
        object verb = Activator.CreateInstance(game.GetType("Verse.Verb_Shoot", true));
        Type states = game.GetType("Verse.VerbState", true);
        Set(verb, "state", Enum.Parse(states, "Bursting"));
        Set(verb, "burstShotsLeft", 3);
        Set(automaticJob, "verbToUse", verb);
        Call(unit.GetType(), "EndAutoCombatJob", unit, false);
        Check(verb.GetType().GetField("state", Fields).GetValue(verb).ToString() == "Idle",
            "退出自动战斗后，真实原版动作仍处于连射。");
        Check(tracker.GetType().GetField("curDriver").GetValue(tracker) == null,
            "清理攻击姿态时旧等待作业驱动器尚未卸下。");
        object nextAutomaticJob = Empty(game, "Verse.AI.Job");
        Set(nextAutomaticJob, "def", jobDef);
        Set(nextAutomaticJob, "jobGiver", giver);
        Set(nextAutomaticJob, "verbToUse", verb);
        Set(tracker, "curJob", nextAutomaticJob);
        driver = Activator.CreateInstance(game.GetType("Verse.AI.JobDriver_Wait", true));
        Set(driver, "pawn", pawn);
        Set(tracker, "curDriver", driver);
        Set(verb, "state", Enum.Parse(states, "Bursting"));
        Type draftPatch = content.GetType("BDP.Content.Trion.Units.Patches.TrionUnitAutoCombatDraftTakeoverPatch", true);
        Call(draftPatch, "Prefix", null, pawn, true);
        Check(verb.GetType().GetField("state", Fields).GetValue(verb).ToString() == "Idle",
            "征召接管前没有结束旧自动连射。");

        object manualJob = Empty(game, "Verse.AI.Job");
        Set(manualJob, "verbToUse", verb);
        Set(tracker, "curJob", manualJob);
        Set(verb, "state", Enum.Parse(states, "Bursting"));
        Call(draftPatch, "Prefix", null, pawn, true);
        Check(verb.GetType().GetField("state", Fields).GetValue(verb).ToString() == "Bursting",
            "自动退出保护错误重置了玩家直接命令的动作。");
        Set(pawn, "jobs", null);
        currentGame.SetValue(null, previousGame);
    }

    /// <summary>只填写真实角色的控制事实，逐项验证自主接管不能绕过其他限制。</summary>
    private static void TestAutoCombatEligibility(Assembly game, object unit)
    {
        object pawn = unit.GetType().GetField("parent", Fields).GetValue(unit);
        Type pawnType = game.GetType("Verse.Pawn", true);
        object faction = Empty(game, "RimWorld.Faction");
        object factionDef = Empty(game, "RimWorld.FactionDef");
        Set(factionDef, "isPlayer", true);
        Set(faction, "def", factionDef);
        Set(pawn, "factionInt", faction);
        object mind = RuntimeHelpers.GetUninitializedObject(pawnType.GetField("mindState").FieldType);
        object mental = Empty(game, "Verse.AI.MentalStateHandler");
        Set(mind, "mentalStateHandler", mental);
        Set(pawn, "mindState", mind);
        object drafter = Empty(game, "RimWorld.Pawn_DraftController");
        Set(pawn, "drafter", drafter);
        object health = pawnType.GetField("health").GetValue(pawn);
        // Spawned（在图）会核对游戏地图列表；只提供一个空地图身份，不推进地图或时钟。
        object gameState = Empty(game, "Verse.Game");
        IList maps = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(game.GetType("Verse.Map")));
        maps.Add(Empty(game, "Verse.Map"));
        Set(gameState, "maps", maps);
        PropertyInfo currentGame = game.GetType("Verse.Current").GetProperty("Game", BindingFlags.Public | BindingFlags.Static);
        object previousGame = currentGame.GetValue(null);
        currentGame.SetValue(null, gameState);
        Set(pawn, "mapIndexOrState", (sbyte)0);
        PropertyInfo allowed = unit.GetType().GetProperty("CanAutoCombat", Fields);
        Set(unit, "ready", true);
        Set(unit, "operational", true);
        Set(unit, "autoCombatEnabled", true);
        Check((bool)allowed.GetValue(unit), "正常运行的未征召玩家兵不能自主接管。");
        object emptyTargets = Empty(game, "Verse.AI.AttackTargetsCache");
        Set(emptyTargets, "targetsHostileToFaction", Activator.CreateInstance(
            emptyTargets.GetType().GetField("targetsHostileToFaction", Fields).FieldType));
        Set(maps[0], "attackTargetsCache", emptyTargets);
        Check(!(bool)unit.GetType().GetProperty("ShouldAutoCombat", Fields).GetValue(unit),
            "原版敌对缓存为空时仍进入了自动战斗。");
        Set(drafter, "draftedInt", true);
        Check(!(bool)allowed.GetValue(unit), "自动战斗抢走了征召控制。");
        Set(drafter, "draftedInt", false);
        Set(health, "healthState", Enum.Parse(game.GetType("Verse.PawnHealthState"), "Down"));
        Check(!(bool)allowed.GetValue(unit), "倒地仍允许自主战斗。");
        Set(health, "healthState", Enum.Parse(game.GetType("Verse.PawnHealthState"), "Mobile"));
        Set(mental, "curStateInt", Empty(game, "Verse.AI.MentalState_Berserk"));
        Check(!(bool)allowed.GetValue(unit), "精神状态异常仍允许本模式接管。");
        Set(mental, "curStateInt", null);
        Set(pawn, "mapIndexOrState", (sbyte)-1);
        Check(!(bool)allowed.GetValue(unit), "离图仍允许地图自主战斗。");
        Set(pawn, "mapIndexOrState", (sbyte)0);
        Set(factionDef, "isPlayer", false);
        Check(!(bool)allowed.GetValue(unit), "非玩家错误进入玩家开关分支。");
        Set(factionDef, "isPlayer", true);
        Set(unit, "operational", false);
        Check(!(bool)allowed.GetValue(unit), "停机仍允许自主战斗。");
        Set(unit, "operational", true);
        Set(unit, "autoCombatEnabled", false);
        Check(!(bool)allowed.GetValue(unit), "关闭后仍允许自主战斗。");
        // 恢复原先的首次初始化测试前置，不污染后续材料身体与投影接线检查。
        Set(unit, "ready", false);
        Set(unit, "operational", false);
        currentGame.SetValue(null, previousGame);
    }
}
