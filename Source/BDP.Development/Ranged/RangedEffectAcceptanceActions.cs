using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP.Content.HitPartPolicy;
using BDP.Content.RangedModules.Debuff;
using BDP.Content.RangedModules.Debuff.Effects;
using BDP.Content.RangedModules.VitalHit;
using BDP.Core.AttackExecution;
using BDP.Core.Projectiles;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Ranged
{
    /// <summary>远程验收入口；所有反射及诊断仅存在于开发层，不为正式程序集添加测试入口。</summary>
    public static class RangedEffectAcceptanceActions
    {
        /// <summary>默认关闭的路径观察开关；删除本开发文件即可整体移除。</summary>
        internal static bool Watching;

        /// <summary>为实际游戏提供局部效果验收与正常射击观察入口。</summary>
        [DebugAction("BDP", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode Menu()
        {
            var root = new DebugActionNode("BDP_RangedAcceptance_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_RangedAcceptance_Generate".Translate(), action: Generate));
            root.AddChild(new DebugActionNode("BDP_RangedAcceptance_Collapse".Translate(), action: CheckCollapse));
            root.AddChild(new DebugActionNode("BDP_RangedAcceptance_Lead".Translate(), action: CheckLead));
            root.AddChild(new DebugActionNode("BDP_RangedAcceptance_NativeAction".Translate(), action: CheckNative));
            root.AddChild(new DebugActionNode("BDP_RangedAcceptance_Watch".Translate(), action: () =>
            {
                Watching = !Watching;
                Log.Message("BDP_RangedAcceptance_WatchState".Translate(Watching.ToString()));
            }));
            return root;
        }

        /// <summary>生成独立验收人物并选中；后续动作会改变此人物健康状态。</summary>
        private static void Generate()
        {
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(Find.CurrentMap.Center, Find.CurrentMap, 10), Find.CurrentMap);
            Find.Selector.ClearSelection();
            Find.Selector.Select(pawn);
            Log.Message("BDP_RangedAcceptance_Generated".Translate(pawn.LabelShort));
        }

        /// <summary>连续命中、分部位累积、第二发移除、失效不重选及无伤口验收。</summary>
        private static void CheckCollapse()
        {
            Pawn pawn = Selected();
            if (pawn == null) return;
            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts.Where(p =>
                (p.def.defName == "Hand" || p.def.defName == "Foot") && !pawn.health.hediffSet.PartIsMissing(p)).Take(2).ToList();
            if (parts.Count < 2) { Log.Error("BDP_RangedAcceptance_NeedParts".Translate()); return; }
            var effect = DefDatabase<RangedDebuffEffectDef>.GetNamed("BDP_DebuffEffect_BodyPartDisintegration");
            int injuries = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().Count();
            Projectile projectile = MakeProjectile("BDP_Projectile_BodyCollapse");
            Hit(pawn, projectile, effect, Exact(pawn, parts[0]));
            Hit(pawn, projectile, effect, Exact(pawn, parts[1]));
            Check(Math.Abs(Severity(pawn, effect.hediffDef, parts[0]) - 1f / 2f) < 0.00001f, "BDP_RangedAcceptance_FirstHitHalf");
            Hit(pawn, projectile, effect, Exact(pawn, parts[0]));
            Check(pawn.health.hediffSet.PartIsMissing(parts[0]) && !pawn.health.hediffSet.PartIsMissing(parts[1])
                && Math.Abs(Severity(pawn, effect.hediffDef, parts[1]) - 1f / 2f) < 0.00001f, "BDP_RangedAcceptance_SeparateParts");
            Check(!Hit(pawn, projectile, effect, Exact(pawn, parts[0])), "BDP_RangedAcceptance_NoReselect");
            Check(pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().Count() == injuries, "BDP_RangedAcceptance_NoInjury");
            // 用同一宿主更换选择策略；输出选择事实，结果仍由同一崩解包消费。
            Hit(pawn, projectile, effect, new VitalHitConfig { SelectionMode = HitPartSelectionMode.AvoidVital });
            Check(Hit(pawn, projectile, effect, null), "BDP_RangedAcceptance_VanillaFallback");
            Dump(pawn);
        }

        /// <summary>直击、范围事件、上限及恢复初态验收；恢复时序由观察器记录真实游戏刻。</summary>
        private static void CheckLead()
        {
            Pawn pawn = Selected();
            if (pawn == null) return;
            var effect = DefDatabase<RangedDebuffEffectDef>.GetNamed("BDP_DebuffEffect_LeadWeight");
            foreach (Hediff existing in pawn.health.hediffSet.hediffs.Where(h => h.def == effect.hediffDef).ToList()) pawn.health.RemoveHediff(existing);
            Projectile direct = MakeProjectile("BDP_Projectile_BodyCollapse");
            Projectile explosive = MakeProjectile("BDP_Projectile_Explosive");
            Hit(pawn, direct, effect, null);
            Check(Math.Abs(Severity(pawn, effect.hediffDef, null) - 0.1f) < 0.00001f, "BDP_RangedAcceptance_DirectWeight");
            Hit(pawn, explosive, effect, null);
            Check(Math.Abs(Severity(pawn, effect.hediffDef, null) - 0.11f) < 0.00001f, "BDP_RangedAcceptance_AreaWeight");
            for (int i = 0; i < 12; i++) Hit(pawn, direct, effect, null);
            Check(Math.Abs(Severity(pawn, effect.hediffDef, null) - 1f) < 0.00001f, "BDP_RangedAcceptance_WeightCap");
            Watching = true;
            Dump(pawn);
        }

        /// <summary>用真实原版承伤对照初始选择与伤害结果最后部位，效果消费后者。</summary>
        private static void CheckNative()
        {
            Pawn pawn = Selected();
            if (pawn == null) return;
            Hit(pawn, MakeProjectile("BDP_Projectile_BodyCollapse"),
                DefDatabase<RangedDebuffEffectDef>.GetNamed("BDP_DebuffEffect_BodyPartDisintegration"),
                new VitalHitConfig { SelectionMode = HitPartSelectionMode.AvoidVital }, true);
            Dump(pawn);
        }

        /// <summary>完整调用独立选择、通用宿主、生产汇总与命中事实派发；不直接绕过宿主调用效果包。</summary>
        private static bool Hit(Pawn pawn, Projectile projectile, RangedDebuffEffectDef effect, VitalHitConfig selection, bool actualDamage = false)
        {
            var modules = new List<IImpactStageModule>();
            if (selection != null)
            {
                var selector = new VitalHitModule();
                Initialize(selector, selection);
                modules.Add(selector);
            }
            bool area = projectile.def.projectile.explosionRadius > 0f;
            var host = new RangedDebuffModule();
            Initialize(host, new RangedDebuffConfig { EffectDef = effect,
                DamageSuppression = actualDamage ? DamageDisposition.Preserve : DamageDisposition.SuppressAllProjectileImpact,
                TargetScope = area ? ExtraEffectTargetScope.VanillaExplosionAffectedPawns : ExtraEffectTargetScope.DirectHitThing });
            modules.Add(host);
            Type hitType = CoreType("Projectiles.RangedFlightProtocol.Model.HitRecord");
            object hit = Activator.CreateInstance(hitType);
            hitType.GetProperty("IsValidHit").SetValue(hit, true);
            hitType.GetProperty("HitThing").SetValue(hit, pawn);
            hitType.GetProperty("HitCell").SetValue(hit, pawn.Position);
            Type serviceType = CoreType("Projectiles.RangedFlightProtocol.Impact.ImpactStageService");
            object service = Activator.CreateInstance(serviceType, new object[] { modules, null });
            object plan = serviceType.GetMethod("Execute").Invoke(service, new object[] { projectile, null, null, hit, null });
            DamageResolution resolution = DamageResolutionRuntime.CreateModuleInterception(pawn);
            if (actualDamage)
            {
                BodyPartRecord selected = (BodyPartRecord)plan.GetType().GetProperty("HitPartOverride").GetValue(plan);
                DamageWorker.DamageResult result = DamageResolutionRuntime.ApplyDamage(pawn,
                    new DamageInfo(projectile.DamageDef, 1f, 0f, hitPart: selected), out resolution);
                Log.Message("BDP_RangedAcceptance_NativeResult".Translate(selected?.Label ?? "-", result.LastHitPart?.Label ?? "-"));
            }
            BodyPartRecord part = area ? null : (BodyPartRecord)AccessTools.Method(typeof(BdpProjectile), "ResolveDirectEffectHitPart")
                .Invoke(projectile, new[] { plan, pawn, (object)resolution });
            bool applied = AttackTargetEventDispatcher.Dispatch(new AttackTargetEvent
            {
                Source = area ? AttackTargetEventSource.ProducedTarget : AttackTargetEventSource.DirectImpact,
                TargetThing = pawn, HitPart = part, TargetCell = pawn.Position, Map = pawn.Map, Projectile = projectile,
                ExtraEffects = (IReadOnlyList<ExtraEffectPlan>)plan.GetType().GetProperty("ExtraEffects").GetValue(plan)
            });
            Log.Message("BDP_RangedAcceptance_Hit".Translate(effect.LabelCap, part?.Label ?? "-", applied.ToString()));
            return applied;
        }

        /// <summary>配置公开业务模块；对内部配置赋值的反射仅供开发验收使用。</summary>
        private static void Initialize(IRangedAttackModuleRuntime module, RangedModuleConfigNode config)
        {
            var context = new RangedAttackModuleRuntimeContext();
            typeof(RangedAttackModuleRuntimeContext).GetProperty("Config").SetValue(context, config);
            module.Initialize(context);
        }

        /// <summary>精确序号始终根据完整身体树计算，包含已缺失的同名部位。</summary>
        private static VitalHitConfig Exact(Pawn pawn, BodyPartRecord part) => new VitalHitConfig
        {
            SelectionMode = HitPartSelectionMode.ExactDefOccurrence, ExactBodyPartDef = part.def,
            ExactBodyPartOccurrenceIndex = pawn.RaceProps.body.AllParts.Where(p => p.def == part.def).ToList().IndexOf(part)
        };

        /// <summary>读取同一健康状态、同一部位当前层数。</summary>
        private static float Severity(Pawn pawn, HediffDef def, BodyPartRecord part) => pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == def && h.Part == part)?.Severity ?? 0f;

        /// <summary>读取验收目标；不暗中选择其他人物。</summary>
        private static Pawn Selected()
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || pawn.Dead) { Log.Error("BDP_RangedAcceptance_NeedPawn".Translate()); return null; }
            return pawn;
        }

        /// <summary>构造真实原版定义对应的投射物，但不把诊断投射物发射到地图。</summary>
        private static Projectile MakeProjectile(string defName)
        {
            var projectile = (Projectile)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName));
            // 诊断只借用伤害信息构造，给未发射实例有效朝向，避免原版读取零长度旋转。
            AccessTools.Field(typeof(Projectile), "origin").SetValue(projectile, UnityEngine.Vector3.zero);
            AccessTools.Field(typeof(Projectile), "destination").SetValue(projectile, UnityEngine.Vector3.forward);
            return projectile;
        }

        /// <summary>开发层读取正式内部类型，不向正式层扩大公开接口。</summary>
        private static Type CoreType(string suffix) => typeof(BdpProjectile).Assembly.GetType("BDP.Core." + suffix, true);

        /// <summary>记录带固定标记的实际验收结果，失败保留为明确错误。</summary>
        private static void Check(bool passed, string key)
        {
            string text = "BDP_RangedAcceptance_Check".Translate(key.Translate(), passed.ToString());
            if (passed) Log.Message(text); else Log.Error(text);
        }

        /// <summary>输出真实健康状态、部位及恢复组件计时，便于对照游戏刻。</summary>
        internal static void Dump(Pawn pawn)
        {
            foreach (Hediff h in pawn.health.hediffSet.hediffs.Where(h => h.def.defName.StartsWith("BDP_Hediff_")))
            {
                var recovery = h.TryGetComp<HediffComp_StackingRecovery>();
                object tick = recovery == null ? null : AccessTools.Field(typeof(HediffComp_StackingRecovery), "lastHitTick").GetValue(recovery);
                Log.Message("BDP_RangedAcceptance_Health".Translate(Find.TickManager.TicksGame, h.def.defName, h.Part?.Label ?? "-", h.Severity, tick?.ToString() ?? "-"));
            }
        }
    }

    /// <summary>只在手动开启时观察真实飞行与健康状态，不改变战斗裁决。</summary>
    public sealed class MapComponent_RangedAcceptanceDiagnostics : MapComponent
    {
        /// <summary>每发最后记录的路径与阶段，避免每刻重复刷屏。</summary>
        private readonly Dictionary<int, string> last = new Dictionary<int, string>();

        /// <summary>按原版地图组件生命周期构造。</summary>
        public MapComponent_RangedAcceptanceDiagnostics(Map map) : base(map) { }

        /// <summary>观察每次路径切换；恢复状态每秒记录一次，存读档后可重新开启。</summary>
        public override void MapComponentTick()
        {
            if (!RangedEffectAcceptanceActions.Watching) { last.Clear(); return; }
            var live = new HashSet<int>();
            foreach (BdpProjectile projectile in map.listerThings.AllThings.OfType<BdpProjectile>())
            {
                live.Add(projectile.thingIDNumber);
                object plan = AccessTools.Field(typeof(BdpProjectile), "launchPlan").GetValue(projectile);
                if (plan == null) continue;
                var snapshot = (AttackContextSnapshot)plan.GetType().GetProperty("AttackContextSnapshot").GetValue(plan);
                string state = "";
                if (snapshot != null)
                    foreach (object entry in (IEnumerable)AccessTools.Method(typeof(AttackContextSnapshot), "GetEntries").Invoke(snapshot, null))
                    {
                        object node = entry.GetType().GetProperty("Node").GetValue(entry);
                        if (node is BDP.Content.RangedModules.RoutePath.RoutePathState route)
                            state += "BDP_RangedAcceptance_RouteState".Translate(route.PathSnapshot.CurrentLegIndex, route.PathSnapshot.ControlReleased.ToString());
                        if (node is BDP.Content.RangedModules.Homing.HomingState homing)
                            state += "BDP_RangedAcceptance_HomingState".Translate(homing.HasTakenFlightControl.ToString(), homing.Phase.ToString(), homing.RelocksUsed);
                    }
                object path = AccessTools.Field(typeof(BdpProjectile), "currentFlightPathSnapshot").GetValue(projectile);
                if (path is ProjectileFlightPathSnapshot flight) state += "BDP_RangedAcceptance_PathState".Translate(flight.Start.ToString(), flight.End.ToString());
                if (!last.TryGetValue(projectile.thingIDNumber, out string previous) || previous != state)
                {
                    last[projectile.thingIDNumber] = state;
                    Log.Message("BDP_RangedAcceptance_Flight".Translate(Find.TickManager.TicksGame, projectile.ThingID, state));
                }
            }
            foreach (int id in last.Keys.Where(id => !live.Contains(id)).ToList()) last.Remove(id);
            if (Find.TickManager.TicksGame % 60 == 0 && Find.Selector.SingleSelectedThing is Pawn pawn && pawn.Map == map)
                RangedEffectAcceptanceActions.Dump(pawn);
        }
    }
}
