using Verse;
using ProjectTrion.Components;

namespace ProjectTrion.Core
{
    /// <summary>
    /// Trion天赋等级。
    /// Trion Talent Grade levels.
    /// </summary>
    public enum TalentGrade
    {
        /// <summary>最高等级</summary>
        S = 6,

        /// <summary>高等级</summary>
        A = 5,

        /// <summary>中高等级</summary>
        B = 4,

        /// <summary>中等级</summary>
        C = 3,

        /// <summary>中低等级</summary>
        D = 2,

        /// <summary>最低等级</summary>
        E = 1,
    }

    /// <summary>
    /// Trion战斗体的生命周期策略接口。
    /// 应用层通过实现此接口定义不同类型单位的战斗体行为。
    /// </summary>
    public interface ILifecycleStrategy
    {
        /// <summary>
        /// 获取此策略的唯一标识符。
        /// Get the unique identifier for this strategy.
        /// </summary>
        string StrategyId { get; }

        /// <summary>
        /// 获取单位的初始Trion天赋。
        /// 此方法在单位生成时调用（仅新建，读档时不调用）。
        ///
        /// Called when unit is first generated to determine initial talent level.
        /// (Not called during save/load)
        /// </summary>
        /// <param name="comp">CompTrion实例</param>
        /// <returns>
        /// TalentGrade: 框架会根据此天赋计算Capacity
        /// null: 框架跳过容量计算，由应用层自己管理Capacity
        /// </returns>
        TalentGrade? GetInitialTalent(CompTrion comp);

        /// <summary>
        /// 战斗体生成时的回调。
        /// 在快照保存、组件初始化完成后调用。
        /// Called after combat body generation, snapshot saved, and components initialized.
        /// </summary>
        void OnCombatBodyGenerated(CompTrion comp);

        /// <summary>
        /// 战斗体摧毁时的回调。
        /// Called when combat body is destroyed for any reason.
        /// </summary>
        /// <param name="comp">Trion组件实例</param>
        /// <param name="reason">摧毁原因</param>
        void OnCombatBodyDestroyed(CompTrion comp, DestroyReason reason);

        /// <summary>
        /// 获取基础维持消耗。
        /// 在每个CompTick周期（60Tick）计算一次。
        /// Get base maintenance consumption per 60-tick cycle.
        /// </summary>
        float GetBaseMaintenance();

        /// <summary>
        /// 每个CompTick周期的回调。
        /// 用于复杂逻辑处理（如AI决策、特殊能力计算等）。
        /// Called every CompTick cycle (60 ticks) for complex logic processing.
        /// </summary>
        void OnTick(CompTrion comp);

        /// <summary>
        /// 关键部位（供给器官）被摧毁时的回调。
        /// Called when a vital part (Trion supply organ) is destroyed.
        /// </summary>
        void OnVitalPartDestroyed(CompTrion comp, BodyPartRecord part);

        /// <summary>
        /// Trion可用量耗尽时的回调。
        /// Called when Available Trion becomes <= 0.
        /// </summary>
        void OnDepleted(CompTrion comp);

        /// <summary>
        /// 检查是否可以执行Bail Out。
        /// Check if Bail Out can be executed.
        /// </summary>
        bool CanBailOut(CompTrion comp);

        /// <summary>
        /// 获取Bail Out的目标位置。
        /// Get the target location for Bail Out teleportation.
        /// 返回IntVec3.Invalid表示无效目标，会导致Bail Out失败。
        /// </summary>
        IntVec3 GetBailOutTarget(CompTrion comp);
    }

    /// <summary>
    /// 战斗体摧毁的原因。
    /// Reason for combat body destruction.
    /// </summary>
    public enum DestroyReason
    {
        /// <summary>
        /// 玩家主动解除
        /// User manually deactivated
        /// </summary>
        Manual,

        /// <summary>
        /// Trion耗尽导致被动破裂
        /// Trion depletion caused passive destruction
        /// </summary>
        TrionDepleted,

        /// <summary>
        /// 供给器官被摧毁导致强制解除
        /// Supply organ destroyed forced deactivation
        /// </summary>
        VitalPartDestroyed,

        /// <summary>
        /// Bail Out成功执行
        /// Bail Out successfully executed
        /// </summary>
        BailOutSuccess,

        /// <summary>
        /// Bail Out失败导致破裂
        /// Bail Out failed caused destruction
        /// </summary>
        BailOutFailed,

        /// <summary>
        /// 其他原因（宿主死亡、mod冲突等）
        /// Other reasons (host death, mod conflict, etc.)
        /// </summary>
        Other
    }
}
