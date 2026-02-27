using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 射击模式编辑窗口（v9.0 FireMode系统）。
    /// 布局：标题 → 预设按钮行 → 分隔线 → D/S/C 三行滑条+锁定 → 总和校验。
    /// </summary>
    public class Window_FireModeEditor : Window
    {
        private readonly CompFireMode fireMode;
        private readonly string chipLabel;

        /// <summary>供 Gizmo_FireMode 按实例匹配窗口唯一性。</summary>
        public CompFireMode FireMode => fireMode;

        // 轴标签与颜色
        private static readonly string[] AxisLabels = { "伤害", "速度", "连射" };
        private static readonly Color[] AxisColors =
        {
            new Color(0.9f, 0.3f, 0.3f), // 伤害 红
            new Color(0.3f, 0.5f, 0.9f), // 速度 蓝
            new Color(0.3f, 0.8f, 0.4f), // 连射 绿
        };

        public override Vector2 InitialSize => new Vector2(300f, 220f);

        public Window_FireModeEditor(CompFireMode fireMode, string chipLabel)
        {
            this.fireMode  = fireMode;
            this.chipLabel = chipLabel;
            draggable            = true;
            closeOnClickedOutside = false;
            doCloseButton        = false;
            doCloseX             = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            // ── 标题行 ──
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, inRect.width, 24f), chipLabel + " · 射击模式");
            y += 28f;

            // ── 预设按钮行 ──
            float btnW = inRect.width / 4f;
            DrawPresetBtn(new Rect(0f,        y, btnW - 2f, 24f), "均衡", CompFireMode.Preset.Balanced);
            DrawPresetBtn(new Rect(btnW,       y, btnW - 2f, 24f), "重伤", CompFireMode.Preset.HeavyDamage);
            DrawPresetBtn(new Rect(btnW * 2f,  y, btnW - 2f, 24f), "速射", CompFireMode.Preset.Rapid);
            DrawPresetBtn(new Rect(btnW * 3f,  y, btnW - 2f, 24f), "狙击", CompFireMode.Preset.Sniper);
            y += 28f;

            // ── 分隔线 ──
            Widgets.DrawLineHorizontal(0f, y + 1f, inRect.width);
            y += 6f;

            // ── 三轴滑条行 ──
            float[] vals = { fireMode.Damage, fireMode.Speed, fireMode.Burst };
            for (int i = 0; i < 3; i++)
            {
                DrawAxisRow(inRect, ref y, i, vals[i]);
            }

            // ── 总和校验 ──
            float sum = fireMode.Damage + fireMode.Speed + fireMode.Burst;
            bool ok = Mathf.Abs(sum - CompFireMode.BUDGET) < 0.01f;
            GUI.color = ok ? Color.green : Color.red;
            Widgets.Label(new Rect(0f, y, inRect.width, 20f),
                $"总和：{sum:F2} / {CompFireMode.BUDGET:F1}");
            GUI.color = Color.white;
        }

        private void DrawPresetBtn(Rect rect, string label, CompFireMode.Preset preset)
        {
            if (Widgets.ButtonText(rect, label))
                fireMode.ApplyPreset(preset);
        }

        private void DrawAxisRow(Rect inRect, ref float y, int axis, float currentVal)
        {
            float lockBtnW  = 22f;
            float axisLblW  = 30f;
            float valLblW   = 46f;
            float sliderW   = inRect.width - lockBtnW - axisLblW - valLblW - 6f;

            float x = 0f;

            // 锁定按钮
            bool isLocked = fireMode.Locked == axis;
            GUI.color = isLocked ? Color.yellow : Color.white;
            if (Widgets.ButtonText(new Rect(x, y, lockBtnW, 22f), isLocked ? "🔒" : "🔓"))
                fireMode.SetLocked(axis);
            GUI.color = Color.white;
            x += lockBtnW + 2f;

            // 轴标签（带颜色）
            GUI.color = AxisColors[axis];
            Widgets.Label(new Rect(x, y + 2f, axisLblW, 20f), AxisLabels[axis]);
            GUI.color = Color.white;
            x += axisLblW;

            // 滑条（锁定时禁用）
            if (isLocked)
            {
                Widgets.FillableBar(new Rect(x, y + 4f, sliderW, 14f),
                    Mathf.InverseLerp(CompFireMode.MIN, CompFireMode.MAX, currentVal),
                    Texture2D.grayTexture);
            }
            else
            {
                float newVal = Widgets.HorizontalSlider(
                    new Rect(x, y + 4f, sliderW, 14f),
                    currentVal, CompFireMode.MIN, CompFireMode.MAX);
                if (Mathf.Abs(newVal - currentVal) > 0.001f)
                    fireMode.SetValue(axis, newVal);
            }
            x += sliderW + 2f;

            // 实际值标签（Speed 轴低于安全阈值时变橙色警告）
            string valStr = GetActualValueLabel(axis);
            if (axis == 1 && fireMode.Speed < CompFireMode.MIN_SPEED + 0.05f)
                GUI.color = new Color(1f, 0.6f, 0.1f);
            Widgets.Label(new Rect(x, y + 2f, valLblW, 20f), valStr);
            GUI.color = Color.white;

            y += 32f;
        }

        /// <summary>根据轴索引返回实际值字符串。无法读取时降级显示倍率。</summary>
        private string GetActualValueLabel(int axis)
        {
            switch (axis)
            {
                case 0: // 伤害
                {
                    int v = fireMode.GetDisplayDamage();
                    return v >= 0 ? v.ToString() : $"×{fireMode.Damage:F2}";
                }
                case 1: // 速度
                {
                    float v = fireMode.GetDisplaySpeed();
                    return v >= 0f ? $"{v:F1}" : $"×{fireMode.Speed:F2}";
                }
                case 2: // 连射
                {
                    int v = fireMode.GetDisplayBurst();
                    return v >= 0 ? $"{v}发" : $"×{fireMode.Burst:F2}";
                }
                default: return "?";
            }
        }
    }
}
