using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>仅投影到武器配置窗口；不改变全局武器目录和排除列表。</summary>
    internal static class FlashingBladesWindowBridge
    {
        /// <summary>当前经过检查的外部接口。</summary>
        private static FlashingBladesDiscovery targets;
        /// <summary>当前只读目录。</summary>
        private static FlashingBladesCatalog catalog;
        /// <summary>独立适配器的失败处理。</summary>
        private static Action<Exception> fail;

        /// <summary>安装前绑定访问器；禁用时清除。</summary>
        internal static void Bind(FlashingBladesDiscovery discovery, FlashingBladesCatalog entries, Action<Exception> failure)
        { targets = discovery; catalog = entries; fail = failure; }

        /// <summary>沿用原窗口筛选语义，每次原列表重建后追加一次。</summary>
        internal static void FilterPostfix(object __instance)
        {
            if (targets == null) return;
            try
            {
                catalog.EnsureInitialized();
                IList rows = (IList)targets.Get(__instance, "filteredEntries");
                string package = (string)targets.Get(__instance, "selectedPackageId");
                string search = ((string)targets.Get(__instance, "searchText") ?? "").Trim().ToLowerInvariant();
                bool overridden = (bool)targets.Get(__instance, "showOverriddenOnly");
                foreach (FlashingBladesEntry entry in catalog.Entries.Values.OrderBy(value => value.Label))
                    if (Matches(entry, package, search, overridden) && !rows.Contains(entry.ForeignEntry)) rows.Add(entry.ForeignEntry);
            }
            catch (Exception error) { fail(error); }
        }

        /// <summary>虚拟条目不参与距离覆盖；只有刀光手动覆盖能满足“仅覆盖”筛选。</summary>
        private static bool Matches(FlashingBladesEntry entry, string package, string search, bool overridden)
        {
            return (string.IsNullOrEmpty(package) || string.Equals(package, entry.PackageId, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(search) || entry.SearchText.Contains(search))
                && (!overridden || (bool)targets.Invoke("IsOverridden", entry.Key));
        }

        /// <summary>仅绘制本目录项；设置菜单和持久化仍由外部模组完成。</summary>
        internal static bool RowPrefix(object __instance, Rect rect, object entry, int index)
        {
            if (targets == null || entry == null) return true;
            FlashingBladesEntry item;
            if (!catalog.Entries.TryGetValue((string)targets.Get(entry, "defName"), out item)) return true;
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            try
            {
                if (index % 2 == 0) Widgets.DrawAltRect(rect);
                if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(new Rect(rect.x + 8f, rect.y + 5f, 297f, 23f), item.Label.Truncate(297f));
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x + 8f, rect.y + 30f, 297f, 20f), item.ModName);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                string ranges = string.Join(" / ", item.Ranges.Select(value => value.ToString("0.##")));
                if (ranges.Length == 0) ranges = "BDP_FB_ParameterUnavailable".Translate().ToString();
                Widgets.Label(new Rect(rect.x + 315f, rect.y, 420f, rect.height), "BDP_FB_AttackRangeReadOnly".Translate(ranges));
                string automatic = catalog.AutomaticLabel(item);
                Widgets.Label(new Rect(rect.x + 745f, rect.y, 110f, rect.height), automatic);
                Text.Anchor = TextAnchor.UpperLeft;
                object manual;
                bool overridden = targets.TryOverride(item.Key, out manual);
                string button = overridden ? "DGJY.WeaponVfxOverride.Button.Override".Translate(targets.CategoryLabel(manual)).ToString()
                    : "DGJY.WeaponVfxOverride.Button.Automatic".Translate(automatic).ToString();
                if (Widgets.ButtonText(new Rect(rect.x + 860f, rect.y + 14f, Math.Max(120f, rect.width - 868f), 32f), button))
                    targets.Methods["VfxMenu"].Invoke(__instance, new[] { entry });
                TooltipHandler.TipRegion(rect, "BDP_FB_RowTooltip".Translate(item.Label, item.ModName, automatic, ranges));
                return false;
            }
            catch (Exception error) { fail(error); return false; }
            finally { Text.Font = font; Text.Anchor = anchor; GUI.color = color; }
        }

        /// <summary>只重建当前配置窗口的模组菜单，原始武器数加虚拟行数。</summary>
        internal static bool ModMenuPrefix(object __instance)
        {
            if (targets == null) return true;
            try
            {
                catalog.EnsureInitialized();
                Dictionary<string, Tuple<string, int>> groups = new Dictionary<string, Tuple<string, int>>(StringComparer.OrdinalIgnoreCase);
                foreach (object item in (IEnumerable)targets.Get(targets.ForeignType("DGJY_MeleeWeaponCatalog"), "AllModFilters"))
                    groups[(string)targets.Get(item, "packageId")] = Tuple.Create((string)targets.Get(item, "label"), (int)targets.Get(item, "count"));
                foreach (IGrouping<string, FlashingBladesEntry> group in catalog.Entries.Values.GroupBy(value => value.PackageId))
                {
                    Tuple<string, int> original;
                    groups.TryGetValue(group.Key, out original);
                    groups[group.Key] = Tuple.Create(original != null ? original.Item1 : group.First().ModName, (original != null ? original.Item2 : 0) + group.Count());
                }
                List<FloatMenuOption> options = new List<FloatMenuOption> {
                    new FloatMenuOption("DGJY.WeaponReachOverride.ModAll".Translate(), () => SelectMod(__instance, "", "")) };
                foreach (KeyValuePair<string, Tuple<string, int>> group in groups.OrderBy(value => value.Value.Item1))
                {
                    string package = group.Key;
                    string label = "BDP_FB_ModCount".Translate(group.Value.Item1, group.Value.Item2).ToString();
                    options.Add(new FloatMenuOption(label, () => SelectMod(__instance, package, label)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
                return false;
            }
            catch (Exception error) { fail(error); return true; }
        }

        /// <summary>与原菜单一致地切换筛选并回到列表顶部。</summary>
        private static void SelectMod(object window, string package, string label)
        {
            if (targets == null) return;
            try
            {
                targets.Set(window, "selectedPackageId", package);
                targets.Set(window, "selectedModLabel", label);
                targets.Set(window, "scrollPosition", Vector2.zero);
            }
            catch (Exception error) { fail(error); }
        }
    }
}
