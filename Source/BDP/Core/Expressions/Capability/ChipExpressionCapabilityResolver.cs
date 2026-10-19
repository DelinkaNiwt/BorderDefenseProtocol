using BDP.Core.AttackExecution;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达能力解析器。
    /// 只依据表达条目本身的攻击事实判断近战、远程和增强能力，不读取芯片分类、职业或名称。
    /// </summary>
    public static class ChipExpressionCapabilityResolver
    {
        /// <summary>
        /// 判断配置条目是否属于主/副 Verb（攻击动作）候选。
        /// Ability、Hediff 和 Passive 不会被当成独立武器。
        /// </summary>
        public static bool IsVerbEntry(ChipExpressionEntryConfig entry)
        {
            return entry != null
                && (entry.EntryType == ChipExpressionEntryTypeConfig.PrimaryVerb
                    || entry.EntryType == ChipExpressionEntryTypeConfig.SecondaryVerb);
        }

        /// <summary>
        /// 判断正规化条目是否属于主/副 Verb 候选。
        /// </summary>
        public static bool IsVerbEntry(ChipExpressionEntryContract entry)
        {
            return entry != null
                && (entry.EntryType == ChipExpressionEntryType.PrimaryVerb
                    || entry.EntryType == ChipExpressionEntryType.SecondaryVerb);
        }

        /// <summary>
        /// 解析配置条目的武器模式。
        /// Verb 条目显式模式优先，其次读取原版近战标记、投射物和 Tool 事实；
        /// Ability 条目只保留作者明确声明的模式，不把普通 Ability 自动远程化。
        /// </summary>
        public static VerbExpressionModeConfig ResolveWeaponMode(ChipExpressionEntryConfig entry)
        {
            if (entry == null)
            {
                return VerbExpressionModeConfig.None;
            }

            if (entry.EntryType == ChipExpressionEntryTypeConfig.Ability)
            {
                return IsExplicitWeaponMode(entry.WeaponMode)
                    ? entry.WeaponMode
                    : VerbExpressionModeConfig.None;
            }

            if (!IsVerbEntry(entry))
            {
                return VerbExpressionModeConfig.None;
            }

            if (IsExplicitWeaponMode(entry.WeaponMode))
            {
                return entry.WeaponMode;
            }

            if (entry.VerbProps != null && entry.VerbProps.IsMeleeAttack)
            {
                return VerbExpressionModeConfig.Melee;
            }

            if (entry.VerbProps != null && entry.VerbProps.defaultProjectile != null)
            {
                return VerbExpressionModeConfig.Ranged;
            }

            // VerbProps 缺省但声明了 Tool 时，保留现有自动合成近战 Verb 的语义。
            if (entry.VerbProps == null && HasDeclaredTool(entry))
            {
                return VerbExpressionModeConfig.Melee;
            }

            return VerbExpressionModeConfig.None;
        }

        /// <summary>
        /// 解析正规化条目的武器模式。
        /// 该入口优先使用正规化结果；显式声明远程模式的 Ability 也必须保留该事实，
        /// 增强模块不会单独产生武器模式。
        /// </summary>
        public static VerbExpressionModeConfig ResolveWeaponMode(ChipExpressionEntryContract entry)
        {
            if (entry == null)
            {
                return VerbExpressionModeConfig.None;
            }

            if (entry.EntryType == ChipExpressionEntryType.Ability)
            {
                return IsExplicitWeaponMode(entry.WeaponMode)
                    ? entry.WeaponMode
                    : VerbExpressionModeConfig.None;
            }

            if (!IsVerbEntry(entry))
            {
                return VerbExpressionModeConfig.None;
            }

            if (IsExplicitWeaponMode(entry.WeaponMode))
            {
                return entry.WeaponMode;
            }

            VerbProperties verbProps = entry.VerbProps
                ?? entry.ResolvedVerbSpec?.SurfaceTemplate;
            if (verbProps != null && verbProps.IsMeleeAttack)
            {
                return VerbExpressionModeConfig.Melee;
            }

            if (entry.ResolvedVerbSpec?.ProjectileDef != null
                || (verbProps != null && verbProps.defaultProjectile != null))
            {
                return VerbExpressionModeConfig.Ranged;
            }

            if (verbProps == null && HasDeclaredTool(entry))
            {
                return VerbExpressionModeConfig.Melee;
            }

            return VerbExpressionModeConfig.None;
        }

        /// <summary>
        /// 判断配置条目是否形成可执行近战表面。
        /// 仅有增强模块、缺少 Tool 或缺少攻击条目的配置都不会通过。
        /// </summary>
        public static bool HasExecutableMeleeSurface(ChipExpressionEntryConfig entry)
        {
            return IsVerbEntry(entry)
                && ResolveWeaponMode(entry) == VerbExpressionModeConfig.Melee
                && HasValidConfigVerb(entry)
                && HasDeclaredTool(entry);
        }

        /// <summary>
        /// 判断正规化条目是否形成可执行近战表面。
        /// </summary>
        public static bool HasExecutableMeleeSurface(ChipExpressionEntryContract entry)
        {
            return IsVerbEntry(entry)
                && ResolveWeaponMode(entry) == VerbExpressionModeConfig.Melee
                && HasValidResolvedVerb(entry)
                && HasDeclaredTool(entry);
        }

        /// <summary>
        /// 判断配置条目是否形成可执行远程表面。
        /// 远程 Verb 必须同时有有效攻击类型和投射物。
        /// </summary>
        public static bool HasExecutableRangedSurface(ChipExpressionEntryConfig entry)
        {
            return IsVerbEntry(entry)
                && ResolveWeaponMode(entry) == VerbExpressionModeConfig.Ranged
                && HasValidConfigVerb(entry)
                && entry.VerbProps != null
                && entry.VerbProps.defaultProjectile != null;
        }

        /// <summary>
        /// 判断正规化条目是否形成可执行远程表面。
        /// </summary>
        public static bool HasExecutableRangedSurface(ChipExpressionEntryContract entry)
        {
            return IsVerbEntry(entry)
                && ResolveWeaponMode(entry) == VerbExpressionModeConfig.Ranged
                && HasValidResolvedVerb(entry)
                && (entry.ResolvedVerbSpec.ProjectileDef != null
                    || (entry.VerbProps != null && entry.VerbProps.defaultProjectile != null));
        }

        /// <summary>
        /// 判断一份正规化契约是否只有远程模块/增强而没有独立可执行武器。
        /// 模块本身是增强来源，不会被提升成独立远程攻击。
        /// </summary>
        public static bool IsAugmentationOnly(ChipExpressionContract contract)
        {
            if (contract == null || contract.Entries == null)
            {
                return false;
            }

            bool hasAugmentation = false;
            for (int index = 0; index < contract.Entries.Count; index++)
            {
                ChipExpressionEntryContract entry = contract.Entries[index];
                if (entry == null)
                {
                    continue;
                }

                if (HasExecutableMeleeSurface(entry) || HasExecutableRangedSurface(entry))
                {
                    return false;
                }

                hasAugmentation = hasAugmentation
                    || (entry.RangedModules != null && entry.RangedModules.Count > 0)
                    || (entry.RangedModuleAugmentations != null
                        && entry.RangedModuleAugmentations.Count > 0);
            }

            return hasAugmentation;
        }

        /// <summary>
        /// 判断配置条目是否至少声明了一把近战 Tool。
        /// </summary>
        private static bool HasDeclaredTool(ChipExpressionEntryConfig entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (entry.Tool != null)
            {
                return true;
            }

            if (entry.tools == null)
            {
                return false;
            }

            for (int index = 0; index < entry.tools.Count; index++)
            {
                if (entry.tools[index] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断正规化条目是否至少保留了一把近战 Tool。
        /// </summary>
        private static bool HasDeclaredTool(ChipExpressionEntryContract entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (entry.Tool != null)
            {
                return true;
            }

            if (entry.DeclaredTools != null)
            {
                for (int index = 0; index < entry.DeclaredTools.Count; index++)
                {
                    if (entry.DeclaredTools[index] != null)
                    {
                        return true;
                    }
                }
            }

            if (entry.DeclaredMeleeToolSurfaces != null)
            {
                for (int index = 0; index < entry.DeclaredMeleeToolSurfaces.Count; index++)
                {
                    if (entry.DeclaredMeleeToolSurfaces[index]?.Tool != null)
                    {
                        return true;
                    }
                }
            }

            return entry.ResolvedVerbSpec?.Tool != null;
        }

        /// <summary>
        /// 判断作者是否明确声明了可识别的武器模式。
        /// </summary>
        private static bool IsExplicitWeaponMode(VerbExpressionModeConfig mode)
        {
            return mode == VerbExpressionModeConfig.Melee
                || mode == VerbExpressionModeConfig.Ranged;
        }

        /// <summary>
        /// 判断配置条目的 VerbProperties 是否足以进入执行表面。
        /// 缺省 VerbProperties 的近战条目允许由 Tool 自动合成。
        /// </summary>
        private static bool HasValidConfigVerb(ChipExpressionEntryConfig entry)
        {
            return entry != null
                && (entry.VerbProps == null
                    || entry.VerbProps.verbClass != null);
        }

        /// <summary>
        /// 判断正规化条目的 Verb 规格是否完整。
        /// </summary>
        private static bool HasValidResolvedVerb(ChipExpressionEntryContract entry)
        {
            return entry != null
                && entry.ResolvedVerbSpec != null
                && entry.ResolvedVerbSpec.VerbClass != null;
        }
    }
}
