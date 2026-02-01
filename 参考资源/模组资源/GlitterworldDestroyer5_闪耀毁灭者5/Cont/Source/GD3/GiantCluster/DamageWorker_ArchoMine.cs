using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class DamageWorker_ArchoMine : DamageWorker
	{
		public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null)
			{
				return base.Apply(dinfo, thing);
			}
			return this.ApplyToPawn(dinfo, pawn);
		}

		private DamageWorker.DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
		{
			DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
			if (dinfo.Amount <= 0f || (pawn.Faction != null && pawn.Faction == dinfo.Instigator.Faction))
			{
				return damageResult;
			}
			if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer)
			{
				return damageResult;
			}
			Map mapHeld = pawn.MapHeld;
			bool spawnedOrAnyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
			IntVec3 pos = dinfo.Instigator.Position;
			if (!dinfo.Instigator.Faction.IsPlayer && pawn.Faction.HostileTo(dinfo.Instigator?.Faction) && pawn.def.defName != "Mech_BlackScyther" && pawn.def.defName != "Mech_BlackLancer" && pawn.def.defName != "Mech_BlackApocriton")
			{
				pawn.Kill(dinfo);
				IEnumerable<Thing> enumerable = from x in dinfo.Instigator.Map.spawnedThings
												where x.Position.DistanceTo(pos) < 1.5f && (x.Faction != null && x.Faction.HostileTo(dinfo.Instigator.Faction))
												select x;
				List<Thing> list = enumerable.ToList();
				for (int i = 0; i < list.Count; i++)
				{
					Corpse c = list[i] as Corpse;
					if (c != null)
					{
						c.Destroy(DestroyMode.Vanish);
					}
				}
				return damageResult;
			}
			if (dinfo.AllowDamagePropagation && dinfo.Amount >= (float)dinfo.Def.minDamageToFragment)
			{
				int num = Rand.RangeInclusive(2, 4);
				for (int i = 0; i < num; i++)
				{
					DamageInfo dinfo2 = dinfo;
					dinfo2.SetAmount(dinfo.Amount / (float)num);
					this.ApplyDamageToPart(dinfo2, pawn, damageResult);
				}
			}
			else
			{
				this.ApplyDamageToPart(dinfo, pawn, damageResult);
				this.ApplySmallPawnDamagePropagation(dinfo, pawn, damageResult);
			}
			if (damageResult.wounded)
			{
				DamageWorker_ArchoMine.PlayWoundedVoiceSound(dinfo, pawn);
				pawn.Drawer.Notify_DamageApplied(dinfo);
				EffecterDef damageEffecter = pawn.RaceProps.FleshType.damageEffecter;
				if (damageEffecter != null)
				{
					if (pawn.health.woundedEffecter != null && pawn.health.woundedEffecter.def != damageEffecter)
					{
						pawn.health.woundedEffecter.Cleanup();
					}
					pawn.health.woundedEffecter = damageEffecter.Spawn();
					pawn.health.woundedEffecter.Trigger(pawn, dinfo.Instigator ?? pawn, -1);
				}
				if (dinfo.Def.damageEffecter != null)
				{
					Effecter effecter = dinfo.Def.damageEffecter.Spawn();
					effecter.Trigger(pawn, pawn, -1);
					effecter.Cleanup();
				}
			}
			if (damageResult.headshot && pawn.Spawned)
			{
				MoteMaker.ThrowText(new Vector3((float)pawn.Position.x + 1f, (float)pawn.Position.y, (float)pawn.Position.z + 1f), pawn.Map, "Headshot".Translate(), Color.white, -1f);
				if (dinfo.Instigator != null)
				{
					Pawn pawn2 = dinfo.Instigator as Pawn;
					if (pawn2 != null)
					{
						pawn2.records.Increment(RecordDefOf.Headshots);
					}
				}
			}
			if ((damageResult.deflected || damageResult.diminished) && spawnedOrAnyParentSpawned)
			{
				EffecterDef effecterDef;
				if (damageResult.deflected)
				{
					if (damageResult.deflectedByMetalArmor && dinfo.Def.canUseDeflectMetalEffect)
					{
						if (dinfo.Def == DamageDefOf.Bullet)
						{
							effecterDef = EffecterDefOf.Deflect_Metal_Bullet;
						}
						else
						{
							effecterDef = EffecterDefOf.Deflect_Metal;
						}
					}
					else if (dinfo.Def == DamageDefOf.Bullet)
					{
						effecterDef = EffecterDefOf.Deflect_General_Bullet;
					}
					else
					{
						effecterDef = EffecterDefOf.Deflect_General;
					}
				}
				else if (damageResult.diminishedByMetalArmor)
				{
					effecterDef = EffecterDefOf.DamageDiminished_Metal;
				}
				else
				{
					effecterDef = EffecterDefOf.DamageDiminished_General;
				}
				if (pawn.health.deflectionEffecter == null || pawn.health.deflectionEffecter.def != effecterDef)
				{
					if (pawn.health.deflectionEffecter != null)
					{
						pawn.health.deflectionEffecter.Cleanup();
						pawn.health.deflectionEffecter = null;
					}
					pawn.health.deflectionEffecter = effecterDef.Spawn();
				}
				pawn.health.deflectionEffecter.Trigger(pawn, dinfo.Instigator ?? pawn, -1);
				if (damageResult.deflected)
				{
					pawn.Drawer.Notify_DamageDeflected(dinfo);
				}
			}
			if (!damageResult.deflected && spawnedOrAnyParentSpawned)
			{
				ImpactSoundUtility.PlayImpactSound(pawn, dinfo.Def.impactSoundType, mapHeld);
			}
			return damageResult;
		}

		[Obsolete]
		private void CheckApplySpreadDamage(DamageInfo dinfo, Thing t)
		{
			if (dinfo.Def == DamageDefOf.Flame && !t.FlammableNow)
			{
				return;
			}
			if (Rand.Chance(0.5f))
			{
				dinfo.SetAmount((float)Mathf.CeilToInt(dinfo.Amount * Rand.Range(0.35f, 0.7f)));
				t.TakeDamage(dinfo);
			}
		}

		private void ApplySmallPawnDamagePropagation(DamageInfo dinfo, Pawn pawn, DamageWorker.DamageResult result)
		{
			if (!dinfo.AllowDamagePropagation)
			{
				return;
			}
			if (result.LastHitPart != null && dinfo.Def.harmsHealth && result.LastHitPart != pawn.RaceProps.body.corePart && result.LastHitPart.parent != null && pawn.health.hediffSet.GetPartHealth(result.LastHitPart.parent) > 0f && result.LastHitPart.parent.coverageAbs > 0f && dinfo.Amount >= 10f && pawn.HealthScale <= 0.5001f)
			{
				DamageInfo dinfo2 = dinfo;
				dinfo2.SetHitPart(result.LastHitPart.parent);
				this.ApplyDamageToPart(dinfo2, pawn, result);
			}
		}

		private void ApplyDamageToPart(DamageInfo dinfo, Pawn pawn, DamageWorker.DamageResult result)
		{
			BodyPartRecord exactPartFromDamageInfo = this.GetExactPartFromDamageInfo(dinfo, pawn);
			if (exactPartFromDamageInfo == null)
			{
				return;
			}
			dinfo.SetHitPart(exactPartFromDamageInfo);
			float num = dinfo.Amount;
			bool flag = !dinfo.InstantPermanentInjury && !dinfo.IgnoreArmor;
			bool deflectedByMetalArmor = false;
			if (flag)
			{
				DamageDef def = dinfo.Def;
				bool diminishedByMetalArmor;
				num = ArmorUtility.GetPostArmorDamage(pawn, num, dinfo.ArmorPenetrationInt, dinfo.HitPart, ref def, out deflectedByMetalArmor, out diminishedByMetalArmor);
				dinfo.Def = def;
				if (num < dinfo.Amount)
				{
					result.diminished = true;
					result.diminishedByMetalArmor = diminishedByMetalArmor;
				}
			}
			if (dinfo.Def.ExternalViolenceFor(pawn))
			{
				num *= pawn.GetStatValue(StatDefOf.IncomingDamageFactor, true, -1);
			}
			if (num <= 0f)
			{
				result.AddPart(pawn, dinfo.HitPart);
				result.deflected = true;
				result.deflectedByMetalArmor = deflectedByMetalArmor;
				return;
			}
			if (DamageWorker_ArchoMine.IsHeadshot(dinfo, pawn))
			{
				result.headshot = true;
			}
			if (dinfo.InstantPermanentInjury && (HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart).CompPropsFor(typeof(HediffComp_GetsPermanent)) == null || dinfo.HitPart.def.permanentInjuryChanceFactor == 0f || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(dinfo.HitPart)))
			{
				return;
			}
			if (!dinfo.AllowDamagePropagation)
			{
				this.FinalizeAndAddInjury(pawn, num, dinfo, result);
				return;
			}
			this.ApplySpecialEffectsToPart(pawn, num, dinfo, result);
		}

		protected virtual void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageWorker.DamageResult result)
		{
			totalDamage = this.ReduceDamageToPreserveOutsideParts(totalDamage, dinfo, pawn);
			this.FinalizeAndAddInjury(pawn, totalDamage, dinfo, result);
			this.CheckDuplicateDamageToOuterParts(dinfo, pawn, totalDamage, result);
		}

		protected float FinalizeAndAddInjury(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageWorker.DamageResult result)
		{
			if (pawn.health.hediffSet.PartIsMissing(dinfo.HitPart))
			{
				return 0f;
			}
			HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
			Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
			hediff_Injury.Part = dinfo.HitPart;
			hediff_Injury.sourceDef = dinfo.Weapon;
			hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
			hediff_Injury.sourceHediffDef = dinfo.WeaponLinkedHediff;
			hediff_Injury.Severity = totalDamage;
			if (dinfo.InstantPermanentInjury)
			{
				HediffComp_GetsPermanent hediffComp_GetsPermanent = hediff_Injury.TryGetComp<HediffComp_GetsPermanent>();
				if (hediffComp_GetsPermanent != null)
				{
					hediffComp_GetsPermanent.IsPermanent = true;
				}
				else
				{
					Log.Error(string.Concat(new object[]
					{
						"Tried to create instant permanent injury on Hediff without a GetsPermanent comp: ",
						hediffDefFromDamage,
						" on ",
						pawn
					}));
				}
			}
			return this.FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
		}

		protected float FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, DamageWorker.DamageResult result)
		{
			HediffComp_GetsPermanent hediffComp_GetsPermanent = injury.TryGetComp<HediffComp_GetsPermanent>();
			if (hediffComp_GetsPermanent != null)
			{
				hediffComp_GetsPermanent.PreFinalizeInjury();
			}
			float partHealth = pawn.health.hediffSet.GetPartHealth(injury.Part);
			if (pawn.IsColonist && !dinfo.IgnoreInstantKillProtection && dinfo.Def.ExternalViolenceFor(pawn) && !Rand.Chance(Find.Storyteller.difficulty.allowInstantKillChance))
			{
				float num = (injury.def.lethalSeverity > 0f) ? (injury.def.lethalSeverity * 1.1f) : 1f;
				float min = 1f;
				float max = Mathf.Min(injury.Severity, partHealth);
				int num2 = 0;
				while (num2 < 7 && pawn.health.WouldDieAfterAddingHediff(injury))
				{
					float num3 = Mathf.Clamp(partHealth - num, min, max);
					if (DebugViewSettings.logCauseOfDeath)
					{
						Log.Message(string.Format("CauseOfDeath: attempt to prevent death for {0} on {1} attempt:{2} severity:{3}->{4} part health:{5}", new object[]
						{
							pawn.Name,
							injury.Part.Label,
							num2 + 1,
							injury.Severity,
							num3,
							partHealth
						}));
					}
					injury.Severity = num3;
					num *= 2f;
					min = 0f;
					num2++;
				}
			}
			pawn.health.AddHediff(injury, null, new DamageInfo?(dinfo), result);
			float num4 = Mathf.Min(injury.Severity, partHealth);
			result.totalDamageDealt += num4;
			result.wounded = true;
			result.AddPart(pawn, injury.Part);
			result.AddHediff(injury);
			return num4;
		}

		private void CheckDuplicateDamageToOuterParts(DamageInfo dinfo, Pawn pawn, float totalDamage, DamageWorker.DamageResult result)
		{
			if (!dinfo.AllowDamagePropagation)
			{
				return;
			}
			if (dinfo.Def.harmAllLayersUntilOutside && dinfo.HitPart.depth == BodyPartDepth.Inside)
			{
				BodyPartRecord parent = dinfo.HitPart.parent;
				do
				{
					if (pawn.health.hediffSet.GetPartHealth(parent) != 0f && parent.coverageAbs > 0f)
					{
						Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, parent), pawn, null);
						hediff_Injury.Part = parent;
						hediff_Injury.sourceDef = dinfo.Weapon;
						hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
						hediff_Injury.Severity = totalDamage;
						if (hediff_Injury.Severity <= 0f)
						{
							hediff_Injury.Severity = 1f;
						}
						this.FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
					}
					if (parent.depth == BodyPartDepth.Outside)
					{
						break;
					}
					parent = parent.parent;
				}
				while (parent != null);
			}
		}

		private static bool IsHeadshot(DamageInfo dinfo, Pawn pawn)
		{
			return !dinfo.InstantPermanentInjury && dinfo.HitPart.groups.Contains(BodyPartGroupDefOf.FullHead) && dinfo.Def == DamageDefOf.Bullet;
		}

		private BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
		{
			if (dinfo.HitPart == null)
			{
				BodyPartRecord bodyPartRecord = this.ChooseHitPart(dinfo, pawn);
				if (bodyPartRecord == null)
				{
					Log.Warning("ChooseHitPart returned null (any part).");
				}
				return bodyPartRecord;
			}
			if (!pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null).Any((BodyPartRecord x) => x == dinfo.HitPart))
			{
				return null;
			}
			return dinfo.HitPart;
		}

		protected virtual BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
		{
			return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth, null);
		}

		private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
		{
			if (pawn.Dead)
			{
				return;
			}
			if (dinfo.InstantPermanentInjury)
			{
				return;
			}
			if (!pawn.SpawnedOrAnyParentSpawned)
			{
				return;
			}
			if (dinfo.Def.ExternalViolenceFor(pawn))
			{
				LifeStageUtility.PlayNearestLifestageSound(pawn, (LifeStageAge lifeStage) => lifeStage.soundWounded, (GeneDef gene) => gene.soundWounded, (MutantDef mutantDef) => mutantDef.soundWounded);
			}
		}

		protected float ReduceDamageToPreserveOutsideParts(float postArmorDamage, DamageInfo dinfo, Pawn pawn)
		{
			if (!DamageWorker_AddInjury.ShouldReduceDamageToPreservePart(dinfo.HitPart))
			{
				return postArmorDamage;
			}
			float partHealth = pawn.health.hediffSet.GetPartHealth(dinfo.HitPart);
			if (postArmorDamage < partHealth)
			{
				return postArmorDamage;
			}
			float maxHealth = dinfo.HitPart.def.GetMaxHealth(pawn);
			float f = (postArmorDamage - partHealth) / maxHealth;
			if (Rand.Chance(this.def.overkillPctToDestroyPart.InverseLerpThroughRange(f)))
			{
				return postArmorDamage;
			}
			return postArmorDamage = partHealth - 1f;
		}

		public static bool ShouldReduceDamageToPreservePart(BodyPartRecord bodyPart)
		{
			return bodyPart.depth == BodyPartDepth.Outside && !bodyPart.IsCorePart;
		}
	}
}
