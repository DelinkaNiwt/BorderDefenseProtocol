using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_GroupLink : Hediff_Overlay
{
	public List<Pawn> linkedPawns = new List<Pawn>();

	public override string OverlayPath => "Other/ForceField";

	public virtual Color OverlayColor => new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.5f);

	public override float OverlaySize => ((Hediff_Ability)this).ability.GetRadiusForPawn();

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		LinkAllPawnsAround();
	}

	public void LinkAllPawnsAround()
	{
		foreach (Pawn item in from x in GenRadial.RadialDistinctThingsAround(((Hediff)(object)this).pawn.Position, ((Hediff)(object)this).pawn.Map, ((Hediff_Ability)this).ability.GetRadiusForPawn(), useCenter: true).OfType<Pawn>()
			where x.RaceProps.Humanlike && x != ((Hediff)(object)this).pawn
			select x)
		{
			if (!linkedPawns.Contains(item))
			{
				linkedPawns.Add(item);
			}
		}
	}

	private void UnlinkAll()
	{
		for (int num = linkedPawns.Count - 1; num >= 0; num--)
		{
			linkedPawns.RemoveAt(num);
		}
	}

	public override void PostRemoved()
	{
		((HediffWithComps)(object)this).PostRemoved();
		UnlinkAll();
	}

	public override void Tick()
	{
		((Hediff)(object)this).Tick();
		for (int num = linkedPawns.Count - 1; num >= 0; num--)
		{
			Pawn pawn = linkedPawns[num];
			if (pawn.Map != ((Hediff)(object)this).pawn.Map || pawn.Position.DistanceTo(((Hediff)(object)this).pawn.Position) > ((Hediff_Ability)this).ability.GetRadiusForPawn())
			{
				linkedPawns.RemoveAt(num);
			}
		}
		if (!linkedPawns.Any())
		{
			((Hediff)(object)this).pawn.health.RemoveHediff((Hediff)(object)this);
		}
	}

	public override void Draw()
	{
		Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		Color overlayColor = OverlayColor;
		MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, overlayColor);
		Matrix4x4 matrix = default(Matrix4x4);
		matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(OverlaySize * 2f * 1.1601562f, 1f, OverlaySize * 2f * 1.1601562f));
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
		foreach (Pawn linkedPawn in linkedPawns)
		{
			GenDraw.DrawLineBetween(linkedPawn.DrawPos, ((Hediff)(object)this).pawn.DrawPos, SimpleColor.Yellow);
		}
	}

	public override void ExposeData()
	{
		((Hediff_Ability)this).ExposeData();
		Scribe_Collections.Look(ref linkedPawns, "linkedPawns", LookMode.Reference);
	}
}
