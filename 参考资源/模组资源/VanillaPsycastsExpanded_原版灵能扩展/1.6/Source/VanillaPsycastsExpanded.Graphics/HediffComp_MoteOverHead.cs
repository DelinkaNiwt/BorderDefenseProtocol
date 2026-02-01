using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using VanillaPsycastsExpanded.HarmonyPatches;
using Verse;

namespace VanillaPsycastsExpanded.Graphics;

public class HediffComp_MoteOverHead : HediffComp
{
	private static readonly List<Vector3> animalHeadOffsets = new List<Vector3>
	{
		new Vector3(0f, 0f, 0.4f),
		new Vector3(0.4f, 0f, 0.25f),
		new Vector3(0f, 0f, 0.1f),
		new Vector3(-0.4f, 0f, 0.25f)
	};

	private Mote mote;

	public HediffCompProperties_Mote Props => props as HediffCompProperties_Mote;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		Pawn pawn = base.Pawn;
		if (base.Pawn.Spawned)
		{
			bool humanlike = pawn.RaceProps.Humanlike;
			List<Vector3> headPosPerRotation = pawn.RaceProps.headPosPerRotation;
			Rot4 rotation = ((pawn.GetPosture() != PawnPosture.Standing) ? pawn.Drawer.renderer.LayingFacing() : (humanlike ? Rot4.North : pawn.Rotation));
			Vector3 vector;
			if (humanlike)
			{
				vector = (pawn.Drawer.renderer.BaseHeadOffsetAt(rotation) + new Vector3(0f, 0f, 0.15f)).RotatedBy(pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None));
			}
			else
			{
				float bodySizeFactor = pawn.ageTracker.CurLifeStage.bodySizeFactor;
				Vector2 vector2 = pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize * bodySizeFactor;
				vector = ((!headPosPerRotation.NullOrEmpty()) ? headPosPerRotation[rotation.AsInt].ScaledBy(new Vector3(vector2.x, 1f, vector2.y)) : (animalHeadOffsets[rotation.AsInt] * pawn.BodySize));
			}
			vector = pawn.DrawPos + vector;
			if (mote == null || mote.Destroyed)
			{
				mote = MakeStaticMote(vector, Props.mote);
				mote.Graphic.MatSingle.color = Props.color;
			}
			else
			{
				mote.exactPosition = vector;
				mote.Maintain();
			}
		}
	}

	public Mote MakeStaticMote(Vector3 loc, ThingDef moteDef, float scale = 1f)
	{
		Mote obj = (Mote)ThingMaker.MakeThing(moteDef);
		obj.exactPosition = loc;
		obj.Scale = scale;
		GenSpawn.Spawn(obj, base.Pawn.Position, base.Pawn.Map);
		return obj;
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		if (mote == null || mote.def.CanBeSaved())
		{
			Scribe_References.Look(ref mote, "mote");
		}
	}
}
