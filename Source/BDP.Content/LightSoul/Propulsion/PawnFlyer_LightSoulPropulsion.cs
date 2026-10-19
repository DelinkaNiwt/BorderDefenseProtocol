using System;
using System.Collections.Generic;
using BDP.Core.Semantics;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Content.GroundDash;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.LightSoul.Propulsion
{
    /// <summary>
    /// 光魂推进专属飞行器。
    ///
    /// 只在中性地面冲刺设施上追加光魂推进的伤害、体型停止力、
    /// 连续推送队形和撞击眩晕；不继承蚱蜢业务类。
    /// </summary>
    public class PawnFlyer_LightSoulPropulsion : PawnFlyer_GroundDash
    {
        /// <summary>本次施放已经处理过的敌对 Pawn。</summary>
        private readonly HashSet<Pawn> affectedPawns = new HashSet<Pawn>();

        /// <summary>按进入推进线的先后顺序保存可推动目标。</summary>
        private readonly List<Pawn> pushedTargets = new List<Pawn>();

        /// <summary>本次冲刺是否已经撞到最终拦截物。</summary>
        private bool impactResolved;

        /// <summary>缓存本次冲刺的初始终点，避免中途提前落地后方向变化。</summary>
        private IntVec3 initialDestinationCell = IntVec3.Invalid;

        /// <summary>本次冲刺的整数方向。</summary>
        private IntVec3 dashDirection = IntVec3.Zero;

        /// <summary>沿途伤害类型。</summary>
        private DamageDef damageDef;

        /// <summary>沿途伤害值。</summary>
        private int damageAmount = 20;

        /// <summary>沿途伤害护甲穿透。</summary>
        private float armorPenetration;

        /// <summary>可推动的最大 Pawn 体型。</summary>
        private float pushStoppingPower = 1f;

        /// <summary>撞击后施加的眩晕时长。</summary>
        private int stunTicks = 120;

        /// <summary>本次施放的本地化能力显示名称。</summary>
        private string sourceLabel;

        /// <summary>由效果组件注入本次施放的运行参数。</summary>
        public void Configure(
            DamageDef configuredDamageDef,
            int configuredDamageAmount,
            float configuredArmorPenetration,
            float configuredPushStoppingPower,
            int configuredStunTicks,
            string configuredSourceLabel)
        {
            damageDef = configuredDamageDef;
            damageAmount = configuredDamageAmount;
            armorPenetration = configuredArmorPenetration;
            pushStoppingPower = configuredPushStoppingPower;
            stunTicks = configuredStunTicks;
            sourceLabel = configuredSourceLabel;
        }

        /// <summary>
        /// 按实际飞行线段解析建筑与 Pawn 碰撞，并让已接触的敌人跟随释放者前进。
        /// </summary>
        protected override void OnDashSegment(IntVec3 from, IntVec3 to)
        {
            if (Map == null || FlyingPawn == null || impactResolved)
            {
                return;
            }

            EnsureDashDirection();
            if (dashDirection == IntVec3.Zero)
            {
                return;
            }

            List<IntVec3> cells = GenSight.BresenhamCellsBetween(from, to);
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                IntVec3 cell = cells[cellIndex];
                if (GroundDashUtility.IsPathBlockingCell(Map, cell))
                {
                    ResolveImpact(cell);
                    return;
                }

                List<Pawn> pawns = GetHostilePawnsAt(cell);
                for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
                {
                    Pawn target = pawns[pawnIndex];
                    if (!affectedPawns.Add(target))
                    {
                        continue;
                    }

                    ApplyBluntDamage(target);
                    if (target.Dead)
                    {
                        continue;
                    }

                    if (!CanPush(target))
                    {
                        ResolveImpact(cell);
                        return;
                    }

                    pushedTargets.Add(target);
                }
            }

            if (pushedTargets.Count == 0)
            {
                return;
            }

            IntVec3 blockingCell;
            if (!TryAlignPushedTargets(to, out blockingCell))
            {
                ResolveImpact(blockingCell);
            }
        }

        /// <summary>缓存初始方向，保证提前落地不会把后续队形方向改掉。</summary>
        private void EnsureDashDirection()
        {
            if (initialDestinationCell.IsValid)
            {
                return;
            }

            initialDestinationCell = DestinationPos.ToIntVec3();
            dashDirection = ResolvePushDirection(
                startVec.ToIntVec3(),
                initialDestinationCell);
        }

        /// <summary>判断目标是否不超过本次推进的停止力。</summary>
        private bool CanPush(Pawn target)
        {
            return target != null
                && target.BodySize <= pushStoppingPower + 0.001f;
        }

        /// <summary>查找指定格中尚未属于推进队列的活着的 Pawn。</summary>
        private Pawn FindNonChainPawnAt(IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(Map))
            {
                return null;
            }

            List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
                if (pawn != null
                    && pawn != FlyingPawn
                    && !pawn.Dead
                    && !pushedTargets.Contains(pawn))
                {
                    return pawn;
                }
            }

            return null;
        }

        /// <summary>
        /// 将当前推进队列排在释放者前方；遇到大型目标、建筑或其它占位 Pawn 时返回拦截格。
        /// </summary>
        private bool TryAlignPushedTargets(IntVec3 playerCell, out IntVec3 blockingCell)
        {
            blockingCell = IntVec3.Invalid;

            // 新目标可能出现在队列前沿，最多重复有限次，避免异常占位造成死循环。
            for (int resolveAttempt = 0; resolveAttempt < 32; resolveAttempt++)
            {
                bool addedTarget = false;
                for (int targetIndex = 0; targetIndex < pushedTargets.Count; targetIndex++)
                {
                    IntVec3 candidate = Add(
                        playerCell,
                        Multiply(dashDirection, targetIndex + 1));

                    if (GroundDashUtility.IsPathBlockingCell(Map, candidate))
                    {
                        blockingCell = candidate;
                        return false;
                    }

                    Pawn obstacle = FindNonChainPawnAt(candidate);
                    if (obstacle == null)
                    {
                        continue;
                    }

                    if (!obstacle.HostileTo(FlyingPawn))
                    {
                        blockingCell = candidate;
                        return false;
                    }

                    if (!affectedPawns.Add(obstacle))
                    {
                        blockingCell = candidate;
                        return false;
                    }

                    ApplyBluntDamage(obstacle);
                    if (obstacle.Dead)
                    {
                        continue;
                    }

                    if (!CanPush(obstacle))
                    {
                        blockingCell = candidate;
                        return false;
                    }

                    pushedTargets.Add(obstacle);
                    addedTarget = true;
                    break;
                }

                if (addedTarget)
                {
                    continue;
                }

                List<IntVec3> formationCells = new List<IntVec3>();
                for (int targetIndex = 0; targetIndex < pushedTargets.Count; targetIndex++)
                {
                    formationCells.Add(Add(
                        playerCell,
                        Multiply(dashDirection, targetIndex + 1)));
                }

                if (!PlacePushedTargets(formationCells))
                {
                    blockingCell = formationCells.Count > 0
                        ? formationCells[0]
                        : playerCell;
                    return false;
                }

                return true;
            }

            blockingCell = playerCell;
            return false;
        }

        /// <summary>
        /// 撞到拦截物后，将队列压到拦截物前方，并把释放者放到队列后方。
        /// </summary>
        private void ResolveImpact(IntVec3 blockingCell)
        {
            if (impactResolved)
            {
                return;
            }

            impactResolved = true;

            if (pushedTargets.Count > 0)
            {
                List<IntVec3> formationCells = new List<IntVec3>();
                for (int targetIndex = 0; targetIndex < pushedTargets.Count; targetIndex++)
                {
                    int distanceFromBlocker = pushedTargets.Count - targetIndex;
                    formationCells.Add(Subtract(
                        blockingCell,
                        Multiply(dashDirection, distanceFromBlocker)));
                }
                PlacePushedTargets(formationCells);
            }

            int playerDistance = pushedTargets.Count + 1;
            IntVec3 preferredPlayerCell = Subtract(
                blockingCell,
                Multiply(dashDirection, playerDistance));
            IntVec3 playerStopCell = FindPlayerStopCell(
                preferredPlayerCell,
                dashDirection);
            LandAtCell(playerStopCell);

            StunPushedTargets();
        }

        /// <summary>把队列中的 Pawn 安全地移动到一格宽的连续队形。</summary>
        private bool PlacePushedTargets(List<IntVec3> formationCells)
        {
            if (formationCells == null || formationCells.Count != pushedTargets.Count)
            {
                return false;
            }

            HashSet<Pawn> chain = new HashSet<Pawn>(pushedTargets);
            HashSet<IntVec3> reservedCells = new HashSet<IntVec3>();

            // 先放最前端目标，避免队列目标临时挡住彼此的最终位置。
            for (int targetIndex = pushedTargets.Count - 1; targetIndex >= 0; targetIndex--)
            {
                Pawn target = pushedTargets[targetIndex];
                IntVec3 cell = formationCells[targetIndex];
                if (target == null || target.Dead)
                {
                    continue;
                }

                if (!CanPlacePushedPawn(target, cell, chain, reservedCells))
                {
                    return false;
                }

                MovePawn(target, cell);
                reservedCells.Add(cell);
            }

            return true;
        }

        /// <summary>判断推进队列目标格是否可行走且不与队列外 Pawn 重叠。</summary>
        private bool CanPlacePushedPawn(
            Pawn target,
            IntVec3 cell,
            HashSet<Pawn> chain,
            HashSet<IntVec3> reservedCells)
        {
            if (!GroundDashUtility.IsValidDashCell(Map, cell)
                || !cell.WalkableBy(Map, target)
                || reservedCells.Contains(cell))
            {
                return false;
            }

            List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn other = things[i] as Pawn;
                if (other != null
                    && other != target
                    && !chain.Contains(other))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>收集当前格上尚未过滤的敌对 Pawn。</summary>
        private List<Pawn> GetHostilePawnsAt(IntVec3 cell)
        {
            List<Pawn> result = new List<Pawn>();
            if (!cell.IsValid || !cell.InBounds(Map))
            {
                return result;
            }

            List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn target = things[i] as Pawn;
                if (target != null
                    && target != FlyingPawn
                    && !target.Dead
                    && target.HostileTo(FlyingPawn))
                {
                    result.Add(target);
                }
            }
            return result;
        }

        /// <summary>对每个敌人施加一次钝伤。</summary>
        private void ApplyBluntDamage(Pawn target)
        {
            DamageDef resolvedDamageDef = damageDef ?? DamageDefOf.Blunt;
            float angle = (target.Position - startVec.ToIntVec3()).AngleFlat;
            DamageResolution resolution;
            DamageResolutionRuntime.ApplyDamage(target, new DamageInfo(
                resolvedDamageDef, damageAmount, armorPenetration, angle, FlyingPawn), out resolution, new SemanticContext
            {
                DisplayLabel = BuildInjurySourceLabel(),
                SourceKind = SemanticSourceKind.AttackAction,
                Instigator = FlyingPawn
            });
        }

        /// <summary>组成伤口界面显示的能力名称和释放者名称。</summary>
        private string BuildInjurySourceLabel()
        {
            if (string.IsNullOrEmpty(sourceLabel) || FlyingPawn == null)
            {
                return sourceLabel;
            }

            return $"{sourceLabel}[{FlyingPawn.LabelShort}]";
        }

        /// <summary>只对已经被推动的目标施加原版眩晕及其显示覆盖效果。</summary>
        private void StunPushedTargets()
        {
            if (stunTicks <= 0)
            {
                return;
            }

            for (int i = 0; i < pushedTargets.Count; i++)
            {
                Pawn target = pushedTargets[i];
                if (!target.Dead)
                {
                    target.stances?.stunner?.StunFor(
                        stunTicks,
                        FlyingPawn,
                        addBattleLog: false,
                        showMote: true);
                }
            }
        }

        /// <summary>寻找拦截物或推进队列后方最后一个安全的玩家落点。</summary>
        private IntVec3 FindPlayerStopCell(IntVec3 preferred, IntVec3 direction)
        {
            IntVec3 candidate = preferred;
            for (int attempt = 0; attempt < 64; attempt++)
            {
                if (GroundDashUtility.IsValidDashCell(Map, candidate)
                    && !HasPawnAt(candidate))
                {
                    return candidate;
                }

                candidate = Subtract(candidate, direction);
                if (!candidate.IsValid || !candidate.InBounds(Map))
                {
                    break;
                }
            }

            return startVec.ToIntVec3();
        }

        /// <summary>判断一个格子是否仍被 Pawn 占用。</summary>
        private bool HasPawnAt(IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(Map))
            {
                return true;
            }

            List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 使用原版 Pawn 位置更新和传送通知完成一次推挤。
        ///
        /// 这里必须结束被推动 Pawn 的当前工作：原版传送通知会清空路径，
        /// 但 endCurrentJob=false 会留下原来的 Goto（移动）工作，且该工作
        /// 不会再次执行 StartPath（重新开始寻路），最终就会出现永久停滞。
        /// </summary>
        private void MovePawn(Pawn target, IntVec3 cell)
        {
            if (target.Position == cell)
            {
                return;
            }

            target.Position = cell;
            target.Notify_Teleported();
        }

        /// <summary>按整数步长从地图格中减去方向。</summary>
        private static IntVec3 Subtract(IntVec3 cell, IntVec3 direction)
        {
            return new IntVec3(cell.x - direction.x, 0, cell.z - direction.z);
        }

        /// <summary>按整数步长向地图格中加入方向。</summary>
        private static IntVec3 Add(IntVec3 cell, IntVec3 direction)
        {
            return new IntVec3(cell.x + direction.x, 0, cell.z + direction.z);
        }

        /// <summary>按整数倍数扩展地图格方向。</summary>
        private static IntVec3 Multiply(IntVec3 direction, int amount)
        {
            return new IntVec3(direction.x * amount, 0, direction.z * amount);
        }

        /// <summary>按照起点到终点的平面方向取得 Pawn 推进步长。</summary>
        private static IntVec3 ResolvePushDirection(
            IntVec3 startCell,
            IntVec3 destinationCell)
        {
            int x = Math.Sign(destinationCell.x - startCell.x);
            int z = Math.Sign(destinationCell.z - startCell.z);
            return new IntVec3(x, 0, z);
        }
    }
}
