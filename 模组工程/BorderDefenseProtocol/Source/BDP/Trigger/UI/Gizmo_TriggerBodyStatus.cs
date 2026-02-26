using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 内联状态Gizmo：常驻显示左手/右手槽激活芯片图标。
    /// SRP：只负责状态显示，不持有业务逻辑。
    /// 点击打开 Window_TriggerBodySlots。
    /// v2.0变更（T23）：Main/Sub → Left/Right
    /// v3.0变更：Left/Right → LeftHand/RightHand
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_TriggerBodyStatus : Gizmo
    {
        private readonly CompTriggerBody triggerBody;

        private static readonly Color EmptySlotColor  = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        private static readonly Color ActiveBorderColor = new Color(0.4f, 0.8f, 0.4f, 1f);
        // v3.1：四态视觉——注册未激活（蓝色边框）、挂载未注册（暗黄边框）
        private static readonly Color RegisteredBorderColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        private static readonly Color LoadedBorderColor = new Color(0.6f, 0.55f, 0.3f, 0.8f);
        // v6.0：切换阶段进度条颜色——后摇（橙红）、前摇（青蓝）
        private static readonly Texture2D WindingDownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.9f, 0.5f, 0.2f, 1f));
        private static readonly Texture2D WarmingUpBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.7f, 0.9f, 1f));

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

            // 左手槽行
            DrawSideRow(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 24f),
                SlotSide.LeftHand, "左手");
            // 左手切换进度条（该侧切换中时显示，颜色区分后摇/前摇）
            if (triggerBody.IsSideSwitching(SlotSide.LeftHand))
            {
                var barRect = new Rect(rect.x + 4f, rect.y + 30f, rect.width - 8f, 3f);
                var barTex = triggerBody.GetSideSwitchPhase(SlotSide.LeftHand) == SwitchPhase.WindingDown
                    ? WindingDownBarTex : WarmingUpBarTex;
                Widgets.FillableBar(barRect, triggerBody.GetSideSwitchProgress(SlotSide.LeftHand), barTex);
            }

            // 右手槽行
            DrawSideRow(new Rect(rect.x + 4f, rect.y + 38f, rect.width - 8f, 24f),
                SlotSide.RightHand, "右手");
            // 右手切换进度条（该侧切换中时显示，颜色区分后摇/前摇）
            if (triggerBody.IsSideSwitching(SlotSide.RightHand))
            {
                var barRect = new Rect(rect.x + 4f, rect.y + 64f, rect.width - 8f, 3f);
                var barTex = triggerBody.GetSideSwitchPhase(SlotSide.RightHand) == SwitchPhase.WindingDown
                    ? WindingDownBarTex : WarmingUpBarTex;
                Widgets.FillableBar(barRect, triggerBody.GetSideSwitchProgress(SlotSide.RightHand), barTex);
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

            // v3.1：获取最佳显示槽位（优先激活→注册→挂载）
            var displaySlot = GetBestDisplaySlot(side);

            if (displaySlot?.loadedChip != null)
            {
                if (displaySlot.isActive)
                {
                    // 有激活芯片：图标 + 绿色高亮边框
                    GUI.DrawTexture(iconRect, displaySlot.loadedChip.def.uiIcon ?? BaseContent.BadTex);
                    GUI.color = ActiveBorderColor;
                    Widgets.DrawBox(iconRect, 1);
                    GUI.color = Color.white;
                }
                else if (triggerBody.IsCombatBodyActive)
                {
                    // 注册未激活：图标 + 蓝色边框
                    GUI.DrawTexture(iconRect, displaySlot.loadedChip.def.uiIcon ?? BaseContent.BadTex);
                    GUI.color = RegisteredBorderColor;
                    Widgets.DrawBox(iconRect, 1);
                    GUI.color = Color.white;
                }
                else
                {
                    // 挂载未注册：图标50%透明 + 暗黄边框
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    GUI.DrawTexture(iconRect, displaySlot.loadedChip.def.uiIcon ?? BaseContent.BadTex);
                    GUI.color = LoadedBorderColor;
                    Widgets.DrawBox(iconRect, 1);
                    GUI.color = Color.white;
                }
            }
            else
            {
                // 空：灰色占位框
                Widgets.DrawBoxSolid(iconRect, EmptySlotColor);
            }

            Text.Font = GameFont.Small;
        }

        /// <summary>
        /// 获取指定侧的"最佳显示槽位"（优先激活→有芯片）。
        /// 用于Gizmo中显示该侧最具代表性的芯片状态。
        /// </summary>
        private ChipSlot GetBestDisplaySlot(SlotSide side)
        {
            // 优先返回激活槽位
            var active = triggerBody.GetActiveSlot(side);
            if (active != null) return active;

            // 其次返回第一个有芯片的槽位
            var slots = side == SlotSide.LeftHand ? triggerBody.LeftHandSlots
                      : side == SlotSide.RightHand ? triggerBody.RightHandSlots
                      : triggerBody.SpecialSlots;
            if (slots == null) return null;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].loadedChip != null) return slots[i];
            return null;
        }
    }
}
