using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Content.Royal.Quest
{
    /// <summary>原版单组进攻部件，仅替换本任务使用的队伍工作。</summary>
    public sealed class QuestPart_AssaultRoyalPursuers : QuestPart_AssaultColony
    {
        /// <summary>原版负责成员归组；本业务负责专用队伍和本任务的唯一胜利标记。</summary>
        protected override Lord MakeLord()
        {
            Faction assaulter = faction ?? pawns[0].Faction;
            Lord lord = LordMaker.MakeNewLord(assaulter, new LordJob_RoyalPursuers(assaulter), Map);
            QuestUtility.AddQuestTag(lord, questTag);
            return lord;
        }

        /// <summary>原版部件保存人物和地图；额外保存其未序列化的队伍任务标记，保障袭击前读档。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref questTag, "questTag");
        }
    }
}
