using System.Collections.Generic;
using BDP.Core.CombatBody;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 战斗体个人服装模板账本的最小业务访问入口。
    /// </summary>
    public static class CombatBodyAppearanceLedgerService
    {
        /// <summary>
        /// 从当前存档解析个人模板 GameComponent。
        /// </summary>
        public static GameComponent_CombatBodyAppearanceTemplates ResolveLedger()
        {
            return Current.Game == null
                ? null
                : Current.Game.GetComponent<GameComponent_CombatBodyAppearanceTemplates>();
        }

        /// <summary>
        /// 尝试读取指定 Pawn 的个人档案。
        /// </summary>
        public static bool TryGetProfile(Pawn pawn, out CombatBodyAppearanceProfile profile)
        {
            profile = null;
            GameComponent_CombatBodyAppearanceTemplates ledger = ResolveLedger();
            return ledger != null && ledger.TryGetProfile(pawn, out profile);
        }

        /// <summary>
        /// 读取或创建指定 Pawn 的个人档案。
        /// </summary>
        public static CombatBodyAppearanceProfile GetOrCreateProfile(Pawn pawn)
        {
            GameComponent_CombatBodyAppearanceTemplates ledger = ResolveLedger();
            return ledger == null ? null : ledger.GetOrCreateProfile(pawn);
        }

        /// <summary>
        /// 向指定 Pawn 的档案添加模板并分配稳定编号。
        /// </summary>
        public static CombatBodyAppearanceTemplate AddTemplate(
            Pawn pawn,
            string name)
        {
            CombatBodyAppearanceProfile profile = GetOrCreateProfile(pawn);
            if (profile == null)
            {
                return null;
            }

            CombatBodyAppearanceTemplate template = new CombatBodyAppearanceTemplate
            {
                TemplateId = profile.AllocateTemplateId(),
                Name = name ?? string.Empty
            };
            profile.Templates.Add(template);
            return template;
        }

        /// <summary>
        /// 选择指定 Pawn 档案中的模板，不自动改变镜像/预设模式。
        /// </summary>
        public static bool SelectTemplate(Pawn pawn, int templateId)
        {
            if (!TryGetProfile(pawn, out CombatBodyAppearanceProfile profile)
                || !profile.TryGetTemplate(templateId, out CombatBodyAppearanceTemplate ignored))
            {
                return false;
            }

            profile.SelectedTemplateId = templateId;
            return true;
        }

        /// <summary>
        /// 重命名指定模板，拒绝空白名称。
        /// </summary>
        public static bool RenameTemplate(Pawn pawn, int templateId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)
                || !TryGetProfile(pawn, out CombatBodyAppearanceProfile profile)
                || !profile.TryGetTemplate(templateId, out CombatBodyAppearanceTemplate template))
            {
                return false;
            }

            template.Name = name.Trim();
            return true;
        }

        /// <summary>
        /// 删除指定模板；删除当前模板后立即回到默认镜像。
        /// </summary>
        public static bool DeleteTemplate(Pawn pawn, int templateId)
        {
            if (!TryGetProfile(pawn, out CombatBodyAppearanceProfile profile)
                || profile.Templates == null)
            {
                return false;
            }

            for (int index = profile.Templates.Count - 1; index >= 0; index--)
            {
                CombatBodyAppearanceTemplate template = profile.Templates[index];
                if (template == null || template.TemplateId != templateId)
                {
                    continue;
                }

                profile.Templates.RemoveAt(index);
                if (profile.SelectedTemplateId == templateId)
                {
                    profile.SelectedTemplateId = CombatBodyAppearanceProfile.NoTemplateId;
                    profile.Mode = CombatBodyFrontMode.MirrorOriginal;
                }
                else if (profile.Mode == CombatBodyFrontMode.Preset
                    && !profile.TryGetTemplate(
                        profile.SelectedTemplateId,
                        out CombatBodyAppearanceTemplate selectedTemplate))
                {
                    profile.SelectedTemplateId = CombatBodyAppearanceProfile.NoTemplateId;
                    profile.Mode = CombatBodyFrontMode.MirrorOriginal;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 设置 Pawn 的个人前台模式；没有可选模板时预设自动回退镜像。
        /// </summary>
        public static bool SetMode(Pawn pawn, CombatBodyFrontMode mode)
        {
            if (!TryGetProfile(pawn, out CombatBodyAppearanceProfile profile))
            {
                return false;
            }

            if (mode == CombatBodyFrontMode.Preset
                && !profile.TryGetTemplate(
                    profile.SelectedTemplateId,
                    out CombatBodyAppearanceTemplate selectedTemplate))
            {
                profile.Mode = CombatBodyFrontMode.MirrorOriginal;
                return false;
            }

            profile.Mode = mode;
            return true;
        }

        /// <summary>
        /// 清理当前仍可确认已经永久销毁的 Pawn 档案。
        /// </summary>
        public static void CleanupDestroyedProfiles()
        {
            GameComponent_CombatBodyAppearanceTemplates ledger = ResolveLedger();
            if (ledger == null)
            {
                return;
            }

            List<Pawn> destroyedPawns = new List<Pawn>();
            foreach (CombatBodyAppearanceProfile profile in ledger.Profiles)
            {
                if (profile != null && profile.Pawn != null && profile.Pawn.Destroyed)
                {
                    destroyedPawns.Add(profile.Pawn);
                }
            }

            for (int index = 0; index < destroyedPawns.Count; index++)
            {
                ledger.RemoveProfile(destroyedPawns[index]);
            }
        }
    }
}
