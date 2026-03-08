using BDP.Combat;
using BDP.Combat.Snapshot;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// 战斗体激活前置条件检查事件参数。
    /// 订阅者可以设置Vetoed=true来否决激活，并提供BlockReason。
    /// </summary>
    public class CanActivateCombatBodyEventArgs
    {
        public Pawn Pawn;
        public bool Vetoed = false;
        public string BlockReason = "";
    }

    /// <summary>
    /// 部位破坏事件参数（v12.2新增：手部缺失联动）。
    /// 从Gene_TrionGland提取到命名空间顶级，便于跨模块引用。
    /// </summary>
    public class PartDestroyedEventArgs
    {
        public Pawn Pawn;
        public BodyPartRecord Part;
        public bool IsHandPart;
        public HandSide HandSide;
    }

    /// <summary>
    /// 伤害接收事件参数（v13.1新增：破裂检测解耦）。
    /// 当Pawn受到伤害后触发，用于通知战斗体系统进行Trion消耗和破裂检测。
    /// </summary>
    public class DamageReceivedEventArgs
    {
        public Pawn Pawn;
        public float TotalDamageDealt;
    }

    /// <summary>
    /// 手部侧边枚举。
    /// 从Gene_TrionGland提取到命名空间顶级，便于跨模块引用。
    /// </summary>
    public enum HandSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Trion腺体基因——天赋配置器。
    /// 表达"此Pawn拥有Trion腺体"这一先天事实。
    /// 向Stat系统贡献基础属性值（通过GeneDef.statOffsets）。
    /// 持有战斗体快照对象，负责初始化战斗体装备。
    ///
    /// 继承Gene而非Gene_Resource的原因：
    ///   Gene_Resource假设自身持有cur/max，
    ///   数据在CompTrion中 → 继承它会产生两份数据源，
    ///   违反Single Source of Truth。
    ///
    /// v13.0重构：God Object瘦身
    ///   - 事件总线职责 → BDPEvents（静态事件类）
    ///   - 运行时聚合职责 → CombatBodyRuntime（运行时数据访问点）
    ///   - Gene仅保留：序列化、初始化、UI控制
    /// </summary>
    public class Gene_TrionGland : Gene
    {
        // 静态构造函数：订阅自身事件
        static Gene_TrionGland()
        {
            BDPEvents.RequestDeactivateCombatBody += OnRequestDeactivate;
        }

        /// <summary>
        /// 响应解除战斗体请求（静态事件处理器）。
        /// </summary>
        private static void OnRequestDeactivate(Pawn pawn)
        {
            if (pawn?.genes == null) return;

            var runtime = BDP.Combat.CombatBodyRuntime.Of(pawn);
            if (runtime != null && runtime.IsActive)
            {
                Log.Message($"[BDP] 收到解除战斗体请求: {pawn.Name}（触发体被卸下）");
                runtime.Deactivate(isEmergency: false);
            }
        }

        // 战斗体快照引用
        private CombatBodySnapshot snapshot;

        // 战斗体状态聚合器
        private CombatBodyState state;

        // 战斗体协调器（不序列化）
        private CombatBodyOrchestrator orchestrator;

        /// <summary>
        /// 战斗体运行时聚合体（公开访问）。
        /// 外部代码通过此属性访问战斗体系统。
        /// </summary>
        public BDP.Combat.CombatBodyRuntime Runtime { get; private set; }

        /// <summary>
        /// 基因定义（供Runtime访问配置）。
        /// </summary>
        public GeneDef GeneDef => def;

        public override void PostAdd()
        {
            base.PostAdd();


            // Stat重新聚合，更新CompTrion.max
            pawn?.GetComp<CompTrion>()?.RefreshMax();

            // 初始化快照对象
            snapshot = new CombatBodySnapshot(pawn);
            Log.Message($"[BDP] 快照对象初始化完成: snapshot={snapshot != null}");

            // 初始化状态聚合器
            state = new CombatBodyState();

            // 初始化协调器
            orchestrator = new CombatBodyOrchestrator();

            // 创建运行时聚合体
            Runtime = new BDP.Combat.CombatBodyRuntime(
                pawn, this, state, snapshot, orchestrator);

            // 初始化战斗体装备
            InitializeCombatApparel();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            // max可能缩小
            pawn.GetComp<CompTrion>()?.RefreshMax();
        }

        /// <summary>
        /// 从 GeneDef.modExtensions 读取战斗体装备配置并生成装备。
        /// </summary>
        private void InitializeCombatApparel()
        {
            var extension = def?.GetModExtension<GeneExtension_CombatBody>();
            if (extension?.defaultCombatApparel != null)
            {
                foreach (var apparelDef in extension.defaultCombatApparel)
                {
                    if (apparelDef == null) continue;

                    var apparel = ThingMaker.MakeThing(apparelDef) as Apparel;
                    if (apparel != null && snapshot?.CombatApparelContainer != null)
                    {
                        snapshot.CombatApparelContainer.TryAdd(apparel);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();


            Scribe_Deep.Look(ref snapshot, "snapshot", pawn);
            Scribe_Deep.Look(ref state, "state");


            // 读档后如果snapshot仍为null,尝试重新初始化
            if (Scribe.mode == LoadSaveMode.PostLoadInit && snapshot == null && pawn != null)
            {
                Log.Warning($"[BDP] {pawn.Name} 读档后snapshot为null,尝试重新初始化");
                snapshot = new CombatBodySnapshot(pawn);
                InitializeCombatApparel();
            }

            // 读档后如果state仍为null,尝试重新初始化
            if (Scribe.mode == LoadSaveMode.PostLoadInit && state == null)
            {
                Log.Warning($"[BDP] {pawn?.Name} 读档后state为null,尝试重新初始化");
                state = new CombatBodyState();
            }

            // 读档后重新初始化orchestrator(不序列化)
            if (Scribe.mode == LoadSaveMode.PostLoadInit && orchestrator == null)
            {
                orchestrator = new CombatBodyOrchestrator();
            }

            // 读档后重建Runtime（不序列化）
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Runtime = new BDP.Combat.CombatBodyRuntime(
                    pawn, this, state, snapshot, orchestrator);
                Log.Message($"[BDP] {pawn?.Name} 读档后重建Runtime完成");
            }
        }

        /// <summary>
        /// 添加战斗体控制按钮
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // 防御性检查
            if (pawn == null)
            {
                yield break;
            }

            // 先返回基类的 gizmos
            var baseGizmos = base.GetGizmos();
            if (baseGizmos != null)
            {
                foreach (var gizmo in baseGizmos)
                {
                    if (gizmo != null)
                    {
                        yield return gizmo;
                    }
                }
            }

            // 只有玩家派系才显示按钮
            if (pawn.Faction == null || Faction.OfPlayer == null || pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            // 准备图标
            Texture2D buttonIcon = null;
            if (BaseContent.BadTex != null)
            {
                buttonIcon = BaseContent.BadTex;
            }

            if (buttonIcon != null && TexCommand.Attack != null)
            {
                buttonIcon = TexCommand.Attack;
            }

            // 如果图标还是 null，就不显示按钮
            if (buttonIcon == null)
            {
                yield break;
            }

            // 创建按钮
            var cmd = new Command_Action();
            cmd.defaultLabel = Runtime?.IsActive == true ? "解除战斗体" : "激活战斗体";
            cmd.defaultDesc = Runtime?.IsActive == true
                ? "解除战斗体，恢复原始状态"
                : "激活战斗体，进入战斗模式";
            cmd.icon = buttonIcon;

            // 预先检查前置条件（仅在未激活时检查）
            if (Runtime?.IsActive != true)
            {
                // 优先检查冷却状态
                if (Runtime?.State != null && !Runtime.State.CanActivate())
                {
                    int remainingTicks = Runtime.State.GetCooldownRemaining();
                    float remainingDays = remainingTicks / 60000f;
                    cmd.Disable($"冷却中 ({remainingDays:F1}天)");
                }
                else
                {
                    // 原有的静态事件检查
                    var checkArgs = new CanActivateCombatBodyEventArgs { Pawn = pawn };
                    BDPEvents.TriggerCanActivateQuery(checkArgs);
                    if (checkArgs.Vetoed)
                    {
                        cmd.Disable(checkArgs.BlockReason);
                    }
                }
            }

            cmd.action = delegate
            {
                try
                {
                    if (Runtime?.IsActive == true)
                    {
                        DeactivateCombatBody();
                    }
                    else
                    {
                        ActivateCombatBody();
                    }
                }
                catch (System.Exception e)
                {
                    Log.Error($"[BDP] 战斗体切换失败: {e}");
                }
            };

            yield return cmd;

            // 紧急脱离按钮（仅战斗体激活时显示）
            if (Runtime?.IsActive == true)
            {
                var emergencyCmd = new Command_Action();
                emergencyCmd.defaultLabel = "紧急脱离";
                emergencyCmd.defaultDesc = "触发战斗体破裂流程（90 ticks延时后自动解除）";
                emergencyCmd.icon = TexCommand.DesirePower;
                emergencyCmd.action = delegate
                {
                    try
                    {
                        // v13.1：统一走破裂流程（TriggerCollapse → 90 ticks延时 → 自动解除）
                        var runtime = Runtime;
                        if (runtime != null && runtime.IsActive)
                        {
                            CombatBodyOrchestrator.TriggerCollapse(pawn, runtime, "手动紧急脱离");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Log.Error($"[BDP] 紧急脱离失败: {e}");
                    }
                };

                yield return emergencyCmd;
            }

            // 开发模式：查看快照状态
            if (Prefs.DevMode && TexCommand.GatherSpotActive != null)
            {
                var debugCmd = new Command_Action();
                debugCmd.defaultLabel = "查看快照";
                debugCmd.defaultDesc = "（开发模式）查看战斗体快照的详细状态";
                debugCmd.icon = TexCommand.GatherSpotActive;
                debugCmd.action = delegate
                {
                    try
                    {
                        InspectSnapshot();
                    }
                    catch (System.Exception e)
                    {
                        Log.Error($"[BDP] 查看快照失败: {e}");
                    }
                };

                yield return debugCmd;
            }
        }

        /// <summary>
        /// 激活战斗体（v13.0重构版 - 委托Runtime）。
        ///
        /// 职责简化：
        /// - 只负责基本的防御性检查
        /// - 将复杂的激活流程委托给Runtime
        /// - 提供清晰的错误处理
        /// </summary>
        private void ActivateCombatBody()
        {
            Log.Message($"[BDP] ActivateCombatBody() 被调用: pawn={pawn?.Name}");

            // 防御性检查
            if (Runtime == null)
            {
                Log.Error($"[BDP] {pawn.Name} 的Runtime对象为 null");
                Messages.Message($"{pawn.Name} 的战斗体系统异常", MessageTypeDefOf.RejectInput);
                return;
            }

            try
            {
                // 委托给Runtime执行完整的激活流程
                Runtime.TryActivate();
            }
            catch (System.Exception e)
            {
                Log.Error($"[BDP] 激活战斗体失败: {e}");
                Messages.Message($"{pawn.Name} 激活战斗体失败", MessageTypeDefOf.RejectInput);
            }
        }

        /// <summary>
        /// 解除战斗体（v13.0重构版 - 委托Runtime）。
        ///
        /// 职责简化：
        /// - 只负责基本的防御性检查
        /// - 将复杂的解除流程委托给Runtime
        /// - 提供清晰的错误处理
        /// </summary>
        /// <param name="isEmergency">是否为紧急脱离（true=强制耗尽+枯竭，false=正常释放）</param>
        public void DeactivateCombatBody(bool isEmergency = false)
        {
            // 防御性检查
            if (Runtime == null)
            {
                Log.Error($"[BDP] {pawn.Name} 的Runtime对象为 null");
                Messages.Message($"{pawn.Name} 的战斗体系统异常", MessageTypeDefOf.RejectInput);
                return;
            }

            try
            {
                // 委托给Runtime执行完整的解除流程
                Runtime.Deactivate(isEmergency);
            }
            catch (System.Exception e)
            {
                Log.Error($"[BDP] 解除战斗体失败: {e}");
                Messages.Message($"{pawn.Name} 解除战斗体失败", MessageTypeDefOf.RejectInput);
            }
        }

        /// <summary>
        /// 查看快照状态（开发模式）
        /// </summary>
        private void InspectSnapshot()
        {
            if (snapshot == null)
            {
                Log.Error($"[BDP] {pawn.Name} 的快照对象为 null");
                return;
            }

            Log.Message($"[BDP] === {pawn.Name} 快照状态 ===");
            Log.Message($"  战斗体状态: {(Runtime?.IsActive == true ? "激活" : "未激活")}");
            Log.Message($"  原衣物容器: {snapshot.OriginalApparelContainer?.Count ?? 0} 件");
            Log.Message($"  战斗体衣物容器: {snapshot.CombatApparelContainer?.Count ?? 0} 件");
            Log.Message($"  原物品容器: {snapshot.OriginalInventoryContainer?.Count ?? 0} 件");

            if (snapshot.OriginalApparelContainer != null && snapshot.OriginalApparelContainer.Count > 0)
            {
                Log.Message("  原衣物列表:");
                foreach (var apparel in snapshot.OriginalApparelContainer)
                {
                    Log.Message($"    - {apparel.Label}");
                }
            }

            if (snapshot.CombatApparelContainer != null && snapshot.CombatApparelContainer.Count > 0)
            {
                Log.Message("  战斗体衣物列表:");
                foreach (var apparel in snapshot.CombatApparelContainer)
                {
                    Log.Message($"    - {apparel.Label}");
                }
            }

            if (snapshot.OriginalInventoryContainer != null && snapshot.OriginalInventoryContainer.Count > 0)
            {
                Log.Message("  原物品列表:");
                foreach (var item in snapshot.OriginalInventoryContainer)
                {
                    Log.Message($"    - {item.Label} x{item.stackCount}");
                }
            }

            Messages.Message($"{pawn.Name} 快照状态已输出到日志", MessageTypeDefOf.TaskCompletion);
        }
    }
}
