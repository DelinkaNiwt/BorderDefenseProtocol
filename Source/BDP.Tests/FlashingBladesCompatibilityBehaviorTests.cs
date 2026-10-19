using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BDP.Core.Chips;
using BDP.Core.Verbs;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using HarmonyLib;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Visual;
using UnityEngine;
using RimWorld;
using Verse;

/// <summary>调用真实兼容程序集及真实契约解释；不模拟地图与界面绘制。</summary>
internal static class FlashingBladesCompatibilityBehaviorTests
{
    /// <summary>失败项数量。</summary>
    private static int failures;
    /// <summary>待检查的真实目录实现。</summary>
    private static Type catalog;
    /// <summary>依赖搜索目录。</summary>
    private static string[] directories;
    /// <summary>真实反射目标。</summary>
    private static object targets;
    /// <summary>真实目录实例。</summary>
    private static object entries;
    /// <summary>真实外部程序集。</summary>
    private static Assembly foreign;
    /// <summary>真实播放桥。</summary>
    private static Type runtime;
    /// <summary>真实生成补丁传给外部方法的效果配置，仅用于跳过地图渲染前的观察。</summary>
    private static object observedProfile;
    /// <summary>真实生成补丁传给外部方法的临时图像类别。</summary>
    private static object observedTexture;

    /// <summary>安装依赖解析后再进入含游戏类型的方法。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        return Run();
    }

    /// <summary>只从显式指定目录解析依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        foreach (string directory in directories)
        {
            string path = Path.Combine(directory, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>检查身份与正式状态选择行为。</summary>
    private static int Run()
    {
        // 独立进程没有 Unity 日志宿主；只跳过 DefOf 的启动警告，不替换契约解释。
        new Harmony("bdp.tests.flashingblades.environment").Patch(AccessTools.Method(typeof(DefOfHelper), "EnsureInitializedInCtor"),
            prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipEnvironment"));
        // 只隔离 ThingDef 默认图形的 Unity 资源加载；被测分类输入会显式填写所需字段。
        new Harmony("bdp.tests.flashingblades.graphics").Patch(AccessTools.Constructor(typeof(ThingDef)),
            prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipEnvironment"));
        // 未启动游戏的夹具不加载语言资源，仅抑制缺少语言包时调用 Unity 日志的环境错误。
        new Harmony("bdp.tests.flashingblades.translation").Patch(AccessTools.Method(typeof(Log), "Error", new[] { typeof(string) }),
            prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipEnvironment"));
        new Harmony("bdp.tests.flashingblades.log").Patch(AccessTools.Method(typeof(Log), "Message", new[] { typeof(string) }),
            prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipEnvironment"));
        new Harmony("bdp.tests.flashingblades.warning").Patch(AccessTools.Method(typeof(Log), "Warning", new[] { typeof(string) }),
            prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipEnvironment"));
        // 游戏 Mono 支持 StringBuilder.Append(StringBuilder)，Windows CLR 不支持；此夹具不验证语言排版。
        new Harmony("bdp.tests.flashingblades.formatting").Patch(AccessTools.Method(typeof(GrammarResolverSimple), "Formatted", new[] { typeof(TaggedString), typeof(List<string>), typeof(List<object>) }),
            prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipFormatting"));
        DamageDefOf.Cut = new DamageDef { defName = "Cut" };
        DamageDefOf.Blunt = new DamageDef { defName = "Blunt" };
        DamageDefOf.Stab = new DamageDef { defName = "Stab" };
        Assembly compatibility = Assembly.LoadFrom(Path.Combine(directories[2], "BDP.Compatibility.dll"));
        catalog = compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesCatalog");
        Test("存在可独立加载的刀光目录", delegate { Check(catalog != null, "缺少刀光目录实现"); });
        if (catalog == null) return 1;
        Test("真实外部程序集必需接口匹配", delegate {
            Assembly.LoadFrom(Path.Combine(directories[3], "BDP.Content.dll"));
            foreign = Assembly.LoadFrom(Path.Combine(directories[4], "DaoGuangJianYing.dll"));
            targets = Activator.CreateInstance(compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesDiscovery"),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { AppDomain.CurrentDomain.GetAssemblies() }, null);
            runtime = compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesRuntimeBridge");
        });
        Test("同一功能状态共享稳定键", delegate {
            Check(Key("F", "Kogetsu", null, null, null) == Key("F", "Kogetsu", "", "", ""), "空层级未归一化");
            Check(Key("F", "Kogetsu", null, null, null) != Key("F", "Scorpion", null, null, null), "不同功能配置串用");
        });
        Test("构型同名状态保留来源范围", delegate {
            Check(Key("C", "Blade", "First", "shield", "guard") != Key("C", "Blade", "Second", "shield", "guard"), "同名状态误合并");
            Check(Key("C", "Blade", null, null, null) == Key("C", "Blade", "", "", ""), "无状态构型未共享");
            Check(Key("F", "a|b", null, null, null) != Key("F", "a", "b", null, null), "分隔符导致身份碰撞");
        });
        Test("无形态只生成基础状态", delegate { Check(States(Config(Melee("blade"))).Count == 1, "基础状态重复"); });
        Test("姿态继承形态公共盾击", delegate {
            ChipExpressionConfig config = Shield();
            Check(States(config).Count == 3, "状态数量不符");
            Check(Tools(config, "shield", "mobile").Count == 1, "灵活姿态丢失公共近战");
            Check(Tools(config, "shield", "guard").Count == 1, "举盾姿态丢失公共近战");
            Check(Tools(config, "heavy", null).Count == 1, "重刃近战丢失");
        });
        Test("纯防御和未启用工具不算近战", delegate {
            ChipExpressionConfig config = Shield();
            config.Modes[0].ActiveEntryIds.Clear();
            Check(Tools(config, "shield", "mobile").Count == 0, "误取另一形态近战");
            Check(Tools(config, "shield", "guard").Count == 0, "把防御效果当近战");
        });
        Test("无效状态不回退生成默认刀光条目", delegate {
            Check(Tools(Shield(), "missing", "guard").Count == 0, "不存在形态回退到默认");
            Check(Tools(Shield(), "shield", "missing").Count == 0, "不存在姿态回退到默认");
        });
        Test("仅远程攻击不算近战", delegate {
            ChipExpressionEntryConfig entry = Melee("ranged");
            entry.WeaponMode = VerbExpressionModeConfig.Ranged;
            entry.Tool = null;
            entry.VerbProps = new VerbProperties { verbClass = typeof(Verb_Shoot) };
            Check(Tools(Config(entry), null, null).Count == 0, "把远程攻击当近战");
        });
        Test("范围读取正式有效近战表面且拒绝不存在状态", delegate {
            ChipExpressionConfig config = Shield();
            config.Entries[0].VerbProps.range = 1.42f;
            config.Entries[1].VerbProps.range = 2.1f;
            Check(Ranges(config, "shield", "guard").SequenceEqual(new[] { 1.42f }), "举盾范围混入其它形态");
            Check(Ranges(config, "heavy", null).SequenceEqual(new[] { 2.1f }), "重刃范围未取正式攻击表面");
            Check(Ranges(config, "missing", null).Length == 0, "不存在状态回退到默认范围");
            Check(config.Entries[1].VerbProps.range == 2.1f, "读取改变攻击参数");
        });
        Test("视觉尺寸复用真实图像选择并区分纯贴图替换", delegate {
            Type visual = compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesVisualSource");
            Check(visual != null, "缺少芯片有效视觉尺寸读取");
            MethodInfo resolve = AccessTools.Method(visual, "ResolveSize");
            ExpressionVisualPresetDef preset = VisualPreset(1.2f, 2f);
            preset.ActiveGraphicData = new GraphicData();
            Set(preset, "cachedActiveGraphic", GraphicSize(1.6f));
            Check(Near(Size(resolve, preset, null, false, WeaponVisualActionStage.Idle, false), 2.4f), "双持绘制倍率丢失");
            Check(Near(Size(resolve, preset, null, true, WeaponVisualActionStage.Idle, false), 3.2f), "激活图像尺寸未生效");
            Check(Near(Size(resolve, preset, null, true, WeaponVisualActionStage.Idle, true), 1.6f), "纯贴图替换错误乘预设倍率");
            ExpressionVisualPresetDef overlay = VisualPreset(1.45f, 9f);
            Check(Near(Size(resolve, preset, overlay, false, WeaponVisualActionStage.Idle, false), 2.9f), "图层覆盖错误改变基础姿态倍率");
            ExpressionVisualStageOverrideConfig stage = new ExpressionVisualStageOverrideConfig { Stage = WeaponVisualActionStage.Firing, GraphicData = new GraphicData() };
            Set(stage, "cachedGraphic", GraphicSize(1.65f)); overlay.StageVisuals = new List<ExpressionVisualStageOverrideConfig> { stage };
            Check(Near(Size(resolve, preset, overlay, false, WeaponVisualActionStage.Firing, false), 3.3f), "动作阶段图像尺寸未生效");
            Check(Near(Size(resolve, null, null, false, WeaponVisualActionStage.Idle, false), 1f), "无视觉仍继承触发体尺寸");
        });
        Test("已发布视觉按攻击结果分手读取且单武器不借另一芯片", delegate {
            Type visualType = compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesVisualSource", true);
            object visual = Activator.CreateInstance(visualType, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { targets }, null);
            ExpressionVisualPresetDef main = VisualPreset(1.2f, 2f), sub = VisualPreset(1.65f, 1f), dual = VisualPreset(1.45f, 2f);
            main.defName = "TestMain"; sub.defName = "TestSub"; dual.defName = "TestDual";
            DefDatabase<ExpressionVisualPresetDef>.Add(main); DefDatabase<ExpressionVisualPresetDef>.Add(sub); DefDatabase<ExpressionVisualPresetDef>.Add(dual);
            Thing chip = new Thing { def = new ThingDef { defName = "TestChip" }, thingIDNumber = 123 };
            object projection = CoreObject("BDP.Core.Expressions.VisualExpressionProjection");
            object first = CoreObject("BDP.Core.Expressions.VisualResidentEntry"), second = CoreObject("BDP.Core.Expressions.VisualResidentEntry");
            Member(first, "ResultId", "main"); Member(first, "VisualPresetDefName", main.defName);
            Member(first, "CompositeVisualPresetDefName", dual.defName); Member(first, "VerbAttackRole", VerbAttackRole.Primary);
            object reference = CoreObject("BDP.Core.Expressions.ExpressionSourceReference"); Member(reference, "ChipThingId", chip.ThingID); Member(first, "SourceReference", reference);
            Member(second, "ResultId", "sub"); Member(second, "VisualPresetDefName", sub.defName);
            IList residents = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(first.GetType())); residents.Add(first); residents.Add(second);
            Member(projection, "ResidentEntries", residents); EnumMember(projection, "HostEquipmentRenderMode", "Replace");
            EnumMember(projection, "RelationKind", "SingleSide");
            Check(Near((float)Invoke(visual, "ReadProjection", projection, null, null, null, "main", chip), 2.4f), "主手结果尺寸错误");
            Check(Near((float)Invoke(visual, "ReadProjection", projection, null, null, null, "sub", chip), 1.65f), "副手串用主手尺寸");
            Check(Near((float)Invoke(visual, "ReadProjection", projection, null, null, null, "missing", chip), 1f), "失配结果借另一只手兜底");
            EnumMember(projection, "RelationKind", "DualWeapon");
            Check(Near((float)Invoke(visual, "ReadProjection", projection, null, null, null, "main", chip), 2.9f), "双持复合预设未生效");
            EnumMember(projection, "HostEquipmentRenderMode", "ReplaceTextureOnly");
            Check(Near((float)Invoke(visual, "ReadProjection", projection, null, null, null, "secondary", chip), 1.2f), "同芯片副攻击未读取实际主图像或错误乘倍率");
            Thing other = new Thing { def = chip.def, thingIDNumber = 456 };
            Check(Near((float)Invoke(visual, "ReadProjection", projection, null, null, null, "secondary", other), 1f), "借另一芯片图像兜底");
        });
        Test("兼容程序集没有内容或外部硬依赖", delegate {
            string[] references = compatibility.GetReferencedAssemblies().Select(value => value.Name).ToArray();
            Check(!references.Contains("BDP.Content") && !references.Contains("BDP.Development") && !references.Contains("DaoGuangJianYing"), "破坏程序集边界");
        });
        Test("真实构型替换与追加复用正式覆盖", delegate {
            ChipManufacturingPresetDef function = Function("First", Config(Melee("cut")));
            function.config.Expression.Entries[0].tools = new List<Tool> { function.config.Expression.Entries[0].Tool };
            function.config.Expression.Entries[0].Tool = null;
            ChipArmamentFormDef form = new ChipArmamentFormDef { overrides = new ChipArmamentFormOverrides {
                tools = new List<Tool> { Club() }, toolsMerge = MergeStrategy.Replace } };
            ChipDefinitionConfig copy = ChipConfigurationMergeService.CloneSingle(function);
            ChipArmamentFormApplicationService.Apply(copy, form);
            List<object> tools = Tools(copy.Expression, null, null);
            Check(tools.Count == 1 && ((Tool)tools[0]).label == "club", "构型未替换工具");
            Check(function.config.Expression.Entries[0].tools[0].label == "blade", "构型修改了原定义");
            form.overrides.toolsMerge = MergeStrategy.Append;
            copy = ChipConfigurationMergeService.CloneSingle(function);
            ChipArmamentFormApplicationService.Apply(copy, form);
            Check(Tools(copy.Expression, null, null).Count == 2, "构型未追加工具");
        });
        Test("目录共享构型且保留各来源自动类别", delegate {
            ChipProfessionDef profession = new ChipProfessionDef { defName = "TestProfession" };
            DefDatabase<ChipProfessionDef>.Add(profession);
            ChipManufacturingPresetDef first = Function("First", Config(Melee("cut")));
            ChipExpressionEntryConfig blunt = Melee("bash"); blunt.Tool = Club();
            ChipManufacturingPresetDef second = Function("Second", Config(blunt));
            DefDatabase<ChipManufacturingPresetDef>.Add(first);
            DefDatabase<ChipManufacturingPresetDef>.Add(second);
            ChipManufacturingPresetDef intrinsic = Function("Shield", Shield());
            DefDatabase<ChipManufacturingPresetDef>.Add(intrinsic);
            ChipExpressionConfig mixed = Shield();
            mixed.Entries.Add(Melee("stance:Blade"));
            mixed.Modes[0].Stances[1].ActiveEntryIds.Add("stance:Blade");
            DefDatabase<ChipManufacturingPresetDef>.Add(Function("Mixed", mixed));
            DefDatabase<ChipArmamentFormDef>.Add(new ChipArmamentFormDef { defName = "Shared", label = "shared", compatibleProfessions = new List<ChipProfessionDef> { profession } });
            DefDatabase<ChipArmamentFormDef>.Add(new ChipArmamentFormDef { defName = "Replace", label = "replacement", compatibleProfessions = new List<ChipProfessionDef> { profession },
                overrides = new ChipArmamentFormOverrides { tool = Club() } });
            DefDatabase<ChipArmamentFormDef>.Add(new ChipArmamentFormDef { defName = "Rejected", label = "rejected", compatibleProfessions = new List<ChipProfessionDef>() });
            entries = Activator.CreateInstance(catalog, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { targets }, null);
            Invoke(entries, "EnsureInitialized");
            object firstRow, secondRow, firstCategory, secondCategory;
            Check(Select("First", "Shared", null, null, out firstRow, out firstCategory), "丢失构型行");
            Check(Select("Second", "Shared", null, null, out secondRow, out secondCategory), "丢失另一来源");
            Check(ReferenceEquals(firstRow, secondRow), "无状态构型未共享条目");
            Check(!Equals(firstCategory, secondCategory), "不同来源自动类别被合并");
            Check((string)Invoke(entries, "AutomaticLabel", firstRow) == "BDP_FB_AutoBySource".Translate().ToString(), "没有动态默认提示");
            Check(Select("First", "Replace", null, null, out firstRow, out firstCategory) && firstCategory.ToString() == "Blunt", "未按构型有效工具分类");
            IDictionary all = (IDictionary)Get(entries, "Entries");
            Check(!all.Keys.Cast<string>().Any(value => value.Contains("Rejected")), "非法构型入列");
            Check(Select("First", null, "First", null, out firstRow, out firstCategory) && (string)Get(firstRow, "Key") == Key("F", "First", null, null, null), "制造形态未归一化");
        });
        Test("公共近战只列形态且姿态新增近战按实际攻击分开", delegate {
            object guard, mobile, category;
            Check(Select("Shield", null, "shield", "guard", out guard, out category), "举盾继承攻击失配");
            Check(Select("Shield", null, "shield", "mobile", out mobile, out category), "灵活继承攻击失配");
            Check(ReferenceEquals(guard, mobile) && (string)Get(guard, "Key") == Key("F", "Shield", null, "shield", null), "公共近战仍按姿态重复配置");
            IDictionary all = (IDictionary)Get(entries, "Entries");
            Check(!all.Contains(Key("F", "Shield", null, "shield", "guard")) && !all.Contains(Key("F", "Shield", null, "shield", "mobile")), "继承姿态残留条目");
            Check(SelectAttack("Mixed", null, "shield", "guard", "bash", out guard, out category)
                && (string)Get(guard, "Key") == Key("F", "Mixed", null, "shield", null), "公共攻击误用姿态刀光");
            Check(SelectAttack("Mixed", null, "shield", "guard", "stance:Blade", out mobile, out category)
                && (string)Get(mobile, "Key") == Key("F", "Mixed", null, "shield", "guard"), "姿态攻击未独立配置");
            Check(!ReferenceEquals(guard, mobile), "姿态攻击混入公共条目");
            Check(SelectAttack("Mixed", "Replace", "shield", "guard", "bash", out guard, out category)
                && (string)Get(guard, "Key") == Key("C", "Replace", "Mixed", "shield", null), "构型公共攻击优先级丢失");
            Check(SelectAttack("Mixed", "Replace", "shield", "guard", "stance:Blade", out mobile, out category)
                && (string)Get(mobile, "Key") == Key("C", "Replace", "Mixed", "shield", "guard"), "构型姿态攻击失配");
            Check(!SelectAttack("Mixed", null, "shield", "mobile", "stance:Blade", out mobile, out category), "未启用的姿态攻击错误回退");
            Check(!Select("Mixed", null, "shield", "guard", out mobile, out category), "缺少攻击身份时猜测混合姿态归属");
            Check(!SelectAttack("Mixed", null, "shield", "missing", "bash", out mobile, out category), "不存在姿态仍套公共配置");
        });
        Test("真实设置数据库重载与上下文冻结不串用", delegate {
            Type settingsType = foreign.GetType("DaoGuangJianYing.DGJY_Settings");
            object settings = FormatterServices.GetUninitializedObject(settingsType);
            Set(settings, "weaponVfxOverrides", Activator.CreateInstance(settingsType.GetField("weaponVfxOverrides").FieldType));
            foreign.GetType("DaoGuangJianYing.DGJY_Mod").GetField("Settings").SetValue(null, settings);
            Type database = foreign.GetType("DaoGuangJianYing.DGJY_WeaponVfxOverrideDatabase");
            new Harmony("bdp.tests.flashingblades.io").Patch(AccessTools.Method(database, "SafeWrite"),
                prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "SkipEnvironment"));
            Dictionary<string, MethodInfo> methods = (Dictionary<string, MethodInfo>)Get(targets, "Methods");
            Type category = foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxCategory");
            object claw = Enum.Parse(category, "Claw");
            methods["SetOverride"].Invoke(null, new[] { Key("F", "First", null, null, null), claw });
            runtime.GetMethod("Bind", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { targets, entries, new Action<Exception>(ThrowFailure) });
            object first = Context(), form = Context();
            Capture(first, "First", null, null, null);
            Capture(form, "First", "Shared", null, null);
            Check(Get(Selection(first), "Texture").Equals(claw), "功能覆盖未生效");
            Check(Get(Selection(form), "Texture").ToString() == "Unknown", "构型自动继承了功能手动覆盖");
            string formKey = Key("C", "Shared", null, null, null);
            methods["SetOverride"].Invoke(null, new[] { formKey, claw });
            AccessTools.Method(database, "MarkDirty").Invoke(null, null);
            object next = Context(); Capture(next, "Second", "Shared", null, null);
            Check(Get(Selection(next), "Texture").Equals(claw), "字符串设置重载丢失");
            Check(Get(Selection(form), "Texture").ToString() == "Unknown", "新设置污染旧上下文");
            Check(!Get(Selection(next), "Category").Equals(Get(Selection(form), "Category")), "上下文自动类别串用");
            methods["ClearOverride"].Invoke(null, new object[] { formKey });
            object reset = Context(); Capture(reset, "First", "Shared", null, null);
            Check(Get(Selection(reset), "Texture").ToString() == "Unknown", "单项重置失败");
            object unchanged = Context(); Capture(unchanged, "First", null, null, null);
            Check(Get(Selection(unchanged), "Texture").Equals(claw), "重置连带清理其它配置");
            methods["ClearAll"].Invoke(null, null);
            object cleared = Context(); Capture(cleared, "First", null, null, null);
            Check(Get(Selection(cleared), "Texture").ToString() == "Unknown", "清空全部漏掉虚拟键");
            object[] classify = { first, null };
            Check(!(bool)runtime.GetMethod("ClassifyPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, classify), "未接管 BDP 分类");
            Check(Get(classify[1], "category").Equals(Get(Selection(first), "Category")), "错误基础配置");
            object previous = classify[1]; classify[1] = null;
            runtime.GetMethod("ClassifyPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, classify);
            Check(!ReferenceEquals(previous, classify[1]), "共享外部分类结果对象");
            object[] spawn = { null, first, Enum.Parse(category, "Punch") };
            runtime.GetMethod("SpawnPrefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { spawn });
            Check(spawn[2].Equals(claw), "生成参数未使用本项纹理");
            object[] automaticSpawn = { null, form, claw };
            runtime.GetMethod("SpawnPrefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { automaticSpawn });
            Check(automaticSpawn[2].ToString() == "Unknown", "构型自动仍受触发体覆盖影响");
            object unknown = Context(); Capture(unknown, "missing", null, null, null);
            Check(Get(Selection(unknown), "ConfigKey") == null && Get(Selection(unknown), "Category").ToString() == "Blunt"
                && Get(Selection(unknown), "Texture").ToString() == "Unknown", "来源失配回退错误");
            object guard = Context(), heavy = Context();
            Capture(guard, "Shield", null, "shield", "guard"); Capture(heavy, "Shield", null, "heavy", null);
            Check((string)Get(Selection(guard), "ConfigKey") == Key("F", "Shield", null, "shield", null), "举盾未继承形态配置");
            Check((string)Get(Selection(heavy), "ConfigKey") == Key("F", "Shield", null, "heavy", null), "切换形态后未更新配置");
        });
        Test("手动刀光在还原后的延迟绘制仍使用本项图像", delegate {
            Type category = foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxCategory");
            object stab = Enum.Parse(category, "Stab"), claw = Enum.Parse(category, "Claw");
            GraphicData slashGraphic = new GraphicData { texPath = "test/slash" };
            GraphicData stabGraphic = new GraphicData { texPath = "test/stab" };
            GraphicData clawGraphic = new GraphicData { texPath = "test/claw" };
            FleckDef slashFleck = new FleckDef { defName = "TestSlash", graphicData = slashGraphic, growthRate = 1.25f };
            FleckDef stabFleck = new FleckDef { defName = "TestStab", graphicData = stabGraphic,
                randomGraphics = new List<GraphicData> { stabGraphic } };
            FleckDef clawFleck = new FleckDef { defName = "TestClaw", randomGraphics = new List<GraphicData> { clawGraphic } };
            FleckDef flash = new FleckDef { defName = "ExplosionFlash", graphicData = slashGraphic };
            EffecterDef effect = new EffecterDef { defName = "TestSlashEffect", children = new List<SubEffecterDef> {
                new SubEffecterDef { fleckDef = slashFleck, scale = new FloatRange(2.8f, 3.8f), speed = new FloatRange(1.8f, 2.1f) },
                new SubEffecterDef { fleckDef = flash } } };
            EffecterDef trail = new EffecterDef { defName = "TestSlashTrail", children = new List<SubEffecterDef> {
                new SubEffecterDef { fleckDef = slashFleck, scale = new FloatRange(2.8f, 3.8f) } } };
            object profile = Profile("Slash", effect, trail);
            Profile("Stab", new EffecterDef { children = new List<SubEffecterDef> { new SubEffecterDef { fleckDef = stabFleck } } }, null);
            Profile("Claw", new EffecterDef { children = new List<SubEffecterDef> { new SubEffecterDef { fleckDef = clawFleck } } }, null);
            // 使用真实外部数据库解析刚加入的测试定义，不替换图像覆盖方法。
            SetStatic(foreign.GetType("DaoGuangJianYing.DGJY_ProfileDatabase"), "initialized", false);
            Dictionary<string, MethodInfo> methods = (Dictionary<string, MethodInfo>)Get(targets, "Methods");
            string key = Key("F", "First", null, null, null);
            try
            {
                methods["SetOverride"].Invoke(null, new[] { key, stab });
                object context = Context(); Capture(context, "First", null, null, null);
                object[] spawn = { profile, context, claw };
                runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { spawn });
                EffecterDef actual = (EffecterDef)Get(spawn[0], "effecterDef");
                FleckDef retained = SpawnAndRestore(actual, spawn[2]);
                Check(retained.GetGraphicData(0).texPath == "test/stab", "手动穿刺生成后被还原成自动挥砍图像");
                Check(!ReferenceEquals(profile, spawn[0]) && !ReferenceEquals(actual, effect)
                    && !ReferenceEquals(actual.children[0], effect.children[0]) && !ReferenceEquals(retained, slashFleck), "手动覆盖仍写共享定义");
                Check(ReferenceEquals(slashFleck.graphicData, slashGraphic) && ReferenceEquals(stabFleck.graphicData, stabGraphic)
                    && ReferenceEquals(effect.children[0].fleckDef, slashFleck), "污染原效果或手动图像来源定义");
                Check(actual.children[0].scale == effect.children[0].scale && actual.children[0].speed == effect.children[0].speed
                    && retained.growthRate == slashFleck.growthRate && Get(spawn[0], "category").ToString() == "Slash", "手动图像改变自动类别或运动参数");
                Check(ReferenceEquals(actual.children[1].fleckDef, flash), "命中闪光被换成刀光");
                Check(ReferenceEquals(retained.randomGraphics, stabFleck.randomGraphics), "随机图像候选没有保留");
                EffecterDef actualTrail = (EffecterDef)Get(spawn[0], "extraTrailEffecterDef");
                Check(SpawnAndRestore(actualTrail, spawn[2]).GetGraphicData(0).texPath == "test/stab", "附加拖尾仍使用自动图像");
                for (int i = 0; i < 3; i++)
                {
                    object[] burst = { profile, context, claw };
                    runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { burst });
                    Check(ReferenceEquals(burst[0], spawn[0]), "相同配置每击重复生成定义");
                    Check(SpawnAndRestore((EffecterDef)Get(burst[0], "effecterDef"), burst[2]).GetGraphicData(0).texPath == "test/stab", "连续攻击丢失手动图像");
                }
                methods["SetOverride"].Invoke(null, new[] { key, claw });
                object other = Context(); Capture(other, "First", null, null, null);
                object[] otherSpawn = { profile, other, stab };
                runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { otherSpawn });
                Check(SpawnAndRestore((EffecterDef)Get(otherSpawn[0], "effecterDef"), otherSpawn[2]).GetGraphicData(0).texPath == "test/claw", "切换手动类别未更新");
                Check(retained.GetGraphicData(0).texPath == "test/stab", "新类别污染早先粒子");
                methods["ClearAll"].Invoke(null, null);
                object automatic = Context(); Capture(automatic, "First", null, null, null);
                object[] autoSpawn = { profile, automatic, claw };
                runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { autoSpawn });
                Check(ReferenceEquals(autoSpawn[0], profile) && autoSpawn[2].ToString() == "Unknown"
                    && SpawnAndRestore(effect, autoSpawn[2]).GetGraphicData(0).texPath == "test/slash", "恢复自动后仍残留手动图像或触发体覆盖");
                object[] vanillaSpawn = { profile, Context(), claw };
                runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { vanillaSpawn });
                Check(ReferenceEquals(vanillaSpawn[0], profile) && vanillaSpawn[2].Equals(claw), "改变了非 BDP 图像路径");
                }
            finally { methods["ClearAll"].Invoke(null, null); }
        });
        Test("配置页搜索筛选与重复刷新", delegate {
            Type windowType = foreign.GetType("DaoGuangJianYing.DGJY_Window_WeaponReachOverrides");
            object window = FormatterServices.GetUninitializedObject(windowType);
            IList rows = (IList)Activator.CreateInstance(AccessTools.Field(windowType, "filteredEntries").FieldType);
            Set(window, "filteredEntries", rows); Set(window, "searchText", "First"); Set(window, "selectedPackageId", ""); Set(window, "showOverriddenOnly", false);
            Type bridge = compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesWindowBridge");
            bridge.GetMethod("Bind", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { targets, entries, new Action<Exception>(ThrowFailure) });
            MethodInfo filter = bridge.GetMethod("FilterPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            filter.Invoke(null, new[] { window });
            Check(rows.Count > 0, "搜索没有找到功能条目");
            int count = rows.Count; filter.Invoke(null, new[] { window }); Check(rows.Count == count, "刷新追加重复行");
            rows.Clear(); Set(window, "selectedPackageId", "another.mod"); filter.Invoke(null, new[] { window }); Check(rows.Count == 0, "模组筛选不生效");
            Set(window, "selectedPackageId", "NIWT.BDP"); filter.Invoke(null, new[] { window }); Check(rows.Count == count, "BDP 筛选丢失条目");
            rows.Clear(); Set(window, "showOverriddenOnly", true); filter.Invoke(null, new[] { window }); Check(rows.Count == 0, "未覆盖条目通过仅覆盖筛选");
            Dictionary<string, MethodInfo> methods = (Dictionary<string, MethodInfo>)Get(targets, "Methods");
            object claw = Enum.Parse(foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxCategory"), "Claw");
            methods["SetOverride"].Invoke(null, new[] { Key("F", "First", null, null, null), claw });
            filter.Invoke(null, new[] { window }); Check(rows.Count == 1, "仅覆盖筛选未找到虚拟设置");
            methods["ClearAll"].Invoke(null, null);
        });
        Test("真实补丁可安装并独立卸载", delegate {
            Type adapterType = compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesAdapter");
            object missing = Activator.CreateInstance(adapterType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { new Assembly[0] }, null);
            object[] init = { new Harmony("bdp.tests.flashingblades.absent"), null };
            Check(!(bool)Invoke(missing, "TryInitialize", init), "外部缺失仍安装");
            object unsupported = Activator.CreateInstance(adapterType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { new[] { foreign } }, null);
            init = new object[] { new Harmony("bdp.tests.flashingblades.unsupported"), null };
            Check(!(bool)Invoke(unsupported, "TryInitialize", init), "缺少必要内容接口仍安装");
            Check(!Harmony.GetAllPatchedMethods().Any(value => Harmony.GetPatchInfo(value).Owners.Contains("bdp.tests.flashingblades.unsupported")), "发现失败留下补丁");
            object adapter = Activator.CreateInstance(adapterType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { AppDomain.CurrentDomain.GetAssemblies() }, null);
            Harmony harmony = new Harmony("bdp.tests.flashingblades.install");
            init = new object[] { harmony, null };
            Check((bool)Invoke(adapter, "TryInitialize", init), "真实补丁安装失败：" + init[1]);
            Dictionary<string, MethodInfo> methods = (Dictionary<string, MethodInfo>)Get(targets, "Methods");
            Check(methods.Values.Count(value => Harmony.GetPatchInfo(value) != null && Harmony.GetPatchInfo(value).Owners.Contains(harmony.Id)) == 8, "补丁安装不完整");
            object[] range = { new BdpVerb_MeleeAttackDamage(), null, 99f };
            Check(!(bool)methods["Reach"].Invoke(null, range) && (float)range[2] == 0f && range[1] == null, "扩距仍接管 BDP");
            object[] vanilla = { new Verb_MeleeAttackDamage(), new object[] { null, null, 99f }, false };
            Check((bool)runtime.GetMethod("ReachPrefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, vanilla), "扩距改变非 BDP 逻辑");
            Assembly development = Assembly.LoadFrom(Path.Combine(directories[5], "BDP.Development.dll"));
            Type diagnostics = development.GetType("BDP.Development.Compatibility.FlashingBladesPlaybackDiagnostics", true);
            MethodInfo toggle = AccessTools.Method(diagnostics, "Toggle");
            toggle.Invoke(null, null);
            Check(AccessTools.Field(diagnostics, "harmony").GetValue(null) != null, "开发诊断未能安装");
            object context = Context(); Capture(context, "First", null, null, null);
            object settings = foreign.GetType("DaoGuangJianYing.DGJY_Mod").GetField("Settings").GetValue(null);
            Set(settings, "enabled", true); Set(settings, "extendedMeleeReachEnabled", true);
            Set(settings, "dynamicWeaponScale", true); Set(settings, "globalScaleMultiplier", 1.2f);
            object scaleProfile = Activator.CreateInstance(foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxProfileDef"));
            Set(scaleProfile, "category", Enum.Parse(foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxCategory"), "Slash"));
            Capture(context, "First", null, null, null, 2.4f);
            Check(Near((float)methods["Scale"].Invoke(null, new[] { scaleProfile, context }), 2.88f), "刀光缩放仍未使用冻结芯片尺寸");
            Set(settings, "dynamicWeaponScale", false);
            Check(Near((float)methods["Scale"].Invoke(null, new[] { scaleProfile, context }), 1.2f), "关闭动态缩放仍受芯片或触发体扩距影响");
            Set(settings, "dynamicWeaponScale", true); Set(settings, "globalScaleMultiplier", 10f);
            Check(Near((float)methods["Scale"].Invoke(null, new[] { scaleProfile, context }), 3.6f), "全局倍率没有沿用原限制");
            object[] normalScale = { scaleProfile, Context(), 0f };
            Check((bool)AccessTools.Method(runtime, "ScalePrefix").Invoke(null, normalScale), "非 BDP 缩放被接管");
            Set(settings, "globalScaleMultiplier", 1f);
            object actualClassification = methods["Classify"].Invoke(null, new[] { context });
            Check(Get(actualClassification, "category").Equals(Get(Selection(context), "Category")), "真实分类补丁未返回冻结类别");
            Type diagnosticWindowType = foreign.GetType("DaoGuangJianYing.DGJY_Window_WeaponReachOverrides");
            object diagnosticWindow = FormatterServices.GetUninitializedObject(diagnosticWindowType);
            Set(diagnosticWindow, "filteredEntries", Activator.CreateInstance(AccessTools.Field(diagnosticWindowType, "filteredEntries").FieldType));
            Set(diagnosticWindow, "searchText", ""); Set(diagnosticWindow, "selectedPackageId", ""); Set(diagnosticWindow, "showOverriddenOnly", false);
            compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesWindowBridge").GetMethod("FilterPostfix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { diagnosticWindow });
            Check(AccessTools.Field(diagnostics, "harmony").GetValue(null) != null && (bool)AccessTools.Field(diagnostics, "listed").GetValue(null), "打开配置页使诊断停止");
            object[] spawn = { null, context, null };
            runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { spawn });
            Check((int)AccessTools.Field(diagnostics, "played").GetValue(null) == 1, "诊断未观察真实生成入口");
            toggle.Invoke(null, null);
            runtime.GetMethod("SpawnPrefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { spawn });
            Check((int)AccessTools.Field(diagnostics, "played").GetValue(null) == 1, "诊断关闭后仍观察");
            // 调用真实 Harmony 包装后的外部生成方法，验证参数确实回写到原调用。
            object manual = Enum.Parse(foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxCategory"), "Stab");
            methods["SetOverride"].Invoke(null, new[] { Key("F", "First", null, null, null), manual });
            object attack = Context(); Capture(attack, "First", null, null, null);
            object originalProfile = AccessTools.Method(foreign.GetType("DaoGuangJianYing.DGJY_ProfileDatabase"), "GetProfile")
                .Invoke(null, new[] { Enum.Parse(manual.GetType(), "Slash") });
            Harmony rendering = new Harmony("bdp.tests.flashingblades.rendering");
            rendering.Patch(methods["Spawn"], prefix: new HarmonyMethod(typeof(FlashingBladesCompatibilityBehaviorTests), "ObserveSpawn") { priority = Priority.Last });
            try
            {
                methods["Spawn"].Invoke(null, new[] { originalProfile, attack, manual });
                Check(!ReferenceEquals(observedProfile, originalProfile) && observedTexture.ToString() == "Unknown"
                    && ((EffecterDef)Get(observedProfile, "effecterDef")).children[0].fleckDef.GetGraphicData(0).texPath == "test/stab", "真实生成补丁没有使用稳定手动图像");
            }
            finally { rendering.UnpatchAll(rendering.Id); methods["ClearAll"].Invoke(null, null); }
            Invoke(adapter, "Disable", new object[] { null });
            Check(!methods.Values.Any(value => Harmony.GetPatchInfo(value) != null && Harmony.GetPatchInfo(value).Owners.Contains(harmony.Id)), "卸载遗留补丁");
        });
        Console.WriteLine("刀光兼容行为检查：" + (failures == 0 ? "通过" : failures + " 项失败"));
        return failures == 0 ? 0 : 1;
    }

    /// <summary>调用真实内部目录方法。</summary>
    private static object Call(string name, params object[] args)
    {
        return catalog.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, args);
    }
    /// <summary>取得真实稳定键。</summary>
    private static string Key(string kind, string definition, string owner, string mode, string stance)
    {
        return (string)Call("BuildKey", kind, definition, owner, mode, stance);
    }
    /// <summary>取得真实状态枚举。</summary>
    private static List<object> States(ChipExpressionConfig config) { return ((IEnumerable)Call("ReadStates", config)).Cast<object>().ToList(); }
    /// <summary>取得真实契约近战工具。</summary>
    private static List<object> Tools(ChipExpressionConfig config, string mode, string stance) { return ((IEnumerable)Call("ReadTools", config, mode, stance)).Cast<object>().ToList(); }
    /// <summary>从正式契约读取范围，不依赖界面排版。</summary>
    private static float[] Ranges(ChipExpressionConfig config, string mode, string stance) { Check(AccessTools.Method(catalog, "ReadRanges") != null, "缺少正式范围读取"); return ((IEnumerable)Call("ReadRanges", config, mode, stance)).Cast<float>().ToArray(); }
    /// <summary>构造真实核心只读投影类型，不模拟解析器。</summary>
    private static object CoreObject(string name) { return Activator.CreateInstance(typeof(ExpressionVisualPresetDef).Assembly.GetType(name, true), true); }
    /// <summary>填写真实只读投影夹具属性。</summary>
    private static void Member(object instance, string name, object value) { AccessTools.Property(instance.GetType(), name).SetValue(instance, value, null); }
    /// <summary>按真实属性枚举类型布置当前投影政策。</summary>
    private static void EnumMember(object instance, string name, string value) { PropertyInfo property = AccessTools.Property(instance.GetType(), name); property.SetValue(instance, Enum.Parse(property.PropertyType, value), null); }
    /// <summary>资源加载之外的真实图像实例，图像选择方法仍执行正式代码。</summary>
    private static Graphic GraphicSize(float size) { Graphic graphic = (Graphic)FormatterServices.GetUninitializedObject(typeof(Graphic_Single)); graphic.drawSize = new Vector2(size, size); return graphic; }
    /// <summary>预热图像缓存以隔离 Unity 资源环境。</summary>
    private static ExpressionVisualPresetDef VisualPreset(float size, float scale) { ExpressionVisualPresetDef preset = new ExpressionVisualPresetDef { GraphicData = new GraphicData(), DrawScale = scale }; Set(preset, "cachedGraphic", GraphicSize(size)); return preset; }
    /// <summary>调用真实兼容尺寸读取。</summary>
    private static float Size(MethodInfo method, ExpressionVisualPresetDef preset, ExpressionVisualPresetDef overlay, bool active, WeaponVisualActionStage stage, bool textureOnly) { return (float)method.Invoke(null, new object[] { preset, overlay, active, stage, textureOnly, null }); }
    /// <summary>容许浮点乘法的正常误差。</summary>
    private static bool Near(float actual, float expected) { return Math.Abs(actual - expected) < 0.0001f; }
    /// <summary>反射调用真实实例，不替换业务方法。</summary>
    private static object Invoke(object instance, string method, params object[] args) { return AccessTools.Method(instance.GetType(), method).Invoke(instance, args); }
    /// <summary>读取内部测试结果。</summary>
    private static object Get(object instance, string field) { return AccessTools.Field(instance.GetType(), field).GetValue(instance); }
    /// <summary>布置独立进程的环境字段。</summary>
    private static void Set(object instance, string field, object value) { AccessTools.Field(instance.GetType(), field).SetValue(instance, value); }
    /// <summary>只重置真实数据库的初始化状态，使本夹具定义参与正常索引。</summary>
    private static void SetStatic(Type type, string field, object value) { AccessTools.Field(type, field).SetValue(null, value); }
    /// <summary>加入仅存在于夹具进程的外部配置，仍由真实外部数据库按类别查找。</summary>
    private static object Profile(string category, EffecterDef effect, EffecterDef trail)
    {
        Type type = foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxProfileDef");
        object profile = Activator.CreateInstance(type);
        ((Def)profile).defName = "TestProfile" + category;
        Set(profile, "category", Enum.Parse(foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxCategory"), category));
        Set(profile, "effecterDef", effect); Set(profile, "extraTrailEffecterDef", trail);
        typeof(DefDatabase<>).MakeGenericType(type).GetMethod("Add", new[] { type }).Invoke(null, new[] { profile });
        return profile;
    }
    /// <summary>按真实外部覆盖/还原时序保留生成时的定义引用；不伪造粒子绘制结果。</summary>
    private static FleckDef SpawnAndRestore(EffecterDef effect, object category)
    {
        Type texture = foreign.GetType("DaoGuangJianYing.DGJY_WeaponVfxTextureOverride");
        object token = AccessTools.Method(texture, "Apply").Invoke(null, new[] { (object)effect, category });
        FleckDef retained = effect.children[0].fleckDef;
        if (token != null) Invoke(token, "Restore");
        return retained;
    }
    /// <summary>执行真实目录选择并读取输出参数。</summary>
    private static bool Select(string function, string form, string mode, string stance, out object row, out object category)
    {
        object[] args = { function, form, mode, stance, null, null };
        bool found = (bool)Invoke(entries, "TrySelect", args); row = args[4]; category = args[5]; return found;
    }
    /// <summary>沿核心单侧结果真实身份格式选择同姿态内的不同攻击。</summary>
    private static bool SelectAttack(string function, string form, string mode, string stance, string attack, out object row, out object category)
    {
        Check(AccessTools.Method(catalog, "TrySelectAttack") != null, "缺少按攻击声明归属的选择");
        object[] args = { function, form, mode, stance, "chip:" + attack, "chip", null, null };
        bool found = (bool)Invoke(entries, "TrySelectAttack", args); row = args[6]; category = args[7]; return found;
    }
    /// <summary>构造分类工具。</summary>
    private static Tool Club() { return new Tool { label = "club", power = 20, cooldownTime = 1, capacities = new List<ToolCapacityDef> { new ToolCapacityDef { defName = "Blunt" } } }; }
    /// <summary>仅在夹具进程增加潜在功能定义。</summary>
    private static ChipManufacturingPresetDef Function(string name, ChipExpressionConfig expression) { return new ChipManufacturingPresetDef { defName = name, label = name, config = new ChipDefinitionConfig { Expression = expression } }; }
    /// <summary>独立上下文，不构造地图和视觉资源。</summary>
    private static object Context() { object context = Activator.CreateInstance(foreign.GetType("DaoGuangJianYing.DGJY_MeleeVfxContext")); Set(context, "tool", Club()); return context; }
    /// <summary>执行正式上下文冻结入口。</summary>
    private static void Capture(object context, string function, string form, string mode, string stance, float size = 1f) { runtime.GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { context, function, form, mode, stance, "result", "chip", size }); }
    /// <summary>读取对应上下文的选择。</summary>
    private static object Selection(object context) { return runtime.GetMethod("ResolveSelection", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { context }); }
    /// <summary>夹具不能吞掉正式适配异常。</summary>
    private static void ThrowFailure(Exception error) { throw error; }
    /// <summary>构造有显式原版表面的近战声明，避免依赖完整游戏 Def 初始化。</summary>
    private static ChipExpressionEntryConfig Melee(string id)
    {
        DamageDef damage = new DamageDef { defName = "Cut" };
        VerbProperties props = new VerbProperties { verbClass = typeof(Verb_MeleeAttackDamage), meleeDamageDef = damage };
        return new ChipExpressionEntryConfig { Id = id, DisplayLabel = id, WeaponMode = VerbExpressionModeConfig.Melee,
            Tool = new Tool { label = "blade", power = 10, cooldownTime = 1, capacities = new List<ToolCapacityDef> { new ToolCapacityDef { defName = "Cut" } } },
            VerbProps = props, Maneuver = new ManeuverDef { defName = "Cut", verb = props } };
    }
    /// <summary>构造不带形态的声明。</summary>
    private static ChipExpressionConfig Config(params ChipExpressionEntryConfig[] entries) { return new ChipExpressionConfig { Entries = entries.ToList() }; }
    /// <summary>构造公共近战加姿态防御的真实声明结构。</summary>
    private static ChipExpressionConfig Shield()
    {
        ChipExpressionConfig config = Config(Melee("bash"), Melee("blade"),
            new ChipExpressionEntryConfig { Id = "defence", EntryType = ChipExpressionEntryTypeConfig.Hediff, HediffDefName = "shield" });
        config.DefaultModeKey = "shield";
        config.Modes = new List<ChipExpressionModeConfig> {
            new ChipExpressionModeConfig { ModeKey = "shield", DisplayLabel = "shield", ActiveEntryIds = new List<string> { "bash" }, DefaultStanceKey = "mobile",
                Stances = new List<ChipExpressionStanceConfig> {
                    new ChipExpressionStanceConfig { StanceKey = "mobile", DisplayLabel = "mobile", ActiveEntryIds = new List<string> { "defence" } },
                    new ChipExpressionStanceConfig { StanceKey = "guard", DisplayLabel = "guard", ActiveEntryIds = new List<string> { "defence" } } } },
            new ChipExpressionModeConfig { ModeKey = "heavy", DisplayLabel = "heavy", ActiveEntryIds = new List<string> { "blade" } } };
        return config;
    }
    /// <summary>执行独立行为断言。</summary>
    private static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("通过：" + name); }
        catch (Exception error) { failures++; while (error.InnerException != null) error = error.InnerException; Console.Error.WriteLine("失败：" + name + "：" + error.GetType().Name + " " + error.Message + " " + error.StackTrace); }
    }
    /// <summary>最小断言。</summary>
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    /// <summary>仅隔离独立夹具不存在的 Unity 启动日志。</summary>
    private static bool SkipEnvironment() { return false; }
    /// <summary>在真实兼容前缀之后记录参数；独立进程只跳过需要地图和 Unity 的生成主体。</summary>
    private static bool ObserveSpawn(object __0, object __2) { observedProfile = __0; observedTexture = __2; return false; }
    /// <summary>只隔离两种运行库的语言排版差异，不替换目录与攻击判断。</summary>
    private static bool SkipFormatting(object[] __args, ref TaggedString __result) { __result = (TaggedString)__args[0]; return false; }
}
