using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VanillaPsycastsExpanded;

public class Ability_Killskip : Ability
{
	private int attackInTicks = -1;

	private static List<SoundDef> castSounds = new List<SoundDef>
	{
		VPE_DefOf.VPE_Killskip_Jump_01a,
		VPE_DefOf.VPE_Killskip_Jump_01b,
		VPE_DefOf.VPE_Killskip_Jump_01c
	};

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		AttackTarget((LocalTargetInfo)targets[0]);
		TryQueueAttackIfDead((LocalTargetInfo)targets[0]);
	}

	private void TryQueueAttackIfDead(LocalTargetInfo target)
	{
		if (target.Pawn.Dead)
		{
			attackInTicks = Find.TickManager.TicksGame + base.def.castTime;
		}
		else
		{
			attackInTicks = -1;
		}
	}

	public override void Tick()
	{
		((Ability)this).Tick();
		if (attackInTicks != -1 && Find.TickManager.TicksGame >= attackInTicks)
		{
			attackInTicks = -1;
			Pawn pawn = FindAttackTarget();
			if (pawn != null)
			{
				AttackTarget(pawn);
				TryQueueAttackIfDead(pawn);
			}
		}
	}

	private void AttackTarget(LocalTargetInfo target)
	{
		((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(base.pawn.Position, base.pawn.Map, 0.72f), base.pawn.Position, 60, (Map)null);
		((Ability)this).AddEffecterToMaintain(VPE_DefOf.VPE_Skip_ExitNoDelayRed.Spawn(target.Cell, base.pawn.Map, 0.72f), target.Cell, 60, (Map)null);
		base.pawn.Position = target.Cell;
		base.pawn.Notify_Teleported(endCurrentJob: false);
		base.pawn.stances.SetStance(new Stance_Mobile());
		VerbProperties_AdjustedMeleeDamageAmount_Patch.multiplyByPawnMeleeSkill = true;
		base.pawn.meleeVerbs.TryMeleeAttack(target.Pawn, null, surpriseAttack: true);
		base.pawn.meleeVerbs.TryMeleeAttack(target.Pawn, null, surpriseAttack: true);
		VerbProperties_AdjustedMeleeDamageAmount_Patch.multiplyByPawnMeleeSkill = false;
		castSounds.RandomElement().PlayOneShot(base.pawn);
	}

	private Pawn FindAttackTarget()
	{
		TargetScanFlags flags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
		return (Pawn)AttackTargetFinder.BestAttackTarget(base.pawn, flags, (Thing x) => x is Pawn pawn && !pawn.Dead, 0f, 999999f);
	}

	public override void ExposeData()
	{
		((Ability)this).ExposeData();
		Scribe_Values.Look(ref attackInTicks, "attackInTicks", -1);
	}
}
