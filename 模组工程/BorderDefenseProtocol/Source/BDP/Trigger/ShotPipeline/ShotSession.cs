using System.Collections.Generic;
using BDP.Projectiles;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击会话：封装单次射击的完整生命周期，管理瞄准和射击阶段的数据流转。
    /// 有状态对象，由 ShotPipeline 创建并在管线各阶段间传递。
    /// </summary>
    public class ShotSession
    {
        // ══════════════════════════════════════════
        //  只读上下文（管线入口快照）
        // ══════════════════════════════════════════

        /// <summary>射击上下文（只读快照）</summary>
        public ShotContext Context { get; private set; }

        // ══════════════════════════════════════════
        //  瞄准阶段可变状态
        // ══════════════════════════════════════════

        /// <summary>瞄准意图列表（各模块产出）</summary>
        public List<AimIntent> AimIntents { get; private set; }

        /// <summary>合并后的瞄准结果（管线输出）</summary>
        public AimResult AimResult { get; set; }

        // ══════════════════════════════════════════
        //  射击阶段可变状态
        // ══════════════════════════════════════════

        /// <summary>射击意图列表（各模块产出）</summary>
        public List<FireIntent> FireIntents { get; private set; }

        /// <summary>合并后的射击结果（管线输出）</summary>
        public FireResult FireResult { get; set; }

        // ══════════════════════════════════════════
        //  共享数据槽（模块间通信）
        // ══════════════════════════════════════════

        /// <summary>
        /// 障碍物绕行路由结果（由 AutoRouteAimModule 写入，AutoRouteFireModule 读取）
        /// </summary>
        public ObstacleRouteResult? RouteResult { get; set; }

        /// <summary>
        /// 通用数据槽：模块间传递自定义数据
        /// </summary>
        public Dictionary<string, object> SharedData { get; private set; }

        // ══════════════════════════════════════════
        //  构造函数
        // ══════════════════════════════════════════

        /// <summary>
        /// 创建射击会话
        /// </summary>
        /// <param name="context">射击上下文（只读快照）</param>
        public ShotSession(ShotContext context)
        {
            Context = context;
            AimIntents = new List<AimIntent>();
            FireIntents = new List<FireIntent>();
            SharedData = new Dictionary<string, object>();
        }

        // ══════════════════════════════════════════
        //  辅助方法
        // ══════════════════════════════════════════

        /// <summary>
        /// 重置会话状态（用于重新瞄准）
        /// </summary>
        public void Reset()
        {
            AimIntents.Clear();
            FireIntents.Clear();
            SharedData.Clear();
            AimResult = default;
            FireResult = default;
            RouteResult = null;
        }
    }
}
