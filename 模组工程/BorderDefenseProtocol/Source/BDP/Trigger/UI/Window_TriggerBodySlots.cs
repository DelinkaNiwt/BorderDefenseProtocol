using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 浮动窗口：显示触发体所有槽位状态，支持点击激活/关闭芯片。
    /// SRP：只负责UI，交互严格调用 CompTriggerBody 公开 API，不绕过状态机逻辑。
    /// v2.0变更（T23）：Main/Sub → Left/Right
    /// v2.1.1变更：specialSlotCount>0时三列布局（左手/右手/特殊槽），特殊槽只读预览；
    ///             specialSlotCount=0时两列布局（左手/右手），窗口宽度自适应。
    /// v3.0变更：Left/Right → LeftHand/RightHand，中文"左侧/右侧/特殊侧"→"左手槽/右手槽/特殊槽"
    /// </summary>
    public class Window_TriggerBodySlots : Window
    {
        private readonly CompTriggerBody triggerBody;

        private static readonly Color ActiveRowColor  = new Color(0.3f, 0.6f, 0.3f, 0.3f);
        private static readonly Color EmptyRowColor   = new Color(0.15f, 0.15f, 0.15f, 0.3f);
        private static readonly Color DisabledColor   = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Color SpecialRowColor = new Color(0.2f, 0.4f, 0.6f, 0.3f);
        // v3.1：四态视觉——挂载未注册（暗黄）、注册未激活（暗蓝）
        private static readonly Color LoadedUnregisteredColor = new Color(0.5f, 0.45f, 0.2f, 0.3f);
        private static readonly Color RegisteredInactiveColor = new Color(0.2f, 0.3f, 0.5f, 0.3f);

        public Window_TriggerBodySlots(CompTriggerBody triggerBody)
        {
            this.triggerBody = triggerBody;
            doCloseX = true;
            doCloseButton = false;
            absorbInputAroundWindow = false;
            forcePause = false;
            resizeable = false;
            draggable = true;
            closeOnClickedOutside = false;
        }

        private bool HasSpecialSlots => triggerBody.Props.specialSlotCount > 0;

        // 有特殊槽时三列560px，无特殊槽时两列380px
        public override Vector2 InitialSize
            => new Vector2(HasSpecialSlots ? 560f : 380f, 260f);

        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 24f),
                $"槽位状态 — {triggerBody.parent.LabelShortCap}");

            float gap = 8f;
            float startY = 28f;

            if (HasSpecialSlots)
            {
                // 三列布局：左手 / 右手 / 特殊槽（只读）
                float specialColW = 160f;
                float lrColW = (inRect.width - specialColW - gap * 2f) / 2f;
                DrawSideColumn(new Rect(0f, startY, lrColW, inRect.height - startY),
                    SlotSide.LeftHand, "左手槽 (Left Hand)", editable: true);
                DrawSideColumn(new Rect(lrColW + gap, startY, lrColW, inRect.height - startY),
                    SlotSide.RightHand, "右手槽 (Right Hand)", editable: true);
                DrawSideColumn(new Rect(lrColW * 2f + gap * 2f, startY, specialColW, inRect.height - startY),
                    SlotSide.Special, "特殊槽 (Special)", editable: false);
            }
            else
            {
                // 两列布局：左手 / 右手
                float colW = (inRect.width - gap) / 2f;
                DrawSideColumn(new Rect(0f, startY, colW, inRect.height - startY),
                    SlotSide.LeftHand, "左手槽 (Left Hand)", editable: true);
                DrawSideColumn(new Rect(colW + gap, startY, colW, inRect.height - startY),
                    SlotSide.RightHand, "右手槽 (Right Hand)", editable: true);
            }
        }

        private void DrawSideColumn(Rect rect, SlotSide side, string title, bool editable)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), title);

            var slots = side == SlotSide.LeftHand ? triggerBody.LeftHandSlots
                      : side == SlotSide.RightHand ? triggerBody.RightHandSlots
                      : triggerBody.SpecialSlots;
            if (slots == null)
            {
                // 右手槽无槽（hasRightHand=false）时提示；特殊槽无槽时静默（空列）
                if (side == SlotSide.RightHand)
                    Widgets.Label(new Rect(rect.x, rect.y + 22f, rect.width, 20f), "（无右手槽）");
                Text.Font = GameFont.Small;
                return;
            }

            float rowH = 34f;
            for (int i = 0; i < slots.Count; i++)
                DrawSlotRow(new Rect(rect.x, rect.y + 22f + i * rowH, rect.width, rowH - 2f),
                    slots[i], side, editable);

            Text.Font = GameFont.Small;
        }

        private void DrawSlotRow(Rect rect, ChipSlot slot, SlotSide side, bool editable)
        {
            // v6.0修复：按侧独立检查切换状态（旧代码用IsSwitching检查全局，导致一侧切换时另一侧也被禁用）
            bool switching = triggerBody.IsSideSwitching(side);
            // 切换/后摇中该侧所有槽位不可交互
            bool canClick = editable && slot.loadedChip != null && !switching;

            // v3.1：四态行颜色判定
            //   激活 → ActiveRowColor/SpecialRowColor
            //   有芯片 + 战斗体激活 → RegisteredInactiveColor（注册未激活）
            //   有芯片 + 战斗体未激活 → LoadedUnregisteredColor（挂载未注册）
            //   空 → EmptyRowColor
            Color rowColor;
            if (slot.isActive)
                rowColor = side == SlotSide.Special ? SpecialRowColor : ActiveRowColor;
            else if (slot.loadedChip != null && triggerBody.IsCombatBodyActive)
                rowColor = RegisteredInactiveColor;
            else if (slot.loadedChip != null)
                rowColor = LoadedUnregisteredColor;
            else
                rowColor = EmptyRowColor;
            Widgets.DrawBoxSolid(rect, rowColor);

            if (!canClick)
                GUI.color = DisabledColor;

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 2f, rect.y + 8f, 18f, rect.height), $"[{slot.index}]");

            if (slot.loadedChip != null)
            {
                var iconRect = new Rect(rect.x + 22f, rect.y + 5f, 24f, 24f);
                // v3.1：挂载未注册时图标50%透明
                if (!slot.isActive && !triggerBody.IsCombatBodyActive)
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(iconRect, slot.loadedChip.def.uiIcon ?? BaseContent.BadTex);
                GUI.color = canClick ? Color.white : DisabledColor;

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
