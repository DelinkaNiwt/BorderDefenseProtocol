using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Core.Abilities;
using RimWorld;
using Verse;

namespace BDP.Content.Abilities
{
    /// <summary>配置驱动的一格建筑生成参数；不绑定芯片、墙体或战斗规则。</summary>
    public sealed class CompProperties_AbilityEffect_BatchBuildingSpawn : CompProperties_AbilityEffect
    {
        /// <summary>每个有效目标格生成的建筑定义。</summary>
        public ThingDef buildingDef;
        /// <summary>需要材料类型的建筑使用此材料；不支付普通施工材料。</summary>
        public ThingDef stuffDef;

        /// <summary>绑定批量生成效果。</summary>
        public CompProperties_AbilityEffect_BatchBuildingSpawn()
        {
            compClass = typeof(CompAbilityEffect_BatchBuildingSpawn);
        }
    }

    /// <summary>消费原版作业保存的批量目标；内容规则在此扩展，不进入 Core。</summary>
    public class CompAbilityEffect_BatchBuildingSpawn : CompAbilityEffect
    {
        /// <summary>当前生成配置。</summary>
        protected new CompProperties_AbilityEffect_BatchBuildingSpawn Props =>
            (CompProperties_AbilityEffect_BatchBuildingSpawn)props;

        /// <summary>只接受一格建筑与有效材料，避免一格一次却产生多格建筑的预算歧义。</summary>
        protected virtual bool ConfigurationValid => parent is BdpBatchAbility
            && Props.buildingDef != null && Props.buildingDef.category == ThingCategory.Building
            && Props.buildingDef.size.x == 1 && Props.buildingDef.size.z == 1
            && (Props.buildingDef.MadeFromStuff
                ? Props.stuffDef?.stuffProps != null && Props.stuffDef.stuffProps.CanMake(Props.buildingDef)
                : Props.stuffDef == null);

        /// <summary>预览与最后校验共用原版建造判定；不再额外要求空格或可站立。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            bool valid = ConfigurationValid && parent.pawn?.Map != null
                && target.Cell.InBounds(parent.pawn.Map)
                && GenConstruct.CanPlaceBlueprintAt(Props.buildingDef, target.Cell, Rot4.North,
                    parent.pawn.Map, false, null, null, Props.stuffDef).Accepted
                && base.Valid(target, throwMessages);
            if (!valid && throwMessages && parent.pawn != null)
                Messages.Message("BDP_BatchAbility_InvalidCell".Translate(), parent.pawn, MessageTypeDefOf.RejectInput, false);
            return valid;
        }

        /// <summary>前摇期间的原版效果检查也必须使用相同放置条件。</summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Valid(target);

        /// <summary>不提供自动选格策略，避免人工智能把批量能力当成单点能力。</summary>
        public override bool AICanTargetNow(LocalTargetInfo target) => false;

        /// <summary>本批只生成指定建筑；异常清理本批对象并继续抛出，不能伪报成功。</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var batch = parent as BdpBatchAbility;
            if (!ConfigurationValid || batch == null)
                throw new InvalidOperationException("BDP_BatchAbility_InvalidRequest".Translate());
            var created = new List<Thing>();
            try
            {
                foreach (IntVec3 cell in batch.CurrentBatchTargets)
                {
                    Thing building = ThingMaker.MakeThing(Props.buildingDef, Props.stuffDef);
                    created.Add(building);
                    building.SetFactionDirect(parent.pawn.Faction);
                    // 正常施工先把蓝图转成框架；即时生成须取消原版允许替换的建筑蓝图。
                    // 实体建筑的生成清理不会自动覆盖旧蓝图，不能留下重叠施工请求。
                    foreach (Thing existing in cell.GetThingList(batch.CurrentBatchMap).ToList())
                        if (existing is Blueprint blueprint && blueprint.def.entityDefToBuild is ThingDef builtDef
                            && builtDef.category == ThingCategory.Building
                            && GenConstruct.CanReplace(Props.buildingDef, blueprint.def, Props.stuffDef, blueprint.Stuff))
                            blueprint.Destroy(DestroyMode.Cancel);
                    // 原版框架完工也使用此模式：按原版规则移开物品、清理植物并处理可替换物。
                    GenSpawn.Spawn(building, cell, batch.CurrentBatchMap, Rot4.North, WipeMode.FullRefund);
                }
                base.Apply(target, dest);
            }
            catch
            {
                foreach (Thing building in created)
                    if (!building.Destroyed) building.Destroy(DestroyMode.Vanish);
                throw;
            }
        }
    }
}
