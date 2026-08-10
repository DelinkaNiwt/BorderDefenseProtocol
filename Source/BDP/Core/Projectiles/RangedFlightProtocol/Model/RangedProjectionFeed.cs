using System.Collections.Generic;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 飞行后半段暴露给只读投影层的摘要。
    /// 它不反向控制 projectile 正式行为。
    /// </summary>
    internal sealed class RangedProjectionFeed
    {
        public string AttackInstanceId { get; set; }

        public FlightPhase FlightPhase { get; set; }

        public List<string> VisibleTrailTags { get; set; } = new List<string>();

        public List<string> VisibleImpactTags { get; set; } = new List<string>();

        public List<string> InfoProjectionTags { get; set; } = new List<string>();

        public string FinalOutcomeSummary { get; set; }
    }
}
