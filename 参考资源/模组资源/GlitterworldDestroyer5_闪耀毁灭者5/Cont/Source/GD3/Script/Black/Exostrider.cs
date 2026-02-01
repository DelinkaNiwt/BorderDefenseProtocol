using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Exostrider : Building_Turret
    {
        protected int burstCooldownTicksLeft;

        protected int burstWarmupTicksLeft;

        private int damagedTicks;

        private int deathTicks;

        public int helpTicks;

        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

        Sustainer sustainer;

        Mote aimTargetMote;

        private bool holdFire;

        private bool burstActivated;

        public Thing gun;

        protected TurretTop top;

        protected CompPowerTrader powerComp;

        protected CompCanBeDormant dormantComp;

        protected CompInitiatable initiatableComp;

        protected CompMannable mannableComp;

        protected CompInteractable interactableComp;

        public CompRefuelable refuelableComp;

        protected Effecter progressBarEffecter;

        protected CompMechPowerCell powerCellComp;

        private const int TryStartShootSomethingIntervalTicks = 10;

        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

        public bool Active
        {
            get
            {
                if ((powerComp == null || powerComp.PowerOn) && (dormantComp == null || dormantComp.Awake) && (initiatableComp == null || initiatableComp.Initiated) && (interactableComp == null || burstActivated))
                {
                    if (powerCellComp != null)
                    {
                        return !powerCellComp.depleted;
                    }

                    return true;
                }

                return false;
            }
        }

        public ExostriderExtension extension
        {
            get
            {
                return this.def.GetModExtension<ExostriderExtension>();
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (health <= extension.health / 2)
                {
                    return extension.graphicData.GraphicColoredFor(this);
                }
                return base.Graphic;
            }
        }

        public List<Building> Mortars
        {
            get
            {
                return Map.listerBuildings.allBuildingsColonist.FindAll(b => b.def.defName == "Turret_GiantAutoMortar_Script");
            }
        }

        public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

        public override LocalTargetInfo CurrentTarget => currentTargetInt;

        private bool WarmingUp => burstWarmupTicksLeft > 0;

        public override Verb AttackVerb => GunCompEq.PrimaryVerb;

        public bool IsMannable => mannableComp != null;

        private bool IsMortar => def.building.IsMortar;

        private bool IsMortarOrProjectileFliesOverhead
        {
            get
            {
                if (!AttackVerb.ProjectileFliesOverhead())
                {
                    return IsMortar;
                }

                return true;
            }
        }

        private bool IsActivable => interactableComp != null;

        protected virtual bool HideForceTargetGizmo => false;

        public TurretTop Top => top;

        public ExostriderDummy dummy
        {
            get
            {
                List<Thing> list = Map.listerThings.AllThings.FindAll(t => t is ExostriderDummy);
                if (list.Count > 0)
                {
                    return list[0] as ExostriderDummy;
                }
                return null;
            }
        }
            
        public Exostrider()
        {
            top = new TurretTop(this);
        }

        public override void PostMake()
        {
            base.PostMake();
            burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
            MakeGun();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();
            interactableComp = GetComp<CompInteractable>();
            refuelableComp = GetComp<CompRefuelable>();
            powerCellComp = GetComp<CompMechPowerCell>();
            if (!respawningAfterLoad)
            {
                top.SetRotationFromOrientation();
                health = extension.health;
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            ResetCurrentTarget();
            progressBarEffecter?.Cleanup();
            health = extension.health;
        }

        public int health = 14;

        public int readyToFightTicks;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.Look(ref damagedTicks, "damagedTicks", 0);
            Scribe_Values.Look(ref deathTicks, "deathTicks", 0);
            Scribe_Values.Look(ref readyToFightTicks, "readyToFightTicks", 0);
            Scribe_TargetInfo.Look(ref currentTargetInt, "currentTarget");
            Scribe_Values.Look(ref holdFire, "holdFire", defaultValue: false);
            Scribe_Values.Look(ref burstActivated, "burstActivated", defaultValue: false);
            Scribe_Deep.Look(ref gun, "gun");
            Scribe_Values.Look(ref health, "health", defaultValue: 14);
            Scribe_Values.Look<float>(ref this.impactAreaRadius, "impactAreaRadius", 5.9f, false);
            BackCompatibility.PostExposeData(this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (gun == null)
                {
                    Log.Error("Turret had null gun after loading. Recreating.");
                    MakeGun();
                }
                else
                {
                    UpdateGunVerbs();
                }
            }
        }

        public override AcceptanceReport ClaimableBy(Faction by)
        {
            if (!base.ClaimableBy(by))
            {
                return false;
            }

            if (mannableComp != null && mannableComp.ManningPawn != null)
            {
                return false;
            }

            if (Active && mannableComp == null)
            {
                return false;
            }

            if (((dormantComp != null && !dormantComp.Awake) || (initiatableComp != null && !initiatableComp.Initiated)) && (powerComp == null || powerComp.PowerOn))
            {
                return false;
            }

            return true;
        }

        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (!targ.IsValid)
            {
                if (forcedTarget.IsValid)
                {
                    ResetForcedTarget();
                }

                return;
            }

            if ((targ.Cell - base.Position).LengthHorizontal < AttackVerb.verbProps.EffectiveMinRange(targ, this))
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if ((targ.Cell - base.Position).LengthHorizontal > AttackVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (forcedTarget != targ)
            {
                forcedTarget = targ;
                if (burstCooldownTicksLeft <= 0)
                {
                    TryStartShootSomething(canBeginBurstImmediately: false);
                }
            }

            if (holdFire)
            {
                Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(def.label), this, MessageTypeDefOf.RejectInput, historical: false);
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (damagedTicks > 0)
            {
                damagedTicks--;
            }
            if (deathTicks > 0)
            {
                deathTicks--;
                if (deathTicks == 330 || deathTicks == 300 || deathTicks == 270 || deathTicks == 240 || deathTicks == 220 || deathTicks == 200 || deathTicks == 180 || deathTicks == 160 || deathTicks == 140 || deathTicks == 120 || deathTicks == 100 || deathTicks == 90 || deathTicks == 80 ||
                    deathTicks == 70 || deathTicks == 60 || deathTicks == 50 || deathTicks == 40 || deathTicks == 30 || deathTicks == 20 || deathTicks == 10)
                {
                    StartRandomFire();
                    GenExplosion.DoExplosion(nextExplosionCell, Map, 3.9f, DamageDefOf.Bomb, null, 40, 0.8f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                    FleckMaker.ThrowFireGlow(nextExplosionCell.ToVector3(), Map, 1.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        FleckMaker.ThrowSmoke(nextExplosionCell.ToVector3(), Map, 2.0f);
                    }
                }
                else if (deathTicks == 1)
                {
                    IntVec3 vec = Position;
                    Map map = Map;
                    Kill();
                    for (int i = 0; i < 5; i++)
                    {
                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.AncientExostriderLeg), vec, map, ThingPlaceMode.Near);
                    }
                    GenExplosion.DoExplosion(vec, map, 14.9f, DamageDefOf.Flame, null, 120, 0.8f, null, null, null, null, ThingDefOf.Filth_Fuel, 0.4f, 1, null, null, 255, false, null, 0f, 1, 0.5f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                    GDDefOf.GiantExplosion.Spawn(vec, map, 1.5f);
                    GDDefOf.GD_BigWave.SpawnMaintained(vec, map, 1f);
                    GDDefOf.ExostriderDeath.PlayOneShotOnCamera();
                    Find.CameraDriver.shaker.DoShake(6f, 90);

                    List<Quest> quests = Find.QuestManager.QuestsListForReading;
                    for (int i = 0; i < quests.Count; i++)
                    {
                        Quest quest = quests[i];
                        if (quest.root.defName == "GD_Quest_Exostrider" && quest.State == QuestState.Ongoing)
                        {
                            quest.End(QuestEndOutcome.Success, true, true);
                            MissionComponent component = Find.World.GetComponent<MissionComponent>();
                            component.intelligenceAdvanced += 2500;
                        }
                    }

                    /*List<Thing> list = map.listerThings.AllThings.FindAll(t => t is ExostriderDummy);
                    if (list.Count > 0)
                    {
                        ExostriderDummy dum = list[0] as ExostriderDummy;
                        QuestUtility.SendQuestTargetSignals(dum.questTags, "exostriderDestroyed", dum.Named("SUBJECT"));
                    }*/
                }
            }

            if (deathTicks == 0)
            {
                helpTicks++;
                if (helpTicks > extension.helpTicks && dummy != null)
                {
                    helpTicks = 0;
                    dummy.DoHelp(this);
                }
            }

            if (Active && (mannableComp == null || mannableComp.MannedNow) && !base.IsStunned && base.Spawned)
            {
                GunCompEq.verbTracker.VerbsTick();
                if (AttackVerb.state == VerbState.Bursting)
                {
                    return;
                }

                burstActivated = false;
                if (WarmingUp)
                {
                    burstWarmupTicksLeft--;
                    if (burstWarmupTicksLeft == 0)
                    {
                        BeginBurst();
                        ResetCurrentTarget();
                        sustainer.End();
                        FleckMaker.Static(this.Position + new IntVec3(0,0,2), this.Map, FleckDefOf.PsycastAreaEffect, 4.0f);
                        FleckMaker.ThrowFireGlow(DrawPos + new Vector3(0, 0, 3.0f), Map, 2.0f);
                        FleckMaker.ThrowHeatGlow(this.Position + new IntVec3(0, 0, 2), Map, 2.0f);
                    }
                }
                else
                {
                    if (burstCooldownTicksLeft > 0)
                    {
                        burstCooldownTicksLeft--;
                        /*if (IsMortar)
                        {
                            if (progressBarEffecter == null)
                            {
                                progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
                            }

                            progressBarEffecter.EffectTick(this, TargetInfo.Invalid);
                            MoteProgressBar mote = ((SubEffecter_ProgressBar)progressBarEffecter.children[0]).mote;
                            mote.progress = 1f - (float)Math.Max(burstCooldownTicksLeft, 0) / (float)BurstCooldownTime().SecondsToTicks();
                            mote.offsetZ = -0.8f;
                        }*/
                    }

                    if (Mortars.Count > 0)
                    {
                        readyToFightTicks++;
                        if (readyToFightTicks >= 240 * 60 && readyToFightTicks % (120 * 60) == 0)
                        {
                            TryActivateBurst();
                        }
                    }

                    /*if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                    {
                        TryStartShootSomething(canBeginBurstImmediately: true);
                    }*/
                }

                if (aimTargetMote != null)
                {
                    aimTargetMote.exactPosition = CurrentTarget.CenterVector3;
                    aimTargetMote.exactRotation = 0f;
                    aimTargetMote.Maintain();
                }
                if (CurrentTarget != LocalTargetInfo.Invalid && sustainer != null && !sustainer.Ended)
                {
                    sustainer.Maintain();
                }

                top.TurretTopTick();
            }
            /*else
            {
                ResetCurrentTarget();
            }*/
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            absorbed = true;
            if (dinfo.Def.defName == "BombScript")
            {
                if (dinfo.Instigator != null && dinfo.Instigator.Faction != null && dinfo.Instigator.Faction == Faction.OfMechanoids)
                {
                    return;
                }
                if (health >= 1)
                {
                    absorbed = false;
                    damagedTicks = 180;
                    helpTicks += extension.helpTicks / 4;
                    GDDefOf.Pawn_Mech_Diabolus_Death.PlayOneShot(this);
                    health -= 1;
                    if (health <= 0 && deathTicks == 0)
                    {
                        deathTicks = 360;
                    }
                }
            }
        }

        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            allowDestroyNonDestroyable = true;
            base.Kill(dinfo, exactCulprit);
        }

        public void TryActivateBurst()
        {
            burstActivated = true;
            TryStartShootSomething(canBeginBurstImmediately: true);
            if (CurrentTarget != LocalTargetInfo.Invalid)
            {
                Building thing = (Building)CurrentTarget.Thing;
                if (aimTargetMote == null)
                {
                    aimTargetMote = MoteMaker.MakeStaticMote(thing.TrueCenter(), thing.Map, GDDefOf.Mote_HellsphereCannon_Target, 2.0f, makeOffscreen: true);
                    if (aimTargetMote != null)
                    {
                        aimTargetMote.exactRotation = 0f;
                        aimTargetMote.ForceSpawnTick(450);
                    }
                }
                if (sustainer == null || sustainer.Ended)
                {
                    SoundInfo info = SoundInfo.InMap(thing, MaintenanceType.PerTick);
                    sustainer = GDDefOf.HellsphereCannon_Aiming.TrySpawnSustainer(info);
                }
                Messages.Message("GD.ExostriderStartFight".Translate(), CurrentTarget.Thing, MessageTypeDefOf.NegativeEvent);
            }
        }

        public void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (progressBarEffecter != null)
            {
                progressBarEffecter.Cleanup();
                progressBarEffecter = null;
            }

            if (!base.Spawned || holdFire || (AttackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) || !AttackVerb.Available())
            {
                ResetCurrentTarget();
                return;
            }

            bool isValid = currentTargetInt.IsValid;
            if (forcedTarget.IsValid)
            {
                currentTargetInt = forcedTarget;
            }
            else
            {
                currentTargetInt = TryFindNewTarget();
            }

            if (!isValid && currentTargetInt.IsValid && def.building.playTargetAcquiredSound)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map));
            }

            if (currentTargetInt.IsValid)
            {
                float randomInRange = def.building.turretBurstWarmupTime.RandomInRange;
                if (randomInRange > 0f)
                {
                    burstWarmupTicksLeft = randomInRange.SecondsToTicks();
                }
                else if (canBeginBurstImmediately)
                {
                    BeginBurst();
                }
                else
                {
                    burstWarmupTicksLeft = 1;
                }
            }
            else
            {
                ResetCurrentTarget();
            }
        }

        public virtual LocalTargetInfo TryFindNewTarget()
        {
            IAttackTargetSearcher attackTargetSearcher = TargSearcher();
            Faction faction = attackTargetSearcher.Thing.Faction;
            float range = AttackVerb.verbProps.range;
            if (AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.FindAll(b => b.def.defName == "Turret_GiantAutoMortar_Script").Where(delegate (Building x)
            {
                float num = AttackVerb.verbProps.EffectiveMinRange(x, this);
                float num2 = x.Position.DistanceToSquared(base.Position);
                return num2 > num * num && num2 < range * range;
            }).TryRandomElement(out Building result))
            {
                return result;
            }

            return null;
        }

        private IAttackTargetSearcher TargSearcher()
        {
            if (mannableComp != null && mannableComp.MannedNow)
            {
                return mannableComp.ManningPawn;
            }

            return this;
        }

        protected virtual void BeginBurst()
        {
            AttackVerb.TryStartCastOn(CurrentTarget);
            OnAttackedTarget(CurrentTarget);
        }

        protected void BurstComplete()
        {
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
        }

        protected float BurstCooldownTime()
        {
            if (def.building.turretBurstCooldownTime >= 0f)
            {
                return def.building.turretBurstCooldownTime;
            }

            return AttackVerb.verbProps.defaultCooldownTime;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            stringBuilder.AppendLine("GD.ExostriderIntro".Translate());
            stringBuilder.AppendLine("GD.Exostrider2Intro".Translate());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            EquipmentUtility.Recoil(def.building.turretGunDef, (Verb_LaunchProjectile)AttackVerb, out Vector3 drawOffset, out float angleOffset, top.CurRotation);

            top.DrawTurret(drawLoc + new Vector3(0, 0, 3.0f), drawOffset, angleOffset);

            if ((170 <= damagedTicks && damagedTicks <= 180) || (150 <= damagedTicks && damagedTicks <= 160) || (130 <= damagedTicks && damagedTicks <= 140) || (110 <= damagedTicks && damagedTicks <= 120) || (90 <= damagedTicks && damagedTicks <= 100))
            {
                Vector3 drawPos = DrawPos;
                drawPos.y = AltitudeLayer.Blueprint.AltitudeFor();
                drawPos += new Vector3(0, 0, 0.5f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(Graphic.drawSize.x, 1.0f, Graphic.drawSize.y));
                Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Exostrider_m", ShaderDatabase.Transparent, new Color(1, 1, 1, 0.5f)), 0);
            }

            if (deathTicks == 0)
            {
                Vector3 drawPos2 = DrawPos;
                drawPos2.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                drawPos2 += new Vector3(0, 0, 6.3f);
                Matrix4x4 matrix2 = default(Matrix4x4);
                matrix2.SetTRS(drawPos2, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(3.5f, 1.0f, 3.5f));
                Graphics.DrawMesh(MeshPool.plane10, matrix2, MaterialPool.MatFrom(GetHealthBar(), ShaderDatabase.Transparent, Color.red), 0);
            }

            base.DrawAt(drawLoc, flip);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            float range = AttackVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(base.Position, range);
            }

            float num = AttackVerb.verbProps.EffectiveMinRange(allowAdjacentShot: true);
            if (num < 90f && num > 0.1f)
            {
                GenDraw.DrawRadiusRing(base.Position, num);
            }

            if (WarmingUp)
            {
                int degreesWide = (int)((float)burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, CurrentTarget, degreesWide, (float)def.size.x * 0.5f);
            }

            if (forcedTarget.IsValid && (!forcedTarget.HasThing || forcedTarget.Thing.Spawned))
            {
                Vector3 b = (!forcedTarget.HasThing) ? forcedTarget.Cell.ToVector3Shifted() : forcedTarget.Thing.TrueCenter();
                Vector3 a = this.TrueCenter();
                b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                a.y = b.y;
                GenDraw.DrawLineBetween(a, b, ForcedTargetLineMat);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Target Random Mortar";
                command_Action.action = delegate
                {
                    TryActivateBurst();
                };
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Set health to 1";
                command_Action2.action = delegate
                {
                    health = 1;
                };
                Command_Action command_Action3 = new Command_Action();
                command_Action3.defaultLabel = "DEV: Summon Assist";
                command_Action3.action = delegate
                {
                    dummy.DoHelp(this);
                };
                Command_Action command_Action4 = new Command_Action();
                command_Action4.defaultLabel = "DEV: Summon All Assists";
                command_Action4.action = delegate
                {
                    List<string> helps = new List<string>() { "Beam","DropPod","Bombardment","SuperBeam" };
                    dummy.DoHelp(this, helps);
                };
                Command_Action command_Action5 = new Command_Action();
                command_Action5.defaultLabel = "DEV: Quest Finish";
                command_Action5.action = delegate
                {
                    QuestUtility.SendQuestTargetSignals(dummy.questTags, "exostriderDestroyed", dummy.Named("SUBJECT"));
                };
                yield return command_Action;
                yield return command_Action2;
                yield return command_Action3;
                yield return command_Action4;
                yield return command_Action5;
            }
            yield break;
        }

        private void ResetForcedTarget()
        {
            forcedTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if (burstCooldownTicksLeft <= 0)
            {
                TryStartShootSomething(canBeginBurstImmediately: false);
            }
        }

        private void ResetCurrentTarget()
        {
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        public void MakeGun()
        {
            gun = ThingMaker.MakeThing(def.building.turretGunDef);
            UpdateGunVerbs();
        }

        private void UpdateGunVerbs()
        {
            List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = BurstComplete;
            }
        }

        private string GetHealthBar()
        {
            if (health >= extension.health * 0.875f)
            {
                return "HealthBar/HealthBar_0";
            }
            else if (health >= extension.health * 0.75f)
            {
                return "HealthBar/HealthBar_1";
            }
            else if (health >= extension.health * 0.625f)
            {
                return "HealthBar/HealthBar_2";
            }
            else if (health >= extension.health * 0.5f)
            {
                return "HealthBar/HealthBar_3";
            }
            else if (health >= extension.health * 0.375f)
            {
                return "HealthBar/HealthBar_4";
            }
            else if (health >= extension.health * 0.25f)
            {
                return "HealthBar/HealthBar_5";
            }
            else if (health >= extension.health * 0.125f)
            {
                return "HealthBar/HealthBar_6";
            }
            return "HealthBar/HealthBar_7";
        }

        private void StartRandomFire()
        {
            nextExplosionCell = (from x in GenRadial.RadialCellsAround(base.Position, impactAreaRadius, useCenter: true)
                                 where x.InBounds(base.Map)
                                 select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / impactAreaRadius));
        }

        private float impactAreaRadius = 6.9f;

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.1f)
        };
    }
}