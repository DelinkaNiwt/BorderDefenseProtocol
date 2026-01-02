using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Comp_WeaponRenderStatic : ThingComp
{
	private readonly Mesh DefaultMesh = MeshPool.plane10;

	public Color Camocolor = Color.white;

	private Material Material_Glow;

	private Material Material_Camo;

	private CompProperties_WeaponRenderStatic Props => (CompProperties_WeaponRenderStatic)props;

	private float AngleOnGround => DrawAngle(parent.DrawPos, parent.def, parent);

	private Material GetMaterial
	{
		get
		{
			if (Material_Glow != null)
			{
				return Material_Glow;
			}
			Material_Glow = MaterialPool.MatFrom(Props.TexturePath, ShaderTypeDefOf.MoteGlow.Shader);
			return Material_Glow;
		}
	}

	private Material GetMaterial_Camo
	{
		get
		{
			if (Material_Camo != null)
			{
				return Material_Camo;
			}
			Camocolor.a = 1f;
			Material_Camo = MaterialPool.MatFrom(Props.TexturePath_Camo, ShaderTypeDefOf.MetaOverlay.Shader, Camocolor);
			return Material_Camo;
		}
	}

	public override void PostDraw()
	{
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = parent.DrawPos + new Vector3(0f, 0.1f, 0f) + parent.Graphic.DrawOffset(parent.Rotation);
		matrix.SetTRS(s: new Vector3(parent.Graphic.drawSize.x, 1f, parent.Graphic.drawSize.y), pos: pos, q: Quaternion.AngleAxis(AngleOnGround, Vector3.up));
		PostDrawExtraGlower(DefaultMesh, matrix);
	}

	public float DrawAngle(Vector3 loc, ThingDef thingDef, Thing thing)
	{
		float result = 0f;
		float? rotInRack = GetRotInRack(thing, thingDef, loc.ToIntVec3());
		if (rotInRack.HasValue)
		{
			result = rotInRack.Value;
		}
		else if (thing != null)
		{
			result = 0f - parent.def.graphicData.onGroundRandomRotateAngle + (float)(thing.thingIDNumber * 542) % (parent.def.graphicData.onGroundRandomRotateAngle * 2f);
		}
		return result;
	}

	private float? GetRotInRack(Thing thing, ThingDef thingDef, IntVec3 loc)
	{
		if (thing == null || !thingDef.IsWeapon || !thing.Spawned || !loc.InBounds(thing.Map) || loc.GetEdifice(thing.Map) == null || loc.GetItemCount(thing.Map) < 2)
		{
			return null;
		}
		if (thingDef.rotateInShelves)
		{
			return -90f;
		}
		return 0f;
	}

	public void PostDrawExtraGlower(Mesh mesh, Matrix4x4 matrix)
	{
		if (Props.TexturePath_Camo != null)
		{
			Graphics.DrawMesh(mesh, matrix, GetMaterial_Camo, 0);
		}
		if (Props.TexturePath != null)
		{
			Graphics.DrawMesh(mesh, matrix, GetMaterial, 0);
		}
	}

	public void UpdateCamoColor()
	{
		Camocolor.a = 1f;
		Material_Camo = null;
		Material_Camo = MaterialPool.MatFrom(Props.TexturePath_Camo, ShaderTypeDefOf.MetaOverlay.Shader, Camocolor);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref Camocolor, "Camocolor", Color.white, forceSave: true);
	}
}
