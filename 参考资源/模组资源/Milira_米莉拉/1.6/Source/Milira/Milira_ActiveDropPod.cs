using RimWorld;
using Verse;
using Verse.Sound;

namespace Milira;

public class Milira_ActiveDropPod : ActiveTransporter
{
	protected override void Tick()
	{
		if (base.Contents != null)
		{
			base.Contents.innerContainer.DoTick();
			if (base.Spawned)
			{
				PodOpen();
			}
		}
	}

	public void PodOpen()
	{
		Map map = base.Map;
		if (base.Contents.despawnPodBeforeSpawningThing)
		{
			DeSpawn();
		}
		for (int num = base.Contents.innerContainer.Count - 1; num >= 0; num--)
		{
			Thing thing = base.Contents.innerContainer[num];
			Rot4 rot = (base.Contents.setRotation.HasValue ? base.Contents.setRotation.Value : Rot4.North);
			if (base.Contents.moveItemsAsideBeforeSpawning)
			{
				GenSpawn.CheckMoveItemsAside(base.Position, rot, thing.def, map);
			}
			Thing lastResultingThing;
			if (base.Contents.spawnWipeMode.HasValue)
			{
				lastResultingThing = ((!base.Contents.setRotation.HasValue) ? GenSpawn.Spawn(thing, base.Position, map, base.Contents.spawnWipeMode.Value) : GenSpawn.Spawn(thing, base.Position, map, base.Contents.setRotation.Value, base.Contents.spawnWipeMode.Value));
			}
			else
			{
				GenPlace.TryPlaceThing(thing, base.Position, map, ThingPlaceMode.Near, out lastResultingThing, delegate(Thing placedThing, int count)
				{
					if (Find.TickManager.TicksGame < 1200 && TutorSystem.TutorialMode && placedThing.def.category == ThingCategory.Item)
					{
						Find.TutorialState.AddStartingItem(placedThing);
					}
				}, null, rot);
			}
			if (lastResultingThing is Pawn pawn)
			{
				if (pawn.RaceProps.Humanlike)
				{
					TaleRecorder.RecordTale(TaleDefOf.LandedInPod, pawn);
				}
				if ((pawn.IsColonist || pawn.IsColonyMechPlayerControlled) && pawn.Spawned && !map.IsPlayerHome)
				{
					pawn.drafter.Drafted = true;
				}
				if (pawn.guest != null && pawn.guest.IsPrisoner)
				{
					pawn.guest.WaitInsteadOfEscapingForDefaultTicks();
				}
			}
		}
		base.Contents.innerContainer.ClearAndDestroyContents();
		FleckMaker.Static(base.Position, map, MiliraDefOf.Milira_DropPodDistortion);
		FleckMaker.Static(base.Position, map, MiliraDefOf.Milira_DropPodAirPuff);
		FleckMaker.Static(base.Position, map, MiliraDefOf.Milira_DropPodLightAnimatedA);
		if (def.soundOpen != null)
		{
			def.soundOpen.PlayOneShot(new TargetInfo(base.Position, map));
		}
		Destroy();
	}
}
