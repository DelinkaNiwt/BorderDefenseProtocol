using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Graphic_RefuelableMulti : Graphic_Multi
{
	private Graphic lowFuelGraphic;

	private Graphic mediumFuelGraphic;

	private Graphic highFuelGraphic;

	public override void Init(GraphicRequest req)
	{
		base.Init(req);
		lowFuelGraphic = CreateStateGraphic(req.path + "_low", req.shader, req.drawSize, req.color, req.colorTwo);
		mediumFuelGraphic = CreateStateGraphic(req.path + "_medium", req.shader, req.drawSize, req.color, req.colorTwo);
		highFuelGraphic = CreateStateGraphic(req.path + "_high", req.shader, req.drawSize, req.color, req.colorTwo);
	}

	private Graphic CreateStateGraphic(string path, Shader shader, Vector2 size, Color color, Color colorTwo)
	{
		return GraphicDatabase.Get(typeof(Graphic_Single), path, shader, size, color, colorTwo);
	}

	public override Material MatAt(Rot4 rot, Thing thing = null)
	{
		if (thing == null)
		{
			return mediumFuelGraphic.MatAt(rot);
		}
		CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
		if (compRefuelable == null)
		{
			return mediumFuelGraphic.MatAt(rot);
		}
		float fuelPercent = compRefuelable.Fuel / compRefuelable.Props.fuelCapacity;
		return GetFuelStateGraphic(fuelPercent).MatAt(rot);
	}

	private Graphic GetFuelStateGraphic(float fuelPercent)
	{
		if (fuelPercent < 0.2f)
		{
			return lowFuelGraphic;
		}
		if (fuelPercent < 0.7f)
		{
			return mediumFuelGraphic;
		}
		return highFuelGraphic;
	}

	public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
	{
		Material mat = MatAt(rot, thing);
		if (mat != null)
		{
			if (thing != null)
			{
				mat.SetFloat(ShaderPropertyIDs.RandomPerObject, thing.thingIDNumber.HashOffset());
			}
			Graphics.DrawMesh(MeshAt(rot), loc, Quaternion.AngleAxis(extraRotation, Vector3.up), mat, 0);
		}
		if (base.ShadowGraphic != null)
		{
			base.ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}
	}

	public override Mesh MeshAt(Rot4 rot)
	{
		return mediumFuelGraphic.MeshAt(rot);
	}
}
