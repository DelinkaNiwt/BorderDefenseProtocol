namespace BDP.Core.Combos
{
    /// <summary>组合技单侧来源形态与姿态范围的只读正式契约。</summary>
    internal sealed class ComboSourceScopeContract
    {
        /// <summary>限定的运行时形态键；为空表示允许任意形态。</summary>
        public string ModeKey;

        /// <summary>限定的运行时姿态键；为空表示允许指定形态下的任意姿态。</summary>
        public string StanceKey;
    }
}
