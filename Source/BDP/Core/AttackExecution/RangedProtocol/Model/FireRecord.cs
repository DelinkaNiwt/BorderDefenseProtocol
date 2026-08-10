using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 发射阶段的正式结果。
    /// 它回答这次攻击最终要怎么展开成一个或多个 emit。
    /// </summary>
    internal sealed class FireRecord
    {
        /// <summary>
        /// 当前 Fire 阶段是否请求中止。
        /// </summary>
        public bool IsAborted { get; set; }

        /// <summary>
        /// 当前 Fire 阶段中止时写回的原因。
        /// </summary>
        public string AbortReason { get; set; }

        /// <summary>
        /// 当前 Fire 阶段裁定后的基线投射物 Def。
        /// </summary>
        public ThingDef ProjectileDef { get; set; }

        /// <summary>
        /// 当前 Fire 阶段最终展开的发射数量。
        /// </summary>
        public int FireCount { get; set; }

        /// <summary>
        /// 当前 Fire 阶段逐发展开后的正式 emit 列表。
        /// </summary>
        public List<FireEmitRecord> Emits { get; set; } = new List<FireEmitRecord>();

        /// <summary>
        /// 当前 Fire 阶段附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
