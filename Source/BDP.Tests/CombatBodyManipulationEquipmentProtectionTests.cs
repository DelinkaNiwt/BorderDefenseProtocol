using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using RimWorld;
using Verse;

/// <summary>执行真实装备销毁入口，验证操控丧失保护不会卸装，也不会扩大到正常销毁。</summary>
internal static class CombatBodyManipulationEquipmentProtectionTests
{
    /// <summary>实际游戏、Harmony 与模组依赖目录。</summary>
    private static string[] paths;
    /// <summary>访问夹具初始化需要的私有成员。</summary>
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary>实际核心程序集。</summary>
    private static Assembly core;
    /// <summary>实际健康检查保护补丁。</summary>
    private static Type healthPatch;
    /// <summary>实际临时保护上下文。</summary>
    private static Type context;
    /// <summary>夹具提供的操控能力值；不运行伤势与身体部位计算。</summary>
    private static float manipulationLevel;
    /// <summary>原版装备移除通知实际到达次数。</summary>
    private static int unequipCalls;
    /// <summary>原版物品销毁入口实际到达次数。</summary>
    private static int destroyCalls;

    /// <summary>先注册依赖解析，再读取游戏类型。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error.InnerException ?? error); return 1; }
    }

    /// <summary>只加载给定目录中的实际依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string candidate = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
        }
        return null;
    }

    /// <summary>覆盖装备保留、已有掉落保护、保护结束及不得拦截的原版销毁。</summary>
    private static void Run()
    {
        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        core = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Core.dll"));
        Assembly content = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll"));
        const string area = "BDP.Content.CombatBody.Protection.";
        healthPatch = content.GetType(area + "Patch_Pawn_HealthTracker_CheckForStateChange_CombatBodyTriggerProtection", true);
        context = content.GetType(area + "CombatBodyTriggerProtectionContext", true);
        Harmony harmony = new Harmony("BDP.Tests.ManipulationEquipmentProtection");
        try
        {
            // 只替代离线环境无法提供的能力计算及卸装后副作用，装备移除与正式保护均执行真实方法。
            harmony.Patch(AccessTools.Method(typeof(DefOfHelper), "EnsureInitializedInCtor"),
                prefix: new HarmonyMethod(typeof(CombatBodyManipulationEquipmentProtectionTests), "SkipDefOfWarning"));
            PawnCapacityDefOf.Manipulation = new PawnCapacityDef { defName = "Manipulation", minForCapable = 0f };
            harmony.Patch(AccessTools.Method(typeof(PawnCapacitiesHandler), "GetLevel"),
                prefix: new HarmonyMethod(typeof(CombatBodyManipulationEquipmentProtectionTests), "SupplyCapacityLevel"));
            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentRemoved"),
                prefix: new HarmonyMethod(typeof(CombatBodyManipulationEquipmentProtectionTests), "CountUnequip"));
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), "Destroy"),
                prefix: new HarmonyMethod(typeof(CombatBodyManipulationEquipmentProtectionTests), "CountDestroy"));
            Type destroyPatch = content.GetType(area + "Patch_Pawn_EquipmentTracker_DestroyEquipment_CombatBodyTriggerProtection");
            // 修复前没有该补丁，仍运行同一原版入口，以实际卸装行为形成失败结果。
            if (destroyPatch != null) harmony.CreateClassProcessor(destroyPatch).Patch();
            harmony.CreateClassProcessor(content.GetType(area + "Patch_Pawn_EquipmentTracker_TryDropEquipment_CombatBodyTriggerProtection", true)).Patch();

            foreach (string phase in new[] { "Active", "Collapsing" })
            {
                manipulationLevel = 0f;
                Pawn pawn = MakePawn(phase);
                ThingWithComps trigger = Equip(pawn, true);
                object state = EnterHealth(pawn);
                try
                {
                    ResetCounts();
                    pawn.equipment.DestroyEquipment(trigger);
                    Check(pawn.equipment.Primary == trigger && pawn.equipment.Contains(trigger), phase + "：操控丧失导致触发体被移出装备栏");
                    Check(unequipCalls == 0 && destroyCalls == 0, phase + "：保护后仍发出卸装通知或执行物品销毁");
                    ThingWithComps dropped;
                    Check(pawn.equipment.TryDropEquipment(trigger, out dropped, IntVec3.Zero) && dropped == null,
                        "已有普通掉落分支保护失效");
                    Check(ReadPhase(pawn) == phase, "仅操控丧失改变了战斗体阶段");
                }
                finally { ExitHealth(pawn, state); }
                ResetCounts();
                pawn.equipment.DestroyEquipment(trigger);
                Check(unequipCalls == 1 && destroyCalls == 1 && !pawn.equipment.Contains(trigger),
                    "健康检查结束后仍阻止正常销毁");
            }

            Allowed("没有健康检查上下文", "Active", true, false, 0f, false);
            Allowed("操控能力正常", "Active", true, false, 1f, true);
            Allowed("已经倒地", "Active", true, true, 0f, true);
            Allowed("普通武器", "Active", false, false, 0f, true);
            Allowed("原身阶段", "Inactive", true, false, 0f, true);
            Allowed("普通人物", null, true, false, 0f, true);
            Pawn owner = MakePawn("Active");
            ThingWithComps primary = Equip(owner, true);
            ThingWithComps secondary = Equip(owner, true);
            manipulationLevel = 0f;
            object outer = EnterHealth(owner);
            try
            {
                ResetCounts();
                owner.equipment.DestroyEquipment(secondary);
                Check(owner.equipment.Primary == primary && !owner.equipment.Contains(secondary)
                    && unequipCalls == 1 && destroyCalls == 1, "保护扩大到了非主手装备");
            }
            finally { ExitHealth(owner, outer); }
            Console.WriteLine("通过：真实销毁入口保留主手、零卸装通知、零销毁、阶段不变、既有掉落保护及正常销毁边界。");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    /// <summary>在同一临时上下文中确认非目标条件仍执行真实装备移除。</summary>
    private static void Allowed(string label, string phase, bool trigger, bool downed, float level, bool scoped)
    {
        Pawn pawn = MakePawn(phase);
        ThingWithComps equipment = Equip(pawn, trigger);
        manipulationLevel = level;
        if (downed)
        {
            FieldInfo field = typeof(Pawn_HealthTracker).GetField("healthState", Hidden);
            field.SetValue(pawn.health, Enum.Parse(field.FieldType, "Down"));
        }
        if (scoped) context.GetMethod("Enter", Hidden).Invoke(null, new object[] { pawn });
        try
        {
            ResetCounts();
            pawn.equipment.DestroyEquipment(equipment);
            Check(!pawn.equipment.Contains(equipment) && unequipCalls == 1 && destroyCalls == 1, label + "：原版销毁被错误阻止");
        }
        finally { if (scoped) context.GetMethod("Exit", Hidden).Invoke(null, new object[] { pawn }); }
    }

    /// <summary>初始化真实宿主与阶段；省略地图和伤势计算。</summary>
    private static Pawn MakePawn(string phase)
    {
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "EquipmentProtectionPawn";
        pawn.def.race = new RaceProperties();
        pawn.kindDef = (PawnKindDef)FormatterServices.GetUninitializedObject(typeof(PawnKindDef));
        pawn.kindDef.destroyGearOnDrop = true;
        pawn.health = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("pawn", Hidden).SetValue(pawn.health, pawn);
        pawn.health.capacities = new PawnCapacitiesHandler(pawn);
        pawn.equipment = new Pawn_EquipmentTracker(pawn);
        if (phase == null) return pawn;
        Type compType = core.GetType("BDP.Core.CombatBody.CompCombatBodyHost", true);
        ThingComp comp = (ThingComp)Activator.CreateInstance(compType);
        comp.parent = pawn;
        comp.props = (CompProperties)Activator.CreateInstance(core.GetType("BDP.Core.CombatBody.CompProperties_CombatBodyHost", true));
        typeof(ThingWithComps).GetField("comps", Hidden).SetValue(pawn, new List<ThingComp> { comp });
        core.GetType("BDP.Core.CombatBody.CombatBodySurfaceAccess", true).GetMethod("ResolveReader").Invoke(null, new object[] { pawn });
        object state = compType.GetField("state", Hidden).GetValue(comp);
        FieldInfo phaseField = state.GetType().GetField("phase", Hidden);
        phaseField.SetValue(state, Enum.Parse(phaseField.FieldType, phase));
        return pawn;
    }

    /// <summary>直接设置装备容器初始事实，避免夹具启动真实装备业务。</summary>
    private static ThingWithComps Equip(Pawn pawn, bool trigger)
    {
        ThingWithComps equipment = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
        equipment.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        equipment.def.defName = "EquipmentProtectionWeapon";
        equipment.def.equipmentType = EquipmentType.Primary;
        equipment.stackCount = 1;
        if (trigger)
        {
            ThingComp comp = (ThingComp)Activator.CreateInstance(core.GetType("BDP.Core.Trigger.CompTriggerBody", true));
            comp.parent = equipment;
            typeof(ThingWithComps).GetField("comps", Hidden).SetValue(equipment, new List<ThingComp> { comp });
        }
        pawn.equipment.AllEquipmentListForReading.Add(equipment);
        typeof(Thing).GetField("holdingOwner", Hidden).SetValue(equipment, pawn.equipment.GetDirectlyHeldThings());
        return equipment;
    }

    /// <summary>使用正式健康前置接线建立操控丧失保护。</summary>
    private static object EnterHealth(Pawn pawn)
    {
        object[] args = { pawn.health, null };
        healthPatch.GetMethod("Prefix").Invoke(null, args);
        return args[1];
    }

    /// <summary>通过正式异常出口恢复保护上下文。</summary>
    private static void ExitHealth(Pawn pawn, object state)
    {
        healthPatch.GetMethod("Finalizer").Invoke(null, new object[] { pawn.health, state, null });
    }

    /// <summary>读取正式宿主返回的阶段。</summary>
    private static string ReadPhase(Pawn pawn)
    {
        object reader = core.GetType("BDP.Core.CombatBody.CombatBodySurfaceAccess", true).GetMethod("ResolveReader").Invoke(null, new object[] { pawn });
        return reader.GetType().GetProperty("Phase").GetValue(reader, null).ToString();
    }

    /// <summary>离线环境直接提供定义，跳过 Unity 日志。</summary>
    private static bool SkipDefOfWarning() { return false; }
    /// <summary>只提供本测试关心的操控能力输入。</summary>
    private static bool SupplyCapacityLevel(PawnCapacityDef capacity, ref float __result)
    {
        if (capacity != PawnCapacityDefOf.Manipulation) return true;
        __result = manipulationLevel;
        return false;
    }
    /// <summary>计数真实移除出口，省略卸装后的战斗体与芯片清理。</summary>
    private static bool CountUnequip() { unequipCalls++; return false; }
    /// <summary>计数真实销毁调用，省略依赖 Unity 的物品生命周期。</summary>
    private static bool CountDestroy() { destroyCalls++; return false; }
    /// <summary>重置当前操作的可观察结果。</summary>
    private static void ResetCounts() { unequipCalls = 0; destroyCalls = 0; }
    /// <summary>失败时报告具体被破坏的玩家行为或原版边界。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
