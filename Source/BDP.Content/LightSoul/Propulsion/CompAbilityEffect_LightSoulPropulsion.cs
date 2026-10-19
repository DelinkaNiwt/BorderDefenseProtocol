using System.Collections.Generic;
using BDP.Content.GroundDash;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.LightSoul.Propulsion
{
    /// <summary>光魂推进的运行参数。</summary>
    public class CompProperties_LightSoulPropulsion : CompProperties_AbilityEffect
    {
        /// <summary>光魂推进使用的地面冲刺飞行器 Def。</summary>
        public ThingDef pawnFlyerDef;

        /// <summary>沿途造成的伤害类型，缺省时运行期使用 Blunt。</summary>
        public DamageDef damageDef;

        /// <summary>每个敌人受到的钝伤伤害值。</summary>
        public int damageAmount = 20;

        /// <summary>钝伤护甲穿透。</summary>
        public float armorPenetration;

        /// <summary>允许被推进的最大 Pawn 体型。</summary>
        public float pushStoppingPower = 1f;

        /// <summary>撞墙或撞大型目标时施加的眩晕时长，单位 tick。</summary>
        public int stunTicks = 120;

        /// <summary>构造并绑定光魂推进效果组件。</summary>
        public CompProperties_LightSoulPropulsion()
        {
            compClass = typeof(CompAbilityEffect_LightSoulPropulsion);
        }
    }

    /// <summary>
    /// 光魂推进 Ability Effect。
    /// 只负责读取组合结果参数、解析有效终点并生成专属飞行器。
    /// </summary>
    public class CompAbilityEffect_LightSoulPropulsion : CompAbilityEffect
    {
        /// <summary>当前效果的强类型配置。</summary>
        protected new CompProperties_LightSoulPropulsion Props
        {
            get { return (CompProperties_LightSoulPropulsion)props; }
        }

        /// <summary>
        /// 绘制一格宽的直线型有效区域。
        /// 地图几何会截断预览；大型敌人只改变其后方颜色，不截断预览。
        /// </summary>
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            Pawn pawn = parent != null ? parent.pawn : null;
            if (pawn == null || pawn.Map == null || !target.IsValid)
            {
                return;
            }

            GroundDashPathResult path;
            if (!GroundDashUtility.TryResolvePath(
                pawn.Map,
                pawn.Position,
                target.Cell,
                parent.def.verbProperties.range,
                out path)
                || !path.HasMovement)
            {
                return;
            }

            List<IntVec3> lineCells = GenSight.BresenhamCellsBetween(
                pawn.Position,
                path.EndCell);
            if (lineCells.Count > 0 && lineCells[0] == pawn.Position)
            {
                lineCells.RemoveAt(0);
            }

            if (lineCells.Count == 0)
            {
                return;
            }

            int firstLargeTargetIndex = FindFirstLargeHostileTarget(lineCells, pawn);
            if (firstLargeTargetIndex < 0)
            {
                GenDraw.DrawFieldEdges(lineCells, Color.white);
                return;
            }

            List<IntVec3> normalCells = lineCells.GetRange(0, firstLargeTargetIndex + 1);
            if (normalCells.Count > 0)
            {
                GenDraw.DrawFieldEdges(normalCells, Color.white);
            }

            int redStartIndex = firstLargeTargetIndex + 1;
            if (redStartIndex < lineCells.Count)
            {
                List<IntVec3> redCells = lineCells.GetRange(
                    redStartIndex,
                    lineCells.Count - redStartIndex);
                GenDraw.DrawFieldEdges(
                    redCells,
                    new Color(1f, 0.35f, 0.35f, 1f));
            }
        }

        /// <summary>把组合技目标转换为被地图几何裁剪后的实际冲刺。</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent != null ? parent.pawn : null;
            if (pawn == null || pawn.Map == null || !target.IsValid)
            {
                return;
            }

            GroundDashPathResult path;
            if (!GroundDashUtility.TryResolvePath(
                pawn.Map,
                pawn.Position,
                target.Cell,
                parent.def.verbProperties.range,
                out path)
                || !path.HasMovement)
            {
                return;
            }

            ThingDef flyerDef = Props.pawnFlyerDef;
            if (flyerDef == null)
            {
                Log.Error("[BDP] LightSoul propulsion has no pawnFlyerDef configured.");
                return;
            }

            VerbProperties verbProps = parent.def.verbProperties;
            GroundDashUtility.DoDash(
                pawn,
                path.EndCell,
                verbProps,
                flyerDef,
                delegate(PawnFlyer flyer)
                {
                    PawnFlyer_LightSoulPropulsion propulsionFlyer = flyer as PawnFlyer_LightSoulPropulsion;
                    if (propulsionFlyer == null)
                    {
                        Log.Error("[BDP] LightSoul propulsion pawnFlyerDef is not PawnFlyer_LightSoulPropulsion.");
                        return;
                    }

                    propulsionFlyer.Configure(
                        Props.damageDef,
                        Props.damageAmount,
                        Props.armorPenetration,
                        Props.pushStoppingPower,
                        Props.stunTicks,
                        parent.def.label);
                });
        }

        /// <summary>查找预览直线上的第一个无法被停止力推动的敌对 Pawn。</summary>
        private int FindFirstLargeHostileTarget(List<IntVec3> lineCells, Pawn caster)
        {
            float stoppingPower = Props.pushStoppingPower;
            for (int cellIndex = 0; cellIndex < lineCells.Count; cellIndex++)
            {
                List<Thing> things = caster.Map.thingGrid.ThingsListAtFast(lineCells[cellIndex]);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Pawn target = things[thingIndex] as Pawn;
                    if (target != null
                        && target != caster
                        && !target.Dead
                        && target.HostileTo(caster)
                        && target.BodySize > stoppingPower + 0.001f)
                    {
                        return cellIndex;
                    }
                }
            }

            return -1;
        }
    }
}
