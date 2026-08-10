using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;

namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>
    /// 一次组合解析的完整只读业务结果。
    /// </summary>
    public sealed class ChipCombinationResolution
    {
        /// <summary>解析状态。</summary>
        public ChipCombinationResolutionStatus Status { get; set; }

        /// <summary>当前动态成品名称。</summary>
        public string ResolvedLabel { get; set; }

        /// <summary>当前动态芯片配置。</summary>
        public ChipDefinitionConfig ResolvedConfig { get; set; }

        /// <summary>按玩家顺序解析出的动作。</summary>
        public IReadOnlyList<ChipActionPresetDef> Actions { get; set; }

        /// <summary>解析出的可空枪壳。</summary>
        public ChipGunShellDef GunShell { get; set; }

        /// <summary>全部稳定失败原因。</summary>
        public IReadOnlyList<ChipCombinationFailureReason> FailureReasons { get; set; }
    }
}
