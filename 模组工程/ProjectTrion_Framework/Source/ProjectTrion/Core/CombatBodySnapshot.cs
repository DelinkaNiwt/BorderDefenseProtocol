using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProjectTrion.Core
{
    /// <summary>
    /// 战斗体生成时的肉身快照。
    /// 用于战斗体摧毁时恢复肉身到原始状态。
    ///
    /// Snapshot of pawn's physical state when combat body is generated.
    /// Used to restore the pawn to original state when combat body is destroyed.
    /// </summary>
    public class CombatBodySnapshot : IExposable
    {
        /// <summary>
        /// 快照保存的健康数据。
        /// Physical health state (Hediffs like injuries, diseases, etc.)
        /// </summary>
        public List<Hediff> hediffs = new List<Hediff>();

        /// <summary>
        /// 快照保存的穿戴服装列表。
        /// List of worn apparel.
        /// </summary>
        public List<Apparel> apparels = new List<Apparel>();

        /// <summary>
        /// 快照保存的装备（武器）列表。
        /// List of equipped weapons/equipment.
        /// </summary>
        public List<Thing> equipment = new List<Thing>();

        /// <summary>
        /// 快照保存的背包物品。
        /// Inventory items carried by the pawn.
        /// </summary>
        public List<Thing> inventory = new List<Thing>();

        /// <summary>
        /// 快照的时间戳（游戏Tick）。
        /// Snapshot timestamp (game tick when snapshot was taken).
        /// </summary>
        public int snapshotTick;

        /// <summary>
        /// 构造函数：从Pawn创建快照。
        /// Constructor: Create snapshot from a Pawn.
        /// </summary>
        public CombatBodySnapshot()
        {
        }

        /// <summary>
        /// 从目标Pawn捕获当前物理状态。
        /// Capture physical state from target pawn.
        ///
        /// 快照包含：
        /// - 所有Hediff（健康状态）
        /// - 所有Apparel（服装）
        /// - 所有Equipment（装备）
        /// - Inventory物品
        ///
        /// 不快照：
        /// - 技能等级和经验值
        /// - 心理状态和心情
        /// - 社交关系
        /// </summary>
        public void CaptureFromPawn(Pawn pawn)
        {
            snapshotTick = Find.TickManager.TicksGame;

            // 快照健康数据
            hediffs.Clear();
            if (pawn.health?.hediffSet?.hediffs != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    hediffs.Add(hediff);
                }
            }

            // 快照穿戴服装
            apparels.Clear();
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    apparels.Add(apparel);
                }
            }

            // 快照装备
            equipment.Clear();
            if (pawn.equipment?.AllEquipmentListForReading != null)
            {
                foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                {
                    equipment.Add(eq);
                }
            }

            // 快照背包物品
            inventory.Clear();
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (var item in pawn.inventory.innerContainer)
                {
                    inventory.Add(item);
                }
            }
        }

        /// <summary>
        /// 将快照状态恢复到目标Pawn。
        /// Restore snapshot state to target pawn.
        ///
        /// 仅恢复物理状态（健康、装备、物品），不恢复心理状态。
        /// Only restores physical state, does not restore psychological state.
        /// </summary>
        public void RestoreToPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CompTrion: 尝试向空Pawn恢复快照数据");
                return;
            }

            // 恢复健康数据
            // 注意：直接操作Hediff列表可能导致不一致。
            // 应由应用层提供更安全的恢复机制。
            // 这里仅为框架示例。
            if (pawn.health != null && hediffs.Count > 0)
            {
                // 移除当前所有Hediff（除了必须保留的）
                var toRemove = new List<Hediff>();
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (!IsEssentialHediff(hediff))
                    {
                        toRemove.Add(hediff);
                    }
                }

                foreach (var hediff in toRemove)
                {
                    pawn.health.RemoveHediff(hediff);
                }

                // 添加快照中的Hediff
                foreach (var hediff in hediffs)
                {
                    if (!IsEssentialHediff(hediff))
                    {
                        pawn.health.AddHediff(hediff);
                    }
                }
            }

            // 恢复装备和物品
            RestoreApparel(pawn);
            RestoreInventory(pawn);

            // 不恢复心理状态
            // - 技能等级和经验值保留
            // - 心理状态和心情保留
            // - 社交关系保留
        }

        /// <summary>
        /// 恢复服装到Pawn。
        /// </summary>
        private void RestoreApparel(Pawn pawn)
        {
            if (pawn.apparel == null)
                return;

            // 脱下当前服装
            var currentApparel = new List<Apparel>(pawn.apparel.WornApparel);
            foreach (var apparel in currentApparel)
            {
                pawn.apparel.TryDrop(apparel);
            }

            // 穿上快照中的服装
            foreach (var apparel in apparels)
            {
                if (apparel != null && pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
                {
                    // RimWorld 1.6 兼容：Wear方法需要三个参数
                    pawn.apparel.Wear(apparel, dropReplacedApparel: true, locked: false);
                }
            }
        }

        /// <summary>
        /// 恢复背包物品到Pawn。
        /// </summary>
        private void RestoreInventory(Pawn pawn)
        {
            if (pawn.inventory == null)
                return;

            // 清空当前背包
            pawn.inventory.innerContainer.ClearAndDestroyContents();

            // 添加快照中的物品
            foreach (var item in inventory)
            {
                if (item != null)
                {
                    pawn.inventory.innerContainer.TryAdd(item, true);
                }
            }
        }

        /// <summary>
        /// 检查Hediff是否是必须保留的（不应被覆盖）。
        /// 应由应用层定义哪些Hediff应该保留。
        /// </summary>
        private bool IsEssentialHediff(Hediff hediff)
        {
            // 框架不定义哪些Hediff是必须保留的
            // 应用层应实现此方法的重写版本
            return false;
        }

        /// <summary>
        /// 序列化快照数据以保存到存档。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref snapshotTick, "snapshotTick");
            Scribe_Collections.Look(ref hediffs, "hediffs", LookMode.Deep);
            Scribe_Collections.Look(ref apparels, "apparels", LookMode.Deep);
            Scribe_Collections.Look(ref equipment, "equipment", LookMode.Deep);
            Scribe_Collections.Look(ref inventory, "inventory", LookMode.Deep);
        }
    }
}
