using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 内联状态Gizmo：常驻显示左/右侧激活芯片图标。
    /// SRP：只负责状态显示，不持有业务逻辑。
    /// 点击打开 Window_TriggerBodySlots。
    /// v2.0变更（T23）：Main/Sub → Left/Right
    /// </summary>
    public class Gizmo_TriggerBodyStatus : Gizmo
    {
        private readonly CompTriggerBody triggerBody;

        private static readonly Color EmptySlotColor  = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        private static readonly Color ActiveBorderColor = new Color(0.4f, 0.8f, 0.4f, 1f);

        public Gizmo_TriggerBodyStatus(CompTriggerBody triggerBody)
        {
            this.triggerBody = triggerBody;
            Order = -99f; // 排在调试按钮前面
        }

        public override float GetWidth(float maxWidth) => 75f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            // 左侧行
            DrawSideRow(new Rect(rect.x + 4f, rect.y + 6f, rect.width - 8f, 28f),
                SlotSide.Left, "左");
            // 右侧行
            DrawSideRow(new Rect(rect.x + 4f, rect.y + 38f, rect.width - 8f, 28f),
                SlotSide.Right, "右");

            // 切换进度条（仅切换中时显示）
            if (triggerBody.IsSwitching)
            {
                var barRect = new Rect(rect.x + 4f, rect.y + 68f, rect.width - 8f, 4f);
                Widgets.FillableBar(barRect, triggerBody.SwitchProgress, BaseContent.WhiteTex);
            }

            TooltipHandler.TipRegion(rect, "点击查看/操作所有槽位");

            // 点击打开窗口（避免重复打开）
            if (Widgets.ButtonInvisible(rect))
            {
                if (!Find.WindowStack.IsOpen(typeof(Window_TriggerBodySlots)))
                    Find.WindowStack.Add(new Window_TriggerBodySlots(triggerBody));
                return new GizmoResult(GizmoState.Interacted);
            }

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawSideRow(Rect rect, SlotSide side, string label)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y + 6f, 14f, rect.height), label);

            var iconRect = new Rect(rect.x + 16f, rect.y + 2f, 24f, 24f);
            var activeSlot = triggerBody.GetActiveSlot(side);

            if (activeSlot?.loadedChip != null)
            {
                // 有激活芯片：显示图标 + 高亮边框
                GUI.DrawTexture(iconRect, activeSlot.loadedChip.def.uiIcon ?? BaseContent.BadTex);
                GUI.color = ActiveBorderColor;
                Widgets.DrawBox(iconRect, 1);
                GUI.color = Color.white;
            }
            else
            {
                // 空：灰色占位框
                Widgets.DrawBoxSolid(iconRect, EmptySlotColor);
            }

            Text.Font = GameFont.Small;
        }
    }
}
