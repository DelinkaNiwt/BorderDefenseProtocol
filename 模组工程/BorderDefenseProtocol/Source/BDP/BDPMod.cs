using HarmonyLib;
using Verse;

namespace BDP
{
    /// <summary>
    /// 模组入口——在游戏启动时应用所有Harmony Patch。
    /// StaticConstructorOnStartup确保在游戏加载完成后执行。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class BDPMod
    {
        static BDPMod()
        {
            var harmony = new Harmony("com.niwt.bdp");
            harmony.PatchAll();
            Log.Message("[BDP] BorderDefenseProtocol 已加载，Harmony Patch已应用。");
        }
    }
}
