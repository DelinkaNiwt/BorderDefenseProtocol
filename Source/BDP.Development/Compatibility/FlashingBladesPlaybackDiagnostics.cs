using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace BDP.Development.Compatibility
{
    /// <summary>显式启用的刀光适配观察器；删除本文件或开发程序集即整体移除。</summary>
    public static class FlashingBladesPlaybackDiagnostics
    {
        /// <summary>只属于诊断的补丁标识。</summary>
        private const string HarmonyId = "niwt.bdp.development.flashingblades";
        /// <summary>当前诊断实例；为空表示完全关闭。</summary>
        private static Harmony harmony;
        /// <summary>缓存冻结选择读取方法，不产生正式程序集依赖。</summary>
        private static MethodInfo resolveSelection;
        /// <summary>窗口桥的目录字段。</summary>
        private static FieldInfo catalogField;
        /// <summary>每次启用后的构建次数。</summary>
        private static int built;
        /// <summary>每次启用后的生成入口尝试次数；不等同于最终渲染成功次数。</summary>
        private static int played;
        /// <summary>仅在目录首次可用时输出一次，避免配置页逐帧日志。</summary>
        private static bool listed;

        /// <summary>原版开发菜单中的本地化开关；没有启用时不安装观察补丁。</summary>
        [DebugAction("BDP", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode PlaybackMenu()
        {
            DebugActionNode root = new DebugActionNode("BDP_Dev_FB_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_FB_Toggle".Translate(), action: Toggle));
            return root;
        }

        /// <summary>按当前实际兼容程序集发现观察入口；失败撤销自己的全部观察补丁。</summary>
        private static void Toggle()
        {
            if (harmony != null) { Stop(); Log.Message("BDP_Dev_FB_Off".Translate()); return; }
            try
            {
                Assembly compatibility = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(value => value.GetName().Name == "BDP.Compatibility");
                Type runtime = compatibility != null ? compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesRuntimeBridge") : null;
                Type window = compatibility != null ? compatibility.GetType("BDP.Compatibility.FlashingBlades.FlashingBladesWindowBridge") : null;
                if (runtime == null || window == null || AccessTools.Field(runtime, "targets").GetValue(null) == null)
                { Log.Message("BDP_Dev_FB_Unavailable".Translate()); return; }
                resolveSelection = AccessTools.Method(runtime, "ResolveSelection");
                catalogField = AccessTools.Field(window, "catalog");
                built = 0; played = 0; listed = false;
                harmony = new Harmony(HarmonyId);
                harmony.Patch(AccessTools.Method(runtime, "BuildPostfix"), postfix: new HarmonyMethod(typeof(FlashingBladesPlaybackDiagnostics), nameof(Built)));
                harmony.Patch(AccessTools.Method(runtime, "SpawnPrefix"), postfix: new HarmonyMethod(typeof(FlashingBladesPlaybackDiagnostics), nameof(Played)));
                harmony.Patch(AccessTools.Method(window, "FilterPostfix"), postfix: new HarmonyMethod(typeof(FlashingBladesPlaybackDiagnostics), nameof(Listed)));
                Log.Message("BDP_Dev_FB_On".Translate());
            }
            catch (Exception error) { Stop(); Log.Warning("BDP_Dev_FB_Error".Translate(error.GetType().Name)); }
        }

        /// <summary>禁用后零持续日志，不撤销正式兼容补丁。</summary>
        private static void Stop()
        {
            if (harmony != null) harmony.UnpatchAll(HarmonyId);
            harmony = null; resolveSelection = null; catalogField = null;
        }

        /// <summary>构建后观察；参数序号避免依赖外部命名。</summary>
        private static void Built(object __1) { Observe(__1, false); }

        /// <summary>兼容层写入纹理参数后观察，计数的是通过外部去重等筛选后的生成入口。</summary>
        private static void Played(object[] __0) { Observe(__0[1], true); }

        /// <summary>只记录离散攻击事件；诊断自身错误立即停用观察。</summary>
        private static void Observe(object context, bool spawning)
        {
            try
            {
                object selection = resolveSelection.Invoke(null, new[] { context });
                if (selection == null) return;
                if (spawning) played++; else built++;
                object key = Read(selection, "ConfigKey");
                string status = (key == null ? "BDP_Dev_FB_Unmatched" : "BDP_Dev_FB_Matched").Translate().ToString();
                string phase = (spawning ? "BDP_Dev_FB_Spawn" : "BDP_Dev_FB_Build").Translate().ToString();
                Log.Message("BDP_Dev_FB_Event".Translate(phase, status, built, played,
                    Value(selection, "ResultId"), Value(selection, "ChipId"), Value(selection, "Mode"), Value(selection, "Stance"),
                    key != null ? key.ToString() : "", Value(selection, "Category"), Value(selection, "Texture")));
            }
            catch (Exception error) { Stop(); Log.Warning("BDP_Dev_FB_Error".Translate(error.GetType().Name)); }
        }

        /// <summary>配置窗口首次有目录时一次性列出键、名称与自动分类来源。</summary>
        private static void Listed()
        {
            if (listed) return;
            try
            {
                object catalog = catalogField.GetValue(null);
                if (catalog == null) return;
                IDictionary entries = (IDictionary)Read(catalog, "Entries");
                listed = true;
                foreach (DictionaryEntry pair in entries)
                {
                    IDictionary categories = (IDictionary)Read(pair.Value, "Categories");
                    // 必须使用 IDictionary 枚举器；转为 IEnumerable 后会得到 KeyValuePair。
                    List<string> sources = new List<string>();
                    foreach (DictionaryEntry category in categories) sources.Add(category.Key + "=" + category.Value);
                    string defaults = string.Join(", ", sources);
                    Log.Message("BDP_Dev_FB_Entry".Translate(pair.Key.ToString(), Value(pair.Value, "Label"), defaults));
                }
            }
            catch (Exception error) { Stop(); Log.Warning("BDP_Dev_FB_Error".Translate(error.GetType().Name)); }
        }

        /// <summary>仅由主动观察事件读取内部冻结事实，不写正式状态。</summary>
        private static object Read(object target, string field) { return AccessTools.Field(target.GetType(), field).GetValue(target); }
        /// <summary>机器键保持原样，空层级显示为空，不调用旧版对象数组翻译接口。</summary>
        private static string Value(object target, string field) { object value = Read(target, field); return value != null ? value.ToString() : ""; }
    }
}
