using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody字段声明（partial class）
    /// v13.1：添加静态构造函数订阅伤害事件
    /// </summary>
    [StaticConstructorOnStartup]
    public partial class CompTriggerBody
    {
        // 静态构造函数：订阅全局伤害事件（v13.1）
        static CompTriggerBody()
        {
            BDPEvents.OnDamageReceived += OnDamageReceivedGlobal;
        }

        /// <summary>
        /// 全局伤害事件处理器（静态，v13.1）
        /// </summary>
        private static void OnDamageReceivedGlobal(DamageReceivedEventArgs args)
        {
            if (args?.Pawn?.equipment?.Primary == null) return;

            // 查找触发体Comp
            var triggerComp = args.Pawn.equipment.Primary.GetComp<CompTriggerBody>();
            triggerComp?.CheckHandIntegrity(args.Pawn);
        }
        // ── 槽位数据（v2.0：mainSlots/subSlots → leftHandSlots/rightHandSlots） ──
        private List<ChipSlot> leftHandSlots;
        private List<ChipSlot> rightHandSlots;
        // v2.1（T29）：特殊槽，全部同时激活/关闭，不参与切换状态机
        private List<ChipSlot> specialSlots;

        // ── 切换状态机（v6.0：按侧独立，null=Idle） ──
        private SwitchContext leftSwitchCtx;
        private SwitchContext rightSwitchCtx;

        // v2.1（T31）：双手锁定槽位（非null=有双手芯片激活，另一侧被锁定）
        private ChipSlot dualHandLockSlot;

        // ── 按侧Verb存储（v2.0 T24：替代单一ActiveVerbProperties/ActiveTools） ──
        // 由VerbChipEffect通过SetSideVerbs设置，DualVerbCompositor合成最终结果
        private List<VerbProperties> leftHandActiveVerbProps;
        private List<Tool> leftHandActiveTools;
        private List<VerbProperties> rightHandActiveVerbProps;
        private List<Tool> rightHandActiveTools;

        // ── v5.0新增：Verb引用缓存（不序列化，RebuildVerbs后重建） ──
        // 供CompGetEquippedGizmosExtra读取，生成Command_BDPChipAttack
        private Verb leftHandAttackVerb;    // 左手芯片独立攻击Verb实例
        private Verb rightHandAttackVerb;   // 右手芯片独立攻击Verb实例
        private Verb dualAttackVerb;        // 双手触发合成Verb实例

        // ── v6.1新增：齐射Verb缓存（不序列化，CreateAndCacheChipVerbs后重建） ──
        // ── v8.0变更：重命名为SecondaryVerb，支持任意副攻击模式（不限于齐射） ──
        private Verb leftHandSecondaryVerb;    // 左手芯片副攻击Verb实例（右键）
        private Verb rightHandSecondaryVerb;   // 右手芯片副攻击Verb实例（右键）
        private Verb dualSecondaryVerb;        // 双手副攻击Verb实例（右键）

        // ── v10.0新增：组合技Verb缓存（不序列化，CreateAndCacheChipVerbs后重建） ──
        private Verb comboAttackVerb;       // 组合技攻击Verb实例
        private Verb comboSecondaryVerb;    // 组合技副攻击Verb实例
        private ComboVerbDef matchedComboDef; // 匹配到的组合技定义（Gizmo用）

        /// <summary>
        /// 芯片Verb序列化列表（v8.0 PMS重构）。
        /// 存档时收集所有芯片Verb，读档时反序列化并注册到LoadedObjectDirectory，
        /// 使Job/Stance在ResolvingCrossRefs阶段能通过loadID找到芯片Verb。
        /// RebuildVerbs时通过loadID匹配复用已反序列化的实例。
        /// </summary>
        private List<Verb> savedChipVerbs;

        // ── v14.0新增：ProxyVerb自动攻击支持 ──

        /// <summary>
        /// 代理Verb实例（v14.0自动攻击）。
        /// 不序列化，RebuildVerbs时重建。供Patch_Pawn_TryGetAttackVerb读取。
        /// </summary>
        private Verb_BDPProxy proxyVerb;

        /// <summary>
        /// 上次芯片Verb tick的游戏tick（v14.0）。
        /// 防止同一tick内double-tick（JobDriver手动tick + Patch_VerbTracker_VerbsTick）。
        /// </summary>
        private int lastChipVerbTickedTick = -1;

        /// <summary>
        /// 已授予的组合能力列表（v10.0）。
        /// 用于跟踪当前激活的组合技能力，在芯片切换时撤销。
        /// </summary>
        private readonly List<ComboAbilityDef> grantedCombos = new List<ComboAbilityDef>();

        /// <summary>
        /// 当前正在激活/关闭的槽位侧别（临时上下文）。
        /// 在DoActivate/DeactivateSlot中设置，供VerbChipEffect等效果类读取自己所在侧。
        /// 调用effect.Activate/Deactivate前设置，调用后清除。
        /// </summary>
        internal SlotSide? ActivatingSide { get; private set; }

        /// <summary>
        /// 当前正在激活/关闭的槽位引用（临时上下文）。
        /// 在DoActivate/DeactivateSlot中与ActivatingSide同步设置/清除。
        /// 供Effect类通过此属性直接读取当前操作槽位的芯片DefModExtension。
        /// </summary>
        internal ChipSlot ActivatingSlot { get; private set; }

        /// <summary>
        /// 战斗体是否处于激活状态（由战斗模块控制）。
        /// true=已生成战斗体（芯片已Allocate），false=未生成。
        /// 影响UI四态显示：挂载未注册 vs 注册未激活。
        /// </summary>
        internal bool IsCombatBodyActive { get; private set; }

        // ── 便利属性 ──
        private Pawn OwnerPawn => Holder;
        private CompTrion TrionComp => OwnerPawn?.GetComp<CompTrion>();
    }
}
