using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>匹配完成后向玩家概括候选战斗用途的侧重定义。</summary>
    public sealed class BlackTriggerEmphasisDef : Def
    {
        /// <summary>形成选择卡上的一句话用途说明语言键。</summary>
        public string summaryKey;

        /// <summary>生命印记中使用的完整说明语言键。</summary>
        public string detailKey;
    }
}
