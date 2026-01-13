using Verse;
using ProjectTrion.Core;
using ProjectTrion.Components;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// ProjectTrion_MVP 模组的启动入口
    ///
    /// 主要职责：
    /// 1. 初始化天赋→容量的查表映射
    /// 2. 注册Strategy类
    /// 3. 设置其他全局配置
    /// </summary>
    public class ProjectTrion_MVP_Mod : Mod
    {
        public ProjectTrion_MVP_Mod(ModContentPack content) : base(content)
        {
            // 在模组加载时初始化全局配置
            InitializeMod();
        }

        /// <summary>
        /// 模组初始化
        /// 在这里设置各种全局委托和配置
        /// </summary>
        private void InitializeMod()
        {
            try
            {
                // 设置天赋→容量查表函数
                // MVP使用固定的容量值：S=2000, A=1800, B=1600, C=1400, D=1200, E=1000
                CompTrion.TalentCapacityProvider = (talent) => talent switch
                {
                    TalentGrade.S => 2000f,  // S级：精英战士，容量+43%
                    TalentGrade.A => 1800f,  // A级：高级战士，容量+29%
                    TalentGrade.B => 1600f,  // B级：中高战士，容量+14%
                    TalentGrade.C => 1400f,  // C级：普通，容量基准
                    TalentGrade.D => 1200f,  // D级：中低战士，容量-14%
                    TalentGrade.E => 1000f,  // E级：新兵，容量-29%
                    _ => 1400f               // 默认值（不应该出现）
                };

                Log.Message($"[Trion_MVP] 模组初始化成功");
                Log.Message($"[Trion_MVP] TalentCapacityProvider 已设置");
                Log.Message($"[Trion_MVP] DefaultTrionStrategy 将在首次CompTrion初始化时使用");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Trion_MVP] 模组初始化失败: {ex}");
            }
        }
    }
}
