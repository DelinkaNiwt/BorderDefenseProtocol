using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public class Hediff_PsychicDrone : Hediff_Overlay
{
	private float curAngle;

	private List<Mote> maintainedMotes = new List<Mote>();

	private List<Pawn> affectedPawns = new List<Pawn>();

	public override string OverlayPath => "Effects/Archotechist/PsychicDrone/PsychicDroneEnergyField";

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		maintainedMotes.Add(SpawnMoteAttached(VPE_DefOf.VPE_PsycastAreaEffectMaintained, ((Hediff_Ability)this).ability.GetRadiusForPawn(), 0f));
	}

	public override void Tick()
	{
		((Hediff)(object)this).Tick();
		curAngle += 0.015f;
		if (curAngle > 360f)
		{
			curAngle = 0f;
		}
		foreach (Mote maintainedMote in maintainedMotes)
		{
			maintainedMote.Maintain();
		}
		if (Find.TickManager.TicksGame % 180 == 0 && (from x in GenRadial.RadialDistinctThingsAround(((Hediff)(object)this).pawn.Position, ((Hediff)(object)this).pawn.Map, ((Hediff_Ability)this).ability.GetRadiusForPawn(), useCenter: true).OfType<Pawn>()
			where !affectedPawns.Contains(x) && !x.InMentalState && x.HostileTo(((Hediff)(object)this).pawn) && x.RaceProps.IsFlesh
			select x).TryRandomElement(out var result))
		{
			MentalStateDef stateDef = (Rand.Bool ? VPE_DefOf.VPE_Wander_Sad : MentalStateDefOf.Berserk);
			if (result.mindState.mentalStateHandler.TryStartMentalState(stateDef, null, forced: false, forceWake: false, causedByMood: false, null, transitionSilently: false, causedByDamage: false, causedByPsycast: true))
			{
				affectedPawns.Add(result);
			}
		}
	}

	public override void Draw()
	{
		Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		Matrix4x4 matrix = default(Matrix4x4);
		float num = ((Hediff_Ability)this).ability.GetRadiusForPawn() * 2f;
		matrix.SetTRS(drawPos, Quaternion.AngleAxis(curAngle, Vector3.up), new Vector3(num, 1f, num));
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
	}

	public Mote SpawnMoteAttached(ThingDef moteDef, float scale, float rotationRate)
	{
		MoteAttachedScaled moteAttachedScaled = MoteMaker.MakeAttachedOverlay(((Hediff)(object)this).pawn, moteDef, Vector3.zero) as MoteAttachedScaled;
		moteAttachedScaled.maxScale = scale;
		moteAttachedScaled.rotationRate = rotationRate;
		if (moteAttachedScaled.def.mote.needsMaintenance)
		{
			moteAttachedScaled.Maintain();
		}
		return moteAttachedScaled;
	}

	public override void ExposeData()
	{
		((Hediff_Ability)this).ExposeData();
		Scribe_Collections.Look(ref affectedPawns, "affectedPawns", LookMode.Reference);
		Scribe_Collections.Look(ref maintainedMotes, "maintainedMotes", LookMode.Reference);
	}
}
