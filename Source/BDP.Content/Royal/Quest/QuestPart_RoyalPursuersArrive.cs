using BDP.Core.CombatBody;
using BDP.Core.Genes;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Content.Royal.Quest
{
    /// <summary>仅补落地图前的正式战斗体激活；抵达、引用和保存沿用原版。</summary>
    public sealed class QuestPart_RoyalPursuersArrive : QuestPart_PawnsArrive
    {
        /// <summary>追兵直接装备的入门原型，不另生成战利品。</summary>
        public ThingDef prototype;

        /// <summary>在原版抵达流程之前完成真实激活，首次出现即为战斗体。</summary>
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignal)
            {
                pawns.RemoveAll(pawn => pawn.Destroyed);
                foreach (Pawn pawn in pawns)
                {
                    if (PrepareCombatBody(pawn)) continue;
                    // 正常生成应具有有效腺体及资源；失败不能派出缺少战斗体的错误追兵。
                    Log.Error("BDP_RoyalArrivalPursuerInitializationFailed".Translate(pawn.LabelShortCap));
                    quest.End(QuestEndOutcome.Fail, sendLetter: true);
                    return;
                }
            }
            base.Notify_QuestSignalReceived(signal);
        }

        /// <summary>使用正式资源、装备和会话入口；人物尚未落地图，不伪造相位或健康标记。</summary>
        private bool PrepareCombatBody(Pawn pawn)
        {
            if (CombatBodySurfaceAccess.ResolveReader(pawn)?.Phase == CombatBodyPhase.Active)
                return true;
            if (!TrionGlandEligibility.HasActiveTrionGland(pawn)) return false;
            CompTrion resource = pawn.GetComp<CompTrion>();
            ICombatBodyCommands body = CombatBodySurfaceAccess.ResolveCommands(pawn);
            if (resource == null || body == null) return false;
            // 此初始化无地图依赖，首次真正落地图不会重置已激活的资源会话。
            resource.PostSpawnSetup(false);
            pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(prototype));
            return body.TryActivate()
                && CombatBodySurfaceAccess.ResolveReader(pawn)?.Phase == CombatBodyPhase.Active;
        }

        /// <summary>保留原版抵达资料，并保存业务装备引用。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref prototype, "prototype");
        }
    }
}
