using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.External;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 将个人模板档案解析为 Core 可消费的前台外观计划。
    /// </summary>
    public static class CombatBodyAppearanceTemplateResolver
    {
        /// <summary>
        /// 解析指定 Pawn 的个人档案；没有有效预设时安全回到动态镜像。
        /// </summary>
        public static bool TryResolve(
            Pawn pawn,
            CombatBodyAppearanceProfile profile,
            out CombatBodyFrontAppearancePlan plan)
        {
            plan = null;
            if (pawn == null || profile == null)
            {
                return false;
            }

            profile.Normalize();
            if (profile.Mode == CombatBodyFrontMode.MirrorOriginal)
            {
                plan = CreatePlan(CombatBodyFrontMode.MirrorOriginal, new List<CombatBodyFrontAppearanceEntry>());
                return true;
            }

            if (profile.Mode != CombatBodyFrontMode.Preset)
            {
                return FallbackToMirror(
                    pawn,
                    profile,
                    "个人模式值无效，已回退镜像。",
                    out plan);
            }

            if (!profile.TryGetTemplate(
                profile.SelectedTemplateId,
                out CombatBodyAppearanceTemplate template))
            {
                return FallbackToMirror(
                    pawn,
                    profile,
                    "个人预设没有有效选中模板，已回退镜像。",
                    out plan);
            }

            List<CombatBodyFrontAppearanceEntry> entries =
                new List<CombatBodyFrontAppearanceEntry>();
            int sourceEntryCount = template.Entries == null ? 0 : template.Entries.Count;
            for (int index = 0; index < sourceEntryCount; index++)
            {
                CombatBodyAppearanceTemplateEntry sourceEntry = template.Entries[index];
                if (!TryConvertEntry(
                    pawn,
                    template,
                    sourceEntry,
                    index,
                    out CombatBodyFrontAppearanceEntry entry))
                {
                    continue;
                }

                entries.Add(entry);
            }

            if (sourceEntryCount > 0 && entries.Count == 0)
            {
                return FallbackToMirror(
                    pawn,
                    profile,
                    "个人预设中的服装定义全部失效，已回退镜像。",
                    out plan);
            }

            plan = CreatePlan(CombatBodyFrontMode.Preset, entries);
            return true;
        }

        /// <summary>
        /// 将一条 Content 模板记录转换为 Core 中性外观条目。
        /// </summary>
        private static bool TryConvertEntry(
            Pawn pawn,
            CombatBodyAppearanceTemplate template,
            CombatBodyAppearanceTemplateEntry sourceEntry,
            int index,
            out CombatBodyFrontAppearanceEntry entry)
        {
            entry = null;
            if (sourceEntry == null || sourceEntry.ApparelDef == null)
            {
                ReportInvalidEntry(pawn, template, index, "缺少服装定义。");
                return false;
            }

            if (sourceEntry.ApparelDef.MadeFromStuff && sourceEntry.StuffDef == null)
            {
                ReportInvalidEntry(pawn, template, index, "服装需要材料但材料定义缺失。");
                return false;
            }

            if (sourceEntry.StuffDef != null && !sourceEntry.StuffDef.IsStuff)
            {
                ReportInvalidEntry(pawn, template, index, "材料定义不是有效 Stuff。");
                return false;
            }

            entry = new CombatBodyFrontAppearanceEntry
            {
                ApparelDef = sourceEntry.ApparelDef,
                StuffDef = sourceEntry.StuffDef,
                HasColor = sourceEntry.HasColor,
                Color = sourceEntry.Color,
                StyleDef = sourceEntry.StyleDef
            };
            return true;
        }

        /// <summary>
        /// 创建动态外观计划，确保条目集合始终可消费。
        /// </summary>
        private static CombatBodyFrontAppearancePlan CreatePlan(
            CombatBodyFrontMode mode,
            List<CombatBodyFrontAppearanceEntry> entries)
        {
            return new CombatBodyFrontAppearancePlan
            {
                Mode = mode,
                Entries = entries ?? new List<CombatBodyFrontAppearanceEntry>(),
                IsDynamic = true,
                IsValid = true
            };
        }

        /// <summary>
        /// 清除失效选中项并将档案回退为镜像计划。
        /// </summary>
        private static bool FallbackToMirror(
            Pawn pawn,
            CombatBodyAppearanceProfile profile,
            string reason,
            out CombatBodyFrontAppearancePlan plan)
        {
            profile.SelectedTemplateId = CombatBodyAppearanceProfile.NoTemplateId;
            profile.Mode = CombatBodyFrontMode.MirrorOriginal;
            BdpDiagnostics.Once(
                "combatbody.appearance.fallback." + SafePawnId(pawn),
                reason + " pawn=" + SafePawnId(pawn));
            plan = CreatePlan(
                CombatBodyFrontMode.MirrorOriginal,
                new List<CombatBodyFrontAppearanceEntry>());
            return true;
        }

        /// <summary>
        /// 记录单条失效服装条目的可搜索诊断。
        /// </summary>
        private static void ReportInvalidEntry(
            Pawn pawn,
            CombatBodyAppearanceTemplate template,
            int index,
            string reason)
        {
            BdpDiagnostics.Once(
                "combatbody.appearance.invalid_entry."
                + SafePawnId(pawn)
                + "."
                + (template != null ? template.TemplateId.ToString() : "unknown")
                + "."
                + index,
                reason + " pawn=" + SafePawnId(pawn));
        }

        /// <summary>
        /// 安全读取 Pawn 的稳定身份。
        /// </summary>
        private static string SafePawnId(Pawn pawn)
        {
            return pawn != null && !string.IsNullOrWhiteSpace(pawn.ThingID)
                ? pawn.ThingID
                : "unknown";
        }
    }
}
