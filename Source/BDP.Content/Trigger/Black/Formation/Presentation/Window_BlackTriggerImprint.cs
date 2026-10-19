using UnityEngine;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>成品黑触发器来源生命印记窗口。</summary>
    public sealed class Window_BlackTriggerImprint : Window
    {
        private readonly CompBlackTriggerImprint imprint;
        private Vector2 scroll;

        /// <summary>构造印记窗口。</summary>
        public Window_BlackTriggerImprint(CompBlackTriggerImprint imprint)
        {
            this.imprint = imprint;
            forcePause = true;
            closeOnClickedOutside = false;
        }

        /// <summary>窗口尺寸。</summary>
        public override Vector2 InitialSize { get { return new Vector2(760f, 520f); } }

        /// <summary>绘制结构化来源资料。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            if (imprint == null) return;
            Rect closeRect = new Rect(inRect.center.x - 70f, inRect.yMax - 36f, 140f, 32f);
            Rect body = new Rect(inRect.x + 12f, inRect.y + 8f, inRect.width - 24f, inRect.height - 58f);
            string text = BlackTriggerImprintFormatter.FormatFull(imprint);
            float contentHeight = Text.CalcHeight(text, body.width - 26f) + 64f;
            Rect view = new Rect(0f, 0f, body.width - 16f, System.Math.Max(body.height, contentHeight));
            Widgets.BeginScrollView(body, ref scroll, view);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, view.width, 32f), "BDP_BlackTrigger_Imprint_Title".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 40f, view.width, contentHeight - 40f), text);
            Widgets.EndScrollView();
            if (Widgets.ButtonText(closeRect, "CloseButton".Translate())) Close();
        }
    }
}
