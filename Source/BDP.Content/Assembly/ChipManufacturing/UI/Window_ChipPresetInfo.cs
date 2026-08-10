using UnityEngine;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>按需显示动作或枪壳说明，不占用制造窗口固定区域。</summary>
    public sealed class Window_ChipPresetInfo : Window
    {
        /// <summary>当前要说明的 Def。</summary>
        private readonly Def preset;

        /// <summary>说明文本滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>建立一张预设信息卡。</summary>
        public Window_ChipPresetInfo(Def preset)
        {
            this.preset = preset;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
        }

        /// <summary>信息卡固定为紧凑尺寸。</summary>
        public override Vector2 InitialSize => new Vector2(520f, 420f);

        /// <summary>绘制名称与可滚动说明。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 36f), preset?.LabelCap ?? "");
            Text.Font = GameFont.Small;

            string description = preset != null && !preset.description.NullOrEmpty()
                ? preset.description
                : "BDP_ChipManufacturing_NoDescription".Translate().ToString();
            Rect outRect = new Rect(0f, 48f, inRect.width, inRect.height - 48f);
            float viewHeight = Mathf.Max(outRect.height, Text.CalcHeight(description, outRect.width - 20f));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Widgets.Label(viewRect, description);
            Widgets.EndScrollView();
        }
    }
}
