using System.Collections.Generic;
using Verse;

namespace BDP.Core.Semantics
{
    /// <summary>
    /// 第一阶段轻量语义上下文实现。
    /// 当前只做数据承载，不提供流程方法。
    /// </summary>
    public sealed class SemanticContext : ISemanticContext, IExposable
    {
        /// <summary>
        /// 过程稳定标识。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 对外显示名。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 来源种类。
        /// </summary>
        public SemanticSourceKind SourceKind { get; set; } = SemanticSourceKind.Unknown;

        /// <summary>
        /// 原因标识。
        /// </summary>
        public string ReasonKey { get; set; }

        /// <summary>
        /// 发起者。
        /// </summary>
        public Thing Instigator { get; set; }

        /// <summary>
        /// 可写标签集合。
        /// 对外仍通过只读接口暴露。
        /// </summary>
        public List<string> MutableTags { get; set; } = new List<string>();

        /// <summary>
        /// 可写扩展信息集合。
        /// 对外仍通过只读接口暴露。
        /// </summary>
        public Dictionary<string, string> MutableExtraData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 对外只读标签视图。
        /// </summary>
        IReadOnlyList<string> ISemanticContext.Tags
        {
            get { return MutableTags; }
        }

        /// <summary>
        /// 对外只读扩展信息视图。
        /// </summary>
        IReadOnlyDictionary<string, string> ISemanticContext.ExtraData
        {
            get { return MutableExtraData; }
        }

        /// <summary>
        /// 统一序列化当前语义上下文。
        /// </summary>
        public void ExposeData()
        {
            string id = Id;
            string displayLabel = DisplayLabel;
            SemanticSourceKind sourceKind = SourceKind;
            string reasonKey = ReasonKey;
            Thing instigator = Instigator;
            List<string> mutableTags = MutableTags;
            Dictionary<string, string> mutableExtraData = MutableExtraData;

            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref displayLabel, "displayLabel");
            Scribe_Values.Look(ref sourceKind, "sourceKind", SemanticSourceKind.Unknown);
            Scribe_Values.Look(ref reasonKey, "reasonKey");
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_Collections.Look(ref mutableTags, "tags", LookMode.Value);
            Scribe_Collections.Look(ref mutableExtraData, "extraData", LookMode.Value, LookMode.Value);

            Id = id;
            DisplayLabel = displayLabel;
            SourceKind = sourceKind;
            ReasonKey = reasonKey;
            Instigator = instigator;
            MutableTags = mutableTags ?? new List<string>();
            MutableExtraData = mutableExtraData ?? new Dictionary<string, string>();
        }
    }
}
