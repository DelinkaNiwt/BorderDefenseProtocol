using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class CompAbility_Saber_A : CompAbilityEffect
{
	public class CompAbilityEffect_SelfSkip : CompAbilityEffect
	{
		public new CompProperties_AbilitySelfSkip Props => (CompProperties_AbilitySelfSkip)props;

		public override IEnumerable<PreCastAction> GetPreCastActions()
		{
			yield return new PreCastAction
			{
				action = delegate(LocalTargetInfo t, LocalTargetInfo d)
				{
					Pawn pawn = parent.pawn;
					if (pawn != null && CanPlaceSelectedTargetAt(t))
					{
						FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.GravshipThrusterExhaust, new Vector3(-0.5f, 0f, -0.5f));
						dataAttachedOverlay.link.detachAfterTicks = 5;
						pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
						FleckMaker.Static(pawn.Position, parent.pawn.Map, CMC_Def.CMC_PulsingDistortionRing, 3f);
						SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, parent.pawn.Map));
						SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(t.Cell, parent.pawn.Map));
						FleckMaker.Static(t.Cell, parent.pawn.Map, CMC_Def.CMC_PulsingDistortionRing, 3f);
					}
				},
				ticksAwayFromCast = 5
			};
		}

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			Pawn pawn = parent.pawn;
			Map map = pawn.Map;
			if (CanPlaceSelectedTargetAt(target) && pawn != null)
			{
				parent.AddEffecterToMaintain(RimWorld.EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn, pawn.Map), pawn.Position, 60);
				bool flag = Find.Selector.IsSelected(pawn);
				pawn.DeSpawn();
				parent.AddEffecterToMaintain(RimWorld.EffecterDefOf.Skip_ExitNoDelay.Spawn(target.Cell, pawn.Map), target.Cell, 60);
				GenSpawn.Spawn(pawn, target.Cell, map);
				pawn.drafter.Drafted = true;
				if (flag)
				{
					Find.Selector.Select(pawn);
				}
			}
		}

		public bool CanPlaceSelectedTargetAt(LocalTargetInfo target)
		{
			Pawn pawn = parent.pawn;
			if (pawn != null)
			{
				return !target.Cell.Impassable(parent.pawn.Map) && target.Cell.WalkableBy(parent.pawn.Map, pawn);
			}
			return false;
		}

		public LocalTargetInfo GetDestination(LocalTargetInfo target)
		{
			return target;
		}
	}
}
