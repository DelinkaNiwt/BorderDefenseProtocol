using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 浮动窗口：显示触发体所有槽位状态，支持点击激活/关闭芯片。
    /// SRP：只负责UI，交互严格调用 CompTriggerBody 公开 API，不绕过状态机逻辑。
    /// v2.0变更（T23）：Main/Sub → Left/Right
    /// </summary>
    public class Window_TriggerBodySlots : Window
    {
        private readonly CompTriggerBody triggerBody;

        private static readonly Color ActiveRowColor  = new Color(0.3f, 0.6f, 0.3f, 0.3f);
        private static readonly Color EmptyRowColor   = new Color(0.15f, 0.15f, 0.15f, 0.3f);
        private static readonly Color DisabledColor   = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public Window_TriggerBodySlots(CompTriggerBody triggerBody)
        {
            this.triggerBody = triggerBody;
            doCloseX = true;
            doCloseButton = false;
            absorbInputAroundWindow = false;
            forcePause = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(380f, 260f);

        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 24f),
                $"槽位状态 — {triggerBody.parent.LabelShortCap}");

            float colW = (inRect.width - 8f) / 2f;
            float startY = 28f;

            DrawSideColumn(new Rect(0f, startY, colW, inRect.height - startY),
                SlotSide.Left, "左侧 (Left)");
            DrawSideColumn(new Rect(colW + 8f, startY, colW, inRect.height - startY),
                SlotSide.Right, "右侧 (Right)");
        }

        private void DrawSideColumn(Rect rect, SlotSide side, string title)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), title);

            var slots = side == SlotSide.Left ? triggerBody.LeftSlots : triggerBody.RightSlots;
            if (slots == null)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 22f, rect.width, 20f), "（无右侧）");
                Text.Font = GameFont.Small;
                return;
            }

            float rowH = 34f;
            for (int i = 0; i < slots.Count; i++)
                DrawSlotRow(new Rect(rect.x, rect.y + 22f + i * rowH, rect.width, rowH - 2f),
                    slots[i], side);

            Text.Font = GameFont.Small;
        }

        private void DrawSlotRow(Rect rect, ChipSlot slot, SlotSide side)
        {
            bool switching = triggerBody.IsSwitching;
            bool canClick = slot.loadedChip != null && (!switching || slot.isActive);

            Widgets.DrawBoxSolid(rect, slot.isActive ? ActiveRowColor : EmptyRowColor);

            if (!canClick)
                GUI.color = DisabledColor;

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 2f, rect.y + 8f, 18f, rect.height), $"[{slot.index}]");

            if (slot.loadedChip != null)
            {
                var iconRect = new Rect(rect.x + 22f, rect.y + 5f, 24f, 24f);
                GUI.DrawTexture(iconRect, slot.loadedChip.def.uiIcon ?? BaseContent.BadTex);

                Widgets.Label(new Rect(rect.x + 50f, rect.y + 8f, rect.width - 70f, rect.height),
                    slot.loadedChip.LabelShortCap);

                if (slot.isActive)
                {
                    GUI.color = Color.green;
                    Widgets.Label(new Rect(rect.xMax - 18f, rect.y + 8f, 16f, rect.height), "●");
                    GUI.color = canClick ? Color.white : DisabledColor;
                }

                if (canClick && Widgets.ButtonInvisible(rect))
                {
                    if (slot.isActive)
                        triggerBody.DeactivateChip(side);
                    else
                        triggerBody.ActivateChip(side, slot.index);
                }
            }
            else
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                Widgets.Label(new Rect(rect.x + 22f, rect.y + 8f, rect.width - 26f, rect.height), "空");
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}