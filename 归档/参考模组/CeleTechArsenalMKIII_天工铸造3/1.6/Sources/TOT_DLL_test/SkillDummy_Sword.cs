using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class SkillDummy_Sword : ThingWithComps, IThingHolder
{
	public ThingOwner innerContainer = new ThingOwner<Thing>();

	public int tickdown = 1200;

	public bool selected = false;

	public bool IsSword = false;

	public bool ShouldDrawPawn = false;

	public Mote CMC_SSMote;

	public Pawn HeldPawn => innerContainer.FirstOrDefault((Thing x) => x is Pawn) as Pawn;

	public float HeldPawnBodyAngle => 0f;

	public float HeldPawnDrawPos_Y => DrawPos.y + 3f;

	public PawnPosture HeldPawnPosture => PawnPosture.Standing;

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.Look(ref innerContainer, "savedpawn", this);
		Scribe_Values.Look(ref tickdown, "tickleft", 0);
		Scribe_Values.Look(ref IsSword, "sword", defaultValue: false);
		Scribe_Deep.Look(ref CMC_SSMote, "CMC_Mote");
	}

	public void SpawnSetUp(Map map, bool respawningAfterLoad)
	{
		SpawnSetup(map, respawningAfterLoad);
	}

	public void Insert(Pawn pawn)
	{
		innerContainer.ClearAndDestroyContents();
		tickdown = 1200;
		selected = Find.Selector.IsSelected(pawn);
		pawn.DeSpawn();
		if (pawn.holdingOwner != null)
		{
			pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
		}
		else
		{
			innerContainer.TryAdd(pawn);
		}
	}

	protected override void Tick()
	{
		tickdown--;
		if (CMC_SSMote.DestroyedOrNull())
		{
			ThingDef cMC_Mote_SwordShowerPawnBG = CMC_Def.CMC_Mote_SwordShowerPawnBG;
			Vector3 vector = new Vector3(0f, 0f, 0.05f);
			CMC_SSMote = MoteMaker.MakeAttachedOverlay(this, cMC_Mote_SwordShowerPawnBG, PawnDrawOffset(), 2.3f);
			CMC_SSMote.exactRotation = 0f;
		}
		CMC_SSMote.Maintain();
		if (tickdown <= 0)
		{
			innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Direct, null, null, playDropSound: false);
			Pawn firstPawn = base.Position.GetFirstPawn(base.Map);
			firstPawn.drafter.Drafted = true;
			if (selected)
			{
				Find.Selector.Select(firstPawn);
			}
			Destroy();
		}
		base.Tick();
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		Map map = base.Map;
		base.Destroy(mode);
		if (innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
		{
			innerContainer.TryDropAll(base.Position, map, ThingPlaceMode.Near);
		}
		innerContainer.ClearAndDestroyContents();
	}

	public void CastSwordShower()
	{
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Pawn heldPawn = HeldPawn;
		if (heldPawn != null)
		{
			Rot4 south = Rot4.South;
			heldPawn.Drawer.renderer.RenderPawnAt(drawLoc + PawnDrawOffset(), south);
		}
	}

	public Vector3 PawnDrawOffset()
	{
		float num = Mathf.Sin((float)Find.TickManager.TicksGame * 0.01f) * 0.04f;
		return new Vector3(0f, 5f, 1.6f + num);
	}
}
