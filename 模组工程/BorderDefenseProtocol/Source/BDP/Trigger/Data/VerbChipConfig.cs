using System.Collections.Generic;
using BDP.Trigger.ShotPipeline;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Verb芯片的DefModExtension配置。
    /// 原因（T36）：武器数据不能放ThingDef.Verbs/tools，否则IsWeapon=true。
    /// Fix-9：从Verb_BDPMelee.cs底部提取到独立文件，便于维护。
    /// v8.0变更：新增primaryVerbProps/secondaryVerbProps，废弃verbProperties/supportsVolley。
    /// v9.0变更：统一架构重构，所有芯片必须提供primaryVerbProps，配置按功能域分组。
    /// </summary>
    public class VerbChipConfig : DefModExtension
    {
        // ═══════════════════════════════════════════════════════
        // 核心Verb配置
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 主攻击Verb配置（左键）。
        /// 所有芯片（远程/近战）都必须提供此配置。
        /// null时芯片无法激活攻击。
        /// </summary>
        public VerbProperties primaryVerbProps;

        /// <summary>
        /// 主攻击的发射模式（默认Sequential）。
        /// Sequential：逐发模式，由引擎burst机制驱动，弹间有间隔。
        /// Simultaneous：齐射模式，单次TryCastShot内循环瞬发所有子弹。
        /// </summary>
        public FiringPattern primaryFiringPattern = FiringPattern.Sequential;

        /// <summary>
        /// 副攻击Verb配置（右键，可选）。
        /// null时右键走默认行为（取消）。
        /// </summary>
        public VerbProperties secondaryVerbProps;

        /// <summary>
        /// 副攻击的发射模式（默认Sequential）。
        /// 仅当secondaryVerbProps非null时生效。
        /// </summary>
        public FiringPattern secondaryFiringPattern = FiringPattern.Sequential;

        // ═══════════════════════════════════════════════════════
        // 功能域配置（分组）
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 近战配置（tools等）。
        /// 仅近战芯片需要提供。
        /// null时表示非近战芯片。
        /// </summary>
        public MeleeConfig melee;

        /// <summary>
        /// 远程配置（齐射散布、引导飞行、穿透等）。
        /// 仅远程芯片需要提供。
        /// null时表示非远程芯片。
        /// </summary>
        public RangedConfig ranged;

        /// <summary>
        /// 范围指示器配置（可选）。
        /// 用于在瞄准阶段显示武器/能力的影响范围。
        /// null时不显示范围指示器。
        /// </summary>
        public AreaIndicatorConfig areaIndicator;

        /// <summary>
        /// 瞄准阶段管线模块配置（XML注入）。
        /// 用于在瞄准阶段执行自定义逻辑（如弹道预测、目标筛选等）。
        /// null或空列表时不执行瞄准管线。
        /// </summary>
        public List<ShotModuleConfig> aimModules;

        /// <summary>
        /// 射击阶段管线模块配置（XML注入）。
        /// 用于在射击阶段执行自定义逻辑（如弹道修正、特效生成等）。
        /// null或空列表时不执行射击管线。
        /// </summary>
        public List<ShotModuleConfig> fireModules;

        // ═══════════════════════════════════════════════════════
        // 辅助方法
        // ═══════════════════════════════════════════════════════

        /// <summary>获取主攻击verb的burstShotCount。</summary>
        public int GetPrimaryBurstCount()
        {
            return primaryVerbProps?.burstShotCount ?? 1;
        }

        /// <summary>获取主攻击verb的投射物。</summary>
        public ThingDef GetPrimaryProjectileDef()
        {
            return primaryVerbProps?.defaultProjectile;
        }
    }
}
