using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Assembly;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.External;
using Verse;
using RimWorldApparel = RimWorld.Apparel;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 使用临时服装副本捕获个人模板的 Pawn 预览。
    /// </summary>
    public sealed class CombatBodyAppearancePreviewService
    {
        /// <summary>按指定 Pawn 当前个人档案捕获一次预览，不改变账本数据。</summary>
        public CombatBodyAppearancePreviewState Capture(
            Pawn pawn,
            CombatBodyAppearanceProfile profile)
        {
            CombatBodyFrontAppearancePlan plan = ResolvePlan(pawn, profile);
            int templateId = profile != null
                ? profile.SelectedTemplateId
                : CombatBodyAppearanceProfile.NoTemplateId;
            CombatBodyFrontMode mode = plan != null
                ? plan.Mode
                : CombatBodyFrontMode.MirrorOriginal;
            return CapturePlan(pawn, plan, templateId, mode);
        }

        /// <summary>按 Core 外观计划捕获预览。</summary>
        public CombatBodyAppearancePreviewState CapturePlan(
            Pawn pawn,
            CombatBodyFrontAppearancePlan plan,
            int templateId,
            CombatBodyFrontMode mode)
        {
            if (pawn == null)
            {
                return new CombatBodyAppearancePreviewState(templateId, mode, null);
            }

            if (plan == null || plan.Mode == CombatBodyFrontMode.MirrorOriginal)
            {
                return new CombatBodyAppearancePreviewState(
                    templateId,
                    CombatBodyFrontMode.MirrorOriginal,
                    AssemblyPawnPortraitCapture.Capture(pawn));
            }

            List<PreviewApparelRecord> originalApparel = CaptureOriginalApparel(pawn);
            List<RimWorldApparel> temporaryApparel = new List<RimWorldApparel>();
            AssemblyPawnPortraitSnapshot portrait = null;
            try
            {
                RemoveOriginalApparel(pawn, originalApparel);
                CreateTemporaryApparel(pawn, plan, temporaryApparel);
                portrait = AssemblyPawnPortraitCapture.Capture(pawn);
                return new CombatBodyAppearancePreviewState(
                    templateId,
                    CombatBodyFrontMode.Preset,
                    portrait);
            }
            catch (Exception ex)
            {
                Log.Error("[BDP] 战斗体个人服装预览失败。\n" + ex);
                portrait?.DestroyResources();
                return new CombatBodyAppearancePreviewState(
                    templateId,
                    CombatBodyFrontMode.Preset,
                    null);
            }
            finally
            {
                RemoveTemporaryApparel(pawn, temporaryApparel);
                RestoreOriginalApparel(pawn, originalApparel);
            }
        }

        /// <summary>将当前档案解析为预览所需计划；无档案时使用镜像。</summary>
        private static CombatBodyFrontAppearancePlan ResolvePlan(
            Pawn pawn,
            CombatBodyAppearanceProfile profile)
        {
            if (profile != null
                && CombatBodyAppearanceTemplateResolver.TryResolve(
                    pawn,
                    profile,
                    out CombatBodyFrontAppearancePlan plan))
            {
                return plan;
            }

            return new CombatBodyFrontAppearancePlan
            {
                Mode = CombatBodyFrontMode.MirrorOriginal,
                IsDynamic = true,
                IsValid = true,
                Entries = new List<CombatBodyFrontAppearanceEntry>()
            };
        }

        /// <summary>保存当前原身服装及其恢复所需的锁定/强制状态。</summary>
        private static List<PreviewApparelRecord> CaptureOriginalApparel(Pawn pawn)
        {
            return pawn?.apparel?.WornApparel
                .Select(apparel => new PreviewApparelRecord
                {
                    Apparel = apparel,
                    WasLocked = pawn.apparel.IsLocked(apparel),
                    WasForced = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false
                })
                .ToList()
                ?? new List<PreviewApparelRecord>();
        }

        /// <summary>从 Pawn 服装栏移除原服装，为临时副本腾出穿着位置。</summary>
        private static void RemoveOriginalApparel(
            Pawn pawn,
            List<PreviewApparelRecord> originalApparel)
        {
            for (int index = 0; index < originalApparel.Count; index++)
            {
                pawn.apparel.Remove(originalApparel[index].Apparel);
            }
        }

        /// <summary>使用 Core 工厂建立并短暂穿上模板服装副本。</summary>
        private static void CreateTemporaryApparel(
            Pawn pawn,
            CombatBodyFrontAppearancePlan plan,
            List<RimWorldApparel> temporaryApparel)
        {
            if (plan?.Entries == null)
            {
                return;
            }

            for (int index = 0; index < plan.Entries.Count; index++)
            {
                RimWorldApparel apparel = CombatBodyFrontApparelFactory.Create(plan.Entries[index]);
                if (apparel == null)
                {
                    continue;
                }

                pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: false);
                if (pawn.apparel.WornApparel.Contains(apparel))
                {
                    temporaryApparel.Add(apparel);
                }
                else if (!apparel.Destroyed)
                {
                    apparel.Destroy();
                }
            }
        }

        /// <summary>移除并销毁临时服装副本，不触碰模板或原实体服装。</summary>
        private static void RemoveTemporaryApparel(
            Pawn pawn,
            List<RimWorldApparel> temporaryApparel)
        {
            for (int index = temporaryApparel.Count - 1; index >= 0; index--)
            {
                RimWorldApparel apparel = temporaryApparel[index];
                if (apparel == null || apparel.Destroyed)
                {
                    continue;
                }

                if (pawn?.apparel != null && pawn.apparel.WornApparel.Contains(apparel))
                {
                    pawn.apparel.Remove(apparel);
                }

                if (!apparel.Destroyed)
                {
                    apparel.Destroy();
                }
            }
        }

        /// <summary>恢复原身服装及其锁定/强制状态。</summary>
        private static void RestoreOriginalApparel(
            Pawn pawn,
            List<PreviewApparelRecord> originalApparel)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            for (int index = originalApparel.Count - 1; index >= 0; index--)
            {
                PreviewApparelRecord record = originalApparel[index];
                if (record?.Apparel == null || record.Apparel.Destroyed)
                {
                    continue;
                }

                if (!pawn.apparel.WornApparel.Contains(record.Apparel))
                {
                    pawn.apparel.Wear(
                        record.Apparel,
                        dropReplacedApparel: false,
                        locked: record.WasLocked);
                }

                if (record.WasForced && pawn.outfits?.forcedHandler != null)
                {
                    pawn.outfits.forcedHandler.SetForced(record.Apparel, true);
                }
            }
        }

        /// <summary>一件原身服装及其预览前状态。</summary>
        private sealed class PreviewApparelRecord
        {
            /// <summary>原始服装实体。</summary>
            public RimWorldApparel Apparel;

            /// <summary>原始锁定状态。</summary>
            public bool WasLocked;

            /// <summary>原始强制穿着状态。</summary>
            public bool WasForced;
        }
    }
}
