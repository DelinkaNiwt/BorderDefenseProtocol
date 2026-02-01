using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace GD3
{
	public class CompExostriderTurret : ThingComp, IAttackTargetSearcher
	{
		private const int StartShootIntervalTicks = 10;

		private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/Gizmos/ToggleTurret");

		public Thing gun;

		protected int burstCooldownTicksLeft;

		protected int burstWarmupTicksLeft;

		protected LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

		private bool fireAtWill = true;

		private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

		private int lastAttackTargetTick;

		public float curRotation;

		public Thing Thing => parent;

		public CompProperties_ExostriderTurret Props => (CompProperties_ExostriderTurret)props;

		public Verb CurrentEffectiveVerb => AttackVerb;

		public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

		public int LastAttackTargetTick => lastAttackTargetTick;

		public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

		public Verb AttackVerb => GunCompEq.PrimaryVerb;

		private bool WarmingUp => burstWarmupTicksLeft > 0;

		private bool CanShoot
		{
			get
			{
				if (parent is Pawn pawn)
				{
					if (!pawn.Spawned || pawn.Downed || pawn.Dead || !pawn.Awake())
					{
						return false;
					}
					if (pawn.stances.stunner.Stunned)
					{
						return false;
					}
					if (TurretDestroyed)
					{
						return false;
					}
					if (pawn.IsColonyMechPlayerControlled && !fireAtWill)
					{
						return false;
					}
				}
				CompCanBeDormant compCanBeDormant = parent.TryGetComp<CompCanBeDormant>();
				if (compCanBeDormant != null && !compCanBeDormant.Awake)
				{
					return false;
				}
				return true;
			}
		}

		public bool TurretDestroyed
		{
			get
			{
				return false;
			}
		}

		public bool AutoAttack => Props.autoAttack;

		public override void PostPostMake()
		{
			base.PostPostMake();
			MakeGun();
		}

		private void MakeGun()
		{
			gun = ThingMaker.MakeThing(Props.turretDef);
			UpdateGunVerbs();
		}

		private void UpdateGunVerbs()
		{
			List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
			for (int i = 0; i < allVerbs.Count; i++)
			{
				Verb verb = allVerbs[i];
				verb.caster = parent;
				verb.castCompleteCallback = delegate
				{
					burstCooldownTicksLeft = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
				};
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!CanShoot)
			{
				return;
			}
			if (currentTarget.IsValid)
			{
				curRotation = (currentTarget.Cell.ToVector3Shifted() - parent.DrawPos).AngleFlat();
			}
			AttackVerb.VerbTick();
			if (AttackVerb.state == VerbState.Bursting)
			{
				return;
			}
			if (WarmingUp)
			{
				burstWarmupTicksLeft--;
				if (burstWarmupTicksLeft == 0)
				{
					AttackVerb.TryStartCastOn(currentTarget, surpriseAttack: false, canHitNonTargetPawns: true, preventFriendlyFire: false, nonInterruptingSelfCast: true);
					lastAttackTargetTick = Find.TickManager.TicksGame;
					lastAttackedTarget = currentTarget;
				}
				return;
			}
			if (burstCooldownTicksLeft > 0)
			{
				burstCooldownTicksLeft--;
			}
			if (burstCooldownTicksLeft <= 0 && parent.IsHashIntervalTick(10))
			{
				currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, t => t.def.defName != "Turret_GiantAutoMortar_Script");
				if (currentTarget.IsValid)
				{
					burstWarmupTicksLeft = 1;
				}
				else
				{
					ResetCurrentTarget();
				}
			}
		}

		private void ResetCurrentTarget()
		{
			currentTarget = LocalTargetInfo.Invalid;
			burstWarmupTicksLeft = 0;
		}

        public override void PostDraw()
        {
            base.PostDraw();
			Thing thing = this.parent;
			Vector3 drawPos = thing.DrawPos;
			drawPos.y = AltitudeLayer.BuildingBelowTop.AltitudeFor();
			drawPos += Props.drawOffset;
			Matrix4x4 matrix = default(Matrix4x4);
			float curRotation = -90f;
			if (Props.changeAngle && currentTarget.IsValid)
            {
				curRotation = (currentTarget.Cell.ToVector3Shifted() - (parent.DrawPos + Props.drawOffset)).AngleFlat() - 90f;
			}
			Quaternion q = curRotation.ToQuat();
			matrix.SetTRS(drawPos, q, new Vector3(1.5f, 1.0f, 1.5f));
			Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom(Props.turretDef.graphicData.texPath, ShaderDatabase.Transparent), 0);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
			Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
			Scribe_TargetInfo.Look(ref currentTarget, "currentTarget_" + Props.ID);
			Scribe_Deep.Look(ref gun, "gun_" + Props.ID);
			Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (gun == null)
				{
					Log.Error("CompTurrentGun had null gun after loading. Recreating.");
					MakeGun();
				}
				else
				{
					UpdateGunVerbs();
				}
			}
		}
	}

}
