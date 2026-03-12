using Verse;

namespace BDP.Glow
{
    /// <summary>
    /// 发光控制器接口——定义发光逻辑的标准协议。
    ///
    /// 设计约定（参考IChipEffect）：
    ///   · 无参构造：通过Activator.CreateInstance实例化，必须有无参构造函数
    ///   · 有状态控制器（如PulseGlow）必须实现ExposeData()进行序列化
    ///   · Initialize()在PostSpawnSetup时调用，传入宿主Thing和配置参数
    ///
    /// 渲染与控制分离：
    ///   CompGlow（渲染组件）只负责绘制，IGlowController只负责逻辑计算。
    /// </summary>
    public interface IGlowController
    {
        /// <summary>初始化控制器，传入宿主Thing和XML配置参数。</summary>
        void Initialize(Thing parent, GlowControllerParams parms);

        /// <summary>每tick更新逻辑（由CompGlow.CompTick调用）。</summary>
        void Tick();

        /// <summary>
        /// 返回当前发光强度，范围[0, 1]。
        /// 0=完全不发光，1=最大强度。
        /// </summary>
        float GetGlowIntensity();

        /// <summary>
        /// 返回当前发光颜色（可选）。
        /// 返回null时使用CompProperties_BDPGlow.graphicData中配置的颜色。
        /// </summary>
        UnityEngine.Color? GetGlowColor();

        /// <summary>序列化控制器状态（有状态控制器必须实现）。</summary>
        void ExposeData();
    }
}
