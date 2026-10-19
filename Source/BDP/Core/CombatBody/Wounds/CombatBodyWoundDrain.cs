using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>一条真实伤口的派生泄露登记，保存引用、速率与绝对期限。</summary>
    internal sealed class CombatBodyWoundDrain : IExposable
    {
        /// <summary>原版最终保留的伤口，不保存合并临时实例。</summary>
        public Hediff wound;
        /// <summary>已登记的每秒速率，自然愈合与读档不重算。</summary>
        public float drainPerSecond;
        /// <summary>本伤口独立的绝对到期刻。</summary>
        public int expiryTick;

        /// <summary>引用由原版健康存档解析，不重复保存伤口本体。</summary>
        public void ExposeData()
        {
            Scribe_References.Look(ref wound, "wound");
            Scribe_Values.Look(ref drainPerSecond, "drainPerSecond", 0f);
            Scribe_Values.Look(ref expiryTick, "expiryTick", 0);
        }
    }
}
