using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Information;
using RimWorld;
using Verse;

namespace BDP.Content.Information
{
    /// <summary>按明确 Def 类型解析芯片信息值的原版标签。</summary>
    public static class ChipInformationDefLabelResolverRegistry
    {
        /// <summary>同步保护解析器表，允许其它模组在启动阶段注册。</summary>
        private static readonly object SyncRoot = new object();

        /// <summary>Def 类型程序集限定名到其标签读取函数的映射。</summary>
        private static readonly Dictionary<string, Func<string, string>> Resolvers =
            new Dictionary<string, Func<string, string>>(StringComparer.Ordinal);

        /// <summary>登记当前信息模型实际引用的原版与 BDP Def 类型。</summary>
        static ChipInformationDefLabelResolverRegistry()
        {
            Register<DamageDef>();
            Register<ThingDef>();
            Register<AbilityDef>();
            Register<HediffDef>();
            Register<ChipCategoryDef>();
            Register<ChipTagDef>();
            Register<ChipExclusionGroupDef>();
            Register<ChipProfessionDef>();
        }

        /// <summary>为指定 Def 子类型注册 LabelCap（定义显示名）解析器。</summary>
        public static bool Register<TDef>() where TDef : Def
        {
            string definitionTypeKey = typeof(TDef).AssemblyQualifiedName;
            if (string.IsNullOrWhiteSpace(definitionTypeKey))
            {
                return false;
            }

            return Register(definitionTypeKey, ResolveNamedDef<TDef>);
        }

        /// <summary>为自定义 Def 类型显式注册标签解析器。</summary>
        public static bool Register(
            string definitionTypeKey,
            Func<string, string> labelResolver)
        {
            if (string.IsNullOrWhiteSpace(definitionTypeKey) || labelResolver == null)
            {
                return false;
            }

            lock (SyncRoot)
            {
                if (Resolvers.ContainsKey(definitionTypeKey))
                {
                    return false;
                }

                Resolvers.Add(definitionTypeKey, labelResolver);
                return true;
            }
        }

        /// <summary>按值中保存的 Def 类型和名称读取标签；缺失时回退到 DefName。</summary>
        internal static string Resolve(ChipInformationValue value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string fallback = value.DefinitionName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value.DefinitionTypeKey)
                || string.IsNullOrWhiteSpace(value.DefinitionName))
            {
                return fallback;
            }

            Func<string, string> resolver;
            lock (SyncRoot)
            {
                if (!Resolvers.TryGetValue(value.DefinitionTypeKey, out resolver))
                {
                    return fallback;
                }
            }

            string label = resolver(value.DefinitionName);
            return string.IsNullOrWhiteSpace(label) ? fallback : label;
        }

        /// <summary>用 RimWorld 原版 DefDatabase（定义数据库）查找并读取 LabelCap。</summary>
        private static string ResolveNamedDef<TDef>(string defName) where TDef : Def
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return null;
            }

            TDef definition = DefDatabase<TDef>.GetNamedSilentFail(defName);
            return definition?.LabelCap.ToString();
        }
    }
}
