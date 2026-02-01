using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_Deathshield : Hediff_Overlay
{
	private static readonly Color RottenColor = new Color(0.29f, 0.25f, 0.22f);

	public float curAngle;

	public Color? skinColor;

	public override float OverlaySize => 1.5f;

	public override string OverlayPath => "Effects/Necropath/Deathshield/Deathshield";

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		if (ModCompatibility.AlienRacesIsActive)
		{
			skinColor = ModCompatibility.GetSkinColorFirst(((Hediff)(object)this).pawn);
			ModCompatibility.SetSkinColorFirst(((Hediff)(object)this).pawn, RottenColor);
		}
		else
		{
			skinColor = ((Hediff)(object)this).pawn.story.skinColorOverride;
			((Hediff)(object)this).pawn.story.skinColorOverride = RottenColor;
		}
		((Hediff)(object)this).pawn.Drawer.renderer.SetAllGraphicsDirty();
	}

	public override void PostRemoved()
	{
		((HediffWithComps)(object)this).PostRemoved();
		if (ModCompatibility.AlienRacesIsActive)
		{
			ModCompatibility.SetSkinColorFirst(((Hediff)(object)this).pawn, skinColor.Value);
		}
		else
		{
			((Hediff)(object)this).pawn.story.skinColorOverride = skinColor;
		}
		((Hediff)(object)this).pawn.Drawer.renderer.SetAllGraphicsDirty();
	}

	public override void Tick()
	{
		((Hediff)(object)this).Tick();
		curAngle += 0.07f;
		if (curAngle > 360f)
		{
			curAngle = 0f;
		}
	}

	public override void Draw()
	{
		if (((Hediff)(object)this).pawn.Spawned)
		{
			Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
			drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.AngleAxis(curAngle, Vector3.up), new Vector3(OverlaySize, 1f, OverlaySize));
			UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
		}
	}

	public override void ExposeData()
	{
		((Hediff_Ability)this).ExposeData();
		Scribe_Values.Look(ref curAngle, "curAngle", 0f);
	}
}
