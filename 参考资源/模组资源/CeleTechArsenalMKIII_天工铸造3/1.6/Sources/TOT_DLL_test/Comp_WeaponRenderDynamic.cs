using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Comp_WeaponRenderDynamic : ThingComp
{
	private Material MaterialS;

	private readonly Mesh DefaultMesh = MeshPool.plane10;

	public Color Camocolor = Color.white;

	private CompProperties_WeaponRenderDynamic Props => (CompProperties_WeaponRenderDynamic)props;

	private float AngleOnGround => DrawAngle(parent.DrawPos, parent.def, parent);

	private Material GetMaterial
	{
		get
		{
			if (MaterialS != null)
			{
				return MaterialS;
			}
			MaterialS = MaterialPool.MatFrom(Props.TexturePath, ShaderTypeDefOf.MoteGlow.Shader);
			return MaterialS;
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
		int num = Find.TickManager.TicksGame / Props.ticksPerFrame % Props.totalFrames;
		Vector2 mainTextureScale = new Vector2(1f / (float)Props.totalFrames, 1f);
		Vector2 mainTextureOffset = new Vector2((float)num * mainTextureScale.x, 0f);
		Material getMaterial = GetMaterial;
		getMaterial.mainTextureOffset = mainTextureOffset;
		getMaterial.mainTextureScale = mainTextureScale;
		getMaterial.shader = ShaderTypeDefOf.MoteGlow.Shader;
		Graphics.DrawMesh(mesh, matrix, getMaterial, 0);
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref Camocolor, "Camocolor");
		base.PostExposeData();
	}
}
