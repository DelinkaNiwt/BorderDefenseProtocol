using System;
using System.Collections.Generic;
using BDP.Core.CombatBody;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 一个 Pawn 的个人战斗体外观模板档案。
    /// </summary>
    public sealed class CombatBodyAppearanceProfile : IExposable
    {
        /// <summary>
        /// Pawn 的持久引用；跨地图时不依赖当前地图。
        /// </summary>
        public Pawn Pawn;

        /// <summary>
        /// Pawn 的稳定 ThingID，作为引用恢复失败时的身份回退。
        /// </summary>
        public string PawnThingId;

        /// <summary>
        /// 当前 Pawn 的个人前台模式，默认使用镜像原身。
        /// </summary>
        public CombatBodyFrontMode Mode = CombatBodyFrontMode.MirrorOriginal;

        /// <summary>
        /// 当前选中的模板编号；负数表示没有选中模板。
        /// </summary>
        public int SelectedTemplateId = NoTemplateId;

        /// <summary>
        /// 当前 Pawn 的全部个人模板。
        /// </summary>
        public List<CombatBodyAppearanceTemplate> Templates =
            new List<CombatBodyAppearanceTemplate>();

        /// <summary>
        /// 下一个自动分配的模板编号。
        /// </summary>
        public int NextTemplateId = 1;

        /// <summary>
        /// 表示没有选中模板的稳定值。
        /// </summary>
        public const int NoTemplateId = -1;

        /// <summary>
        /// 供原版深度存档和反序列化使用的无参构造函数。
        /// </summary>
        public CombatBodyAppearanceProfile()
        {
        }

        /// <summary>
        /// 为指定 Pawn 建立默认镜像档案。
        /// </summary>
        public CombatBodyAppearanceProfile(Pawn pawn)
        {
            BindPawn(pawn);
        }

        /// <summary>
        /// 绑定当前可用的 Pawn 引用，同时保留稳定身份值。
        /// </summary>
        public void BindPawn(Pawn pawn)
        {
            Pawn = pawn;
            if (pawn != null && !string.IsNullOrWhiteSpace(pawn.ThingID))
            {
                PawnThingId = pawn.ThingID;
            }
        }

        /// <summary>
        /// 判断档案是否属于指定 Pawn。
        /// </summary>
        public bool Matches(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (Pawn == pawn)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(PawnThingId)
                && !string.IsNullOrWhiteSpace(pawn.ThingID)
                && string.Equals(PawnThingId, pawn.ThingID, StringComparison.Ordinal);
        }

        /// <summary>
        /// 按稳定模板编号查找模板。
        /// </summary>
        public bool TryGetTemplate(int templateId, out CombatBodyAppearanceTemplate template)
        {
            template = null;
            if (Templates == null || templateId == NoTemplateId)
            {
                return false;
            }

            for (int index = 0; index < Templates.Count; index++)
            {
                CombatBodyAppearanceTemplate candidate = Templates[index];
                if (candidate != null && candidate.TemplateId == templateId)
                {
                    template = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 分配档案内单调递增且不与现有模板冲突的编号。
        /// </summary>
        public int AllocateTemplateId()
        {
            if (NextTemplateId < 1)
            {
                NextTemplateId = 1;
            }

            while (ContainsTemplateId(NextTemplateId))
            {
                NextTemplateId++;
            }

            int allocatedId = NextTemplateId;
            NextTemplateId++;
            return allocatedId;
        }

        /// <summary>
        /// 规范化读档后的集合和模式，保留合法空模板。
        /// </summary>
        public void Normalize()
        {
            if (Templates == null)
            {
                Templates = new List<CombatBodyAppearanceTemplate>();
            }

            if (NextTemplateId < 1)
            {
                NextTemplateId = 1;
            }

            if (Mode == CombatBodyFrontMode.Preset
                && !TryGetTemplate(SelectedTemplateId, out CombatBodyAppearanceTemplate ignored))
            {
                Mode = CombatBodyFrontMode.MirrorOriginal;
                SelectedTemplateId = NoTemplateId;
            }
        }

        /// <summary>
        /// 保存或读取 Pawn 档案字段。
        /// </summary>
        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn");
            Scribe_Values.Look(ref PawnThingId, "pawnThingId");
            Scribe_Values.Look(ref Mode, "mode", CombatBodyFrontMode.MirrorOriginal);
            Scribe_Values.Look(ref SelectedTemplateId, "selectedTemplateId", NoTemplateId);
            Scribe_Collections.Look(ref Templates, "templates", LookMode.Deep);
            Scribe_Values.Look(ref NextTemplateId, "nextTemplateId", 1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Pawn != null && string.IsNullOrWhiteSpace(PawnThingId))
                {
                    PawnThingId = Pawn.ThingID;
                }

                Normalize();
            }
        }

        /// <summary>
        /// 判断模板编号是否已被当前档案占用。
        /// </summary>
        private bool ContainsTemplateId(int templateId)
        {
            if (Templates == null)
            {
                return false;
            }

            for (int index = 0; index < Templates.Count; index++)
            {
                CombatBodyAppearanceTemplate template = Templates[index];
                if (template != null && template.TemplateId == templateId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
