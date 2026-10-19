using System.Collections.Generic;

namespace BDP.Core.Combos
{
    /// <summary>参与组合技匹配的一枚成品芯片身份快照。</summary>
    internal sealed class ComboSourceAdmissionSnapshot
    {
        /// <summary>成品最终职业键。</summary>
        public string ProfessionKey;

        /// <summary>成品主分类键。</summary>
        public string CategoryKey;

        /// <summary>成品普通标签键集合。</summary>
        public IReadOnlyList<string> TagKeys;

        /// <summary>成品实际来源变体键。</summary>
        public string SourceVariantKey;

        /// <summary>芯片当前运行时形态键；旧调用入口未提供状态时为空。</summary>
        public string ModeKey;

        /// <summary>芯片当前运行时姿态键；没有有效姿态时为空。</summary>
        public string StanceKey;

        /// <summary>是否由当前运行时加载状态构建；旧调用入口为 false。</summary>
        public bool HasRuntimeState;
    }
}
