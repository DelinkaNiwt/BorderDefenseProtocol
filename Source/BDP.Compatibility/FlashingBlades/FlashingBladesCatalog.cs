using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP.Core.Chips;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>功能自身声明的完整状态；不包含制造包装形态。</summary>
    internal sealed class FlashingBladesState
    {
        /// <summary>形态；无声明时为空。</summary>
        internal ChipExpressionModeConfig Mode;
        /// <summary>该形态的姿态；无声明时为空。</summary>
        internal ChipExpressionStanceConfig Stance;
    }

    /// <summary>配置页一个共享设置项；默认类别可随实际功能来源不同。</summary>
    internal sealed class FlashingBladesEntry
    {
        /// <summary>持久化配置键。</summary>
        internal string Key;
        /// <summary>玩家可见名称。</summary>
        internal string Label;
        /// <summary>所属定义模组名称。</summary>
        internal string ModName;
        /// <summary>所属定义模组包标识。</summary>
        internal string PackageId;
        /// <summary>原始功能/构型定义名，便于搜索。</summary>
        internal string DefinitionName;
        /// <summary>各功能的自动类别，构型基础设置可由多个功能共享。</summary>
        internal readonly Dictionary<string, object> Categories = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>各有效来源近战表面的范围参数；只读展示，不用于外部扩距。</summary>
        internal readonly SortedSet<float> Ranges = new SortedSet<float>();
        /// <summary>原配置页使用的虚拟行。</summary>
        internal object ForeignEntry;
        /// <summary>完整行搜索文字。</summary>
        internal string SearchText { get { return (Label + " " + DefinitionName + " " + ModName + " " + PackageId).ToLowerInvariant(); } }
    }

    /// <summary>只读目录投影；复用正式解释器及构型覆盖，不写入全局物品库。</summary>
    internal sealed class FlashingBladesCatalog
    {
        /// <summary>正式契约解释入口。</summary>
        private static readonly MethodInfo Interpret = typeof(ChipExpressionConfig).Assembly.GetType("BDP.Core.Expressions.ChipExpressionContractInterpreter", true)
            .GetMethod("ResolveUncached", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(ChipExpressionConfig), typeof(string), typeof(string) }, null);
        /// <summary>外部访问器。</summary>
        private readonly FlashingBladesDiscovery targets;
        /// <summary>完整配置条目。</summary>
        internal readonly Dictionary<string, FlashingBladesEntry> Entries = new Dictionary<string, FlashingBladesEntry>();
        /// <summary>功能原声明，用于运行时形态归一化。</summary>
        private readonly Dictionary<string, ChipExpressionConfig> functions = new Dictionary<string, ChipExpressionConfig>(StringComparer.OrdinalIgnoreCase);
        /// <summary>有效运行状态内，各攻击声明到所属配置项的映射；公共攻击不按继承姿态重复。</summary>
        private readonly Dictionary<string, Dictionary<string, string>> stateEntries = new Dictionary<string, Dictionary<string, string>>();
        /// <summary>定义目录只构建一次，避免外部缓存逐帧增长。</summary>
        private bool initialized;

        /// <summary>绑定已检查的反射入口。</summary>
        internal FlashingBladesCatalog(FlashingBladesDiscovery targets)
        {
            if (Interpret == null || Interpret.ReturnType != typeof(ChipExpressionResolvedContract))
                throw new MissingMethodException("BDP.Core.Expressions.ChipExpressionContractInterpreter", "ResolveUncached");
            this.targets = targets;
        }

        /// <summary>固定分段与转义；实例、角色、语言和左右手不参与身份。</summary>
        internal static string BuildKey(string kind, string definition, string owner, string mode, string stance)
        {
            return "BDP.FB.v1|" + string.Join("|", new[] { kind, definition, owner, mode, stance }
                .Select(value => Uri.EscapeDataString(value ?? string.Empty)));
        }
        /// <summary>只展开真实形态与所属姿态，不生成无意义的交叉组合。</summary>
        internal static IEnumerable<FlashingBladesState> ReadStates(ChipExpressionConfig config)
        {
            if (config == null) yield break;
            if (config.Modes == null || config.Modes.Count == 0) { yield return new FlashingBladesState(); yield break; }
            foreach (ChipExpressionModeConfig mode in config.Modes)
            {
                if (mode == null) continue;
                if (mode.Stances == null || mode.Stances.Count == 0) yield return new FlashingBladesState { Mode = mode };
                else foreach (ChipExpressionStanceConfig stance in mode.Stances)
                    if (stance != null) yield return new FlashingBladesState { Mode = mode, Stance = stance };
            }
        }
        /// <summary>复用正规化契约；显式拒绝不存在的状态，不能回退默认后误列。</summary>
        private static List<ChipExpressionEntryContract> ReadAttacks(ChipExpressionConfig config, string mode, string stance)
        {
            FlashingBladesState state = ReadStates(config).FirstOrDefault(value => Same(value.Mode != null ? value.Mode.ModeKey : null, mode)
                && Same(value.Stance != null ? value.Stance.StanceKey : null, stance));
            if (state == null || Interpret == null) return new List<ChipExpressionEntryContract>();
            ChipExpressionResolvedContract resolved = (ChipExpressionResolvedContract)Interpret.Invoke(null, new object[] { config, mode, stance });
            if (resolved.Validation == null || !resolved.Validation.IsValid || resolved.Contract == null) return new List<ChipExpressionEntryContract>();
            return resolved.Contract.Entries.Where(value => value != null && value.WeaponMode == VerbExpressionModeConfig.Melee
                && (value.EntryType == ChipExpressionEntryType.PrimaryVerb || value.EntryType == ChipExpressionEntryType.SecondaryVerb)
                && value.DeclaredMeleeToolSurfaces != null && value.DeclaredMeleeToolSurfaces.Any(surface => surface != null && surface.Tool != null)).ToList();
        }
        /// <summary>读取有效攻击表面，保留正式解释后的形态/姿态筛选。</summary>
        private static List<MeleeToolSurface> ReadSurfaces(ChipExpressionConfig config, string mode, string stance)
        { return ReadAttacks(config, mode, stance).SelectMany(value => value.DeclaredMeleeToolSurfaces).Where(value => value != null && value.Tool != null).ToList(); }
        /// <summary>有效近战工具与范围必须来自同一份正式解释表面。</summary>
        internal static List<Tool> ReadTools(ChipExpressionConfig config, string mode, string stance)
        { return ReadSurfaces(config, mode, stance).Select(value => value.Tool).ToList(); }
        /// <summary>读取声明范围；原版近战接触判定不能被此数值替代。</summary>
        internal static List<float> ReadRanges(ChipExpressionConfig config, string mode, string stance)
        { return ReadSurfaces(config, mode, stance).Where(value => value.VerbProps != null).Select(value => value.VerbProps.range).Distinct().OrderBy(value => value).ToList(); }
        /// <summary>空键归一化后比较状态。</summary>
        private static bool Same(string first, string second) { return string.Equals(first ?? "", second ?? "", StringComparison.OrdinalIgnoreCase); }

        /// <summary>首次打开页面或近战播放时构建；此时游戏定义已就绪。</summary>
        internal void EnsureInitialized()
        {
            if (initialized) return;
            foreach (Def function in FlashingBladesDiscovery.AllDefs(targets.FunctionType))
            {
                ChipDefinitionConfig config = (ChipDefinitionConfig)targets.Invoke("Clone", function);
                if (config == null || config.Expression == null) continue;
                functions[function.defName] = config.Expression;
                AddStates(function, null, config.Expression);
                foreach (Def form in FlashingBladesDiscovery.AllDefs(targets.FormType))
                {
                    if (!targets.IsCompatible(function, form, config)) continue;
                    ChipDefinitionConfig applied = (ChipDefinitionConfig)targets.Invoke("Clone", function);
                    targets.Invoke("Apply", applied, form);
                    AddStates(function, form, applied.Expression);
                }
            }
            initialized = true;
        }
        /// <summary>登记一个来源的有效近战状态，并缓存外部分类型输入。</summary>
        private void AddStates(Def function, Def form, ChipExpressionConfig config)
        {
            foreach (FlashingBladesState activeState in ReadStates(config))
            {
                string mode = activeState.Mode != null ? activeState.Mode.ModeKey : null;
                string activeStance = activeState.Stance != null ? activeState.Stance.StanceKey : null;
                List<ChipExpressionEntryContract> attacks = ReadAttacks(config, mode, activeStance);
                // 声明在形态公共列表的攻击归形态；姿态只拥有自己追加的近战。
                foreach (var group in attacks.GroupBy(value => activeState.Stance != null
                    && !(activeState.Mode.ActiveEntryIds ?? new List<string>()).Any(id => Same(id, value.Id)) ? activeState.Stance : null))
                {
                    FlashingBladesState state = new FlashingBladesState { Mode = activeState.Mode, Stance = group.Key };
                    string stance = state.Stance != null ? state.Stance.StanceKey : null;
                    List<MeleeToolSurface> surfaces = group.SelectMany(value => value.DeclaredMeleeToolSurfaces).Where(value => value != null && value.Tool != null).ToList();
                    List<Tool> tools = surfaces.Select(value => value.Tool).ToList();
                    if (tools.Count == 0) continue;
                    Def owner = form ?? function;
                    string key = BuildKey(form == null ? "F" : "C", owner.defName, form != null && mode != null ? function.defName : null, mode, stance);
                    FlashingBladesEntry entry;
                    if (!Entries.TryGetValue(key, out entry))
                    {
                        string label = (string)targets.Get(owner, "ResolvedLabel");
                        if (form != null && mode != null) label = "BDP_FB_WithFunction".Translate(label, (string)targets.Get(function, "ResolvedLabel")).ToString();
                        if (state.Mode != null) label = "BDP_FB_WithMode".Translate(label, Label(state.Mode.DisplayLabelKey, state.Mode.DisplayLabel)).ToString();
                        if (state.Stance != null) label = "BDP_FB_WithStance".Translate(label, Label(state.Stance.DisplayLabelKey, state.Stance.DisplayLabel)).ToString();
                        entry = new FlashingBladesEntry { Key = key, Label = label, DefinitionName = owner.defName,
                            ModName = owner.modContentPack != null ? owner.modContentPack.Name : "BorderDefenseProtocol",
                            PackageId = owner.modContentPack != null ? owner.modContentPack.PackageId : "niwt.bdp" };
                        entry.ForeignEntry = Activator.CreateInstance(targets.EntryType);
                        targets.Set(entry.ForeignEntry, "defName", key); targets.Set(entry.ForeignEntry, "label", label);
                        targets.Set(entry.ForeignEntry, "modName", entry.ModName); targets.Set(entry.ForeignEntry, "packageId", entry.PackageId);
                        Entries.Add(key, entry);
                    }
                    // 只作现有分类接口输入，不注册、不生成物品；保持原定义与真实装备归属不变。
                    ThingDef input = new ThingDef { defName = owner.defName, label = (string)targets.Get(owner, "ResolvedLabel"), tools = tools };
                    entry.Categories[function.defName] = targets.Invoke("ClassifyDef", input);
                    entry.Ranges.UnionWith(surfaces.Where(value => value.VerbProps != null).Select(value => value.VerbProps.range));
                    string stateKey = BuildKey(form == null ? "F" : "C", owner.defName, form != null && mode != null ? function.defName : null, mode, activeStance);
                    Dictionary<string, string> bindings;
                    if (!stateEntries.TryGetValue(stateKey, out bindings))
                        stateEntries.Add(stateKey, bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                    foreach (ChipExpressionEntryContract attack in group) bindings[attack.Id] = key;
                }
            }
        }
        /// <summary>已有语言键优先，不把状态机器键作为玩家名称。</summary>
        private static string Label(string key, string fallback) { return !string.IsNullOrEmpty(key) ? key.Translate().ToString() : fallback ?? ""; }

        /// <summary>把制造形态归一化回功能原生状态；构型完整状态优先。</summary>
        internal bool TrySelect(string function, string form, string mode, string stance, out FlashingBladesEntry entry, out object category)
        { return TrySelectAttack(function, form, mode, stance, null, null, out entry, out category); }

        /// <summary>同姿态可同时有公共与专属近战；按本次正式单侧结果识别攻击声明，不能仅凭当前姿态选刀光。</summary>
        internal bool TrySelectAttack(string function, string form, string mode, string stance, string result, string chip,
            out FlashingBladesEntry entry, out object category)
        {
            EnsureInitialized();
            entry = null; category = targets.Unknown;
            ChipExpressionConfig config;
            if (function == null || !functions.TryGetValue(function, out config)) return false;
            if (config.Modes == null || config.Modes.Count == 0) { mode = null; stance = null; }
            else if (Same(mode, function) && config.Modes.Count == 1) mode = config.Modes[0].ModeKey;
            // 核心来源材料键为 chip.ThingID + ":" + declaration.Id，单侧结果原样保留。
            // 只剥离已核对的完整芯片前缀，声明键内的分隔符必须原样保留；不解析组合结果。
            string prefix = chip != null ? chip + ":" : null;
            string attack = prefix != null && result != null && result.StartsWith(prefix, StringComparison.Ordinal) ? result.Substring(prefix.Length) : null;
            if (form == null || !FindStateEntry(BuildKey("C", form, mode != null ? function : null, mode, stance), attack, out entry))
                FindStateEntry(BuildKey("F", function, null, mode, stance), attack, out entry);
            return entry != null && entry.Categories.TryGetValue(function, out category);
        }
        /// <summary>有正式攻击键时必须精确匹配；无键时只接受唯一归属，不把公共近战误配给姿态。</summary>
        private bool FindStateEntry(string stateKey, string attack, out FlashingBladesEntry entry)
        {
            entry = null;
            Dictionary<string, string> bindings;
            if (!stateEntries.TryGetValue(stateKey, out bindings)) return false;
            string key;
            if (attack != null) { if (!bindings.TryGetValue(attack, out key)) return false; }
            else
            {
                string[] keys = bindings.Values.Distinct().ToArray();
                if (keys.Length != 1) return false;
                key = keys[0];
            }
            return Entries.TryGetValue(key, out entry);
        }
        /// <summary>固定自动类别或明确显示随实际来源变化。</summary>
        internal string AutomaticLabel(FlashingBladesEntry entry)
        {
            object[] categories = entry.Categories.Values.Distinct().ToArray();
            return categories.Length == 1 ? targets.CategoryLabel(categories[0]) : "BDP_FB_AutoBySource".Translate().ToString();
        }
    }
}
