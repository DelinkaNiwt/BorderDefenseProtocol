using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片物品的CompProperties——XML可配置参数。
    /// </summary>
    public class CompProperties_TriggerChip : CompProperties
    {
        /// <summary>激活时一次性Trion消耗。</summary>
        public float activationCost = 0f;

        /// <summary>
        /// IChipEffect实现类（XML中填写全限定类名，如"BDP.Trigger.VerbChipEffect"）。
        /// 实例化方式：Activator.CreateInstance(chipEffectClass)，要求无参构造函数。
        /// </summary>
        public Type chipEffectClass;

        // ── v2.1 统一配置协议（T30）──

        /// <summary>
        /// Trion占用/锁定量（战斗模块读取），默认0。
        /// 战斗体激活时由HComp_TrionAllocate读取并调用CompTrion.Allocate()。
        /// </summary>
        public float allocationCost = 0f;

        /// <summary>
        /// 每天持续Trion消耗（统一层，替代各效果类自管理RegisterDrain）。
        /// 0=无持续消耗。RegisterDrain key格式：chip_{side}_{index}。
        /// </summary>
        public float drainPerDay = 0f;

        /// <summary>
        /// 激活预热时间（ticks）。切换时取 max(switchCooldown, warmup)。
        /// 0=无预热，立即激活。
        /// </summary>
        public int activationWarmup = 0;

        /// <summary>
        /// 是否为双手芯片（T31）。true时激活后锁定双侧（dualHandLockSlot），
        /// 另一侧不可独立激活/切换，直到本芯片关闭。
        /// </summary>
        public bool isDualHand = false;

        /// <summary>
        /// 最低输出功率要求（0=无要求）。
        /// CanActivate时检查：pawn.GetStatValue(TrionOutputPower) >= minOutputPower。
        /// </summary>
        public float minOutputPower = 0f;

        /// <summary>
        /// 互斥标签列表（对称检查）。
        /// 新芯片和已激活芯片的exclusionTags互相比较，有交集则不可激活。
        /// 例：["stealth"] 表示不能与其他带stealth标签的芯片同时激活。
        /// </summary>
        public List<string> exclusionTags;

        /// <summary>
        /// 关闭后摇时长（ticks），默认0（瞬间关闭）。
        /// 切换芯片时，旧芯片在后摇期间仍保持isActive=true，
        /// 后摇到期后才执行Deactivate，然后进入新芯片的前摇阶段。
        /// </summary>
        public int deactivationDelay = 0;

        /// <summary>
        /// 槽位限制类型（v3.1）。
        /// None=无限制，SpecialOnly=只能插特殊槽位，HandsOnly=只能插左右手槽位。
        /// </summary>
        public ChipSlotRestriction slotRestriction = ChipSlotRestriction.None;

        /// <summary>
        /// 芯片类别标签列表（v3.1，多维度分类）。
        /// 用于筛选、分组、批量设置等功能。一个芯片可以有多个类别标签。
        /// 推荐使用ChipCategories常量类中的标签，但也可以自定义。
        /// 例：["Weapon", "Ranged", "Energy", "Sniper", "Guided"]
        /// </summary>
        public List<string> categories;

        public CompProperties_TriggerChip()
        {
            compClass = typeof(TriggerChipComp);
        }

        // ═══════════════════════════════════════════════════════
        // 辅助方法 - 类别标签查询
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 检查芯片是否包含指定类别标签。
        /// </summary>
        public bool HasCategory(string category)
        {
            return categories != null && categories.Contains(category);
        }

        /// <summary>
        /// 检查芯片是否包含任意一个指定的类别标签。
        /// </summary>
        public bool HasAnyCategory(params string[] cats)
        {
            return categories != null && cats.Any(c => categories.Contains(c));
        }

        /// <summary>
        /// 检查芯片是否包含所有指定的类别标签。
        /// </summary>
        public bool HasAllCategories(params string[] cats)
        {
            return categories != null && cats.All(c => categories.Contains(c));
        }

        /// <summary>
        /// 获取芯片的所有类别标签（只读）。
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            return categories ?? Enumerable.Empty<string>();
        }
    }
}
