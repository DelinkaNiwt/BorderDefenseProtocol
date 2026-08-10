using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 远程攻击前半段对外暴露的只读投影视图。
    /// 它只服务视觉与信息层，不反向主导主协议。
    /// </summary>
    internal sealed class RangedProjectionSeed
    {
        public string AttackInstanceId { get; set; }

        public LocalTargetInfo MainTarget { get; set; }

        public VerbAttackRole AttackRole { get; set; }

        public string SourceResultId { get; set; }

        public int FireCount { get; set; }

        public List<string> AimTags { get; set; } = new List<string>();

        public List<string> FireTags { get; set; } = new List<string>();

        public List<string> VisualHintTags { get; set; } = new List<string>();

        public List<string> InfoProjectionTags { get; set; } = new List<string>();
    }
}
