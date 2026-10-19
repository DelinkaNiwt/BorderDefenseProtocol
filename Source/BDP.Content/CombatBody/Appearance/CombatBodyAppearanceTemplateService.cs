using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorldApparel = RimWorld.Apparel;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 负责把 Pawn 当前穿着服装一次性上传为个人模板。
    /// </summary>
    public sealed class CombatBodyAppearanceTemplateService
    {
        /// <summary>
        /// 读取当前穿着、转入库存、建立模板并自动选中新模板。
        /// </summary>
        public CombatBodyAppearanceUploadResult UploadCurrentApparel(Pawn pawn)
        {
            if (pawn == null)
            {
                return Failure(CombatBodyAppearanceUploadResult.MissingPawnMessageKey);
            }

            GameComponent_CombatBodyAppearanceTemplates ledger =
                CombatBodyAppearanceLedgerService.ResolveLedger();
            if (ledger == null)
            {
                return Failure(CombatBodyAppearanceUploadResult.LedgerUnavailableMessageKey);
            }

            if (pawn.apparel == null || pawn.inventory?.innerContainer == null)
            {
                return Failure(CombatBodyAppearanceUploadResult.MissingContainerMessageKey);
            }

            List<RimWorldApparel> wornApparel = new List<RimWorldApparel>(pawn.apparel.WornApparel);
            List<UploadedApparelRecord> records = new List<UploadedApparelRecord>();
            CombatBodyAppearanceTemplate template = new CombatBodyAppearanceTemplate();

            CombatBodyAppearanceProfile existingProfile;
            bool hasExistingProfile = ledger.TryGetProfile(pawn, out existingProfile);
            int nextTemplateNumber = hasExistingProfile && existingProfile.NextTemplateId > 0
                ? existingProfile.NextTemplateId
                : 1;
            template.Name =
                "BDP_CombatBodyEditor_DefaultTemplateName".Translate(nextTemplateNumber).ToString();

            for (int index = 0; index < wornApparel.Count; index++)
            {
                RimWorldApparel apparel = wornApparel[index];
                if (!TryCaptureApparel(pawn, apparel, template, records))
                {
                    return Failure(CombatBodyAppearanceUploadResult.CaptureFailedMessageKey);
                }
            }

            if (!TryMoveApparelToInventory(pawn, records))
            {
                return Failure(CombatBodyAppearanceUploadResult.TransferFailedMessageKey);
            }

            bool templateAdded = false;
            CombatBodyAppearanceProfile profile = existingProfile;
            try
            {
                if (profile == null)
                {
                    profile = ledger.GetOrCreateProfile(pawn);
                }

                if (profile == null)
                {
                    RestoreApparel(pawn, records);
                    return Failure(CombatBodyAppearanceUploadResult.CommitFailedMessageKey);
                }

                template.TemplateId = profile.AllocateTemplateId();
                profile.Templates.Add(template);
                templateAdded = true;
                if (!CombatBodyAppearanceLedgerService.SelectTemplate(pawn, template.TemplateId))
                {
                    throw new System.InvalidOperationException();
                }

                return new CombatBodyAppearanceUploadResult
                {
                    Succeeded = true,
                    Template = template
                };
            }
            catch
            {
                if (templateAdded && profile != null)
                {
                    profile.Templates.Remove(template);
                }

                RestoreApparel(pawn, records);
                return Failure(CombatBodyAppearanceUploadResult.CommitFailedMessageKey);
            }
        }

        /// <summary>
        /// 读取一件服装的全部外观字段，不改变 Pawn 当前状态。
        /// </summary>
        private static bool TryCaptureApparel(
            Pawn pawn,
            RimWorldApparel apparel,
            CombatBodyAppearanceTemplate template,
            List<UploadedApparelRecord> records)
        {
            if (pawn == null || apparel == null || apparel.def == null)
            {
                return false;
            }

            CompColorable colorable = apparel.TryGetComp<CompColorable>();
            bool hasColor = colorable != null && colorable.Active;
            template.Entries.Add(new CombatBodyAppearanceTemplateEntry
            {
                ApparelDef = apparel.def,
                StuffDef = apparel.Stuff,
                HasColor = hasColor,
                Color = hasColor ? colorable.Color : UnityEngine.Color.white,
                StyleDef = apparel.StyleDef
            });

            records.Add(new UploadedApparelRecord
            {
                Apparel = apparel,
                WasLocked = pawn.apparel.IsLocked(apparel),
                WasForced = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false
            });
            return true;
        }

        /// <summary>
        /// 将全部原服装移入库存；中途失败时恢复整批服装。
        /// </summary>
        private static bool TryMoveApparelToInventory(
            Pawn pawn,
            List<UploadedApparelRecord> records)
        {
            if (pawn?.apparel == null || pawn.inventory?.innerContainer == null)
            {
                return false;
            }

            for (int index = 0; index < records.Count; index++)
            {
                UploadedApparelRecord record = records[index];
                if (record?.Apparel == null)
                {
                    RestoreApparel(pawn, records);
                    return false;
                }

                pawn.apparel.Remove(record.Apparel);
                if (!pawn.inventory.innerContainer.TryAdd(record.Apparel, false))
                {
                    RestoreApparel(pawn, records);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 回滚已经转入库存或已经从服装栏移除的原实体。
        /// </summary>
        private static void RestoreApparel(
            Pawn pawn,
            List<UploadedApparelRecord> records)
        {
            if (pawn?.apparel == null || records == null)
            {
                return;
            }

            for (int index = records.Count - 1; index >= 0; index--)
            {
                UploadedApparelRecord record = records[index];
                RimWorldApparel apparel = record?.Apparel;
                if (apparel == null || apparel.Destroyed)
                {
                    continue;
                }

                if (apparel.holdingOwner == pawn.inventory?.innerContainer)
                {
                    pawn.inventory.innerContainer.Remove(apparel);
                }

                if (!pawn.apparel.WornApparel.Contains(apparel))
                {
                    pawn.apparel.Wear(
                        apparel,
                        dropReplacedApparel: false,
                        locked: record.WasLocked);
                }

                if (record.WasForced && pawn.outfits?.forcedHandler != null)
                {
                    pawn.outfits.forcedHandler.SetForced(apparel, true);
                }
            }
        }

        /// <summary>
        /// 创建失败结果，调用方只需把语言键交给原版界面显示。
        /// </summary>
        private static CombatBodyAppearanceUploadResult Failure(string messageKey)
        {
            return new CombatBodyAppearanceUploadResult
            {
                Succeeded = false,
                FailureMessageKey = messageKey
            };
        }

        /// <summary>
        /// 一件上传服装的原始归属状态，用于失败回滚。
        /// </summary>
        private sealed class UploadedApparelRecord
        {
            /// <summary>
            /// 当前服装实体。
            /// </summary>
            public RimWorldApparel Apparel;

            /// <summary>
            /// 上传前是否被锁定。
            /// </summary>
            public bool WasLocked;

            /// <summary>
            /// 上传前是否被强制服装策略选中。
            /// </summary>
            public bool WasForced;
        }
    }
}
