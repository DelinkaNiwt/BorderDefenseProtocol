using System.Linq;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 射击模式Gizmo（v9.0 FireMode系统）。
    /// 显示芯片名 + 三条迷你彩色进度条（D红/S蓝/C绿）。
    /// 点击弹出 Window_FireModeEditor 编辑窗口。
    /// Order=-98f，排在 Gizmo_TriggerBodyStatus 之后。
    /// </summary>
    public class Gizmo_FireMode : Gizmo
    {
        private readonly CompFireMode fireMode;
        private readonly string chipLabel;

        // 三轴颜色
        private static readonly Color ColorD = new Color(0.9f, 0.3f, 0.3f); // 红
        private static readonly Color ColorS = new Color(0.3f, 0.5f, 0.9f); // 蓝
        private static readonly Color ColorC = new Color(0.3f, 0.8f, 0.4f); // 绿

        public Gizmo_FireMode(CompFireMode fireMode, string chipLabel)
        {
            this.fireMode  = fireMode;
            this.chipLabel = chipLabel;
            Order = -98f;
        }

        public override float GetWidth(float maxWidth) => 75f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            // 芯片名（截断显示）
            Rect labelRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 16f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, chipLabel);

            // 三条迷你进度条
            float barW = rect.width - 8f;
            float barH = 8f;
            float barX = rect.x + 4f;
            DrawBar(new Rect(barX, rect.y + 22f, barW, barH), fireMode.Damage, ColorD, "D");
            DrawBar(new Rect(barX, rect.y + 36f, barW, barH), fireMode.Speed,  ColorS, "S");
            DrawBar(new Rect(barX, rect.y + 50f, barW, barH), fireMode.Burst,  ColorC, "C");

            Text.Font = GameFont.Small;

            // 点击打开编辑窗口
            if (Widgets.ButtonInvisible(rect))
            {
                // 按 CompFireMode 实例匹配，两个芯片可各自独立开窗口
                bool alreadyOpen = Find.WindowStack.Windows
                    .OfType<Window_FireModeEditor>()
                    .Any(w => w.FireMode == fireMode);
                if (!alreadyOpen)
                    Find.WindowStack.Add(new Window_FireModeEditor(fireMode, chipLabel));
            }

            // Tooltip：显示实际值
            int dmg   = fireMode.GetDisplayDamage();
            float spd = fireMode.GetDisplaySpeed();
            int bst   = fireMode.GetDisplayBurst();
            string tip = "射击模式（点击调整）\n"
                + $"伤害：{(dmg >= 0 ? dmg.ToString() : "?")}\n"
                + $"速度：{(spd >= 0f ? spd.ToString("F1") : "?")}\n"
                + $"连射：{(bst >= 0 ? bst + "发" : "?")}";
            TooltipHandler.TipRegion(rect, tip);

            return new GizmoResult(GizmoState.Clear);
        }

        private static void DrawBar(Rect rect, float value, Color color, string axisLabel)
        {
            // 背景
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f));
            // 填充（value 范围 [0.1, 2.8]，映射到 [0,1]）
            float fill = Mathf.InverseLerp(CompFireMode.MIN, CompFireMode.MAX, value);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * fill, rect.height), color);
            // 轴标签
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 1f, rect.y - 1f, 12f, rect.height + 2f), axisLabel);
        }
    }
}
