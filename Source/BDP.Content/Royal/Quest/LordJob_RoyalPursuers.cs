using System.Collections.Generic;
using BDP.Core.CombatBody;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Content.Royal.Quest
{
    /// <summary>皇室入口追兵沿用原版进攻；战斗体暂时倒地不等同于原身失能。</summary>
    public sealed class LordJob_RoyalPursuers : LordJob_AssaultColony
    {
        /// <summary>只记录被暂缓移除的成员；保存引用，以免崩解期间读档丢失收尾。</summary>
        private List<Pawn> pendingRecovery = new List<Pawn>();

        /// <summary>供原版保存系统创建；新队伍默认关闭退却、绑架和偷窃。</summary>
        public LordJob_RoyalPursuers() : this(null) { }

        /// <summary>采用普通进攻，不额外添加装备、再激活或复杂战术。</summary>
        public LordJob_RoyalPursuers(Faction faction)
            : base(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: false) { }

        /// <summary>原版倒地先通知队伍，BDP 后置处理才请求崩解；必须在此保留身份。</summary>
        public override bool ShouldRemovePawn(Pawn pawn, PawnLostCondition reason)
        {
            if (reason == PawnLostCondition.Incapped && IsAwaitingRestore(pawn))
            {
                if (!pendingRecovery.Contains(pawn)) pendingRecovery.Add(pawn);
                return false;
            }
            return base.ShouldRemovePawn(pawn, reason);
        }

        /// <summary>只核对待恢复的小列表；原身恢复行动时原版已刷新职责，仍倒地则补发正常失能。</summary>
        public override void LordJobTick()
        {
            base.LordJobTick();
            if (lord == null) return;
            for (int i = pendingRecovery.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pendingRecovery[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead || !lord.ownedPawns.Contains(pawn))
                {
                    pendingRecovery.RemoveAt(i);
                    continue;
                }
                if (IsAwaitingRestore(pawn)) continue;
                pendingRecovery.RemoveAt(i);
                // 相位已退出，后遗症也已完成施加；不能永久保留真正无法行动的原身。
                // 交回原版通知链，最后一名失能时仍由原版队伍结束和任务信号负责。
                if (pawn.Downed) lord.Notify_PawnLost(pawn, PawnLostCondition.Incapped);
            }
        }

        /// <summary>真正离队时立即移除等待记录；死亡和被俘一直遵循原版处理。</summary>
        public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
        {
            pendingRecovery.Remove(pawn);
            base.Notify_PawnLost(pawn, condition);
        }

        /// <summary>保留原版派系与进攻开关保存，再保存本队伍尚未恢复的成员。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingRecovery, "pendingRecovery", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pendingRecovery == null)
                pendingRecovery = new List<Pawn>();
        }

        /// <summary>只识别正式激活与崩解相位；冷却中的原身不享有暂缓。</summary>
        private static bool IsAwaitingRestore(Pawn pawn)
        {
            ICombatBodyReader body = CombatBodySurfaceAccess.ResolveReader(pawn);
            return body != null && (body.Phase == CombatBodyPhase.Active
                || body.Phase == CombatBodyPhase.Collapsing);
        }
    }
}
