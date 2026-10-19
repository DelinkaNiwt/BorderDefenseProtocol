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

        /// <summary>成品记录确定的最终职业 DefName。</summary>
        public string SourceProfessionKey { get; set; }

        /// <summary>当前动态芯片配置。</summary>
        public ChipDefinitionConfig ResolvedConfig { get; set; }

        /// <summary>应用武装构型前的有效基础配置，用于信息来源链追踪。</summary>
        public ChipDefinitionConfig DeclaredConfig { get; set; }

        /// <summary>按玩家顺序解析出的芯片功能定义。</summary>
        public IReadOnlyList<ChipManufacturingPresetDef> FunctionDefs { get; set; }

        /// <summary>解析出的可空有效武装型；可能是隐藏默认型。</summary>
        public ChipArmamentFormDef ArmamentForm { get; set; }

        /// <summary>全部稳定失败原因。</summary>
        public IReadOnlyList<ChipCombinationFailureReason> FailureReasons { get; set; }
    }
}
