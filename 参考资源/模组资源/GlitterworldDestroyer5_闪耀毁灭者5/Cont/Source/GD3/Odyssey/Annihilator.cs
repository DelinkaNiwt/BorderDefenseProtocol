using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Annihilator : Pawn, BossMusic
    {
        public AnimationDef animation;

        public Vector3 drawPosOffset;

        public LocalTargetInfo jumpTarget;

        private bool willSuspend;

        public bool Damaged => health.summaryHealth.SummaryHealthPercent < 0.4f;

        public int laserTick;

        public int laserTickPassed;

        public bool longJump;

        public TargetInfo longJumpTarget;

        public bool NoAI;

        public bool stopAnimationTracking;

        public BodyPartGroupDef targetGroup;

        public BodyPartRecord torso;

        private CompAffectsSky compAffectsSky;

        public CompAffectsSky CompAffectsSky => compAffectsSky == null ? compAffectsSky = this.TryGetComp<CompAffectsSky>() : compAffectsSky;

        public bool ShouldBeDead => health.hediffSet.GetPartHealth(torso) <= 0;

        public bool Dying => dyingTick >= 0;

        public int dyingTick = -1;
        
        private Sustainer sustainer;

        private Effecter beamEffecter;

        private Material cachedShadowMaterial;

        protected Material ShadowMaterial
        {
            get
            {
                if (cachedShadowMaterial == null)
                {
                    cachedShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
                }

                return cachedShadowMaterial;
            }
        }

        public List<Ability> abilitiesList;

        private static List<AbilityDef> abilitiesFight = new List<AbilityDef>
        {
            GDDefOf.Annihilator_JumpAbility,
            GDDefOf.Annihilator_JumpAndSuspend,
            GDDefOf.Annihilator_Suspend,
        };

        public static float IfEnemyClose = 10.9f;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        private static readonly List<int> DyingExplosionTick = new List<int>
        {
            1, 60, 120, 180, 210, 240, 270, 300, 330, 360, 390, 420, 440, 460, 480, 500, 520, 540, 550, 560, 570, 580, 590
        };

        public SongDef Music => GDDefOf.MechCommander;

        public bool IsPlaying => this.HostileTo(Faction.OfPlayer);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                animation = GDDefOf.Annihilator_Ambient;
            }
            abilitiesList = new List<Ability>();
            for (int i = 0; i < abilitiesFight.Count; i++)
            {
                AbilityDef def = abilitiesFight[i];
                abilitiesList.Add(abilities.GetAbility(def));
            }
            abilitiesList.RemoveAll(e => e == null);
            torso = health.hediffSet.GetBodyPartRecord(GDDefOf.MechanicalThoraxAnnihilator);
            ResetAllAbilities(300);
        }

        protected override void Tick()
        {
            base.Tick();
            AnimationPostFix();
            Laser();

            if (Flying && this.IsHashIntervalTick(30))
            {
                DestroyRoofs();
            }

            DyingTick();
        }

        #region ACTION:地图内跳跃
        public void JumpTo(LocalTargetInfo info, bool andSuspend = false)
        {
            GDDefOf.Annihilator_PreJump.PlayOneShot(this);
            animation = GDDefOf.Annihilator_Jump;
            jumpTarget = info;
            willSuspend = andSuspend;
            longJump = false;
            ResetAllAbilities(600);
        }

        public void DoJump(bool andSuspend = false)
        {
            animation = GDDefOf.Annihilator_Jumping;
            Drawer.renderer.SetAnimation(animation);
            Map map = MapHeld;
            IntVec3 position = PositionHeld;
            GDDefOf.Annihilator_DoJump.PlayOneShot(new TargetInfo(position, map));
            GDDefOf.GD_ImpactDustCloud.Spawn(this, this, 1.2f).Cleanup();
            Find.CameraDriver.shaker.DoShake(0.3f, 40);
            if (position.ShouldSpawnMotesAt(map) && Map.thingGrid.ThingAt<Building_CentralCharger>(Position) == null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPos, map, GDDefOf.GD_GroundCrack);
                dataStatic.scale = 10f;
                dataStatic.rotation = Rand.Range(0, 360);
                map.flecks.CreateFleck(dataStatic);
            }
            DestroyRoofs();

            bool selected = Find.Selector.IsSelected(this);
            bool drafted = Drafted;
            DeSpawn();
            Skyfaller_JumpingMech jumping = (Skyfaller_JumpingMech)SkyfallerMaker.MakeSkyfaller(GDDefOf.GD_Skyfaller_JumpingMech);
            jumping.Pawn = this;
            GenSpawn.Spawn(jumping, position, map);

            IntVec3 targCell = jumpTarget.Cell;
            if (!jumpTarget.Cell.InBounds(map, 9) && CellFinder.TryFindRandomCellNear(jumpTarget.Cell, map, 9, c => c.InBounds(map, 9) && c.Standable(map), out IntVec3 result))
            {
                targCell = result;
            }
            if (!andSuspend)
            {
                Skyfaller_LandingMech landing = (Skyfaller_LandingMech)SkyfallerMaker.MakeSkyfaller(GDDefOf.GD_Skyfaller_LandingMech, this);
                landing.selected = selected;
                landing.drafted = drafted;
                GenSpawn.Spawn(landing, targCell, map);
            }
            else
            {
                Skyfaller_SuspendReady landing = (Skyfaller_SuspendReady)SkyfallerMaker.MakeSkyfaller(GDDefOf.GD_Skyfaller_SuspendReady, this);
                landing.selected = selected;
                landing.drafted = drafted;
                GenSpawn.Spawn(landing, targCell, map);
            }

            willSuspend = false;
        }
        #endregion

        #region ACTION:原地悬浮
        public void ReadySuspend()
        {
            animation = GDDefOf.Annihilator_Jump_Light;
            longJump = false;
            GDDefOf.Annihilator_PreJump.PlayOneShot(this);
        }

        public void DoSuspend()
        {
            animation = GDDefOf.Annihilator_ReadySuspend_Light;
            GDDefOf.Annihilator_Transform.PlayOneShot(this);
            jobs.EndCurrentJob(JobCondition.InterruptForced);
            pather.StopDead();
            flight.StartFlying();
        }
        #endregion

        #region ACTION:地图外跳跃
        public void LongJumpTo(TargetInfo info)
        {
            GDDefOf.Annihilator_PreJump.PlayOneShot(this);
            animation = GDDefOf.Annihilator_Jump;
            willSuspend = false;
            longJumpTarget = info;
            longJump = true;
            ResetAllAbilities(600);
        }

        public void DoLongJump()
        {
            animation = GDDefOf.Annihilator_Jumping;
            Drawer.renderer.SetAnimation(animation);
            Map map = MapHeld;
            Map newMap = longJumpTarget.Map;
            IntVec3 position = PositionHeld;
            IntVec3 newPosition = longJumpTarget.Cell;
            GDDefOf.Annihilator_DoJump.PlayOneShot(new TargetInfo(position, map));
            GDDefOf.GD_ImpactDustCloud.Spawn(this, this, 1.2f).Cleanup();
            Find.CameraDriver.shaker.DoShake(0.3f, 40);
            if (position.ShouldSpawnMotesAt(map) && Map.thingGrid.ThingAt<Building_CentralCharger>(Position) == null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPos, map, GDDefOf.GD_GroundCrack);
                dataStatic.scale = 10f;
                dataStatic.rotation = Rand.Range(0, 360);
                map.flecks.CreateFleck(dataStatic);
            }
            DestroyRoofs();

            bool selected = Find.Selector.IsSelected(this);
            bool drafted = Drafted;
            DeSpawn();
            Skyfaller_JumpingMech jumping = (Skyfaller_JumpingMech)SkyfallerMaker.MakeSkyfaller(GDDefOf.GD_Skyfaller_JumpingMech);
            jumping.Pawn = this;
            GenSpawn.Spawn(jumping, position, map);

            IntVec3 targCell = newPosition;
            if (!newPosition.InBounds(newMap, 9) && CellFinder.TryFindRandomCellNear(newPosition, newMap, 9, c => c.InBounds(newMap, 9) && c.Standable(newMap), out IntVec3 result))
            {
                newPosition = result;
            }
            Skyfaller_LandingMech landing = (Skyfaller_LandingMech)SkyfallerMaker.MakeSkyfaller(GDDefOf.GD_Skyfaller_LandingMech, this);
            landing.selected = selected;
            landing.drafted = drafted;
            GenSpawn.Spawn(landing, targCell, newMap);
            landing.ticksToImpact += Find.WorldGrid.TraversalDistanceBetween(newMap.Tile, map.Tile) * 10;
        }
        #endregion

        public void AnimationPostFix()
        {
            if (animation != null)
            {
                if (Drawer.renderer.CurAnimation != animation && !stopAnimationTracking)
                {
                    Drawer.renderer.SetAnimation(animation);
                }
                if (animation == GDDefOf.Annihilator_Jump)
                {
                    if (Drawer.renderer.renderTree.AnimationTick >= animation.durationTicks - 1)
                    {
                        if (longJump) DoLongJump();
                        else DoJump(willSuspend);
                    }
                }
                else if (animation == GDDefOf.Annihilator_ResetPosture)
                {
                    if (Drawer.renderer.renderTree.AnimationTick >= animation.durationTicks - 1)
                    {
                        animation = GDDefOf.Annihilator_Ambient;
                        GDDefOf.Annihilator_DoJump.PlayOneShot(this);
                        GDDefOf.GD_ImpactDustCloud.Spawn(this, this, 1.0f).Cleanup();
                        Find.CameraDriver.shaker.DoShake(0.1f, 30);
                        List<Thing> things = Map.listerThings.AllThings.Where(p => p.Position.IsInside(this) && p != this).ToList();
                        if (things.Any())
                        {
                            foreach (Thing victim in things)
                            {
                                float amount = (victim is Building && victim.def.useHitPoints) ? Math.Max(victim.MaxHitPoints * 0.75f, 1500) : 180;
                                victim.TakeDamage(new DamageInfo(DamageDefOf.Crush, amount, 30000f, -1, this));
                            }
                        }
                        ResetAllAbilities(200);
                    }
                }
                else if (animation == GDDefOf.Annihilator_Jump_Light)
                {
                    if (Drawer.renderer.renderTree.AnimationTick >= animation.durationTicks - 1)
                    {
                        DoSuspend();
                    }
                }
                else if (animation == GDDefOf.Annihilator_ReadySuspend_Light)
                {
                    if (Drawer.renderer.renderTree.AnimationTick >= animation.durationTicks - 1)
                    {
                        animation = GDDefOf.Annihilator_Suspending;
                        laserTick = 600;
                        GDDefOf.GD_ImpactDustCloud.Spawn(this, this, 1.2f).Cleanup();
                    }
                }
            }
        }

        #region TICK:柱形激光
        public void Laser()
        {
            if (!Spawned)
            {
                return;
            }
            if (laserTick > 0)
            {
                laserTick--;
                laserTickPassed++;
                if (laserTickPassed == 1)
                {
                    CompAffectsSky.StartFadeInHoldFadeOut(10, laserTick, 10);
                    Find.CameraDriver.shaker.DoShake(0.15f, 20);
                }
                if (sustainer == null)
                {
                    sustainer = GDDefOf.Annihilator_Laser.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                }
                sustainer.Maintain();
                if (beamEffecter == null)
                {
                    beamEffecter = new Effecter(GDDefOf.AnnihilatorLaserRing);
                    beamEffecter.offset = Vector3.zero;
                    beamEffecter.scale = 1.0f;
                    beamEffecter.Trigger(this, this);
                }
                beamEffecter.EffectTick(this, this);
                Find.CameraDriver.shaker.DoShake(0.05f, 2);
                if (this.IsHashIntervalTick(20))
                {
                    List<IntVec3> cells = GenRadial.RadialCellsAround(PositionHeld, 5.6f, true).Where(c => c.InBounds(MapHeld)).ToList();
                    if (cells.Any())
                    {
                        foreach (IntVec3 cell in cells)
                        {
                            if (Rand.Chance(0.2f))
                            {
                                FilthMaker.TryMakeFilth(cell, MapHeld, ThingDefOf.Filth_Ash);
                            }
                        }
                    }
                    List<Thing> targets = MapHeld?.listerThings.AllThings.Where(t => t.Position.IsInside(this) && t != this).ToList();
                    if (targets.Count > 0)
                    {
                        foreach (Thing target in targets)
                        {
                            if (!target.def.destroyable)
                            {
                                continue;
                            }
                            float amount = (target is Building && target.def.useHitPoints) ? Math.Max(target.MaxHitPoints / 10f, 250) : 150;
                            target.TakeDamage(new DamageInfo(GDDefOf.GD_Beam, amount, 1.5f, -1, this));
                        }
                    }
                }
                if (laserTick == 0)
                {
                    laserTickPassed = 0;
                    ForceLand();
                }
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPos - new Vector3(0, 0, flight.PositionOffsetFactor / 2f), Map, GDDefOf.Annihilator_BeamSpark, Rand.Range(3.5f, 4.5f));
                dataStatic.rotationRate = Rand.Range(-30f, 30f);
                dataStatic.velocityAngle = Rand.Range(0, 360);
                dataStatic.velocitySpeed = Rand.Range(3.5f, 4.7f);
                Map.flecks.CreateFleck(dataStatic);
            }
            else
            {
                sustainer?.End();
                sustainer = null;
                beamEffecter = null;
            }
        }
        #endregion

        #region TICK:死亡动画
        public void DyingTick()
        {
            if (dyingTick == -1 && ShouldBeDead)
            {
                if (Flying && animation != GDDefOf.Annihilator_ResetPosture)
                {
                    laserTick = 0;
                    laserTickPassed = 0;
                    ForceLand();
                }
                if (animation == GDDefOf.Annihilator_Ambient)
                {
                    dyingTick++;
                    animation = GDDefOf.Annihilator_Dying;
                    Find.CameraDriver.shaker.DoShake(1.55f, 30);
                    GDDefOf.Pawn_Mech_Diabolus_Death.PlayOneShot(this);
                }
            }
            else if (dyingTick >= 0)
            {
                dyingTick++;
                if (DyingExplosionTick.Contains(dyingTick))
                {
                    IntVec3 cell = MapHeld.AllCells.Where(c => c.IsInside(this)).RandomElement();
                    GenExplosion.DoExplosion(cell, MapHeld, 3.9f, DamageDefOf.Bomb, null, 40, 0.8f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, new List<Thing> { this }, null, true, 1f, 0f, true, null, 1f);
                    FleckMaker.ThrowFireGlow(cell.ToVector3Shifted(), MapHeld, 1.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), MapHeld, 2.0f);
                    }
                }
                if (dyingTick == 400)
                {
                    GDDefOf.Annihilator_Transform.PlayOneShot(this);
                }
                if (dyingTick >= 599)
                {
                    IntVec3 vec = PositionHeld;
                    Map map = MapHeld;
                    bool isPlayer = Faction == Faction.OfPlayer;
                    ThingDef ruin = isPlayer ? GDDefOf.AnnihilatorCorpse : GDDefOf.AnnihilatorCorpse_Ancient;

                    Quest quest = GDUtility.GetQuestOfThing(this);
                    if (this.HostileTo(Faction.OfPlayer) && quest != null)
                    {
                        GDUtility.SendSignal(quest, "Defeated");
                    }

                    Thing.allowDestroyNonDestroyable = true;
                    Kill(null);
                    Thing.allowDestroyNonDestroyable = false;

                    GenExplosion.DoExplosion(vec, map, 14.9f, DamageDefOf.Flame, null, 120, 0.8f, null, null, null, null, ThingDefOf.Filth_Fuel, 0.4f, 1, null, null, 255, false, null, 0f, 1, 0.5f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                    GDDefOf.GiantExplosion.Spawn(vec, map, 1.5f);
                    GDDefOf.GD_BigWave.SpawnMaintained(vec, map, 1f);
                    GDDefOf.ExostriderDeath.PlayOneShotOnCamera();
                    Find.CameraDriver.shaker.DoShake(6f, 90);

                    GenSpawn.Spawn(ruin, vec, map);
                }
            }
        }
        #endregion

        public void ForceLand()
        {
            if (Flying)
            {
                animation = GDDefOf.Annihilator_ResetPosture;
                GDDefOf.Annihilator_Transform.PlayOneShot(this);
                jobs.EndCurrentJob(JobCondition.InterruptForced);
                pather.StopDead();
                flight.ForceLand();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (Spawned && Map != null)
            {
                //影子
                Material shadowMaterial = ShadowMaterial;
                if (shadowMaterial != null)
                {
                    drawLoc.y = AltitudeLayer.Shadows.AltitudeFor();
                    if (Flying)
                    {
                        drawLoc.z -= flight.PositionOffsetFactor / 2f;
                    }
                    Skyfaller.DrawDropSpotShadow(drawLoc, base.Rotation, shadowMaterial, new Vector2(16, 16), 0);
                }
                //激光
                if (laserTick > 0)
                {
                    drawLoc.x -= 0.1f;
                    Vector3 drawPos = drawLoc - new Vector3(0, 0, flight.PositionOffsetFactor / 3f);
                    float num = (drawLoc.z + flight.PositionOffsetFactor / 2f - drawPos.z) * 1f;
                    float angle = 0;
                    Vector3 vector = Vector3Utility.FromAngleFlat(angle - 90f);
                    Vector3 vector2 = drawPos + vector * num * 0.5f;
                    vector2.y = AltitudeLayer.Projectile.AltitudeFor();
                    float alpha = Mathf.Min((float)laserTickPassed / 10f, 1f);
                    float num2 = 1f;
                    Vector3 vector3 = vector * ((1f - num2) * num);
                    float num3 = 0.975f + Mathf.Sin((float)laserTickPassed * 0.3f) * 0.025f;
                    if (laserTick < 10)
                    {
                        num3 *= (float)laserTick / 10f;
                    }
                    Color color = new Color(1f, 0.96f, 0.84f, 0.95f * alpha);
                    color.a *= num3;
                    MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
                    Matrix4x4 matrix = default(Matrix4x4);
                    float width = 10f;
                    float beamEndHeight = width * 0.5f;
                    matrix.SetTRS(vector2 + vector * beamEndHeight * 0.5f + vector3, Quaternion.Euler(0f, angle, 0f), new Vector3(width, 1f, num));
                    Graphics.DrawMesh(MeshPool.plane10, matrix, BeamMat, 0, null, 0, MatPropertyBlock);
                    Vector3 pos = drawPos + vector3;
                    pos.y = AltitudeLayer.Projectile.AltitudeFor();
                    Matrix4x4 matrix2 = default(Matrix4x4);
                    matrix2.SetTRS(pos, Quaternion.Euler(0f, angle, 0f), new Vector3(width, 1f, beamEndHeight));
                    Graphics.DrawMesh(MeshPool.plane10, matrix2, BeamEndMat, 0, null, 0, MatPropertyBlock);
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (forceNoDeathNotification)
            {
                Thing.allowDestroyNonDestroyable = true;
            }
            base.Destroy(mode);
            Thing.allowDestroyNonDestroyable = false;
        }

        public void ResetAllAbilities(int tick)
        {
            foreach (Ability a in abilities.AllAbilitiesForReading)
            {
                if (a.CooldownTicksRemaining > tick)
                {
                    continue;
                }
                a.StartCooldown(tick);
            }
        }

        public void DestroyRoofs()
        {
            List<IntVec3> cells = MapHeld?.AllCells.Where(c => c.IsInside(this)).ToList();
            if (!cells.NullOrEmpty())
            {
                foreach (IntVec3 cell in cells)
                {
                    if (cell.Roofed(MapHeld))
                    {
                        MapHeld.roofGrid.SetRoof(cell, null);
                        FleckMaker.ThrowDustPuff(cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.6f), MapHeld, 2f);
                    }
                }
                Find.CameraDriver.shaker.DoShake(0.1f, 20);
                SoundDefOf.Roof_Collapse.PlayOneShot(this);
            }
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if (ShouldBeDead)
            {
                absorbed = true;
                return;
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (Flying && !dinfo.Def.isRanged && !dinfo.Def.isExplosive && dinfo.Def != DamageDefOf.EMP)
            {
                Pawn pawn = dinfo.Instigator as Pawn;
                if (pawn != null && pawn.Flying)
                {
                    return;
                }
                absorbed = true;
            }
            if (dinfo.Def == DamageDefOf.Crush)
            {
                absorbed = true;
                return;
            }
            else if (dinfo.Def == DamageDefOf.Bullet && this.HostileTo(Faction.OfPlayer))
            {
                dinfo.SetAmount(dinfo.Amount * 0.5f);
            }
            if (targetGroup != null && Rand.Chance(0.8f))
            {
                IEnumerable<BodyPartRecord> parts = health.hediffSet.GetNotMissingParts();
                if (parts.Any())
                {
                    BodyPartRecord part = parts.ToList().Find(r => r.groups.Contains(targetGroup));
                    if (part != null)
                    {
                        dinfo.SetHitPart(part);
                    }
                }
            }
            if (Flying)
            {
                dinfo.SetAmount(dinfo.Amount * 0.4f);
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            Drawer.renderer.SetAllGraphicsDirty();
            if (mindState.enemyTarget == null)
            {
                mindState.enemyTarget = dinfo.Instigator;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!(gizmo is Command_Target command) || command.defaultLabel != "CommandMeleeAttack".Translate())
                {
                    yield return gizmo;
                }
            }
            if (this.HostileTo(Faction.OfPlayer))
            {
                Command_TargetPart command_TargetPart = new Command_TargetPart()
                {
                    defaultLabel = "GD.Target.Annihilator".Translate() + TargetLabel().Translate(),
                    defaultDesc = "GD.Target.AnnihilatorDesc".Translate(LabelShort),
                    icon = ContentFinder<Texture2D>.Get(TargetIconPath(), true),
                    annihilator = this,
                    action = delegate
                    {

                    },
                };
                yield return command_TargetPart;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Toggle command_Toggle = new Command_Toggle
                {
                    defaultLabel = "DEV: No AI",
                    toggleAction = delegate ()
                    {
                        NoAI = !NoAI;
                    },
                    isActive = (() => NoAI)
                };
                Command_Toggle command_Toggle2 = new Command_Toggle
                {
                    defaultLabel = "DEV: Stop Animation Tracking",
                    toggleAction = delegate ()
                    {
                        stopAnimationTracking = !stopAnimationTracking;
                    },
                    isActive = (() => stopAnimationTracking)
                };
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "DEV: Instant Kill",
                    action = delegate
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (health.hediffSet.GetPartHealth(torso) > 0)
                            {
                                this.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 5000, 5000, -1, null, torso));
                            }
                        }
                    }
                };
                yield return command_Toggle;
                yield return command_Toggle2;
                yield return command_Action;
            }
        }

        private string TargetLabel()
        {
            if (targetGroup == GDDefOf.Annihilator_Leg_LFront) return "GD.Target.LegLF";
            else if (targetGroup == GDDefOf.Annihilator_Leg_RFront) return "GD.Target.LegRF";
            else if (targetGroup == GDDefOf.Annihilator_Leg_LMiddle) return "GD.Target.LegLM";
            else if (targetGroup == GDDefOf.Annihilator_Leg_RMiddle) return "GD.Target.LegRM";
            else if (targetGroup == GDDefOf.Annihilator_Leg_LHind) return "GD.Target.LegLH";
            else if (targetGroup == GDDefOf.Annihilator_Leg_RHind) return "GD.Target.LegRH";
            else return "GD.Target.Any";
        }

        private string TargetIconPath()
        {
            if (targetGroup == GDDefOf.Annihilator_Leg_LFront) return "UI/Buttons/Annihilator_TargetLF";
            else if (targetGroup == GDDefOf.Annihilator_Leg_RFront) return "UI/Buttons/Annihilator_TargetRF";
            else if (targetGroup == GDDefOf.Annihilator_Leg_LMiddle) return "UI/Buttons/Annihilator_TargetLM";
            else if (targetGroup == GDDefOf.Annihilator_Leg_RMiddle) return "UI/Buttons/Annihilator_TargetRM";
            else if (targetGroup == GDDefOf.Annihilator_Leg_LHind) return "UI/Buttons/Annihilator_TargetLH";
            else if (targetGroup == GDDefOf.Annihilator_Leg_RHind) return "UI/Buttons/Annihilator_TargetRH";
            else return "UI/Buttons/Annihilator_TargetAny";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref drawPosOffset, "drawPosOffset");
            Scribe_Values.Look(ref willSuspend, "willSuspend");
            Scribe_Values.Look(ref laserTick, "laserTick");
            Scribe_Values.Look(ref laserTickPassed, "laserTickPassed");
            Scribe_Values.Look(ref longJump, "longJump");
            Scribe_Values.Look(ref NoAI, "NoAI");
            Scribe_Values.Look(ref dyingTick, "dyingTick", -1);
            Scribe_Values.Look(ref stopAnimationTracking, "stopAnimationTracking");
            Scribe_Defs.Look(ref animation, "animation");
            Scribe_Defs.Look(ref targetGroup, "targetGroup");
            Scribe_TargetInfo.Look(ref jumpTarget, "jumpTarget");
            Scribe_TargetInfo.Look(ref longJumpTarget, "longJumpTarget");
        }
    }
}
