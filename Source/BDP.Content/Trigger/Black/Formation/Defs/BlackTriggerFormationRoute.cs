using System.Collections.Generic;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>一件完整黑触发器自行声明的一条形成路线。</summary>
    public sealed class BlackTriggerFormationRoute
    {
        /// <summary>可选的开发诊断标识；留空时由触发器 Def 名和路线序号生成。</summary>
        public string routeId;

        /// <summary>路线通过后允许分配的唯一战斗侧重。</summary>
        public BlackTriggerEmphasisDef emphasis;

        /// <summary>必须全部存在于来源快照中的事实。</summary>
        public List<BlackTriggerFactReference> allOf = new List<BlackTriggerFactReference>();

        /// <summary>至少命中指定数量的事实。</summary>
        public List<BlackTriggerFactReference> anyOf = new List<BlackTriggerFactReference>();

        /// <summary>anyOf 至少命中的数量；未填写时按一项处理。</summary>
        public int anyOfMinimum = 1;

        /// <summary>来源快照中不得存在的事实。</summary>
        public List<BlackTriggerFactReference> noneOf = new List<BlackTriggerFactReference>();
    }
}
