using System;
using System.Collections.Generic;
using BDP.Content.Trion.Units;
using BDP.Core.CombatBody;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Trion.Units
{
    /// <summary>永久身体真实生命周期的开发入口，随开发程序集整体移除。</summary>
    public static class TrionUnitDebugActions
    {
        /// <summary>所有可见菜单内容从开发语言文件读取。</summary>
        [DebugAction("BDP", requiresBiotech = true, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode Menu()
        {
            var root = new DebugActionNode("BDP_Dev_TrionUnit_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Spawn".Translate(), DebugActionType.ToolMap, () => Spawn(true)));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_SpawnEnemy".Translate(), DebugActionType.ToolMap, () => Spawn(false)));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Bind".Translate(), DebugActionType.ToolMap, () =>
                WithUnit(unit => unit.TryBindTo(Verse.UI.MouseCell().GetEdifice(Find.CurrentMap)))));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Permit".Translate(), action: () =>
                WithUnit(unit => Source(unit)?.SetPermit(!Source(unit).PermitEnabled))));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_NoBandwidth".Translate(), action: () =>
                WithUnit(unit => Source(unit)?.SetBandwidth(0))));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Bandwidth".Translate(), action: () =>
                WithUnit(unit => Source(unit)?.SetBandwidth(2))));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Unbind".Translate(), action: () => WithUnit(unit => unit.Unbind())));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_DestroyOwner".Translate(), action: () =>
                WithUnit(unit => unit.ControlOwner?.Destroy())));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Wound".Translate(), action: () => WithUnit(Wound)));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Zero".Translate(), action: () =>
                WithUnit(unit => TrionSurfaceAccess.ResolveCommands(unit.Pawn)?.TrySetCurrent(0f))));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Refill".Translate(), action: () => WithUnit(Refill)));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_Check".Translate(), action: () => WithUnit(Check)));
            root.AddChild(new DebugActionNode("BDP_Dev_TrionUnit_ClearMap".Translate(), action: ClearMapSamples));
            return root;
        }

        /// <summary>原版生成一名样本和一个无正式玩法的测试归属对象。</summary>
        private static void Spawn(bool player)
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = Verse.UI.MouseCell();
            IntVec3 sourceCell = cell + IntVec3.East;
            if (map == null || !cell.InBounds(map) || !cell.Standable(map)
                || (player && (!sourceCell.InBounds(map) || !sourceCell.Standable(map)
                    || sourceCell.GetEdifice(map) != null)))
            {
                Messages.Message("BDP_Dev_TrionUnit_InvalidCell".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Pawn unitPawn = PawnGenerator.GeneratePawn(PawnKindDef.Named("BDP_DebugTrionUnit"),
                player ? Faction.OfPlayer : Faction.OfMechanoids);
            GenSpawn.Spawn(unitPawn, cell, map);
            if (!player)
            {
                // 敌方样本不挂归属，不改变当前玩家选择，便于继续点击自动战斗按钮。
                Messages.Message("BDP_Dev_TrionUnit_EnemySpawned".Translate(), unitPawn, MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            var owner = (Building)ThingMaker.MakeThing(ThingDef.Named("BDP_DebugTrionUnitSource"));
            owner.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(owner, sourceCell, map);
            CompTrionUnit unit = unitPawn.TryGetComp<CompTrionUnit>();
            owner.TryGetComp<CompTrionUnitDebugSource>().SetPermit(true);
            unit.TryBindTo(owner);
            Find.Selector.ClearSelection();
            Find.Selector.Select(unitPawn);
            Messages.Message("BDP_Dev_TrionUnit_PlayerSpawned".Translate(), unitPawn, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>测试开关只识别开发来源，不修改正式核心。</summary>
        private static CompTrionUnitDebugSource Source(CompTrionUnit unit) =>
            (unit.ControlOwner as ThingWithComps)?.TryGetComp<CompTrionUnitDebugSource>();
        /// <summary>仅在选中真实测试兵时执行菜单动作。</summary>
        private static void WithUnit(Action<CompTrionUnit> action)
        {
            CompTrionUnit unit = (Find.Selector.SingleSelectedThing as Pawn)?.TryGetComp<CompTrionUnit>();
            if (unit == null || !IsTestSample(unit.Pawn))
            {
                Messages.Message("BDP_Dev_TrionUnit_Select".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            action(unit);
        }

        /// <summary>仅按专属开发定义识别样本和尸体，正式兵与普通机械体不进入清除范围。</summary>
        internal static bool IsTestSample(Thing thing)
        {
            if (thing is Corpse corpse) thing = corpse.InnerPawn;
            return (thing is Pawn && thing.def?.defName == "BDP_DebugTrionUnitRace")
                || (thing is Building && thing.def?.defName == "BDP_DebugTrionUnitSource");
        }

        /// <summary>直接调用资源命令补满测试兵，不修复伤势、不实现正式恢复站。</summary>
        private static void Refill(CompTrionUnit unit)
        {
            ITrionReader resource = TrionSurfaceAccess.ResolveReader(unit.Pawn);
            if (resource == null || resource.Max <= 0f)
            {
                Messages.Message("BDP_Dev_TrionUnit_WaitInitialization".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            TrionSurfaceAccess.ResolveCommands(unit.Pawn)?.TrySetCurrent(resource.Max);
        }

        /// <summary>清除本图可见样本、尸体与测试座；原版销毁入口负责订阅和归属清理。</summary>
        private static void ClearMapSamples()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            int count = 0;
            foreach (Thing thing in new List<Thing>(map.listerThings.AllThings))
            {
                if (thing.Destroyed || !IsTestSample(thing)) continue;
                thing.Destroy(DestroyMode.Vanish);
                count++;
            }
            Messages.Message("BDP_Dev_TrionUnit_Cleared".Translate(count.ToString()), MessageTypeDefOf.NeutralEvent, false);
        }
        /// <summary>向原版核心部位添加可泄露的普通切伤，伤口计算仍走真实补丁链。</summary>
        private static void Wound(CompTrionUnit unit)
        {
            Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Cut, unit.Pawn, unit.Pawn.RaceProps.body.corePart);
            injury.Severity = 2f;
            unit.Pawn.health.AddHediff(injury);
        }
        /// <summary>现场检查宿主、资源、装配与停机效果；不能由编译代替。</summary>
        private static void Check(CompTrionUnit unit)
        {
            unit.RefreshOperatingConditions();
            ITrionReader resource = TrionSurfaceAccess.ResolveReader(unit.Pawn);
            ITriggerLoadoutReader loadout = TriggerSurfaceAccess.ResolveLoadoutReader(unit.Pawn);
            int active = 0;
            if (loadout != null) foreach (ITriggerSlotState slot in loadout.GetActiveSlots()) if (!slot.IsBindingMirror) active++;
            // 读取当前真实槽位，复活前后可区分装配丢失、未激活与功能未发布；不改变测试对象。
            int loaded = 0;
            var slotDetails = new List<string>();
            if (loadout != null)
                foreach (ITriggerSlotState slot in loadout.GetAllSlots())
                {
                    if (slot.LoadedChip != null && !slot.IsBindingMirror) loaded++;
                    slotDetails.Add("BDP_Dev_TrionUnit_SlotSnapshot".Translate(
                        slot.Side.ToString(), (slot.Index + 1).ToString(), slot.LoadedChip?.LabelCap.ToString() ?? "-",
                        slot.LoadedChip?.ThingID ?? "-", slot.IsActive.ToString(), slot.IsDisabled.ToString()));
                }
            bool valid = resource != null && resource.Max > 0 && resource.Allocated == 0
                && CombatBodyMaterialAccess.IsActive(unit.Pawn)
                && CombatBodySurfaceAccess.ResolveReader(unit.Pawn) == null
                && unit.Pawn.OverseerSubject == null && unit.Pawn.needs.energy == null
                && unit.InternalEquipment != null && active == 2;
            // 不只数槽位：实际投影必须随运行同步，否则会漏掉发布入口或读档重建错误。
            valid &= unit.Pawn.health.hediffSet.HasHediff(HediffDef.Named("BDP_Hediff_EnergyShield_Auto"))
                == unit.IsOperational;
            Log.Message("[BDP.TrionUnit] " + "BDP_Dev_TrionUnit_Snapshot".Translate(
                valid.ToString(), unit.IsOperational.ToString(), (resource?.Cur ?? 0).ToString("0.##"),
                (resource?.Max ?? 0).ToString("0.##"), active.ToString())
                + " " + "BDP_Dev_TrionUnit_CombatSnapshot".Translate(unit.AutoCombatEnabled.ToString(),
                    unit.Pawn.Drafted.ToString(), unit.Pawn.CurJob?.def?.defName ?? "-")
                + "\n" + "BDP_Dev_TrionUnit_LoadoutSnapshot".Translate(unit.InternalEquipment?.ThingID ?? "-",
                    unit.Pawn.equipment?.Primary?.ThingID ?? "-", loaded.ToString())
                + "\n" + string.Join("\n", slotDetails));
        }
    }
}
