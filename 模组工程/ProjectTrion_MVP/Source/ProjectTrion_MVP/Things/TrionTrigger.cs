using System.Collections.Generic;
using Verse;
using RimWorld;
using ProjectTrion.Components;
using UnityEngine;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion 触发器装备 - 继承 Apparel（穿戴装备）
    ///
    /// 关键改动：
    /// - 改为 Apparel 而非 ThingWithComps
    /// - 穿戴关系由 Apparel 系统全自动管理
    /// - wearer 属性由 Apparel 提供
    /// - 使用延迟初始化（访问时自动创建mounts）
    ///
    /// 工作流程：
    /// 1. 玩家从地图获取或制造 TrionTrigger 物品
    /// 2. 穿戴到Pawn身上：pawn.apparel.Wear(trigger)
    /// 3. 首次访问 Mounts 时自动初始化3个挂载点
    /// 4. 通过 GetWearerCompTrion() 与Pawn的CompTrion交互
    /// 5. 脱下时 Apparel 系统自动清理穿戴状态
    /// </summary>
    public class TrionTrigger : Apparel
    {
        // ============ 数据 ============
        private List<TriggerMount> _mounts = null;

        // ============ 属性 ============
        /// <summary>
        /// 获取挂载点列表（延迟初始化）
        /// </summary>
        public List<TriggerMount> Mounts
        {
            get
            {
                // 首次访问时创建默认的3个挂载点
                if (_mounts == null)
                {
                    _mounts = new List<TriggerMount>
                    {
                        new TriggerMount(this, "LeftHand", 4),
                        new TriggerMount(this, "RightHand", 4),
                        new TriggerMount(this, "Special", 1)
                    };
                    Log.Message("[Trion] 触发器已初始化，3个挂载点准备完毕");
                }
                return _mounts;
            }
        }

        public int MountCount => Mounts.Count;

        // ============ 查询方法 ============

        /// <summary>
        /// 获取穿着者（Pawn）
        /// wearer 属性由 Apparel 系统提供
        /// </summary>
        public Pawn GetWearer()
        {
            return Wearer;  // 使用 Apparel 的 Wearer 属性
        }

        /// <summary>
        /// 获取穿着者的 CompTrion
        /// </summary>
        public CompTrion GetWearerCompTrion()
        {
            return Wearer?.GetComp<CompTrion>();
        }

        /// <summary>
        /// 根据槽位名称获取挂载点
        /// </summary>
        public TriggerMount GetMount(string slotName)
        {
            foreach (var mount in Mounts)
            {
                if (mount.SlotName == slotName)
                    return mount;
            }
            return null;
        }

        // ============ Gizmo 界面 ============

        /// <summary>
        /// 穿戴装备时提供的 Gizmo（按钮）
        /// 在此提供变身/激活Trion的按钮
        /// </summary>
        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            foreach (var gizmo in base.GetWornGizmos())
            {
                yield return gizmo;
            }

            // 只有穿着者存在时才显示按钮
            if (Wearer == null)
                yield break;

            var comp = GetWearerCompTrion();

            // 变身按钮：激活Trion战斗体
            if (!CanGenerateCombatBody(out string disableReason))
            {
                yield return new Command_Action
                {
                    defaultLabel = "生成战斗体",
                    defaultDesc = $"激活Trion能力，生成战斗体。\n\n【无法使用】{disableReason}",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/CastAbility", true),
                    action = () => Messages.Message($"无法生成战斗体：{disableReason}", MessageTypeDefOf.RejectInput, historical: false)
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "生成战斗体",
                    defaultDesc = "激活Trion能力，生成战斗体。\n\n需要条件：\n- 已植入Trion腺体\n- Trion可用值 > 0\n- 已装备触发器",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/CastAbility", true),
                    action = () => GenerateCombatBodyAction()
                };
            }
        }

        /// <summary>
        /// 检查是否满足变身条件
        /// </summary>
        private bool CanGenerateCombatBody(out string reason)
        {
            reason = "";

            // 检查1：是否有Trion腺体
            if (!Wearer.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_TrionGland", false)))
            {
                reason = "未植入Trion腺体";
                return false;
            }

            var comp = GetWearerCompTrion();
            if (comp == null)
            {
                reason = "无Trion能力";
                return false;
            }

            // 检查2：Trion可用值是否 > 0
            if (comp.Available <= 0)
            {
                reason = $"可用Trion不足 ({comp.Available:F1}/{comp.Capacity:F1})";
                return false;
            }

            // 检查3：是否已在战斗体状态
            if (comp.IsInCombat)
            {
                reason = "已处于战斗体状态";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成战斗体的按钮操作
        /// </summary>
        private void GenerateCombatBodyAction()
        {
            if (!CanGenerateCombatBody(out string reason))
            {
                Messages.Message($"无法生成战斗体：{reason}", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            var comp = GetWearerCompTrion();
            if (comp != null)
            {
                comp.GenerateCombatBody();
            }
        }

        // ============ 序列化 ============

        /// <summary>
        /// 存档/读档处理
        ///
        /// Apparel 会自动序列化：
        /// - wearer 引用
        /// - 穿戴状态
        ///
        /// 我们只需要序列化自定义的 _mounts 列表
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _mounts, "mounts", LookMode.Deep);
        }
    }
}
