using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// Trion资源条Gizmo——三段色显示（背景/占用/可用）。
    /// 继承Gizmo_Slider，不可拖拽，纯显示用途。
    ///
    /// 绘制层次（从底到顶）：
    ///   1. 背景（空段）
    ///   2. 占用段（暗青，0 → allocated/max）
    ///   3. 可用段（明绿，allocated/max → cur/max）
    ///   4. 文字标签（居中）
    /// </summary>
    public class Gizmo_TrionBar : Gizmo_Slider
    {
        private readonly CompTrion comp;

        // 缓存纹理（仅创建一次）
        private Texture2D allocatedTex;
        private Texture2D availableTex;
        private bool texInitialized;

        public Gizmo_TrionBar(CompTrion comp)
        {
            this.comp = comp;
        }

        // ── 抽象成员实现（占位，不可拖拽） ──

        protected override float Target
        {
            get => 0f;
            set { }
        }

        protected override float ValuePercent => comp.Percent;

        protected override string Title
        {
            get
            {
                // 多选时附加pawn名以区分
                if (comp.parent is Pawn pawn && Find.Selector.NumSelected > 1)
                    return "Trion - " + pawn.LabelShortCap;
                return "Trion";
            }
        }

        protected override bool DraggingBar
        {
            get => false;
            set { }
        }

        // ── 虚成员重写 ──

        protected override Color BarColor => comp.Props.barColor;

        protected override bool IsDraggable => false;

        protected override string BarLabel => $"{comp.Cur:F0} / {comp.Max:F0}";

        protected override string GetTooltip()
        {
            float totalDrain = comp.TotalDrainPerDay;
            string frozenStr = comp.Frozen ? "\n恢复冻结: 是" : "";

            // Pawn显示Stat恢复速率，非Pawn显示XML配置的recoveryPerDay
            string recoveryStr;
            if (comp.parent is Pawn pawn)
            {
                float rate = pawn.GetStatValue(BDP_DefOf.BDP_TrionRecoveryRate);
                recoveryStr = $"\n恢复速率: {rate:F1}/天";
            }
            else if (comp.Props.recoveryPerDay > 0f)
            {
                recoveryStr = $"\n恢复速率: {comp.Props.recoveryPerDay:F1}/天";
            }
            else
            {
                recoveryStr = "";
            }

            return $"Trion能量" +
                   $"\n当前: {comp.Cur:F1} / {comp.Max:F1}" +
                   $"\n可用: {comp.Available:F1}" +
                   (comp.Allocated > 0f ? $"\n占用: {comp.Allocated:F1}" : "") +
                   recoveryStr +
                   (totalDrain > 0f ? $"\n消耗速率: {totalDrain:F1}/天" : "") +
                   frozenStr;
        }

        // ═══════════════════════════════════════════
        //  三段色绘制
        // ═══════════════════════════════════════════

        /// <summary>
        /// 延迟初始化纹理。在第一次GizmoOnGUI时创建，避免静态构造器时序问题。
        /// </summary>
        private void EnsureTexInitialized()
        {
            if (texInitialized) return;
            texInitialized = true;
            allocatedTex = SolidColorMaterials.NewSolidColorTexture(comp.Props.allocatedBarColor);
            availableTex = SolidColorMaterials.NewSolidColorTexture(comp.Props.barColor);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            EnsureTexInitialized();

            // ── 布局（与基类一致） ──
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = rect.ContractedBy(8f);
            Widgets.DrawWindowBackground(rect);

            // ── 标题 ──
            bool mouseOverElement = false;
            Text.Font = GameFont.Small;
            Rect headerRect = inner;
            headerRect.height = Text.LineHeight;
            DrawHeader(headerRect, ref mouseOverElement);

            // ── 资源条区域 ──
            barRect = inner;
            barRect.yMin = headerRect.yMax + 8f;

            float curPct = comp.Percent;                                       // cur/max
            float allocPct = comp.Max > 0f ? comp.Allocated / comp.Max : 0f;   // allocated/max

            // 1. 背景（空段）—— 用FillableBar绘制0%填充，只画背景
            Widgets.FillableBar(barRect, 0f, BaseContent.ClearTex, BaseContent.ClearTex, doBorder: true);
            // 手动绘制空段背景
            Rect bgRect = barRect.ContractedBy(2f);
            GUI.DrawTexture(bgRect, SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f)));

            // 2. 占用段（暗青，从左到 allocPct）
            if (allocPct > 0f)
            {
                Rect allocRect = new Rect(bgRect.x, bgRect.y, bgRect.width * Mathf.Min(allocPct, 1f), bgRect.height);
                GUI.DrawTexture(allocRect, allocatedTex);
            }

            // 3. 可用段（明绿，从 allocPct 到 curPct）
            if (curPct > allocPct)
            {
                float startX = bgRect.x + bgRect.width * allocPct;
                float width = bgRect.width * (curPct - allocPct);
                Rect availRect = new Rect(startX, bgRect.y, width, bgRect.height);
                GUI.DrawTexture(availRect, availableTex);
            }

            // 4. 文字标签（居中）
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, BarLabel);
            Text.Anchor = TextAnchor.UpperLeft;

            // ── 悬停提示 ──
            if (Mouse.IsOver(rect) && !mouseOverElement)
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, GetTooltip, Gen.HashCombineInt(GetHashCode(), 8573612));
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
