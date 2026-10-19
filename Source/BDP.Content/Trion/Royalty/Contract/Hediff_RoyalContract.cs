using Verse;

namespace BDP.Content.Trion.Royalty.Contract
{
    /// <summary>人物级永久契约；不是可遗传或可提取的基因数据。</summary>
    public sealed class Hediff_RoyalContract : Hediff
    {
        /// <summary>绑定的具体人物；存档引用支持跨地图和旅行。</summary>
        public Pawn partner;
        /// <summary>是否为发起契约的皇室方；保留一生一次记录。</summary>
        public bool royalSigner;

        /// <summary>关系不随严重程度、基因移除或对象死亡自动消失。</summary>
        public override bool ShouldRemove => false;

        /// <summary>不同人物的契约记录不能合并。</summary>
        public override bool TryMergeWith(Hediff other) => false;

        /// <summary>在原版健康栏展示契约对象，名称读取语言包。</summary>
        public override string LabelInBrackets => partner == null
            ? base.LabelInBrackets
            : "BDP_RoyalContractPartner".Translate(partner.LabelShortCap).ToString();

        /// <summary>记录独立于基因对象；复制排除和战斗体排除由内容定义声明。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref partner, "partner");
            Scribe_Values.Look(ref royalSigner, "royalSigner");
        }

        /// <summary>只监听真正死亡。原版先置死亡再通知，所以双向调用不会递归。</summary>
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            // 不转发致命伤害：另一人不是受了同一处伤，更不是一次战斗体崩解。
            if (partner != null && !partner.Dead)
                partner.Kill(null, this);
        }
    }
}
