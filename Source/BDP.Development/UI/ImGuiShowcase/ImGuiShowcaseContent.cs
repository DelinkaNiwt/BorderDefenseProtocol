#if BDP_RIMWORLD_1_7
using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using Verse;

namespace BDP.Development.UI.ImGuiShowcase
{
    /// <summary>随时可删除的原生控件与二维绘制样例；只依赖 ImGui 和本地化，不依赖 BDP 业务。</summary>
    internal sealed class ImGuiShowcaseContent
    {
        /// <summary>示例统一强调色，颜色编辑器只修改此窗口内的副本。</summary>
        private float4 accent = new float4(.2f, .8f, .8f, 1f);
        /// <summary>示例填充透明度。</summary>
        private float opacity = .45f;
        /// <summary>示例线宽。</summary>
        private float thickness = 2f;
        /// <summary>是否启用原生抗锯齿。</summary>
        private bool antialias = true;
        /// <summary>是否推进界面动画。</summary>
        private bool animate = true;
        /// <summary>动画相位，不读取游戏刻或业务计时器。</summary>
        private float phase;
        /// <summary>按钮点击次数，不写入存档。</summary>
        private int clicks;
        /// <summary>拖拽点的归一化坐标。</summary>
        private float2 handle = new float2(.5f, .5f);
        /// <summary>当前原版界面缩放，对原生物理像素坐标只应用一次。</summary>
        private float scale;

        /// <summary>绘制真实原生控件及代表性样例，并恢复绘制列表状态。</summary>
        internal void Draw(float uiScale)
        {
            scale = uiScale;
            if (animate) phase += Math.Min(ImGui.GetIO().DeltaTime, .1f);
            ImGui.Text(L("Title"));
            ImGui.Text(L("Intro"));
            ImGui.SeparatorText(L("Controls"));
            ImGui.Checkbox(L("Animate"), ref animate);
            ImGui.SameLine();
            ImGui.Checkbox(L("Antialias"), ref antialias);
            ImGui.SetNextItemWidth(280f * scale);
            ImGui.SliderFloat(L("Opacity"), ref opacity, 0f, 1f);
            ImGui.SetNextItemWidth(280f * scale);
            ImGui.SliderFloat(L("Thickness"), ref thickness, .5f, 8f);
            ImGui.SetNextItemWidth(360f * scale);
            ImGui.ColorEdit4(L("Color"), ref accent, ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoAlpha);
            if (ImGui.Button(L("Click"))) clicks++;
            ImGui.SameLine();
            ImGui.Text(string.Format(L("Clicks"), clicks));
            ImGui.SameLine();
            if (ImGui.Button(L("Reset"))) Reset();
            ImGui.ProgressBar((MathF.Sin(phase) + 1f) * .5f, new float2(360f * scale, 20f * scale), L("Progress"));

            ImDrawListPtr list = ImGui.GetWindowDrawList();
            ImDrawListFlags previous = list.Flags;
            try
            {
                SetAntialias(list, antialias);
                DrawColors(list);
                DrawShapes(list);
                DrawAntialias(list);
                DrawCharts(list);
                DrawClipAndDrag(list);
                DrawAnimation(list);
            }
            finally { list.Flags = previous; }
            ImGui.Text(L("Footer"));
        }

        /// <summary>颜色、透明混合和四角渐变；棋盘格使透明度变化可见。</summary>
        private void DrawColors(ImDrawListPtr list)
        {
            float2 size;
            float2 p = Canvas("Colors", 175f, out size);
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 14; x++)
                    list.AddRectFilled(p + S(x * 18f, y * 18f), p + S((x + 1) * 18f, (y + 1) * 18f),
                        (x + y) % 2 == 0 ? C(.18f,.22f,.27f) : C(.1f,.13f,.17f));
            list.AddCircleFilled(p + S(88f,48f), 40f * scale, Accent(opacity));
            list.AddCircleFilled(p + S(133f,48f), 40f * scale, C(.9f,.3f,.5f,opacity));
            float left = Math.Min(size.X / scale * .55f, 400f);
            list.AddRectFilledMultiColor(p + S(left,8f), p + new float2(size.X - 12f * scale,90f * scale),
                C(.2f,.85f,.85f).AsUint(), C(.9f,.3f,.6f).AsUint(), C(.95f,.7f,.2f).AsUint(), C(.25f,.4f,.9f).AsUint());
            list.AddRectFilled(p + S(16f,116f), p + S(220f,153f), Accent(opacity), 15f * scale);
            list.AddRect(p + S(16f,116f), p + S(220f,153f), Accent(), 15f * scale, ImDrawFlags.None, thickness * scale);
        }

        /// <summary>圆角、圆、三角形、凹多边形、圆弧和三次贝塞尔曲线。</summary>
        private void DrawShapes(ImDrawListPtr list)
        {
            float2 size;
            float2 p = Canvas("Shapes", 185f, out size);
            list.AddCircle(p + S(55f,65f), 38f * scale, Accent(), 0, thickness * scale);
            list.AddTriangleFilled(p + S(120f,104f), p + S(160f,22f), p + S(200f,104f), C(.95f,.65f,.2f,opacity));
            float2[] star = new float2[10];
            for (int i = 0; i < star.Length; i++)
            {
                float angle = -MathF.PI * .5f + i * MathF.PI / 5f;
                float r = i % 2 == 0 ? 43f : 20f;
                star[i] = p + S(274f + MathF.Cos(angle)*r, 67f + MathF.Sin(angle)*r);
            }
            list.AddConcavePolyFilled(star, Accent(opacity));
            list.AddPolyline(star, Accent(), ImDrawFlags.Closed, thickness * scale);
            list.PathClear();
            list.PathArcTo(p + S(370f,70f), 42f * scale, -.8f * MathF.PI, .5f * MathF.PI);
            list.PathStroke(C(.95f,.65f,.2f), ImDrawFlags.None, thickness * scale);
            list.AddBezierCubic(p + S(22f,151f), p + S(170f,50f), p + new float2(size.X-170f*scale,210f*scale),
                p + new float2(size.X-22f*scale,130f*scale), Accent(), thickness * scale);
        }

        /// <summary>在同一列表中分别关闭和开启抗锯齿，以同样几何对照边缘。</summary>
        private void DrawAntialias(ImDrawListPtr list)
        {
            float2 size;
            float2 p = Canvas("AA", 125f, out size);
            bool saved = antialias;
            for (int side = 0; side < 2; side++)
            {
                SetAntialias(list, side == 1);
                float x = 30f + side * size.X / (2f * scale);
                list.AddLine(p + S(x,20f), p + S(x+170f,88f), Accent(), thickness*scale);
                list.AddCircle(p + S(x+225f,58f), 35f*scale, C(.95f,.65f,.2f), 0, thickness*scale);
            }
            SetAntialias(list, saved);
        }

        /// <summary>固定假数据演示雷达填充、折线、虚线、圆点和柱状图。</summary>
        private void DrawCharts(ImDrawListPtr list)
        {
            float2 size;
            float2 p = Canvas("Charts", 225f, out size);
            float2 center = p + S(120f,105f);
            float[] values = {.75f,.95f,.4f,.86f,.5f,.65f};
            float2[] radar = new float2[6];
            for (int level = 1; level <= 4; level++)
            {
                float2[] ring = new float2[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = -MathF.PI*.5f+i*MathF.PI/3f;
                    float2 direction = new float2(MathF.Cos(angle),MathF.Sin(angle));
                    ring[i] = center + direction*(80f*scale*level/4f);
                    if (level == 4)
                    {
                        radar[i] = center+direction*(80f*scale*values[i]);
                        list.AddLine(center, ring[i], C(.3f,.55f,.6f,.4f));
                    }
                }
                list.AddPolyline(ring,C(.3f,.55f,.6f,.5f),ImDrawFlags.Closed,scale);
            }
            list.AddConcavePolyFilled(radar,Accent(opacity));
            list.AddPolyline(radar,Accent(),ImDrawFlags.Closed,thickness*scale);
            float x0=265f, chartWidth=Math.Max(120f,size.X/scale-x0-20f);
            for(int level=0;level<4;level++)
                list.AddLine(p+S(x0,28f+level*50f),p+S(x0+chartWidth,28f+level*50f),C(.3f,.55f,.6f,.35f));
            float2[] line=new float2[7];
            for(int i=0;i<line.Length;i++)
            {
                float x=x0+i*chartWidth/6f;
                float h=35f+(MathF.Sin(i*.8f)+1f)*45f;
                line[i]=p+S(x,190f-h);
                list.AddRectFilled(p+S(x-9f,190f-h*.65f),p+S(x+9f,190f),C(.95f,.65f,.2f,.35f),3f*scale);
                list.AddCircleFilled(line[i],3.5f*scale,Accent());
            }
            list.AddPolyline(line,Accent(),ImDrawFlags.None,thickness*scale);
            for(float x=0f;x<chartWidth;x+=12f)
                list.AddLine(p+S(x0+x,70f+x*.16f),p+S(x0+Math.Min(x+6f,chartWidth),71f+x*.16f),C(.8f,.5f,.8f,.7f),scale);
        }

        /// <summary>真实裁切矩形及可拖拽画布；鼠标仅改变示例点位置。</summary>
        private void DrawClipAndDrag(ImDrawListPtr list)
        {
            float2 size;
            float2 p = Canvas("Clip", 170f, out size, true);
            if(ImGui.IsItemActive())
            {
                float2 mouse=ImGui.GetIO().MousePos;
                handle=new float2(Clamp((mouse.X-p.X)/size.X),Clamp((mouse.Y-p.Y)/size.Y));
            }
            float2 min=p+S(20f,20f), max=p+new float2(size.X*.48f,145f*scale);
            list.AddRect(min,max,C(.95f,.65f,.2f),0f,ImDrawFlags.None,scale);
            list.PushClipRect(min,max,true);
            try
            {
                for(int i=0;i<9;i++)
                    list.AddCircleFilled(p+S(10f+i*36f,60f+MathF.Sin(phase+i)*25f),30f*scale,Accent(opacity));
            }
            finally { list.PopClipRect(); }
            float2 point=p+new float2(handle.X*size.X,handle.Y*size.Y);
            list.AddLine(point-S(15f,0f),point+S(15f,0f),C(1f,.6f,.2f),thickness*scale);
            list.AddLine(point-S(0f,15f),point+S(0f,15f),C(1f,.6f,.2f),thickness*scale);
            list.AddCircle(point,10f*scale,Accent(),0,thickness*scale);
        }

        /// <summary>用同一组二维图元构建动态仪表、旋转轨道和波形，不使用游戏数据。</summary>
        private void DrawAnimation(ImDrawListPtr list)
        {
            float2 size;
            float2 p=Canvas("Animation",145f,out size);
            float2 center=p+S(75f,72f);
            list.AddCircle(center,50f*scale,C(.3f,.55f,.6f,.5f),0,scale);
            list.PathClear();
            list.PathArcTo(center,50f*scale,-MathF.PI*.5f,-MathF.PI*.5f+(.15f+(MathF.Sin(phase)+1f)*.4f)*MathF.PI*2f);
            list.PathStroke(Accent(),ImDrawFlags.None,8f*scale);
            list.AddCircleFilled(center+S(MathF.Cos(phase)*35f,MathF.Sin(phase)*35f),7f*scale,C(.95f,.65f,.2f));
            float width=Math.Max(120f,size.X/scale-180f);
            float2[] wave=new float2[80];
            for(int i=0;i<wave.Length;i++)
                wave[i]=p+S(170f+i*width/(wave.Length-1),72f+MathF.Sin(i*.18f-phase*2f)*35f);
            list.AddPolyline(wave,Accent(),ImDrawFlags.None,thickness*scale);
        }

        /// <summary>标题与说明均来自语言包；画布占位参与原生滚动和控件布局。</summary>
        private float2 Canvas(string key,float height,out float2 size,bool interactive=false)
        {
            ImGui.SeparatorText(L(key+"Title"));
            ImGui.Text(L(key+"Description"));
            float2 p=ImGui.GetCursorScreenPos();
            size=new float2(Math.Max(480f*scale,ImGui.GetContentRegionAvail().X),height*scale);
            if(interactive) ImGui.InvisibleButton("##BDP_Dev_ImGui_"+key,size);
            else ImGui.Dummy(size);
            return p;
        }

        /// <summary>只修改当前列表的线条和填充抗锯齿位，其他标记不动。</summary>
        private static void SetAntialias(ImDrawListPtr list,bool enabled)
        {
            ImDrawListFlags mask=ImDrawListFlags.AntiAliasedLines|ImDrawListFlags.AntiAliasedFill;
            list.Flags=enabled ? list.Flags|mask : list.Flags&~mask;
        }

        /// <summary>重置所有示例状态。</summary>
        private void Reset()
        {
            accent=new float4(.2f,.8f,.8f,1f); opacity=.45f; thickness=2f;
            antialias=true; animate=true; phase=0f; clicks=0; handle=new float2(.5f,.5f);
        }

        /// <summary>按当前界面缩放转换示例尺寸。</summary>
        private float2 S(float x,float y) { return new float2(x*scale,y*scale); }
        /// <summary>当前强调色与独立透明度。</summary>
        private ImColor8 Accent(float alpha=1f) { return C(accent.X,accent.Y,accent.Z,alpha); }
        /// <summary>把归一化颜色转换为原生四通道颜色。</summary>
        private static ImColor8 C(float r,float g,float b,float a=1f)
        {
            return new ImColor8((byte)(Clamp(r)*255f),(byte)(Clamp(g)*255f),(byte)(Clamp(b)*255f),(byte)(Clamp(a)*255f));
        }
        /// <summary>限制示例输入范围。</summary>
        private static float Clamp(float v) { return Math.Max(0f,Math.Min(1f,v)); }
        /// <summary>所有面向用户的文字均从独立开发语言包读取。</summary>
        private static string L(string key) { return ("BDP_Dev_ImGui_"+key).Translate().ToString(); }
    }
}
#endif
