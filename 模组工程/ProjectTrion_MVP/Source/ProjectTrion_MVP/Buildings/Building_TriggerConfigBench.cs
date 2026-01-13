using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion 触发器配置台
    ///
    /// MVP简化版：
    /// - 提供基础的gizmo菜单入口
    /// - 验证战斗体激活状态（激活后禁止修改）
    /// - 实际的UI界面由玩家通过右键菜单操作
    ///
    /// 未来扩展：可添加Window对话框进行高级配置
    /// </summary>
    public class Building_TriggerConfigBench : Building
    {
        // ============ 常数 ============
        private const string GIZMO_LABEL = "配置触发器";
        private const string GIZMO_DESC = "在此配置台上装卸 Trion 触发器上的组件";

        // ============ 数据 ============
        private TrionTrigger _selectedTrigger = null;  // 当前选中的触发器

        // ============ 属性 ============
        public TrionTrigger SelectedTrigger => _selectedTrigger;

        // ============ Gizmo ============

        /// <summary>
        /// 返回该建筑的操作按钮（右键菜单）
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            // 检查附近是否有有效的Pawn
            var pawn = GetNearbyPawn();
            var canUse = pawn != null && HasTrionAbility(pawn);
            var disabledReason = "";

            if (pawn == null)
                disabledReason = "附近没有穿戴触发器的Pawn";
            else if (!HasTrionAbility(pawn))
                disabledReason = "该Pawn没有植入Trion腺体";

            // 配置触发器 Gizmo
            if (!canUse)
            {
                yield return new Command_Action
                {
                    defaultLabel = GIZMO_LABEL,
                    defaultDesc = $"{GIZMO_DESC}\n\n【无法使用】{disabledReason}",
                    action = () => Messages.Message($"无法配置触发器：{disabledReason}", MessageTypeDefOf.RejectInput, historical: false),
                    icon = TexCommand.Install
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = GIZMO_LABEL,
                    defaultDesc = GIZMO_DESC,
                    action = OpenConfigMenu,
                    icon = TexCommand.Install
                };
            }
        }

        // ============ 交互方法 ============

        /// <summary>
        /// 打开配置菜单
        /// 检查附近是否有Pawn穿戴了TrionTrigger
        /// </summary>
        private void OpenConfigMenu()
        {
            // 查找附近的Pawn（使用此建筑的Pawn）
            var pawn = GetNearbyPawn();

            if (pawn == null)
            {
                Messages.Message("[Trion] 附近没有穿戴触发器的Pawn", MessageTypeDefOf.NeutralEvent);
                return;
            }

            // 获取Pawn穿戴的TrionTrigger
            _selectedTrigger = pawn.apparel?.WornApparel
                .OfType<TrionTrigger>()
                .FirstOrDefault();

            if (_selectedTrigger == null)
            {
                Messages.Message("[Trion] 该Pawn未穿戴任何触发器", MessageTypeDefOf.NeutralEvent);
                return;
            }

            // 检查战斗体是否激活
            var compTrion = _selectedTrigger.GetWearerCompTrion();
            if (compTrion?.IsInCombat ?? false)
            {
                Messages.Message("[Trion] 战斗体已激活，无法修改配置", MessageTypeDefOf.NeutralEvent);
                return;
            }

            // 打开组件菜单
            OpenComponentMenu(pawn, _selectedTrigger);
        }

        /// <summary>
        /// 打开组件装卸菜单
        /// </summary>
        private void OpenComponentMenu(Pawn pawn, TrionTrigger trigger)
        {
            var options = new List<FloatMenuOption>();

            foreach (var mount in trigger.Mounts)
            {
                options.Add(new FloatMenuOption(
                    $"编辑 {mount.SlotName} ({mount.ComponentCount}/{mount.MaxSlots})",
                    () => OpenMountMenu(pawn, mount)
                ));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        /// <summary>
        /// 打开挂载点菜单，允许添加/移除组件
        /// </summary>
        private void OpenMountMenu(Pawn pawn, TriggerMount mount)
        {
            var options = new List<FloatMenuOption>();

            // 列出已装备的组件（可以卸下）
            foreach (var comp in mount.EquippedComponents.ToList())
            {
                options.Add(new FloatMenuOption(
                    $"卸下: {comp.Def.label}",
                    () => mount.TryRemoveComponent(comp)
                ));
            }

            options.Add(new FloatMenuOption("---", null));  // 分隔符

            // 列出背包中的组件（可以装上）
            if (mount.HasSlotSpace)
            {
                var items = pawn.inventory.innerContainer
                    .OfType<TrionComponent_Thing>()
                    .ToList();

                if (items.Count == 0)
                {
                    options.Add(new FloatMenuOption("(背包中无组件物品)", null));
                }
                else
                {
                    foreach (var item in items)
                    {
                        var comp = TriggerComponent.CreateFromDef(item.def);
                        options.Add(new FloatMenuOption(
                            $"装上: {item.Label} (Reserved: {comp.ReservedCost})",
                            () =>
                            {
                                if (mount.TryAddComponent(comp))
                                {
                                    pawn.inventory.innerContainer.Remove(item);
                                }
                            }
                        ));
                    }
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 检查Pawn是否有Trion能力（已植入腺体）
        /// </summary>
        private bool HasTrionAbility(Pawn pawn)
        {
            if (pawn == null)
                return false;

            return pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_TrionGland", false));
        }

        /// <summary>
        /// 查找附近穿戴了TrionTrigger的Pawn
        /// 搜索范围：建筑周围5格
        /// </summary>
        private Pawn GetNearbyPawn()
        {
            var map = Map;
            if (map == null)
                return null;

            var cellsInRadius = GenRadial.RadialCellsAround(Position, 5f, useCenter: true);

            foreach (var cell in cellsInRadius)
            {
                if (!cell.InBounds(map))
                    continue;

                var pawnAtCell = cell.GetFirstPawn(map);
                if (pawnAtCell != null && pawnAtCell.apparel != null)
                {
                    // 检查是否穿戴了TrionTrigger
                    var trigger = pawnAtCell.apparel.WornApparel
                        .OfType<TrionTrigger>()
                        .FirstOrDefault();

                    if (trigger != null)
                        return pawnAtCell;
                }
            }

            return null;
        }

        // ============ 序列化 ============

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref _selectedTrigger, "selectedTrigger");
        }
    }
}
