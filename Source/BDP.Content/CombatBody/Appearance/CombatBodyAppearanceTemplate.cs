using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 一个 Pawn 的个人服装模板。
    /// </summary>
    public sealed class CombatBodyAppearanceTemplate : IExposable
    {
        /// <summary>
        /// 档案内稳定不复用的模板编号。
        /// </summary>
        public int TemplateId;

        /// <summary>
        /// 玩家可见的模板名称。
        /// </summary>
        public string Name;

        /// <summary>
        /// 模板中的服装外观记录；空集合代表合法空模板。
        /// </summary>
        public List<CombatBodyAppearanceTemplateEntry> Entries =
            new List<CombatBodyAppearanceTemplateEntry>();

        /// <summary>
        /// 当前模板是否没有服装条目。
        /// </summary>
        public bool IsEmpty
        {
            get { return Entries == null || Entries.Count == 0; }
        }

        /// <summary>
        /// 保存或读取模板字段。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref TemplateId, "templateId", 0);
            Scribe_Values.Look(ref Name, "name");
            Scribe_Collections.Look(ref Entries, "entries", LookMode.Deep);
            if (Entries == null)
            {
                Entries = new List<CombatBodyAppearanceTemplateEntry>();
            }
        }
    }
}
