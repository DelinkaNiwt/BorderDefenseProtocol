using Verse;
using HarmonyLib;

namespace Aldon.Energy
{
    /// <summary>
    /// 模组入口点 - 初始化Harmony补丁
    /// </summary>
    public class AldonMod : Mod
    {
        public AldonMod(ModContentPack content) : base(content)
        {
            // 初始化Harmony
            var harmony = new Harmony("aldon.energy.mvp");

            try
            {
                harmony.PatchAll();
                Log.Message("[Aldon] Harmony patches已加载成功");
                Log.Message("[Aldon] MVP能量管理系统已初始化");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Aldon] Harmony patch失败: {ex}");
            }
        }
    }
}
