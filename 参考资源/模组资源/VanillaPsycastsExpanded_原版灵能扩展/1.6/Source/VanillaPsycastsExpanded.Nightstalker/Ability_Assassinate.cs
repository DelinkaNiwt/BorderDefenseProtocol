using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Nightstalker;

public class Ability_Assassinate : Ability
{
	private int attacksLeft;

	private IntVec3 originalPosition;

	private Pawn target;

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		target = targets.FirstOrDefault((GlobalTargetInfo t) => t.Thing is Pawn).Thing as Pawn;
		if (target != null)
		{
			attacksLeft = Mathf.RoundToInt(((Ability)this).GetPowerForPawn());
			Map map = base.pawn.Map;
			originalPosition = base.pawn.Position;
			target.stances.stunner.StunFor(attacksLeft * 2, base.pawn);
			TeleportPawnTo((from c in GenAdjFast.AdjacentCellsCardinal(target.Position)
				where c.Walkable(map)
				select c).RandomElement());
		}
	}

	public override void Tick()
	{
		((Ability)this).Tick();
		if (attacksLeft > 0)
		{
			attacksLeft--;
			DoAttack();
			if (attacksLeft == 0)
			{
				VPE_DefOf.VPE_Assassinate_Return.PlayOneShot(base.pawn);
				TeleportPawnTo(originalPosition);
			}
		}
	}

	private void DoAttack()
	{
		Verb verb = base.pawn.meleeVerbs.GetUpdatedAvailableVerbsList(terrainTools: false).MaxBy((VerbEntry v) => VerbUtility.DPS(v.verb, base.pawn)).verb;
		base.pawn.meleeVerbs.TryMeleeAttack(target, verb, surpriseAttack: true);
		base.pawn.stances.CancelBusyStanceHard();
		FleckMaker.AttachedOverlay(target, VPE_DefOf.VPE_Slash, Rand.InsideUnitCircle * 0.3f);
	}

	private void TeleportPawnTo(IntVec3 c)
	{
		FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(base.pawn, FleckDefOf.PsycastSkipFlashEntry, Vector3.zero);
		dataAttachedOverlay.link.detachAfterTicks = 1;
		base.pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
		TargetInfo targetInfo = new TargetInfo(c, base.pawn.Map);
		FleckMaker.Static(targetInfo.Cell, targetInfo.Map, FleckDefOf.PsycastSkipInnerExit);
		FleckMaker.Static(targetInfo.Cell, targetInfo.Map, FleckDefOf.PsycastSkipOuterRingExit);
		SoundDefOf.Psycast_Skip_Entry.PlayOneShot(base.pawn);
		SoundDefOf.Psycast_Skip_Exit.PlayOneShot(targetInfo);
		((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(base.pawn, base.pawn.Map), base.pawn.Position, 60, (Map)null);
		((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(targetInfo.Cell, targetInfo.Map), targetInfo.Cell, 60, (Map)null);
		base.pawn.Position = c;
		base.pawn.Notify_Teleported();
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		Pawn pawn = target.Pawn;
		if (pawn != null)
		{
			if (pawn.Map.glowGrid.GroundGlowAt(pawn.Position) <= 0.29f)
			{
				return true;
			}
			if (showMessages)
			{
				Messages.Message("VPE.MustBeInDark".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
		}
		return false;
	}

	public override void ExposeData()
	{
		((Ability)this).ExposeData();
		Scribe_Values.Look(ref attacksLeft, "attacksLeft", 0);
		Scribe_Values.Look(ref originalPosition, "originalPosition");
		Scribe_References.Look(ref target, "target");
	}
}
