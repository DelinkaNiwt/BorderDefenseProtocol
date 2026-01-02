using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityApplyAttachmentOnPawnArea : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	public new CompProperties_AbilityApplyAttachmentOnPawnArea Props => (CompProperties_AbilityApplyAttachmentOnPawnArea)props;

	private Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		List<Thing> list = new List<Thing>();
		foreach (IntVec3 item in AffectedCells(target.Cell))
		{
			list.AddRange(item.GetThingList(Caster.Map));
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn pawn)
			{
				Attachment attachment = (Attachment)GenSpawn.Spawn(Props.attachment, pawn.Position, pawn.Map);
				attachment.SetTarget(pawn);
			}
		}
		if (Props.effecter != null)
		{
			Effecter effecter = Props.effecter.Spawn();
			effecter.Trigger(new TargetInfo(target.Cell, Caster.Map), new TargetInfo(target.Cell, Caster.Map));
			effecter.Cleanup();
		}
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawFieldEdges(AffectedCells(target.Cell));
	}

	private List<IntVec3> AffectedCells(IntVec3 target)
	{
		tmpCells.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target, Props.radius, useCenter: true))
		{
			if (item.IsValid || item.InBounds(Caster.Map))
			{
				tmpCells.Add(item);
			}
		}
		tmpCells = tmpCells.Distinct().ToList();
		tmpCells.RemoveAll((IntVec3 cell) => !CanUseCell(cell));
		return tmpCells;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(Caster.Map))
			{
				return false;
			}
			return true;
		}
	}
}
