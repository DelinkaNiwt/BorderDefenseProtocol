using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld.IO;
using Verse;

/// <summary>验证正式出场配置的可组合时序，不在离线环境伪造地图或材质。</summary>
internal static class GateArrivalTests
{
    /// <summary>当前游戏和模组依赖目录。</summary>
    private static string[] paths;

    /// <summary>加载真实业务程序集并执行契约场景。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); Console.WriteLine("黑洞门时序与重定向契约通过。"); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>只使用本机实际安装的依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string file = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(file)) return Assembly.LoadFrom(file);
        }
        return null;
    }

    /// <summary>同时开门和依次开门均可选择本门就绪或整波就绪后释放。</summary>
    private static void Run()
    {
        Assembly content = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll"));
        Type settingsType = content.GetType("BDP.Content.Factions.GateArrival.GateArrivalSettings");
        Check(settingsType != null, "缺少可配置的黑洞门入场契约");
        MethodInfo gateCountFor = settingsType.GetMethod("GateCountFor");
        Check(gateCountFor != null, "缺少按袭击人数动态计算门数的方法");
        object gateSettings = Activator.CreateInstance(settingsType);
        Check((int)settingsType.GetField("pawnsPerGate").GetValue(gateSettings) == 4, "每门目标人数默认值应为 4");
        Check((int)settingsType.GetField("maxGateCount").GetValue(gateSettings) == 6, "最大门数默认值应为 6");
        Set(gateSettings, "pawnsPerGate", 4);
        Set(gateSettings, "maxGateCount", 6);
        Check((int)gateCountFor.Invoke(gateSettings, new object[] { 0 }) == 0, "没有袭击人员时不应创建门");
        Check((int)gateCountFor.Invoke(gateSettings, new object[] { 1 }) == 1, "一名袭击者至少需要一扇门");
        Check((int)gateCountFor.Invoke(gateSettings, new object[] { 6 }) == 2, "6 名袭击者按每门 4 人应生成 2 扇门");
        Check((int)gateCountFor.Invoke(gateSettings, new object[] { 25 }) == 6, "门数应受配置的最大门数限制");
        FieldInfo closeDelayField = settingsType.GetField("closeDelayTicks");
        Check(closeDelayField != null, "缺少最后一人落地后的关门延时配置");
        Check((int)closeDelayField.GetValue(gateSettings) == 60, "关门延时默认值应为 60 刻");
        Type schedule = content.GetType("BDP.Content.Factions.GateArrival.GateArrivalSchedule", true);
        MethodInfo closeReadyTick = schedule.GetMethod("CloseReadyTick");
        Check(closeReadyTick != null, "缺少关门就绪时刻计算");
        Set(gateSettings, "closeDelayTicks", 90);
        Set(gateSettings, "minimumOpenTicks", 0);
        Check((int)closeReadyTick.Invoke(null, new object[] { 100, 160, 200, true, gateSettings }) == 290,
            "整波统一关门应从全波最后一人落地后计时");
        Check((int)closeReadyTick.Invoke(null, new object[] { 100, 160, 200, false, gateSettings }) == 250,
            "分别关门应从本门最后一人落地后计时");
        Set(gateSettings, "minimumOpenTicks", 300);
        Check((int)closeReadyTick.Invoke(null, new object[] { 100, 160, 200, false, gateSettings }) == 400,
            "最低开门时长较长时应优先满足最低展示时长");
        Set(gateSettings, "minimumOpenTicks", 0);
        Check((int)closeReadyTick.Invoke(null, new object[] { 100, -1, -1, false, gateSettings }) == 190,
            "没有成功出兵记录的空门应以开启时间作为延时起点");
        Set(gateSettings, "closeDelayTicks", -10);
        object snapshot = settingsType.GetMethod("Snapshot").Invoke(gateSettings, null);
        Check((int)closeDelayField.GetValue(snapshot) == 0, "负数关门延时应在每波快照中收敛为 0");
        CheckArrivalModeSettings();
        CheckRandomOpeningWindow(content, settingsType, schedule);
        CheckOpeningProbability(content, settingsType, schedule);
        CheckMaintainedEffect(settingsType, schedule);
        CheckGateInducer(content);
        object settings = Activator.CreateInstance(settingsType);
        Set(settings, "openDelayTicks", 60);
        Set(settings, "openingTicks", 12);
        Check(CallInt(schedule, "OpenTick", 100, 2, settings) == 160, "同时开门必须共用开启时刻");
        Check(CallInt(schedule, "ReleaseReadyTick", 160, settings) == 172, "开门动画结束前不能释放人物");
        Set(settings, "releaseDelayTicks", 45);
        Check(CallInt(schedule, "ReleaseReadyTick", 160, settings) == 205, "首批出兵等待应从门开始显现时计时");
        Set(settings, "releaseDelayTicks", 0);
        Set(settings, "gateOpenIntervalTicks", 30);
        Check(CallInt(schedule, "OpenTick", 100, 2, settings) == 220, "依次开门间隔应独立于出兵间隔");
        Check(CallInt(schedule, "ReleaseReadyTick", 160, settings) == 172, "本门就绪模式允许首门先出兵");
        Set(settings, "releaseDelayTicks", 300);
        Check(CallInt(schedule, "ReleaseReadyTick", 160, settings) == 460, "首门应在开始显现 300 刻后出兵");
        Check(CallInt(schedule, "ReleaseReadyTick", 220, settings) == 520, "后开门应在自身显现 300 刻后出兵");
        MethodInfo releaseReadyTick = schedule.GetMethod("ReleaseReadyTick");
        Check(releaseReadyTick != null, "实际放兵判断必须复用首批出场时刻计算");
        Check((int)releaseReadyTick.Invoke(null, new object[] { 160, settings }) == 460,
            "控制器实际就绪时刻必须与从门显现起算的延时一致");
        Set(settings, "waitForAllGates", true);
        Check(CallInt(schedule, "ReleaseReadyTick", 220, settings) == 520, "统一出兵应从最后一扇门实际显现后计时");
        Set(settings, "waitForAllGates", false);
        Set(settings, "openingTicks", 360);
        Check(CallInt(schedule, "ReleaseReadyTick", 160, settings) == 520, "开门动画更长时不得早于动画结束出兵");
        Check((int)releaseReadyTick.Invoke(null, new object[] { 160, settings }) == 520,
            "实际放兵判断必须尊重更长的开门动画");
        Set(settings, "openingTicks", 12);
        Set(settings, "releaseDelayTicks", 0);
        Set(settings, "releaseAllAtOnce", true);
        Check((int)schedule.GetMethod("BatchCount").Invoke(null, new object[] { 9, settings }) == 9, "全量出兵必须释放全部待出场成员");
        Set(settings, "releaseAllAtOnce", false);
        Set(settings, "batchSize", 2);
        Check((int)schedule.GetMethod("BatchCount").Invoke(null, new object[] { 9, settings }) == 2, "分批出兵必须尊重每批人数");
        float early = Age(schedule, 180, 160, -1, settings);
        float late = Age(schedule, 100000, 160, -1, settings);
        Check(Math.Abs(early - late) < 0.0001f, "门维持期间不能因实际经过时间进入消退");
        Check(Age(schedule, 300, 160, 280, settings) > late, "关门必须恢复消退阶段");
        CheckLandingPatterns(content);
        CheckPerGateHolderOwnership(content);
        Type requestType = content.GetType("BDP.Content.Factions.GateArrival.GateRedirectRequest", true);
        Type cellType = Assembly.Load("Assembly-CSharp").GetType("Verse.IntVec3", true);
        object original = Activator.CreateInstance(cellType, new object[] { 1, 0, 1 });
        object first = Activator.CreateInstance(cellType, new object[] { 2, 0, 2 });
        object second = Activator.CreateInstance(cellType, new object[] { 3, 0, 3 });
        object request = Activator.CreateInstance(requestType, new object[] { original });
        MethodInfo propose = requestType.GetMethod("Propose");
        propose.Invoke(request, new object[] { first, 10, "B" });
        propose.Invoke(request, new object[] { second, 10, "A" });
        Check(requestType.GetProperty("Destination").GetValue(request, null).Equals(second), "同优先级诱导必须按稳定标识排序");
        propose.Invoke(request, new object[] { first, 20, "Z" });
        Check(requestType.GetProperty("Destination").GetValue(request, null).Equals(first), "高优先级诱导必须优先");
        CheckLanguages();
    }

    /// <summary>验证整波窗口而非相邻门累加，并确认堵塞后的出兵以实际开门为准。</summary>
    private static void CheckRandomOpeningWindow(Assembly content, Type settingsType, Type schedule)
    {
        FieldInfo rangeField = settingsType.GetField("gateOpenRandomOffsetTicks");
        Check(rangeField != null, "缺少可配置的随机开门窗口");
        object settings = Activator.CreateInstance(settingsType);
        rangeField.SetValue(settings, new IntRange(0, 90));
        Check(CallInt(schedule, "OpenTickWithOffset", 100, 0, 90, settings) == 160,
            "首门必须锚定窗口起点，不能额外随机延后");
        Check(CallInt(schedule, "OpenTickWithOffset", 100, 1, 90, settings) == 250,
            "后续门可以使用窗口上界90刻");
        Check(CallInt(schedule, "OpenTickWithOffset", 100, 5, 90, settings) == 250,
            "第六扇门不能累加五次随机间隔");
        Check(CallInt(schedule, "OpenTickWithOffset", 100, 5, 0, settings) == 160,
            "随机窗口必须包含同时开启的0刻偏移");
        rangeField.SetValue(settings, new IntRange(0, 0));
        Check(CallInt(schedule, "OpenTickWithOffset", 100, 5, 90, settings) == 160,
            "关闭随机窗口后必须恢复同时开门");
        Set(settings, "gateOpenIntervalTicks", 30);
        rangeField.SetValue(settings, new IntRange(0, 90));
        Check(CallInt(schedule, "OpenTickWithOffset", 100, 2, 45, settings) == 265,
            "随机窗口不得移除原有固定依次开门能力");
        Set(settings, "releaseDelayTicks", 300);
        Check(CallInt(schedule, "ReleaseReadyTick", 500, settings) == 800,
            "即使预定开门较早，阻塞延迟后仍须从实际开门等300刻");
        rangeField.SetValue(settings, new IntRange(90, -10));
        object snapshot = settingsType.GetMethod("Snapshot").Invoke(settings, null);
        IntRange sanitized = (IntRange)rangeField.GetValue(snapshot);
        Check(sanitized.min == 0 && sanitized.max == 90, "随机范围反序或负数必须在快照中规范化");
        Type wave = content.GetType("BDP.Content.Factions.GateArrival.Thing_GateArrival", true);
        Check(wave.GetProperty("IsHostileRaid") != null, "门必须公开发起方传入的敌方袭击身份");
    }

    /// <summary>用固定随机种子核查零延迟质量、递减权重和窗口缩放，不依赖地图伪对象。</summary>
    private static void CheckOpeningProbability(Assembly content, Type settingsType, Type schedule)
    {
        FieldInfo zeroChance = settingsType.GetField("gateOpenZeroProbability");
        FieldInfo curveField = settingsType.GetField("gateOpenDelayWeightCurve");
        MethodInfo sample = schedule.GetMethod("SampleOpeningOffset");
        Check(zeroChance != null && curveField != null && sample != null, "缺少零延迟概率与可配置递减曲线");
        object settings = Activator.CreateInstance(settingsType);
        Set(settings, "gateOpenRandomOffsetTicks", new IntRange(0, 90));
        Check((float)zeroChance.GetValue(settings) == 0.4f, "零延迟概率默认应为40%");
        Rand.PushState(83741);
        try
        {
            int zero = 0, early = 0, middle = 0, late = 0;
            for (int i = 0; i < 10000; i++)
            {
                int delay = (int)sample.Invoke(null, new[] { settings });
                Check(delay >= 0 && delay <= 90, "抽样延迟越出配置窗口");
                if (delay == 0) zero++;
                else if (delay <= 30) early++;
                else if (delay <= 60) middle++;
                else late++;
            }
            Check(zero > 3700 && zero < 4300, "零延迟抽样比例应约为40%");
            Check(early > middle && middle > late && early > late * 2,
                "正延迟概率必须随时间递减，不能仍为均匀分布");
            Console.WriteLine("开门抽样10000次：零延迟={0}，正延迟前/中/后三段={1}/{2}/{3}。", zero, early, middle, late);
            Set(settings, "gateOpenZeroProbability", 1f);
            for (int i = 0; i < 20; i++) Check((int)sample.Invoke(null, new[] { settings }) == 0, "100%概率应全部零延迟");
            Set(settings, "gateOpenZeroProbability", 0f);
            Set(settings, "gateOpenRandomOffsetTicks", new IntRange(0, 180));
            bool beyondOldWindow = false;
            for (int i = 0; i < 100; i++)
            {
                int delay = (int)sample.Invoke(null, new[] { settings });
                Check(delay > 0 && delay <= 180, "零概率应只抽正延迟，且上界由新配置决定");
                beyondOldWindow |= delay > 90;
            }
            Check(beyondOldWindow, "曲线不能把上界写死在原来的90刻");
            Set(settings, "gateOpenZeroProbability", 1f);
            Set(settings, "gateOpenRandomOffsetTicks", new IntRange(7, 7));
            Check((int)sample.Invoke(null, new[] { settings }) == 7, "窗口明确不含0时应遵守其正数下界");
            Set(settings, "gateOpenRandomOffsetTicks", new IntRange(0, 0));
            Check((int)sample.Invoke(null, new[] { settings }) == 0, "同时开门配置必须保持0延迟");
            Set(settings, "gateOpenZeroProbability", 0f);
            Set(settings, "gateOpenRandomOffsetTicks", new IntRange(0, 20));
            curveField.SetValue(settings, new SimpleCurve { new CurvePoint(0f, 0f), new CurvePoint(1f, 0f) });
            for (int i = 0; i < 20; i++)
            {
                int delay = (int)sample.Invoke(null, new[] { settings });
                Check(delay > 0 && delay <= 20, "全零权重应安全回退正区间，不能报错或绕过零概率配置");
            }
        }
        finally { Rand.PopState(); }
        object snapshot = settingsType.GetMethod("Snapshot").Invoke(settings, null);
        var sourceCurve = (SimpleCurve)curveField.GetValue(settings);
        var copiedCurve = (SimpleCurve)curveField.GetValue(snapshot);
        Check(!ReferenceEquals(sourceCurve, copiedCurve), "每波曲线快照必须与定义隔离");
        float before = copiedCurve.Points[0].y;
        sourceCurve.Points[0] = new CurvePoint(0f, 99f);
        Check(copiedCurve.Points[0].y == before, "修改定义曲线不能改变已启动波次");
        Type request = content.GetType("BDP.Content.Factions.GateArrival.GateRedirectRequest", true);
        Check(request.GetProperty("Map") != null, "初始规划事件必须显式提供地图，不能要求波已经生成");
    }

    /// <summary>维持视觉覆盖等待及出兵阶段，展开前/关门后不生成，空配置可整体关闭。</summary>
    private static void CheckMaintainedEffect(Type settingsType, Type schedule)
    {
        FieldInfo effectField = settingsType.GetField("maintainEffecter");
        MethodInfo active = schedule.GetMethod("ShouldMaintainEffect");
        Check(effectField != null && active != null, "缺少可配置的维持期效果与阶段判断");
        object settings = Activator.CreateInstance(settingsType);
        effectField.SetValue(settings, new EffecterDef());
        Set(settings, "openingTicks", 12);
        Set(settings, "releaseDelayTicks", 300);
        Check(!(bool)active.Invoke(null, new object[] { 100, -1, -1, settings }), "未开门不能发射电弧");
        Check(!(bool)active.Invoke(null, new object[] { 111, 100, -1, settings }), "展开动画结束前不能发射维持电弧");
        Check((bool)active.Invoke(null, new object[] { 112, 100, -1, settings }), "门展开结束就应能发射，不必等出兵");
        Check((bool)active.Invoke(null, new object[] { 300, 100, -1, settings }), "等待出兵期间必须保持电弧");
        Check((bool)active.Invoke(null, new object[] { 500, 100, -1, settings }), "出兵开始后仍应保持电弧");
        Check(!(bool)active.Invoke(null, new object[] { 500, 100, 500, settings }), "开始关门同刻必须停止发射");
        object snapshot = settingsType.GetMethod("Snapshot").Invoke(settings, null);
        Check(ReferenceEquals(effectField.GetValue(settings), effectField.GetValue(snapshot)), "效果定义按原版Def共享，波次保存定义引用");
        effectField.SetValue(settings, null);
        Check(!(bool)active.Invoke(null, new object[] { 500, 100, -1, settings }), "空配置必须整体关闭维持特效");

        string root = Directory.GetParent(paths[2]).Parent.FullName;
        var xml = new XmlDocument();
        xml.Load(Path.Combine(root, "1.6", "Content", "Defs", "EffecterDef", "GateArrival.xml"));
        XmlNode children = xml.SelectSingleNode("/Defs/EffecterDef[defName='BDP_GateMaintainedElectricity']/children");
        Check(children != null && children.ChildNodes.OfType<XmlElement>().Count() == 1,
            "维持期只包含一个随机电火花发射器，不能重播完整折跃爆发");
        XmlNode sprayer = children.SelectSingleNode("li");
        Check(NodeText(sprayer.SelectSingleNode("subEffecterClass")) == typeof(SubEffecter_SprayerChance).FullName
            && NodeText(sprayer.SelectSingleNode("fleckDef")) == "ElectricalSpark", "必须复用原版随机发射器与折跃电火花");
        Check(NodeText(sprayer.SelectSingleNode("burstCount")) == "1~2"
            && NodeText(sprayer.SelectSingleNode("chancePeriodTicks")) == "12"
            && NodeText(sprayer.SelectSingleNode("positionRadiusMin")) == "0.8"
            && NodeText(sprayer.SelectSingleNode("positionRadius")) == "1.3", "电弧应少量、间歇、沿边缘生成");
    }

    /// <summary>默认模式不能吸引普通任务门；切换后覆盖全部门，建筑与文案参数保持可装载。</summary>
    private static void CheckGateInducer(Assembly content)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type modeType = content.GetType("BDP.Content.Factions.GateArrival.GateInductionMode", true);
        Check(Enum.GetNames(modeType).Length == 2, "诱导模式只应有敌方袭击和全部门，不能增加关闭模式");
        Type compType = content.GetType("BDP.Content.Factions.GateArrival.CompGateInducer", true);
        object comp = Activator.CreateInstance(compType);
        Type waveType = content.GetType("BDP.Content.Factions.GateArrival.Thing_GateArrival", true);
        object wave = Activator.CreateInstance(waveType);
        MethodInfo accepts = compType.GetMethod("Accepts");
        Check(!(bool)accepts.Invoke(comp, new[] { wave }), "默认模式不能诱导未声明为袭击的任务门");
        waveType.GetField("isHostileRaid", flags).SetValue(wave, true);
        Check((bool)accepts.Invoke(comp, new[] { wave }), "默认模式必须接受敌方袭击门");
        waveType.GetField("isHostileRaid", flags).SetValue(wave, false);
        compType.GetField("mode", flags).SetValue(comp, Enum.Parse(modeType, "AllGates"));
        Check((bool)accepts.Invoke(comp, new[] { wave }), "全部门模式必须接受非袭击门");
        Check(!(bool)accepts.Invoke(comp, new object[] { null }), "没有门波次时不能执行诱导");
        Type properties = content.GetType("BDP.Content.Factions.GateArrival.CompProperties_GateInducer", true);
        object props = Activator.CreateInstance(properties);
        Check((float)properties.GetField("radius").GetValue(props) == 10f, "诱导半径默认应为10格且可配置");
        Check((float)properties.GetField("successChance").GetValue(props) == 0.9f, "单门诱导成功率默认应为90%");
        Set(comp, "props", props);
        PropertyInfo chance = compType.GetProperty("SuccessChance");
        Check((float)chance.GetValue(comp, null) == 0.9f, "组件必须读取建筑定义中的成功率");
        compType.GetMethod("SetDebugForcedOperation").Invoke(comp, new object[] { true });
        Check((float)chance.GetValue(comp, null) == 0.9f, "强制运作不能把诱导成功率覆盖为100%");
        Set(props, "successChance", 0f);
        Check((float)chance.GetValue(comp, null) == 0f, "零成功率配置必须生效");
        Set(props, "successChance", 1f);
        Check((float)chance.GetValue(comp, null) == 1f, "100%成功率配置必须生效");

        string root = Directory.GetParent(paths[2]).Parent.FullName;
        var xml = new XmlDocument();
        xml.Load(Path.Combine(root, "1.6", "Content", "Defs", "ThingDef", "Buildings", "GateInducer.xml"));
        XmlNode def = xml.SelectSingleNode("/Defs/ThingDef[defName='BDP_GateInducer']");
        Check(def != null && NodeText(def.SelectSingleNode("designationCategory")) == "Biotech",
            "诱导装置必须出现在生物科技建造菜单");
        Check(NodeText(def.SelectSingleNode("size")) == "(1,1)"
            && NodeText(def.SelectSingleNode("costList/Steel")) == "40"
            && NodeText(def.SelectSingleNode("costList/ComponentIndustrial")) == "1",
            "首版尺寸与造价应参考轨道贸易信标");
        Check(NodeText(def.SelectSingleNode("comps/li/compClass")) == "CompPowerTrader"
            && NodeText(def.SelectSingleNode("comps/li/basePowerConsumption")) == "40"
            && def.SelectSingleNode("comps/li[@Class='CompProperties_Flickable']") != null,
            "诱导建筑必须使用原版40W电力与手动开关");
        Check(NodeText(def.SelectSingleNode("comps/li[@Class='" + properties.FullName + "']/radius")) == "10",
            "定义必须显式配置10格诱导范围");
        Check(NodeText(def.SelectSingleNode("comps/li[@Class='" + properties.FullName + "']/successChance")) == "0.9",
            "建筑定义必须显式配置90%成功率");
        var injected = new XmlDocument();
        injected.Load(Path.Combine(root, "Languages", "ChineseSimplified (简体中文)", "DefInjected", "ThingDef", "GateInducer.xml"));
        Check(NodeText(injected.SelectSingleNode("/LanguageData/BDP_GateInducer.label")) == "门诱导装置",
            "建筑名称必须由简体中文语言包提供");
    }

    /// <summary>空间突袭是抵达方式，与边缘步行同级；落点样式随抵达方式配置。</summary>
    private static void CheckLandingPatterns(Assembly content)
    {
        Type patternType = content.GetType("BDP.Content.Factions.GateArrival.GateArrivalLandingPattern", true);
        Check(Enum.GetNames(patternType).Contains("Edge") && Enum.GetNames(patternType).Contains("MultiSide")
            && Enum.GetNames(patternType).Contains("BreachLine"), "落点样式必须包含旧边缘、多边分散和火力线内侧");
        Type extensionType = content.GetType("BDP.Content.Factions.GateArrival.GateArrivalModeExtension", true);
        object extension = Activator.CreateInstance(extensionType);
        Check(extensionType.GetField("landingPattern").GetValue(extension).ToString() == "Edge",
            "抵达方式扩展缺省必须保留旧边缘样式");
        extensionType.GetField("landingPattern").SetValue(extension, Enum.Parse(patternType, "BreachLine"));
        Check(extensionType.GetField("landingPattern").GetValue(extension).ToString() == "BreachLine",
            "抵达方式扩展必须能显式选择火力线内侧样式");

        string modRoot = Directory.GetParent(paths[2]).Parent.FullName;
        string defs = Path.Combine(modRoot, "1.6", "Content", "Defs");
        CheckSpaceRaidArrivalMode(modRoot, defs);
        var configuredStrategies = new List<string>();
        foreach (string file in Directory.GetFiles(defs, "*.xml", SearchOption.AllDirectories))
        {
            var xml = new XmlDocument();
            xml.Load(file);
            foreach (XmlNode strategy in xml.SelectNodes("//RaidStrategyDef[defName='BDP_SpaceRaid']"))
                configuredStrategies.Add(strategy.InnerText);
        }
        Check(configuredStrategies.Count == 0,
            "空间突袭必须是抵达方式，不能新增袭击策略");
        Type landingType = content.GetType("BDP.Content.Factions.GateArrival.GateArrivalLanding", true);
        MethodInfo sideForGate = landingType.GetMethod("SideForGate");
        int[] threeSides = Enumerable.Range(0, 3)
            .Select(index => (int)sideForGate.Invoke(null, new object[] { index, 3, 0 })).ToArray();
        Check(threeSides.Distinct().Count() == 3, "三扇多边门应分配到三条不同的地图边");
        Check((int)sideForGate.Invoke(null, new object[] { 1, 2, 0 }) == 2,
            "两扇多边门应尽量分配到相对的地图边");
        MethodInfo collect = landingType.GetMethod("CollectBreachLineCandidates");
        Check(collect != null, "避险网格落点应提供可独立验证的内侧候选计算");
        const int width = 11;
        const int height = 11;
        var danger = new byte[width * height];
        danger[5 * width + 8] = 45;
        Type cellType = Assembly.Load("Assembly-CSharp").GetType("Verse.IntVec3", true);
        object center = Activator.CreateInstance(cellType, new object[] { 5, 0, 5 });
        var candidates = (System.Collections.IEnumerable)collect.Invoke(null, new object[] { width, height, center, danger, 45, 2 });
        List<string> cells = candidates.Cast<object>().Select(cell => cell.ToString()).ToList();
        Check(cells.Contains("(7, 0, 5)"), "火力高危格朝殖民地中心一侧的相邻安全格应成为候选；实际候选：" + string.Join("、", cells));
        Check(!cells.Contains("(9, 0, 5)"), "火力高危格外侧不能误判为突入后的落点");
        Check(!cells.Contains("(5, 0, 5)"), "核心缓冲距离内不能生成落点");
        var buffered = (System.Collections.IEnumerable)collect.Invoke(null, new object[] { width, height, center, danger, 45, 3 });
        Check(!buffered.Cast<object>().Any(), "增大中心缓冲后，过近的内侧候选必须全部被过滤");
        var noThreat = (System.Collections.IEnumerable)collect.Invoke(null, new object[] { width, height, center, new byte[width * height], 45, 2 });
        Check(!noThreat.Cast<object>().Any(), "地图没有危险格时不能伪造突防落点");
    }

    /// <summary>确认空间突袭是近界专属抵达方式，并在该方式上启用越过防线落点。</summary>
    private static void CheckSpaceRaidArrivalMode(string modRoot, string defs)
    {
        string modeFile = Path.Combine(defs, "PawnsArrivalModeDef", "GateArrival.xml");
        Check(File.Exists(modeFile), "缺少黑洞门抵达方式定义");
        var modeXml = new XmlDocument();
        modeXml.Load(modeFile);
        XmlNode edgeMode = modeXml.SelectSingleNode("/Defs/PawnsArrivalModeDef[defName='BDP_NearRealmGate']");
        XmlNode spaceMode = modeXml.SelectSingleNode("/Defs/PawnsArrivalModeDef[defName='BDP_SpaceRaid']");
        Check(edgeMode != null && spaceMode != null, "必须同时保留旧边缘门和空间突袭门两种抵达方式");
        Check(NodeText(edgeMode.SelectSingleNode("workerClass")) == "BDP.Content.Factions.GateArrival.PawnsArrivalModeWorker_Gate",
            "原有边缘门抵达必须使用黑洞门抵达工作类");
        Check(NodeText(spaceMode.SelectSingleNode("workerClass")) == "BDP.Content.Factions.GateArrival.PawnsArrivalModeWorker_Gate",
            "空间突袭必须复用黑洞门抵达工作类");
        Check(NodeText(edgeMode.SelectSingleNode("modExtensions/li[@Class='BDP.Content.Factions.GateArrival.GateArrivalModeExtension']/landingPattern")) == "Edge",
            "原有近界门抵达必须保留边缘落点");
        Check(NodeText(spaceMode.SelectSingleNode("modExtensions/li[@Class='BDP.Content.Factions.GateArrival.GateArrivalModeExtension']/landingPattern")) == "BreachLine",
            "空间突袭抵达方式必须启用越过防线落点");
        foreach (XmlNode mode in new[] { edgeMode, spaceMode })
        {
            XmlNode weight = mode.SelectSingleNode("selectionWeightCurve/points/li");
            Check(weight != null && weight.InnerText.Trim() == "(0, 1)",
                "两种黑洞门抵达方式应使用相同的基础抽选权重 1");
        }

        string patchFile = Path.Combine(modRoot, "1.6", "Content", "Patches", "NearRealmGateArrivalModes.xml");
        var patchXml = new XmlDocument();
        patchXml.Load(patchFile);
        XmlNodeList patchOperations = patchXml.SelectNodes("/Patch/Operation");
        Check(patchOperations.Count == 2
            && patchXml.SelectNodes("/Patch/Operation/value/li[text()='BDP_NearRealmGate' or text()='BDP_SpaceRaid']").Count == 2,
            "原版立即进攻候选列表必须包含两种黑洞门抵达方式");
        var vanillaStrategy = new XmlDocument();
        vanillaStrategy.LoadXml("<Defs><RaidStrategyDef><defName>ImmediateAttack</defName><arriveModes><li>EdgeWalkIn</li></arriveModes></RaidStrategyDef><RaidStrategyDef><defName>Siege</defName><arriveModes /></RaidStrategyDef></Defs>");
        foreach (XmlNode operation in patchOperations)
            Check(vanillaStrategy.SelectNodes(NodeText(operation.SelectSingleNode("xpath"))).Count == 1,
                "黑洞门抵达列表补丁必须只命中原版立即进攻策略");
        string factionFile = Path.Combine(defs, "FactionDef", "PlaceholderFactions.xml");
        var factionXml = new XmlDocument();
        factionXml.Load(factionFile);
        XmlNode whitelist = factionXml.SelectSingleNode("/Defs/FactionDef[defName='BDP_Faction_NearRealm']/arrivalModeWhitelist");
        Check(whitelist != null && whitelist.SelectNodes("li").Count == 2
            && whitelist.SelectSingleNode("li[text()='BDP_NearRealmGate']") != null
            && whitelist.SelectSingleNode("li[text()='BDP_SpaceRaid']") != null,
            "近界派系白名单必须只开放两种黑洞门抵达方式");

        string languageFile = Path.Combine(modRoot, "Languages", "ChineseSimplified (简体中文)",
            "DefInjected", "PawnsArrivalModeDef", "GateArrival.xml");
        var languageXml = new XmlDocument();
        languageXml.Load(languageFile);
        Check(!string.IsNullOrWhiteSpace(NodeText(languageXml.SelectSingleNode("/LanguageData/BDP_SpaceRaid.textEnemy"))),
            "空间突袭抵达方式必须提供简体中文出场说明");
    }

    /// <summary>确认正式抵达模式 XML 实际提供动态门数和关门延时配置。</summary>
    private static void CheckArrivalModeSettings()
    {
        string modRoot = Directory.GetParent(paths[2]).Parent.FullName;
        string file = Path.Combine(modRoot, "1.6", "Content", "Defs", "PawnsArrivalModeDef", "GateArrival.xml");
        var xml = new XmlDocument();
        xml.Load(file);
        XmlNode settings = xml.SelectSingleNode("/Defs/PawnsArrivalModeDef[defName='BDP_NearRealmGate']/modExtensions/li/settings");
        Check(settings != null, "正式抵达模式缺少黑洞门设置");
        XmlNode pawnsPerGate = settings.SelectSingleNode("pawnsPerGate");
        XmlNode maxGateCount = settings.SelectSingleNode("maxGateCount");
        XmlNode closeDelayTicks = settings.SelectSingleNode("closeDelayTicks");
        foreach (string defName in new[] { "BDP_NearRealmGate", "BDP_SpaceRaid" })
        {
            XmlNode modeSettings = xml.SelectSingleNode("/Defs/PawnsArrivalModeDef[defName='" + defName
                + "']/modExtensions/li[@Class='BDP.Content.Factions.GateArrival.GateArrivalModeExtension']/settings");
            Check(modeSettings != null, defName + " 必须显式配置黑洞门时序");
            Check(NodeText(modeSettings.SelectSingleNode("releaseDelayTicks")) == "300",
                defName + " 必须配置门开始显现后 300 刻出兵");
            Check(NodeText(modeSettings.SelectSingleNode("closeDelayTicks")) == "60",
                defName + " 必须配置最后一人落地后 60 刻关门");
            Check(NodeText(modeSettings.SelectSingleNode("gateOpenRandomOffsetTicks")) == "0~90",
                defName + " 必须配置0到90刻的随机开门窗口");
            Check(NodeText(modeSettings.SelectSingleNode("gateOpenZeroProbability")) == "0.4"
                && modeSettings.SelectNodes("gateOpenDelayWeightCurve/points/li").Count >= 2,
                defName + " 必须显式配置40%零延迟概率及相对权重曲线");
            Check(NodeText(modeSettings.SelectSingleNode("maintainEffecter")) == "BDP_GateMaintainedElectricity",
                defName + " 必须显式启用可配置的维持期电弧");
        }
        Check(pawnsPerGate != null && pawnsPerGate.InnerText == "4", "正式配置每门目标人数不是 4");
        Check(maxGateCount != null && maxGateCount.InnerText == "6", "正式配置最大门数不是 6");
        Check(closeDelayTicks != null && closeDelayTicks.InnerText == "60", "正式配置关门延时不是 60 刻");
    }

    /// <summary>每扇门以独立原版持有容器提供自身位置，并移除箭头映射补丁。</summary>
    private static void CheckPerGateHolderOwnership(Assembly content)
    {
        Type holderType = content.GetType("BDP.Content.Factions.GateArrival.Thing_GateArrivalPawnHolder", false);
        Check(holderType != null, "缺少每门独立的原版人物持有物");
        Type pointType = content.GetType("BDP.Content.Factions.GateArrival.GateArrivalPoint", true);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object firstHolder = Activator.CreateInstance(holderType, true);
        object secondHolder = Activator.CreateInstance(holderType, true);
        MethodInfo getThings = holderType.GetMethod("GetDirectlyHeldThings", flags);
        Check(getThings != null, "门持有物必须接入原版 IThingHolder 遍历");
        object firstContainer = getThings.Invoke(firstHolder, null);
        object secondContainer = getThings.Invoke(secondHolder, null);
        Check(firstContainer != null && secondContainer != null && !ReferenceEquals(firstContainer, secondContainer),
            "不同门必须拥有互相独立的原版人物容器");

        FieldInfo holderField = pointType.GetField("pawnHolder", flags);
        Check(holderField != null, "门状态必须保存所属人物持有物引用");
        object point = Activator.CreateInstance(pointType, true);
        holderField.SetValue(point, firstHolder);
        Check(ReferenceEquals(holderField.GetValue(point), firstHolder), "门状态必须稳定引用自己的持有物");

        Check(content.GetType("BDP.Content.Factions.GateArrival.Patch_TargetHighlighter_GateArrival", false) == null,
            "门位箭头必须复用原版位置解析，不保留专用绘制补丁");
        Type waveType = content.GetType("BDP.Content.Factions.GateArrival.Thing_GateArrival", true);
        Check(waveType.GetField("innerContainer", flags) == null,
            "波控制器不能再通过单一容器持有所有门的人物");

        string modRoot = Directory.GetParent(paths[2]).Parent.FullName;
        string definitionsPath = Path.Combine(modRoot, "1.6", "Content", "Defs", "ThingDef", "GateArrival.xml");
        var definitions = new XmlDocument();
        definitions.Load(definitionsPath);
        XmlNode holderDefinition = definitions.SelectSingleNode("/Defs/ThingDef[defName='BDP_GateArrivalPawnHolder']");
        Check(holderDefinition != null
            && NodeText(holderDefinition.SelectSingleNode("thingClass")) == holderType.FullName
            && NodeText(holderDefinition.SelectSingleNode("category")) == "Ethereal"
            && NodeText(holderDefinition.SelectSingleNode("drawerType")) == "None",
            "门持有物 ThingDef 必须使用正确类别及不可见绘制设置");
        string languagePath = Path.Combine(modRoot, "Languages", "ChineseSimplified (简体中文)",
            "DefInjected", "ThingDef", "GateArrival.xml");
        var language = new XmlDocument();
        language.Load(languagePath);
        Check(!string.IsNullOrWhiteSpace(NodeText(language.SelectSingleNode(
            "/LanguageData/BDP_GateArrivalPawnHolder.label"))), "门持有物名称必须来自简体中文语言包");
    }

    /// <summary>按原版加载次序与文件去重规则读取正式、开发文案，防止同名开发文件遮蔽正式语言键。</summary>
    private static void CheckLanguages()
    {
        string root = Directory.GetParent(paths[2]).Parent.FullName;
        var folders = new XmlDocument();
        folders.Load(Path.Combine(root, "LoadFolders.xml"));
        var roots = new List<string>();
        foreach (XmlNode item in folders.SelectNodes("/loadFolders/v1.6/li"))
            roots.Add(Path.GetFullPath(Path.Combine(root, item.InnerText.Trim('/'))));
        roots.Reverse();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var language = (LoadedLanguage)FormatterServices.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "ChineseSimplified (简体中文)";
        language.loadErrors = new List<string>();
        foreach (string fieldName in new[] { "tmpAlreadyLoadedFiles", "keyedReplacements" })
        {
            FieldInfo field = typeof(LoadedLanguage).GetField(fieldName, flags);
            field.SetValue(language, Activator.CreateInstance(field.FieldType));
        }
        typeof(LoadedLanguage).GetField("dataIsLoaded", flags).SetValue(language, true);
        var mod = (ModContentPack)FormatterServices.GetUninitializedObject(typeof(ModContentPack));
        MethodInfo load = typeof(LoadedLanguage).GetMethod("LoadFromFile_Keyed", flags);
        foreach (string directory in roots)
        {
            string keyed = Path.Combine(directory, "Languages", language.folderName, "Keyed");
            if (!Directory.Exists(keyed)) continue;
            foreach (string file in Directory.GetFiles(keyed, "*Gate*.xml"))
            {
                if (!language.TryRegisterFileIfNew(new Tuple<VirtualDirectory, ModContentPack, string>(null, mod, directory), file)) continue;
                VirtualFile source = AbstractFilesystem.GetDirectory(directory).GetFile(file.Substring(directory.Length + 1));
                load.Invoke(language, new object[] { source, File.ReadAllText(file) });
            }
        }
        Check(language.loadErrors.Count == 0, "黑洞门语言文件读取失败");
        foreach (string key in new[] { "BDP_GateArrival_InvalidGroups", "BDP_GateArrival_HookError", "BDP_Dev_Gate_Menu", "BDP_Dev_Gate_Point",
            "BDP_GateInducer_ModeCommand", "BDP_GateInducer_ModeDescription", "BDP_GateInducer_HostileRaids",
            "BDP_GateInducer_AllGates", "BDP_GateInducer_Running", "BDP_GateInducer_ForcedRunning", "BDP_GateInducer_Stopped", "BDP_GateInducer_SuccessChance",
            "BDP_Dev_GateInducer_ForceOperation", "BDP_Dev_GateInducer_ForceOperationDescription", "BDP_Dev_Gate_RandomWindow", "BDP_Dev_Gate_Timing" })
        {
            TaggedString value;
            Check(language.TryGetTextFromKey(key, out value), "原版未加载黑洞门语言键：" + key);
        }
    }

    /// <summary>设置实际公开配置字段。</summary>
    private static void Set(object target, string field, object value) { target.GetType().GetField(field).SetValue(target, value); }

    /// <summary>以旧版 C# 编译器兼容的方式读取 XML 节点文本。</summary>
    private static string NodeText(XmlNode node) { return node == null ? null : node.InnerText; }

    /// <summary>调用实际整数时序计算。</summary>
    private static int CallInt(Type type, string method, params object[] args) { return (int)type.GetMethod(method).Invoke(null, args); }

    /// <summary>调用材质使用的实际虚拟年龄。</summary>
    private static float Age(Type type, int now, int opened, int closed, object settings)
    { return (float)type.GetMethod("VirtualAge").Invoke(null, new object[] { now, opened, closed, settings }); }

    /// <summary>失败时指出玩家可见行为的违约点。</summary>
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
