using HarmonyLib;
using Verse;

namespace BDP
{
    /// <summary>
    /// 模组入口。
    /// 这里只做 Harmony 初始化，不在构造阶段输出诊断日志，避免过早触发游戏状态读取。
    /// </summary>
    public sealed class BDPMod : Mod
    {
        /// <summary>
        /// 构造模组入口并注册 Harmony 补丁。
        /// </summary>
        public BDPMod(ModContentPack content) : base(content)
        {
            // 当前模组启动时只做补丁注册。
            new Harmony("niwt.bdp").PatchAll();
        }
    }
}
