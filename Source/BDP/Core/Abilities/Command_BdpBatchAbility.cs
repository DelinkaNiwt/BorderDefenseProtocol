using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>能力按钮只打开原版划选工具，不在按钮回调中生成内容。</summary>
    public sealed class Command_BdpBatchAbility : Command_Ability
    {
        /// <summary>原版按定义构建按钮。</summary>
        public Command_BdpBatchAbility(Ability ability, Pawn pawn) : base(ability, pawn) { }

        /// <summary>划选工具绑定当前角色，使用期间仍以能力资格为准。</summary>
        public override void ProcessInput(Event ev)
        {
            var batch = Ability as BdpBatchAbility;
            if (batch == null || !batch.CanQueueCast || batch.GizmoDisabled(out string _)) return;
            Find.Targeter.StopTargeting();
            Find.DesignatorManager.Select(new Designator_BdpBatchAbility(batch));
        }
    }
}
