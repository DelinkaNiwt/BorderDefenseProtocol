using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace GD3
{
	[StaticConstructorOnStartup]
	public class AlphaSkyfaller : ThingWithComps, IThingHolder
	{
		public int LeaveMapAfterTicks
		{
			get
			{
				if (this.ticksToDiscard <= 0)
				{
					return 220;
				}
				return this.ticksToDiscard;
			}
		}

		public CompSkyfallerRandomizeDirection RandomizeDirectionComp
		{
			get
			{
				return this.randomizeDirectionComp;
			}
		}

		public override void PostPostMake()
		{
			base.PostPostMake();
			this.randomizeDirectionComp = base.GetComp<CompSkyfallerRandomizeDirection>();
		}

		public override Graphic Graphic
		{
			get
			{
				Thing thingForGraphic = this.GetThingForGraphic();
				if (this.def.skyfaller.fadeInTicks > 0 || this.def.skyfaller.fadeOutTicks > 0)
				{
					return this.def.graphicData.GraphicColoredFor(thingForGraphic);
				}
				if (thingForGraphic == this)
				{
					return base.Graphic;
				}
				return thingForGraphic.Graphic.ExtractInnerGraphicFor(thingForGraphic, null).GetShadowlessGraphic();
			}
		}

		public override Vector3 DrawPos
		{
			get
			{
				switch (this.def.skyfaller.movementType)
				{
					case SkyfallerMovementType.Accelerate:
						return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, this.ticksToImpact, this.angle, this.CurrentSpeed, false, this.randomizeDirectionComp);
					case SkyfallerMovementType.ConstantSpeed:
						return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(base.DrawPos, this.ticksToImpact, this.angle, this.CurrentSpeed, false, this.randomizeDirectionComp);
					case SkyfallerMovementType.Decelerate:
						return SkyfallerDrawPosUtility.DrawPos_Decelerate(base.DrawPos, this.ticksToImpact, this.angle, this.CurrentSpeed, false, this.randomizeDirectionComp);
					default:
						Log.ErrorOnce("SkyfallerMovementType not handled: " + this.def.skyfaller.movementType, this.thingIDNumber ^ 1948576711);
						return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, this.ticksToImpact, this.angle, this.CurrentSpeed, false, this.randomizeDirectionComp);
				}
			}
		}

		public override Color DrawColor
		{
			get
			{
				if (this.def.skyfaller.fadeInTicks > 0 && this.ageTicks < this.def.skyfaller.fadeInTicks)
				{
					Color drawColor = base.DrawColor;
					drawColor.a *= Mathf.Lerp(0f, 1f, Mathf.Min((float)this.ageTicks / (float)this.def.skyfaller.fadeInTicks, 1f));
					return drawColor;
				}
				if (this.FadingOut)
				{
					Color drawColor2 = base.DrawColor;
					drawColor2.a *= Mathf.Lerp(1f, 0f, Mathf.Max((float)this.ageTicks - (float)(this.LeaveMapAfterTicks - this.def.skyfaller.fadeOutTicks), 0f) / (float)this.def.skyfaller.fadeOutTicks);
					return drawColor2;
				}
				return base.DrawColor;
			}
			set
			{
				base.DrawColor = value;
			}
		}

		public bool FadingOut
		{
			get
			{
				return this.def.skyfaller.fadeOutTicks > 0 && this.ageTicks >= this.LeaveMapAfterTicks - this.def.skyfaller.fadeOutTicks;
			}
		}

		private Material ShadowMaterial
		{
			get
			{
				if (this.cachedShadowMaterial == null && !this.def.skyfaller.shadow.NullOrEmpty())
				{
					this.cachedShadowMaterial = MaterialPool.MatFrom(this.def.skyfaller.shadow, ShaderDatabase.Transparent);
				}
				return this.cachedShadowMaterial;
			}
		}

		protected float TimeInAnimation
		{
			get
			{
				if (this.def.skyfaller.reversed)
				{
					return (float)this.ticksToImpact / (float)this.LeaveMapAfterTicks;
				}
				return 1f - (float)this.ticksToImpact / (float)this.ticksToImpactMax;
			}
		}

		private float CurrentSpeed
		{
			get
			{
				if (this.def.skyfaller.speedCurve == null)
				{
					return this.def.skyfaller.speed;
				}
				return this.def.skyfaller.speedCurve.Evaluate(this.TimeInAnimation) * this.def.skyfaller.speed;
			}
		}

		private bool SpawnTimedMotes
		{
			get
			{
				return this.def.skyfaller.moteSpawnTime != float.MinValue && Mathf.Approximately(this.def.skyfaller.moteSpawnTime, this.TimeInAnimation);
			}
		}

		public AlphaSkyfaller()
		{
			this.innerContainer = new ThingOwner<Thing>(this);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
			{
				this
			});
			Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
			Scribe_Values.Look<int>(ref this.ticksToDiscard, "ticksToDiscard", 0, false);
			Scribe_Values.Look<int>(ref this.ageTicks, "ageTicks", 0, false);
			Scribe_Values.Look<int>(ref this.ticksToImpactMax, "ticksToImpactMax", this.LeaveMapAfterTicks, false);
			Scribe_Values.Look<float>(ref this.angle, "angle", 0f, false);
			Scribe_Values.Look<float>(ref this.shrapnelDirection, "shrapnelDirection", 0f, false);
		}

		public override void PostMake()
		{
			base.PostMake();
			if (this.def.skyfaller.MakesShrapnel)
			{
				this.shrapnelDirection = Rand.Range(0f, 360f);
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				this.ticksToImpact = (this.ticksToImpactMax = this.def.skyfaller.ticksToImpactRange.RandomInRange);
				this.ticksToDiscard = ((this.def.skyfaller.ticksToDiscardInReverse != IntRange.Zero) ? this.def.skyfaller.ticksToDiscardInReverse.RandomInRange : -1);
				if (this.def.skyfaller.MakesShrapnel)
				{
					float num = GenMath.PositiveMod(this.shrapnelDirection, 360f);
					if (num < 270f && num >= 90f)
					{
						this.angle = Rand.Range(0f, 33f);
					}
					else
					{
						this.angle = Rand.Range(-33f, 0f);
					}
				}
				else if (this.def.skyfaller.angleCurve != null)
				{
					this.angle = this.def.skyfaller.angleCurve.Evaluate(0f);
				}
				else
				{
					this.angle = -33.7f;
				}
				if (this.def.rotatable && this.innerContainer.Any)
				{
					base.Rotation = this.innerContainer[0].Rotation;
				}
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
			this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
			if (this.anticipationSoundPlaying != null)
			{
				this.anticipationSoundPlaying.End();
				this.anticipationSoundPlaying = null;
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Thing thingForGraphic = GetThingForGraphic();
			float num = 0f;
			if (def.skyfaller.rotateGraphicTowardsDirection)
			{
				num = angle;
			}
			if (randomizeDirectionComp != null)
			{
				num += randomizeDirectionComp.ExtraDrawAngle;
			}
			if (def.skyfaller.angleCurve != null)
			{
				angle = def.skyfaller.angleCurve.Evaluate(TimeInAnimation);
			}
			if (def.skyfaller.rotationCurve != null)
			{
				num += def.skyfaller.rotationCurve.Evaluate(TimeInAnimation);
			}
			if (def.skyfaller.xPositionCurve != null)
			{
				drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(TimeInAnimation);
			}
			if (def.skyfaller.zPositionCurve != null)
			{
				drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(TimeInAnimation);
			}
			Graphic.Draw(drawLoc, flip ? thingForGraphic.Rotation.Opposite : thingForGraphic.Rotation, thingForGraphic, num);
			DrawDropSpotShadow();
		}

		public float DrawAngle()
		{
			float num = 0f;
			if (this.def.skyfaller.rotateGraphicTowardsDirection)
			{
				num = this.angle;
			}
			num += this.def.skyfaller.rotationCurve.Evaluate(this.TimeInAnimation);
			if (this.randomizeDirectionComp != null)
			{
				num += this.randomizeDirectionComp.ExtraDrawAngle;
			}
			return num;
		}

		protected override void Tick()
		{
			base.Tick();
			this.innerContainer.DoTick();
			if (this.SpawnTimedMotes)
			{
				CellRect cellRect = this.OccupiedRect();
				for (int i = 0; i < cellRect.Area * this.def.skyfaller.motesPerCell; i++)
				{
					FleckMaker.ThrowDustPuff(cellRect.RandomVector3, base.Map, 2f);
				}
			}
			if (this.def.skyfaller.floatingSound != null && (this.floatingSoundPlaying == null || this.floatingSoundPlaying.Ended))
			{
				this.floatingSoundPlaying = this.def.skyfaller.floatingSound.TrySpawnSustainer(SoundInfo.InMap(new TargetInfo(this), MaintenanceType.PerTick));
			}
			Sustainer sustainer = this.floatingSoundPlaying;
			if (sustainer != null)
			{
				sustainer.Maintain();
			}
			if (this.def.skyfaller.reversed)
			{
				this.ticksToImpact++;
				if (!this.anticipationSoundPlayed && this.def.skyfaller.anticipationSound != null && this.ticksToImpact > this.def.skyfaller.anticipationSoundTicks)
				{
					this.anticipationSoundPlayed = true;
					TargetInfo source = new TargetInfo(base.Position, base.Map, false);
					if (this.def.skyfaller.anticipationSound.sustain)
					{
						this.anticipationSoundPlaying = this.def.skyfaller.anticipationSound.TrySpawnSustainer(source);
					}
					else
					{
						this.def.skyfaller.anticipationSound.PlayOneShot(source);
					}
				}
				if (this.ticksToImpact == this.LeaveMapAfterTicks)
				{
					this.LeaveMap();
				}
				else if (this.ticksToImpact > this.LeaveMapAfterTicks)
				{
					Log.Error("ticksToImpact > LeaveMapAfterTicks. Was there an exception? Destroying skyfaller.");
					this.Destroy(DestroyMode.Vanish);
				}
			}
			else
			{
				this.ticksToImpact--;
				if (this.ticksToImpact == 15)
				{
					this.HitRoof();
				}
				if (!this.anticipationSoundPlayed && this.def.skyfaller.anticipationSound != null && this.ticksToImpact < this.def.skyfaller.anticipationSoundTicks)
				{
					this.anticipationSoundPlayed = true;
					TargetInfo source2 = new TargetInfo(base.Position, base.Map, false);
					if (this.def.skyfaller.anticipationSound.sustain)
					{
						this.anticipationSoundPlaying = this.def.skyfaller.anticipationSound.TrySpawnSustainer(source2);
					}
					else
					{
						this.def.skyfaller.anticipationSound.PlayOneShot(source2);
					}
				}
				Sustainer sustainer2 = this.anticipationSoundPlaying;
				if (sustainer2 != null)
				{
					sustainer2.Maintain();
				}
				if (this.ticksToImpact == 0)
				{
					this.Impact();
				}
				else if (this.ticksToImpact < 0)
				{
					Log.Error("ticksToImpact < 0. Was there an exception? Destroying skyfaller.");
					this.Destroy(DestroyMode.Vanish);
				}
			}
			this.ageTicks++;
		}

		protected virtual void HitRoof()
		{
			if (!this.def.skyfaller.hitRoof)
			{
				return;
			}
			CellRect cr = this.OccupiedRect();
			if (cr.Cells.Any((IntVec3 x) => x.Roofed(this.Map)))
			{
				RoofDef roof = cr.Cells.First((IntVec3 x) => x.Roofed(this.Map)).GetRoof(base.Map);
				if (!roof.soundPunchThrough.NullOrUndefined())
				{
					roof.soundPunchThrough.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
				}
				RoofCollapserImmediate.DropRoofInCells(cr.ExpandedBy(1).ClipInsideMap(base.Map).Cells.Where(delegate (IntVec3 c)
				{
					if (!c.InBounds(this.Map))
					{
						return false;
					}
					if (cr.Contains(c))
					{
						return true;
					}
					if (c.GetFirstPawn(this.Map) != null)
					{
						return false;
					}
					Building edifice = c.GetEdifice(this.Map);
					return edifice == null || !edifice.def.holdsRoof;
				}), base.Map, null);
			}
		}

		protected virtual void SpawnThings()
		{
			for (int i = this.innerContainer.Count - 1; i >= 0; i--)
			{
				GenPlace.TryPlaceThing(this.innerContainer[i], base.Position, base.Map, ThingPlaceMode.Near, delegate (Thing thing, int count)
				{
					PawnUtility.RecoverFromUnwalkablePositionOrKill(thing.Position, thing.Map);
					if (thing.def.Fillage == FillCategory.Full && this.def.skyfaller.CausesExplosion && this.def.skyfaller.explosionDamage.isExplosive && thing.Position.InHorDistOf(base.Position, this.def.skyfaller.explosionRadius))
					{
						base.Map.terrainGrid.Notify_TerrainDestroyed(thing.Position);
					}
				}, null, this.innerContainer[i].def.defaultPlacingRot);
			}
		}

		protected virtual void Impact()
		{
			if (this.def.skyfaller.CausesExplosion)
			{
				GenExplosion.DoExplosion(base.Position, base.Map, this.def.skyfaller.explosionRadius, this.def.skyfaller.explosionDamage, this.instigator, GenMath.RoundRandom((float)this.def.skyfaller.explosionDamage.defaultDamage * this.def.skyfaller.explosionDamageFactor), -1f, null, this.equipment, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, (!this.def.skyfaller.damageSpawnedThings) ? this.innerContainer.ToList<Thing>() : null, null, true, 1f, 0f, true, null, 1f);
			}
			this.SpawnThings();
			this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
			CellRect cellRect = this.OccupiedRect();
			for (int i = 0; i < cellRect.Area * this.def.skyfaller.motesPerCell; i++)
			{
				FleckMaker.ThrowDustPuff(cellRect.RandomVector3, base.Map, 2f);
			}
			if (this.def.skyfaller.MakesShrapnel)
			{
				SkyfallerShrapnelUtility.MakeShrapnel(base.Position, base.Map, this.shrapnelDirection, this.def.skyfaller.shrapnelDistanceFactor, this.def.skyfaller.metalShrapnelCountRange.RandomInRange, this.def.skyfaller.rubbleShrapnelCountRange.RandomInRange, true);
			}
			if (this.def.skyfaller.cameraShake > 0f && base.Map == Find.CurrentMap)
			{
				Find.CameraDriver.shaker.DoShake(this.def.skyfaller.cameraShake);
			}
			if (this.def.skyfaller.impactSound != null)
			{
				this.def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map, false), MaintenanceType.None));
			}
			this.Destroy(DestroyMode.Vanish);
		}

		protected virtual void LeaveMap()
		{
			this.Destroy(DestroyMode.Vanish);
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return this.innerContainer;
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
		}

		private Thing GetThingForGraphic()
		{
			if (this.def.graphicData != null || !this.innerContainer.Any)
			{
				return this;
			}
			return this.innerContainer[0];
		}

		protected void DrawDropSpotShadow()
		{
			Material shadowMaterial = this.ShadowMaterial;
			if (shadowMaterial == null)
			{
				return;
			}
			Skyfaller.DrawDropSpotShadow(base.DrawPos, base.Rotation, shadowMaterial, this.def.skyfaller.shadowSize, this.ticksToImpact);
		}

		public static void DrawDropSpotShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize, int ticksToImpact)
		{
			if (rot.IsHorizontal)
			{
				Gen.Swap<float>(ref shadowSize.x, ref shadowSize.y);
			}
			ticksToImpact = Mathf.Max(ticksToImpact, 0);
			Vector3 pos = center;
			pos.y = AltitudeLayer.Shadows.AltitudeFor();
			float num = 1f + (float)ticksToImpact / 100f;
			Vector3 s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
			Color white = Color.white;
			if (ticksToImpact > 150)
			{
				white.a = Mathf.InverseLerp(200f, 150f, (float)ticksToImpact);
			}
			AlphaSkyfaller.shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos, rot.AsQuat, s);
			Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, AlphaSkyfaller.shadowPropertyBlock);
		}

		public ThingOwner innerContainer;

		public int ticksToImpact;

		public int ageTicks;

		public int ticksToDiscard;

		public float angle;

		public float shrapnelDirection;

		public Pawn instigator = null;

		public ThingDef equipment = null;

		private int ticksToImpactMax = 220;

		private Material cachedShadowMaterial;

		private bool anticipationSoundPlayed;

		private Sustainer floatingSoundPlaying;

		private Sustainer anticipationSoundPlaying;

		private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

		public const float DefaultAngle = -33.7f;

		private const int RoofHitPreDelay = 15;

		private const int LeaveMapAfterTicksDefault = 220;

		private CompSkyfallerRandomizeDirection randomizeDirectionComp;
	}
}
