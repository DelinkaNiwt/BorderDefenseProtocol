using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>一次性校验目标成员；不引入外部或 Content 的编译依赖。</summary>
    internal sealed class FlashingBladesDiscovery
    {
        /// <summary>外部程序集。</summary>
        internal readonly Assembly External;
        /// <summary>功能定义类型。</summary>
        internal readonly Type FunctionType;
        /// <summary>构型定义类型。</summary>
        internal readonly Type FormType;
        /// <summary>外部配置窗口。</summary>
        internal readonly Type WindowType;
        /// <summary>外部目录项类型。</summary>
        internal readonly Type EntryType;
        /// <summary>外部分类结果类型。</summary>
        internal readonly Type ResultType;
        /// <summary>外部未知类别。</summary>
        internal readonly object Unknown;
        /// <summary>必需方法，按本适配器内部名称索引。</summary>
        internal readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();
        /// <summary>已校验的数据成员。</summary>
        private readonly Dictionary<Tuple<Type, string>, MemberInfo> members = new Dictionary<Tuple<Type, string>, MemberInfo>();

        /// <summary>发现全部必需成员后才允许安装补丁。</summary>
        internal FlashingBladesDiscovery(Assembly[] assemblies)
        {
            External = assemblies.FirstOrDefault(value => value.GetType("DaoGuangJianYing.DGJY_ContextBuilder") != null);
            if (External == null) throw new InvalidOperationException("BDP_FB_NotPresent");
            Assembly content = assemblies.FirstOrDefault(value => value.GetName().Name == "BDP.Content");
            if (content == null) throw new MissingMemberException("BDP.Content");
            FunctionType = RequireType(content, "BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef");
            FormType = RequireType(content, "BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef");
            WindowType = ForeignType("DGJY_Window_WeaponReachOverrides");
            EntryType = ForeignType("DGJY_MeleeWeaponEntry");
            ResultType = ForeignType("DGJY_ClassificationResult");
            Type category = ForeignType("DGJY_MeleeVfxCategory");
            if (!category.IsEnum) throw new MissingMemberException(category.FullName);
            foreach (string name in new[] { "Unknown", "Slash", "Stab", "Blunt", "Punch", "Claw" }) Enum.Parse(category, name);
            Unknown = Enum.Parse(category, "Unknown");
            Type context = ForeignType("DGJY_MeleeVfxContext");
            Type profile = ForeignType("DGJY_MeleeVfxProfileDef");
            Add("Build", ForeignType("DGJY_ContextBuilder"), "Build", typeof(Verb_MeleeAttack), typeof(LocalTargetInfo));
            Add("Classify", ForeignType("DGJY_MeleeVfxClassifier"), "ClassifyDetailed", context);
            Add("ClassifyDef", ForeignType("DGJY_MeleeVfxClassifier"), "ClassifyWeaponDef", typeof(ThingDef));
            Add("ClassifyTool", ForeignType("DGJY_MeleeVfxClassifier"), "ClassifyTool", typeof(Tool), typeof(bool), typeof(string).MakeByRefType());
            Add("ClassifyText", ForeignType("DGJY_MeleeVfxClassifier"), "ClassifyDefName", typeof(string));
            Add("Spawn", ForeignType("DGJY_MeleeVfxSpawner"), "Spawn", profile, context, category);
            Add("Scale", ForeignType("DGJY_WeaponVisualDatabase"), "GetEffectScaleFactor", profile, context);
            Add("UsesSize", ForeignType("DGJY_WeaponVisualDatabase"), "UsesSourceSizeScale", category);
            Add("TextureFleck", ForeignType("DGJY_WeaponVfxTextureOverride"), "GetPrimaryVisualFleck", category);
            Add("Reach", ForeignType("DGJY_ExtendedMeleeReachUtility"), "TryGetExtendedRangeData", typeof(Verb), ForeignType("DGJY_WeaponReachData").MakeByRefType(), typeof(float).MakeByRefType());
            Add("Filter", WindowType, "BuildFilteredEntries");
            Add("Row", WindowType, "DrawWeaponRow", typeof(UnityEngine.Rect), EntryType, typeof(int));
            Add("ModMenu", WindowType, "OpenModFilterMenu");
            Add("VfxMenu", WindowType, "OpenVfxMenu", EntryType);
            Type overrides = ForeignType("DGJY_WeaponVfxOverrideDatabase");
            Add("GetOverride", overrides, "TryGetOverride", typeof(string), category.MakeByRefType());
            Add("IsOverridden", overrides, "IsOverridden", typeof(string));
            Add("DisplayCategory", overrides, "DisplayCategory", category);
            Add("SetOverride", overrides, "SetOverride", typeof(string), category);
            Add("ClearOverride", overrides, "ClearOverride", typeof(string));
            Add("ClearAll", overrides, "ClearAll");
            Add("Clone", RequireType(content, "BDP.Content.Assembly.ChipManufacturing.Resolution.ChipConfigurationMergeService"), "CloneSingle", FunctionType);
            Add("Apply", RequireType(content, "BDP.Content.Assembly.ChipManufacturing.Resolution.ChipArmamentFormApplicationService"), "Apply", typeof(ChipDefinitionConfig), FormType);
            Add("Validate", RequireType(content, "BDP.Content.Assembly.ChipManufacturing.Resolution.ChipCombinationCompatibilityService"), "Validate", typeof(ChipCategoryDef), RequireType(content, "BDP.Content.Assembly.ChipManufacturing.Defs.ChipProfessionDef"), typeof(IList<>).MakeGenericType(FunctionType), FormType);
            RequireReturn("Build", context); RequireReturn("Classify", ResultType);
            RequireReturn("ClassifyDef", category); RequireReturn("ClassifyTool", category);
            RequireReturn("Spawn", typeof(void)); RequireReturn("Reach", typeof(bool));
            RequireReturn("Scale", typeof(float)); RequireReturn("UsesSize", typeof(bool));
            RequireReturn("TextureFleck", typeof(FleckDef));
            RequireReturn("GetOverride", typeof(bool)); RequireReturn("Clone", typeof(ChipDefinitionConfig));
            RequireReturn("ClassifyText", category); RequireReturn("IsOverridden", typeof(bool));
            RequireReturn("DisplayCategory", typeof(string)); RequireReturn("Apply", typeof(void));
            foreach (string key in new[] { "Filter", "Row", "ModMenu", "VfxMenu", "SetOverride", "ClearOverride", "ClearAll" }) RequireReturn(key, typeof(void));
            RequireMembers(WindowType, "filteredEntries", "searchText", "selectedPackageId", "selectedModLabel", "scrollPosition", "showOverriddenOnly");
            RequireMembers(EntryType, "def", "defName", "label", "modName", "packageId", "SearchText");
            RequireMembers(ResultType, "category", "source", "detail");
            RequireMembers(context, "valid", "tool", "damageDef");
            RequireMemberType(profile, "effecterDef", typeof(EffecterDef));
            RequireMemberType(profile, "extraTrailEffecterDef", typeof(EffecterDef));
            RequireMembers(ForeignType("DGJY_MeleeWeaponCatalog"), "AllModFilters");
            RequireMembers(ForeignType("DGJY_ModFilterItem"), "packageId", "label", "count");
            RequireMembers(FunctionType, "config", "ResolvedLabel");
            RequireMembers(FormType, "ResolvedLabel");
            RequireMemberType(WindowType, "filteredEntries", typeof(List<>).MakeGenericType(EntryType));
            foreach (string name in new[] { "searchText", "selectedPackageId", "selectedModLabel" }) RequireMemberType(WindowType, name, typeof(string));
            RequireMemberType(WindowType, "scrollPosition", typeof(UnityEngine.Vector2));
            RequireMemberType(WindowType, "showOverriddenOnly", typeof(bool));
            foreach (string name in new[] { "defName", "label", "modName", "packageId", "SearchText" }) RequireMemberType(EntryType, name, typeof(string));
            RequireMemberType(ResultType, "category", category);
            RequireMemberType(ResultType, "source", typeof(string)); RequireMemberType(ResultType, "detail", typeof(string));
            RequireMemberType(context, "valid", typeof(bool)); RequireMemberType(context, "tool", typeof(Tool)); RequireMemberType(context, "damageDef", typeof(DamageDef));
            RequireMemberType(profile, "category", category);
            RequireMemberType(ForeignType("DGJY_Mod"), "Settings", ForeignType("DGJY_Settings"));
            RequireMemberType(ForeignType("DGJY_Settings"), "dynamicWeaponScale", typeof(bool));
            RequireMemberType(ForeignType("DGJY_Settings"), "globalScaleMultiplier", typeof(float));
            RequireMemberType(context, "debugScaleFactor", typeof(float)); RequireMemberType(context, "debugScaleSummary", typeof(string));
        }

        /// <summary>只允许预期返回类型，避免静默适配错误版本。</summary>
        private void RequireReturn(string key, Type expected)
        {
            if (Methods[key].ReturnType != expected) throw new MissingMethodException(Methods[key].ToString());
        }
        /// <summary>解析一个必需类型。</summary>
        private static Type RequireType(Assembly assembly, string name) { return assembly.GetType(name, true); }
        /// <summary>解析当前外部命名空间的类型。</summary>
        internal Type ForeignType(string name) { return RequireType(External, "DaoGuangJianYing." + name); }
        /// <summary>按完整参数签名发现一个方法。</summary>
        private void Add(string key, Type type, string name, params Type[] parameters)
        {
            MethodInfo method = AccessTools.Method(type, name, parameters);
            if (method == null || method.IsStatic != (type != WindowType)) throw new MissingMethodException(type.FullName, name);
            Methods.Add(key, method);
        }
        /// <summary>预先校验并缓存全部必需字段或属性。</summary>
        private void RequireMembers(Type type, params string[] names)
        {
            foreach (string name in names) Member(type, name);
        }
        /// <summary>数据布局也是兼容签名的一部分，安装前拒绝类型不符的版本。</summary>
        private void RequireMemberType(Type type, string name, Type expected)
        {
            MemberInfo member = Member(type, name);
            FieldInfo field = member as FieldInfo;
            Type actual = field != null ? field.FieldType : ((PropertyInfo)member).PropertyType;
            if (actual != expected) throw new MissingMemberException(type.FullName, name);
        }
        /// <summary>缓存反射访问器；不逐帧搜索类型。</summary>
        private MemberInfo Member(Type type, string name)
        {
            Tuple<Type, string> key = Tuple.Create(type, name);
            MemberInfo member;
            if (!members.TryGetValue(key, out member))
            {
                member = (MemberInfo)AccessTools.Field(type, name) ?? AccessTools.Property(type, name);
                if (member == null) throw new MissingMemberException(type.FullName, name);
                members.Add(key, member);
            }
            return member;
        }
        /// <summary>读取已缓存的数据成员；Type 参数用于静态成员。</summary>
        internal object Get(object target, string name)
        {
            Type type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;
            MemberInfo member = Member(type, name);
            FieldInfo field = member as FieldInfo;
            return field != null ? field.GetValue(instance) : ((PropertyInfo)member).GetValue(instance, null);
        }
        /// <summary>写入当前适配对象，不修改定义原件。</summary>
        internal void Set(object target, string name, object value)
        {
            MemberInfo member = Member(target.GetType(), name);
            FieldInfo field = member as FieldInfo;
            if (field != null) field.SetValue(target, value);
            else ((PropertyInfo)member).SetValue(target, value, null);
        }
        /// <summary>调用已经检查签名的方法。</summary>
        internal object Invoke(string key, params object[] args) { return Methods[key].Invoke(null, args); }
        /// <summary>读取指定业务类型的全部已加载定义。</summary>
        internal static IEnumerable<Def> AllDefs(Type type)
        {
            return ((IEnumerable)typeof(DefDatabase<>).MakeGenericType(type).GetProperty("AllDefsListForReading").GetValue(null, null)).Cast<Def>();
        }
        /// <summary>复用现有制造规则，存在一个合法职业即可列出潜在构型。</summary>
        internal bool IsCompatible(Def function, Def form, ChipDefinitionConfig config)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(FunctionType));
            list.Add(function);
            foreach (Def profession in AllDefs(Methods["Validate"].GetParameters()[1].ParameterType))
            {
                IList errors = (IList)Invoke("Validate", config.Profile != null ? config.Profile.Category : null, profession, list, form);
                if (errors.Count == 0) return true;
            }
            return false;
        }
        /// <summary>按原设置数据库读取字符串覆盖。</summary>
        internal bool TryOverride(string key, out object category)
        {
            object[] args = { key, Unknown };
            bool found = key != null && (bool)Methods["GetOverride"].Invoke(null, args);
            category = found ? args[1] : Unknown;
            return found;
        }
        /// <summary>外部类别的原有本地化名称。</summary>
        internal string CategoryLabel(object category) { return (string)Invoke("DisplayCategory", category); }
        /// <summary>直接复用当前工具回退分类，不为失配攻击创建无限定义缓存。</summary>
        internal object ToolCategory(Tool tool, DamageDef damage)
        {
            object category = Methods["ClassifyTool"].Invoke(null, new object[] { tool, false, null });
            return Equals(category, Unknown) && damage != null ? Invoke("ClassifyText", damage.defName) : category;
        }
    }
}
