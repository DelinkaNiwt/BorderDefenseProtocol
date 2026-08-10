using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>一轮页签打开期间保存不同分类/职业路径的全部草稿。</summary>
    public sealed class ChipManufacturingEditorState
    {
        /// <summary>每条分类/职业路径自己的草稿。</summary>
        private readonly Dictionary<ChipManufacturingDraftKey, ChipManufacturingDraft> drafts =
            new Dictionary<ChipManufacturingDraftKey, ChipManufacturingDraft>();

        /// <summary>当前主分类。</summary>
        public ChipCategoryDef CurrentCategory { get; private set; }

        /// <summary>当前可空职业。</summary>
        public ChipProfessionDef CurrentProfession { get; private set; }

        /// <summary>当前路径草稿。</summary>
        public ChipManufacturingDraft CurrentDraft { get; private set; }

        /// <summary>切换路径并恢复已有草稿；首次进入时建立空草稿。</summary>
        public ChipManufacturingDraft Switch(
            ChipCategoryDef category,
            ChipProfessionDef profession)
        {
            CurrentCategory = category;
            CurrentProfession = profession;
            ChipManufacturingDraftKey key = new ChipManufacturingDraftKey(
                category?.defName,
                profession?.defName);
            if (!drafts.TryGetValue(key, out ChipManufacturingDraft draft))
            {
                draft = new ChipManufacturingDraft(key);
                drafts.Add(key, draft);
            }

            CurrentDraft = draft;
            return draft;
        }

        /// <summary>关闭页签时清空整轮编辑会话。</summary>
        public void Clear()
        {
            drafts.Clear();
            CurrentCategory = null;
            CurrentProfession = null;
            CurrentDraft = null;
        }
    }
}
