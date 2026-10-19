using System.Collections.Generic;
using System.Linq;
using BDP.Content.Factions.GateArrival;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace BDP.Content.Royal.Quest
{
    /// <summary>皇室入口任务：接受时正式加入，追兵袭击仍作为任务内容。</summary>
    public sealed class QuestNode_Root_RoyalArrival : QuestNode
    {
        /// <summary>随机再接触入口标识，供开发菜单生成；防重复按共用根节点识别两类入口。</summary>
        public const string QuestDefName = "BDP_RoyalArrival";
        /// <summary>皇室任务追兵使用不销毁掉落装备的近界民斥候类型。</summary>
        private const string PursuerPawnKindDefName = "BDP_NearRealm_Scout";
        /// <summary>皇室加入任务唯一使用的角色类型标识。</summary>
        private const string RoyalPawnKindDefName = "BDP_Royal_FallenRoyal";
        /// <summary>用于识别正式皇室成员的血脉基因定义名。</summary>
        private const string RoyalBloodlineGeneDefName = "BDP_Gene_RoyalBloodline";
        /// <summary>皇室加入任务临时派系模板标识。</summary>
        private const string RoyalFactionDefName = "BDP_Faction_ViserEmpire";
        /// <summary>皇室任务专用、包含可遗传皇室基因组合的异种定义。</summary>
        public XenotypeDef royalXenotype;
        /// <summary>追兵启动原型必备的内源腺体。</summary>
        public GeneDef glandGene;
        /// <summary>追兵直接装备的已有原型。</summary>
        public ThingDef prototype;
        /// <summary>皇室人物携带的实体核心材料，本轮不接研究和建筑消耗。</summary>
        public ThingDef driveCore;
        /// <summary>从定义读取材料数量，保持以后平衡可调整。</summary>
        public int driveCoreCount;
        /// <summary>从定义读取整波人数；整波始终是一组队伍。</summary>
        public int pursuerCount;
        /// <summary>从定义读取抵达间隔；不是保护时限或自动胜利倒计时。</summary>
        public int raidDelayTicks;

        /// <summary>沿原版任务池出现；没有正式皇室且无待接受／进行中的同类任务才可生成。</summary>
        protected override bool TestRunInt(Slate slate)
        {
            return ModsConfig.BiotechActive && HasRoyalXenotype() && glandGene != null
                && prototype != null && driveCore != null && driveCoreCount > 0
                && pursuerCount > 0 && raidDelayTicks >= 0
                && Find.Storyteller.difficulty.allowViolentQuests
                && QuestGen_Get.GetMap() != null && FindPursuerFaction() != null
                && FindPursuerPawnKind() != null
                && FindRoyalFactionDef() != null
                && FindRoyalPawnKind() != null
                && !HasRoyalColonist() && !HasPendingQuest();
        }

        /// <summary>借住者不提前算正式皇室；外出中的正式成员也算，不局限当前地图。</summary>
        private bool HasRoyalColonist()
        {
            GeneDef royalBloodlineGene = FindRoyalBloodlineGene();
            if (royalBloodlineGene == null)
                return false;

            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves
                .Any(pawn => !pawn.IsQuestLodger() && pawn.genes?.GetGene(royalBloodlineGene)?.Active == true);
        }

        /// <summary>任务必须配置完整的皇室基因组合，并包含身份识别基因。</summary>
        private bool HasRoyalXenotype()
        {
            return royalXenotype?.genes != null && royalXenotype.genes.Count > 0
                && royalXenotype.genes.All(gene => gene != null)
                && FindRoyalBloodlineGene() != null;
        }

        /// <summary>从内源基因组合中取得用于识别皇室成员的基因。</summary>
        private GeneDef FindRoyalBloodlineGene()
        {
            return royalXenotype?.genes?.FirstOrDefault(gene => gene?.defName == RoyalBloodlineGeneDefName);
        }

        /// <summary>按共用任务根节点识别首发和随机入口；任一未结束任务都阻止重复。</summary>
        private bool HasPendingQuest()
        {
            return Find.QuestManager.QuestsListForReading.Any(existing =>
                existing.root?.root is QuestNode_Root_RoyalArrival
                && (existing.State == QuestState.NotYetAccepted || existing.State == QuestState.Ongoing));
        }

        /// <summary>追兵固定使用正式近界宙域派系。</summary>
        private static Faction FindPursuerFaction()
        {
            FactionDef nearRealmDef = DefDatabase<FactionDef>.GetNamedSilentFail("BDP_Faction_NearRealm");
            return nearRealmDef == null
                ? null
                : Find.FactionManager.AllFactionsListForReading.FirstOrDefault(candidate =>
                    !candidate.IsPlayer && candidate.def == nearRealmDef && !candidate.defeated);
        }

        /// <summary>解析任务专用斥候角色；缺少定义时阻止任务生成。</summary>
        private static PawnKindDef FindPursuerPawnKind()
        {
            return DefDatabase<PawnKindDef>.GetNamedSilentFail(PursuerPawnKindDefName);
        }

        /// <summary>解析任务专用皇室角色类型；缺少定义时阻止任务生成。</summary>
        private static PawnKindDef FindRoyalPawnKind()
        {
            return DefDatabase<PawnKindDef>.GetNamedSilentFail(RoyalPawnKindDefName);
        }

        /// <summary>解析仅作为运行时临时派系模板的薇瑟帝国定义。</summary>
        private static FactionDef FindRoyalFactionDef()
        {
            return DefDatabase<FactionDef>.GetNamedSilentFail(RoyalFactionDefName);
        }

        /// <summary>按原版任务临时派系流程创建中立、可见且不生成据点的薇瑟帝国实例。</summary>
        private static Faction CreateRoyalTemporaryFaction()
        {
            FactionDef factionDef = FindRoyalFactionDef();
            if (factionDef == null)
                return null;

            var relations = new List<FactionRelation>();
            foreach (Faction existingFaction in Find.FactionManager.AllFactionsListForReading)
            {
                if (existingFaction.def.PermanentlyHostileTo(factionDef))
                    continue;

                relations.Add(new FactionRelation
                {
                    other = existingFaction,
                    kind = FactionRelationKind.Neutral
                });
            }

            // 原版生成器在 hidden=true 时不创建初始据点；创建后再公开临时派系。
            Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(factionDef, relations, hidden: true);
            faction.temporary = true;
            faction.hidden = false;
            Find.FactionManager.Add(faction);
            return faction;
        }

        /// <summary>生成资料和原版任务部件；真正抵达与袭击由玩家接受后的信号驱动。</summary>
        protected override void RunInt()
        {
            RimWorld.Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap();
            Faction pursuerFaction = FindPursuerFaction();
            PawnKindDef pursuerPawnKind = FindPursuerPawnKind();
            PawnKindDef royalPawnKind = FindRoyalPawnKind();
            if (royalPawnKind == null || !HasRoyalXenotype())
                return;

            Faction royalFaction = CreateRoyalTemporaryFaction();
            if (royalFaction == null)
                return;

            // 皇室背景在 PawnKindDef 中固定；跳过原版随机固定人物小传分支，以免覆盖固定背景。
            Pawn royal = quest.GeneratePawn(new PawnGenerationRequest(royalPawnKind,
                royalFaction, tile: map.Tile, forceGenerateNewPawn: true,
                forcedXenotype: royalXenotype,
                onlyUseForcedBackstories: true,
                dontGiveWeapon: true,
                developmentalStages: DevelopmentalStage.Adult));
            // 每颗核心都是独立实物；调整数量时也不制造超过堆叠上限的物品。
            for (int i = 0; i < driveCoreCount; i++)
                royal.inventory.innerContainer.TryAdd(ThingMaker.MakeThing(driveCore));
            var royalPawns = new List<Pawn> { royal };
            var pursuers = new List<Pawn>();
            for (int i = 0; i < pursuerCount; i++)
            {
                pursuers.Add(quest.GeneratePawn(new PawnGenerationRequest(pursuerPawnKind,
                    pursuerFaction, tile: map.Tile, forceGenerateNewPawn: true,
                    mustBeCapableOfViolence: true, dontGiveWeapon: true,
                    forcedEndogenes: new List<GeneDef> { glandGene },
                    developmentalStages: DevelopmentalStage.Adult)));
            }
            slate.Set("map", map);
            slate.Set("royal", royal);
            slate.Set("pursuers", pursuers);
            // 原版会为这些人物补任务标记；队伍使用同一个完整标记，保存后不再重新解析。
            string destroyed = QuestGenUtility.HardcodedSignalWithQuestID("royal.Destroyed");
            string kidnapped = QuestGenUtility.HardcodedSignalWithQuestID("royal.Kidnapped");
            string defeated = QuestGenUtility.HardcodedSignalWithQuestID("pursuers.AllEnemiesDefeated");
            string pursuerTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("pursuers");

            quest.PawnsArrive(royalPawns, mapParent: map.Parent, joinPlayer: true,
                customLetterLabel: "BDP_RoyalArrivalArrivedLabel".Translate(),
                customLetterText: "BDP_RoyalArrivalArrivedText".Translate(royal.LabelShortCap),
                isSingleReward: true);
            quest.Delay(raidDelayTicks, () => AddPursuerArrival(quest, map, pursuerFaction, pursuers, pursuerTag));

            // 到达部件先把人物置入玩家派系；同一接受信号再由原版招募部件正式转为殖民者。
            quest.JoinPlayer(map.Parent, royalPawns, joinPlayer: true);
            quest.End(QuestEndOutcome.Success, inSignal: defeated, sendStandardLetter: true);
            quest.End(QuestEndOutcome.Fail, inSignal: destroyed, sendStandardLetter: true);
            quest.End(QuestEndOutcome.Fail, inSignal: kidnapped, sendStandardLetter: true);

            slate.Set("resolvedQuestName", "BDP_RoyalArrivalName".Translate().ToString());
            slate.Set("resolvedQuestDescription", "BDP_RoyalArrivalDescription".Translate(
                royal.LabelShortCap, pursuerCount, (raidDelayTicks / (float)GenDate.TicksPerDay).ToString("0.##")).ToString());
        }

        /// <summary>在同一原版延迟完成信号中先抵达后归队，全波只有一个胜利来源。</summary>
        private void AddPursuerArrival(RimWorld.Quest quest, Map map, Faction faction,
            List<Pawn> pursuers, string questTag)
        {
            string inSignal = QuestGen.slate.Get<string>("inSignal");
            quest.AddPart(new QuestPart_RoyalPursuersArrive
            {
                inSignal = inSignal,
                mapParent = map.Parent,
                pawns = new List<Pawn>(pursuers),
                arrivalMode = GateArrivalDefOf.BDP_NearRealmGate,
                prototype = prototype,
                customLetterDef = LetterDefOf.ThreatBig,
                customLetterLabel = "BDP_RoyalArrivalRaidLabel".Translate(),
                customLetterText = "BDP_RoyalArrivalRaidText".Translate()
            });
            quest.AddPart(new QuestPart_AssaultRoyalPursuers
            {
                inSignal = inSignal,
                mapParent = map.Parent,
                faction = faction,
                pawns = new List<Pawn>(pursuers),
                questTag = questTag,
                canKidnap = false,
                canTimeoutOrFlee = false,
                canSteal = false
            });
        }
    }
}
