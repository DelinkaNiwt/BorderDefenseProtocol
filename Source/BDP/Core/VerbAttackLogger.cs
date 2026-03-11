using System.Text;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// 攻击动作调试日志工具类。
    /// 在每次执行攻击时记录详细信息：攻击类型、芯片构成、攻击模式、主/副攻击、子弹数量。
    /// </summary>
    public static class VerbAttackLogger
    {
        /// <summary>
        /// 记录单侧攻击信息。
        /// </summary>
        /// <param name="verb">Verb实例</param>
        /// <param name="chipDefName">芯片DefName</param>
        /// <param name="slotSide">槽位（左手/右手/null表示未指定）</param>
        /// <param name="firingPattern">发射模式</param>
        /// <param name="shotCount">开枪次数（引擎调用TryCastShot的次数）</param>
        /// <param name="bulletCount">子弹数量（实际发射的子弹总数）</param>
        /// <param name="isSecondary">是否为副攻击</param>
        public static void LogSingleAttack(
            Verb verb,
            string chipDefName,
            Trigger.SlotSide? slotSide,
            Trigger.FiringPattern firingPattern,
            int shotCount,
            int bulletCount,
            bool isSecondary = false)
        {
            var sb = new StringBuilder();
            sb.Append("[BDP攻击] 单侧攻击");
            sb.Append($" | 芯片: {chipDefName}");
            if (slotSide.HasValue)
                sb.Append($" | 槽位: {GetSlotName(slotSide.Value)}");
            sb.Append($" | 模式: {GetPatternName(firingPattern)}");
            sb.Append($" | 类型: {(isSecondary ? "副攻击" : "主攻击")}");
            sb.Append($" | 开枪次数: {shotCount}");
            sb.Append($" | 子弹数量: {bulletCount}");
            sb.Append($" | Verb: {verb.GetType().Name}");

            Log.Message(sb.ToString());
        }

        /// <summary>
        /// 记录双侧攻击信息。
        /// </summary>
        /// <param name="verb">Verb实例</param>
        /// <param name="leftChipDefName">左手芯片DefName</param>
        /// <param name="rightChipDefName">右手芯片DefName</param>
        /// <param name="leftPattern">左手发射模式</param>
        /// <param name="rightPattern">右手发射模式</param>
        /// <param name="leftBulletCount">左手子弹数量</param>
        /// <param name="rightBulletCount">右手子弹数量</param>
        /// <param name="totalShotCount">总开枪次数</param>
        /// <param name="isSecondary">是否为副攻击</param>
        public static void LogDualAttack(
            Verb verb,
            string leftChipDefName,
            string rightChipDefName,
            Trigger.FiringPattern leftPattern,
            Trigger.FiringPattern rightPattern,
            int leftBulletCount,
            int rightBulletCount,
            int totalShotCount,
            bool isSecondary = false)
        {
            var sb = new StringBuilder();
            sb.Append("[BDP攻击] 双侧攻击");
            sb.Append($" | 左手: {leftChipDefName}({GetPatternName(leftPattern)}, {leftBulletCount}弹)");
            sb.Append($" | 右手: {rightChipDefName}({GetPatternName(rightPattern)}, {rightBulletCount}弹)");
            sb.Append($" | 类型: {(isSecondary ? "副攻击" : "主攻击")}");
            sb.Append($" | 开枪次数: {totalShotCount}");
            sb.Append($" | 子弹数量: {leftBulletCount + rightBulletCount}");
            sb.Append($" | Verb: {verb.GetType().Name}");

            Log.Message(sb.ToString());
        }

        /// <summary>
        /// 记录组合技攻击信息。
        /// </summary>
        /// <param name="verb">Verb实例</param>
        /// <param name="comboDefName">组合技DefName</param>
        /// <param name="chipDefNames">参与组合的芯片DefName列表</param>
        /// <param name="firingPattern">发射模式</param>
        /// <param name="shotCount">开枪次数</param>
        /// <param name="bulletCount">子弹数量</param>
        /// <param name="isSecondary">是否为副攻击</param>
        public static void LogComboAttack(
            Verb verb,
            string comboDefName,
            string[] chipDefNames,
            Trigger.FiringPattern firingPattern,
            int shotCount,
            int bulletCount,
            bool isSecondary = false)
        {
            var sb = new StringBuilder();
            sb.Append("[BDP攻击] 组合技");
            sb.Append($" | 组合技: {comboDefName}");
            sb.Append($" | 芯片: {string.Join(" + ", chipDefNames)}");
            sb.Append($" | 模式: {GetPatternName(firingPattern)}");
            sb.Append($" | 类型: {(isSecondary ? "副攻击" : "主攻击")}");
            sb.Append($" | 开枪次数: {shotCount}");
            sb.Append($" | 子弹数量: {bulletCount}");
            sb.Append($" | Verb: {verb.GetType().Name}");

            Log.Message(sb.ToString());
        }

        /// <summary>
        /// 获取发射模式的中文名称。
        /// </summary>
        private static string GetPatternName(Trigger.FiringPattern pattern)
        {
            if (pattern == Trigger.FiringPattern.Sequential)
                return "逐发";
            else if (pattern == Trigger.FiringPattern.Simultaneous)
                return "齐射";
            else
                return "未知";
        }

        /// <summary>
        /// 获取槽位的中文名称。
        /// </summary>
        private static string GetSlotName(Trigger.SlotSide side)
        {
            if (side == Trigger.SlotSide.LeftHand)
                return "左手";
            else if (side == Trigger.SlotSide.RightHand)
                return "右手";
            else
                return "未知";
        }
    }
}
