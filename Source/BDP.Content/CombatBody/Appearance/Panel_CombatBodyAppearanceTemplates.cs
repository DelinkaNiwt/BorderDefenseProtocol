using System;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 战斗体编辑台左侧模板列表与右侧模板详情面板。
    /// </summary>
    public sealed class Panel_CombatBodyAppearanceTemplates
    {
        /// <summary>模板列表滚动位置。</summary>
        private Vector2 templateScroll;

        /// <summary>模板详情滚动位置。</summary>
        private Vector2 detailScroll;

        /// <summary>绘制左侧模板列表和管理按钮。</summary>
        public void DrawTemplateList(
            Rect left,
            CombatBodyAppearanceProfile profile,
            int selectedTemplateId,
            Action<int> onSelected,
            Action onNew,
            Action onRename,
            Action onDelete)
        {
            Widgets.DrawMenuSection(left);
            Rect inner = left.ContractedBy(8f);
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, 28f),
                "BDP_Window_CombatBodyAppearance_Templates".Translate());

            Rect listRect = new Rect(inner.x, inner.y + 32f, inner.width, inner.height - 132f);
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(1f, listRect.width - 16f),
                Mathf.Max(listRect.height, (profile?.Templates?.Count ?? 0) * 34f));
            Widgets.BeginScrollView(listRect, ref templateScroll, viewRect);
            if (profile?.Templates != null)
            {
                for (int index = 0; index < profile.Templates.Count; index++)
                {
                    CombatBodyAppearanceTemplate template = profile.Templates[index];
                    if (template == null)
                    {
                        continue;
                    }

                    Rect row = new Rect(0f, index * 34f, viewRect.width, 30f);
                    if (template.TemplateId == selectedTemplateId)
                    {
                        Widgets.DrawHighlightSelected(row);
                    }
                    else if (Mouse.IsOver(row))
                    {
                        Widgets.DrawHighlight(row);
                    }

                    Widgets.Label(
                        row,
                        (template.Name ?? string.Empty)
                        + "  "
                        + "BDP_Window_CombatBodyAppearance_ApparelCount"
                            .Translate(template.Entries?.Count ?? 0));
                    if (Widgets.ButtonInvisible(row))
                    {
                        onSelected?.Invoke(template.TemplateId);
                    }
                }
            }

            Widgets.EndScrollView();
            float buttonY = inner.yMax - 88f;
            if (Widgets.ButtonText(new Rect(inner.x, buttonY, inner.width, 24f),
                "BDP_Window_CombatBodyAppearance_NewTemplate".Translate()))
            {
                onNew?.Invoke();
            }

            float halfWidth = (inner.width - 6f) / 2f;
            if (Widgets.ButtonText(new Rect(inner.x, buttonY + 30f, halfWidth, 24f),
                "BDP_Window_CombatBodyAppearance_Rename".Translate()))
            {
                onRename?.Invoke();
            }

            if (Widgets.ButtonText(new Rect(inner.x + halfWidth + 6f, buttonY + 30f, halfWidth, 24f),
                "BDP_Window_CombatBodyAppearance_Delete".Translate()))
            {
                onDelete?.Invoke();
            }
        }

        /// <summary>绘制右侧选中模板详情和服装字段。</summary>
        public void DrawTemplateDetail(
            Rect right,
            CombatBodyAppearanceProfile profile,
            int selectedTemplateId)
        {
            Widgets.DrawMenuSection(right);
            Rect inner = right.ContractedBy(8f);
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, 28f),
                "BDP_Window_CombatBodyAppearance_Details".Translate());

            CombatBodyAppearanceTemplate template = profile?.Templates == null
                ? null
                : FindTemplate(profile, selectedTemplateId);
            if (template == null)
            {
                Widgets.Label(
                    new Rect(inner.x, inner.y + 36f, inner.width, 28f),
                    "BDP_Window_CombatBodyAppearance_NoTemplate".Translate());
                return;
            }

            Rect outRect = new Rect(inner.x, inner.y + 34f, inner.width, inner.height - 34f);
            float contentHeight = 60f + (template.Entries?.Count ?? 0) * 96f;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, outRect.width - 16f),
                Mathf.Max(outRect.height, contentHeight));
            Widgets.BeginScrollView(outRect, ref detailScroll, viewRect);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, 28f), template.Name ?? string.Empty);
            Widgets.Label(
                new Rect(0f, 30f, viewRect.width, 24f),
                "BDP_Window_CombatBodyAppearance_ApparelCount"
                    .Translate(template.Entries?.Count ?? 0));

            if (template.Entries != null)
            {
                for (int index = 0; index < template.Entries.Count; index++)
                {
                    CombatBodyAppearanceTemplateEntry entry = template.Entries[index];
                    Rect row = new Rect(0f, 60f + index * 96f, viewRect.width, 90f);
                    Widgets.DrawBoxSolid(row, new Color(0.08f, 0.10f, 0.12f, 0.7f));
                    Widgets.Label(
                        new Rect(row.x + 8f, row.y + 6f, row.width - 16f, 20f),
                        entry?.ApparelDef?.LabelCap
                        ?? "BDP_Window_CombatBodyAppearance_InvalidEntry".Translate());
                    Widgets.Label(
                        new Rect(row.x + 8f, row.y + 28f, row.width - 16f, 18f),
                        "BDP_Window_CombatBodyAppearance_Stuff"
                            .Translate(entry?.StuffDef?.LabelCap
                                ?? "BDP_Window_CombatBodyAppearance_None".Translate().ToString()));
                    Widgets.Label(
                        new Rect(row.x + 8f, row.y + 48f, row.width - 16f, 18f),
                        "BDP_Window_CombatBodyAppearance_Style"
                            .Translate(entry?.StyleDef?.LabelCap
                                ?? "BDP_Window_CombatBodyAppearance_None".Translate().ToString()));
                    Widgets.Label(
                        new Rect(row.x + 8f, row.y + 68f, row.width - 16f, 18f),
                        "BDP_Window_CombatBodyAppearance_Color"
                            .Translate(entry != null && entry.HasColor
                                ? entry.Color.ToString()
                                : "BDP_Window_CombatBodyAppearance_None".Translate().ToString()));
                }
            }

            Widgets.EndScrollView();
        }

        /// <summary>按稳定模板编号查找模板。</summary>
        private static CombatBodyAppearanceTemplate FindTemplate(
            CombatBodyAppearanceProfile profile,
            int templateId)
        {
            return profile.TryGetTemplate(templateId, out CombatBodyAppearanceTemplate template)
                ? template
                : null;
        }
    }
}
