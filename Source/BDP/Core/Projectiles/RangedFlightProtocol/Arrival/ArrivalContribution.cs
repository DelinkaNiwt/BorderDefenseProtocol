using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Arrival
{
    /// <summary>
    /// Arrival 阶段模块贡献。
    /// 它只允许模块影响“继续飞还是进入 hit”的结论。
    /// </summary>
    public sealed class ArrivalContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前局部贡献是否显式接管“本段之后是否继续飞行”的裁定。
        /// 为 `false` 时，宿主继续沿用聚合前的默认结论。
        /// </summary>
        public bool HasOverrideContinueFlight { get; set; }

        /// <summary>
        /// <summary>
        /// 当前局部贡献给出的“本段之后继续飞行”结论。
        /// 只有 `HasOverrideContinueFlight（已接管继续飞行裁定）` 为 `true` 时它才生效。
        /// </summary>
        public bool OverrideContinueFlight { get; set; }

        /// <summary>
        /// <summary>
        /// 当前局部贡献是否显式接管下一段目的地。
        /// 这里的“目的地”只表达几何落点，不直接表达命中语义。
        /// </summary>
        public bool HasNextDestination { get; set; }

        /// <summary>
        /// <summary>
        /// 当前局部贡献给出的下一段目的地。
        /// 它服务后续飞行路径推进，不等同于正式语义目标。
        /// </summary>
        public Vector3 NextDestination { get; set; }

        /// <summary>
        /// <summary>
        /// 当前局部贡献是否显式接管下一段正式目标。
        /// 这里的目标是后续阶段消费的目标位，不负责回写整次攻击的最终确认。
        /// </summary>
        public bool HasNextTarget { get; set; }

        /// <summary>
        /// <summary>
        /// 当前局部贡献给出的下一段正式目标。
        /// 它通常和 `NextDestination（下一段目的地）` 配套，但两者仍保持分离，避免把导航与语义混成一值。
        /// </summary>
        public LocalTargetInfo NextTarget { get; set; }

        /// <summary>
        /// 褰撳墠灞€閮ㄨ础鐚槸鍚︽樉寮忔帴绠′笅涓€娈?vanilla 鍛戒腑缁戝畾鐩爣銆?
        /// </summary>
        public bool HasNextBindingTarget { get; set; }

        /// <summary>
        /// 褰撳墠灞€閮ㄨ础鐚粰鍑虹殑涓嬩竴娈?vanilla 鍛戒腑缁戝畾鐩爣銆?
        /// </summary>
        public LocalTargetInfo NextBindingTarget { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式接管下一段飞行路径快照。
        /// </summary>
        public bool HasNextFlightPathSnapshot { get; set; }

        /// <summary>
        /// 当前局部贡献给出的下一段飞行路径快照。
        /// 宿主后续只消费它的路径几何，不消费业务意图。
        /// </summary>
        public ProjectileFlightPathSnapshot NextFlightPathSnapshot { get; set; }

        /// <summary>
        /// 当前局部贡献附带的标签集合。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
