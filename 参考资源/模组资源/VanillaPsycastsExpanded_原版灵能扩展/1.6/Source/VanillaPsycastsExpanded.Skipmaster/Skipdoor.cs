using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VEF.Buildings;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Skipmaster;

[StaticConstructorOnStartup]
public class Skipdoor : DoorTeleporter, IMinHeatGiver, ILoadReferenceable
{
	public Pawn Pawn;

	public bool IsActive => ((Thing)this).Spawned;

	public int MinHeat => 50;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		((DoorTeleporter)this).SpawnSetup(map, respawningAfterLoad);
		Pawn.Psycasts().AddMinHeatGiver(this);
		if (!respawningAfterLoad)
		{
			Pawn.psychicEntropy.TryAddEntropy(50f, (Thing)(object)this, scale: true, overLimit: true);
		}
	}

	public override void ExposeData()
	{
		((DoorTeleporter)this).ExposeData();
		Scribe_References.Look(ref Pawn, "pawn");
	}

	protected override void Tick()
	{
		((DoorTeleporter)this).Tick();
		if (((Thing)(object)this).IsHashIntervalTick(30) && ((Thing)(object)this).HitPoints < ((Thing)this).MaxHitPoints)
		{
			((Thing)(object)this).HitPoints++;
		}
	}

	public override void DoTeleportEffects(Thing thing, int ticksLeftThisToil, Map targetMap, ref IntVec3 targetCell, DoorTeleporter dest)
	{
		switch (ticksLeftThisToil)
		{
		case 5:
			FleckMaker.Static(thing.Position, thing.Map, FleckDefOf.PsycastSkipFlashEntry);
			FleckMaker.Static(targetCell, targetMap, FleckDefOf.PsycastSkipInnerExit);
			FleckMaker.Static(targetCell, targetMap, FleckDefOf.PsycastSkipOuterRingExit);
			SoundDefOf.Psycast_Skip_Entry.PlayOneShot((Thing)(object)this);
			SoundDefOf.Psycast_Skip_Exit.PlayOneShot((Thing)(object)dest);
			break;
		case 15:
			targetCell = (from c in GenAdj.CellsAdjacentCardinal((Thing)(object)dest)
				where c.Standable(targetMap)
				select c).RandomElement();
			base.teleportEffecters[thing] = EffecterDefOf.Skip_Exit.Spawn(targetCell, targetMap);
			base.teleportEffecters[thing].ticksLeft = 15;
			break;
		}
		if (base.teleportEffecters.ContainsKey(thing))
		{
			base.teleportEffecters[thing].EffectTick(new TargetInfo(targetCell, targetMap), new TargetInfo(targetCell, targetMap));
		}
	}

	public override IEnumerable<Gizmo> GetDoorTeleporterGismoz()
	{
		DoorTeleporterExtension extension = ((Def)((Thing)this).def).GetModExtension<DoorTeleporterExtension>();
		DoorTeleporterMaterials doorMaterials = DoorTeleporter.doorTeleporterMaterials[((Thing)this).def];
		if (doorMaterials.DestroyIcon != null)
		{
			yield return new Command_Action
			{
				defaultLabel = extension.destroyLabelKey.Translate(),
				defaultDesc = extension.destroyDescKey.Translate(Pawn.NameFullColored),
				icon = doorMaterials.DestroyIcon,
				action = delegate
				{
					((Thing)(object)this).Destroy(DestroyMode.Vanish);
				}
			};
		}
		if (doorMaterials.RenameIcon != null)
		{
			yield return new Command_Action
			{
				defaultLabel = extension.renameLabelKey.Translate(),
				defaultDesc = extension.renameDescKey.Translate(),
				icon = doorMaterials.RenameIcon,
				action = delegate
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_0010: Expected O, but got Unknown
					Find.WindowStack.Add((Window)new Dialog_RenameDoorTeleporter((DoorTeleporter)(object)this));
				}
			};
		}
	}

	protected override void PlaySustainer(SoundDef sustainer)
	{
		if (PsycastsMod.Settings.muteSkipdoor)
		{
			base.sustainer?.End();
			base.sustainer = null;
			return;
		}
		if (base.sustainer == null)
		{
			base.sustainer = sustainer.TrySpawnSustainer((Thing)(object)this);
		}
		base.sustainer.Maintain();
	}
}
