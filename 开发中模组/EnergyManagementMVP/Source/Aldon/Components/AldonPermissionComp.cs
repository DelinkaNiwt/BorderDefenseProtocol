using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Aldon.Energy
{
    /// <summary>
    /// 权限等级枚举
    /// </summary>
    public enum AldonPermissionLevel
    {
        Unauthorized = 0,   // Lv0 - 无权限
        Authorized = 1      // Lv1 - 授权用户
    }

    /// <summary>
    /// 权限组件 - 挂载在殖民者身上
    /// 控制该殖民者是否可以使用能量系统
    /// </summary>
    public class AldonPermissionComp : ThingComp
    {
        // ===== 状态数据 =====
        private AldonPermissionLevel permissionLevel = AldonPermissionLevel.Unauthorized;

        // ===== 公开属性 =====
        public AldonPermissionLevel PermissionLevel => permissionLevel;
        public bool HasPermission => permissionLevel >= AldonPermissionLevel.Authorized;

        // ===== 权限操作 =====

        /// <summary>
        /// 赋予权限 - 设置为Lv1授权用户
        /// </summary>
        public void GrantPermission()
        {
            permissionLevel = AldonPermissionLevel.Authorized;

            Pawn pawn = parent as Pawn;
            if (pawn != null)
            {
                Messages.Message(
                    $"已赋予 {pawn.Name.ToStringShort} Aldon能量系统权限",
                    MessageTypeDefOf.PositiveEvent
                );
                Log.Message($"[Aldon] 赋予权限: {pawn.Name.ToStringShort}");
            }
        }

        /// <summary>
        /// 撤销权限 - 设置为Lv0无权限
        /// </summary>
        public void RevokePermission()
        {
            permissionLevel = AldonPermissionLevel.Unauthorized;

            Pawn pawn = parent as Pawn;
            if (pawn != null)
            {
                Messages.Message(
                    $"已撤销 {pawn.Name.ToStringShort} 的Aldon权限",
                    MessageTypeDefOf.NeutralEvent
                );
                Log.Message($"[Aldon] 撤销权限: {pawn.Name.ToStringShort}");
            }
        }

        // ===== UI Gizmo =====

        /// <summary>
        /// 获取组件额外的按钮
        /// 选中殖民者时显示权限控制和能量调试按钮
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
                yield break;

            // ===== 权限控制按钮 =====
            if (!HasPermission)
            {
                // 显示授权按钮
                yield return new Command_Action
                {
                    defaultLabel = "授予Aldon权限",
                    defaultDesc = "赋予此殖民者使用Aldon能量系统的权限，可以通过武器消耗能量",
                    icon = TexCommand.Attack,
                    action = () => GrantPermission()
                };
            }
            else
            {
                // 显示撤销权限按钮
                yield return new Command_Action
                {
                    defaultLabel = "撤销Aldon权限",
                    defaultDesc = "撤销此殖民者的Aldon能量系统权限，无法再使用消耗能量的武器",
                    icon = TexCommand.CannotShoot,
                    action = () => RevokePermission()
                };
            }

            // ===== 能量调试按钮（开发模式） =====
            if (!Prefs.DevMode)
                yield break;

            // 获取能量组件
            AldonEnergyComp energyComp = pawn.GetComp<AldonEnergyComp>();
            if (energyComp == null)
                yield break;

            // 按钮1：查看能量状态
            yield return new Command_Action
            {
                defaultLabel = "查看能量",
                defaultDesc = $"容量: {energyComp.CurrentCapacity}\n可用: {energyComp.Available}\n已消耗: {energyComp.UsedEnergy}\n已锁定: {energyComp.LockedEnergy}",
                icon = TexCommand.Attack,
                action = () => LogEnergyStatus(pawn, energyComp)
            };

            // 按钮2：充能+100
            yield return new Command_Action
            {
                defaultLabel = "充能+100",
                defaultDesc = "增加100点能量（测试用）",
                icon = TexCommand.Attack,
                action = () =>
                {
                    energyComp.AddEnergy(100);
                    Log.Message($"[Aldon] {pawn.Name.ToStringShort} 手动充能+100");
                }
            };

            // 按钮3：放电-100
            yield return new Command_Action
            {
                defaultLabel = "放电-100",
                defaultDesc = "消耗100点能量（测试用）",
                icon = TexCommand.CannotShoot,
                action = () =>
                {
                    if (energyComp.TryConsume(100))
                    {
                        Log.Message($"[Aldon] {pawn.Name.ToStringShort} 手动放电-100");
                    }
                    else
                    {
                        Log.Warning($"[Aldon] {pawn.Name.ToStringShort} 放电失败（能量或权限不足）");
                    }
                }
            };
        }

        /// <summary>
        /// 输出能量状态到日志
        /// </summary>
        private void LogEnergyStatus(Pawn pawn, AldonEnergyComp energyComp)
        {
            if (energyComp == null)
            {
                Log.Warning($"[Aldon] {pawn.Name.ToStringShort} 没有能量组件");
                return;
            }

            Log.Message($"====== {pawn.Name.ToStringShort} 能量状态 ======");
            Log.Message($"容量上限: {energyComp.CurrentCapacity}点");
            Log.Message($"已消耗(恢复中): {energyComp.UsedEnergy}点");
            Log.Message($"已锁定(装备占用): {energyComp.LockedEnergy}点");
            Log.Message($"当前可用: {energyComp.Available}点");
            Log.Message($"恢复进度: {energyComp.RecoveryPercent * 100:F1}%");
            Log.Message($"信号状态: {(AldonSignalManager.Instance.IsActive ? "激活" : "关闭")}");
            Log.Message($"权限状态: {(HasPermission ? "已授权" : "无权限")}");
            Log.Message($"==========================================");
        }

        // ===== 数据持久化 =====

        /// <summary>
        /// 存档保存/加载
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref permissionLevel, "permissionLevel", AldonPermissionLevel.Unauthorized);
        }
    }
}
