using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_FRShield : Building
{
	public CompFullProjectileInterceptor compFullProjectileInterceptor;

	public static Material ScreenTexture = MaterialPool.MatFrom("Things/Buildings/CMC_shield_Stationary", ShaderDatabase.MoteGlow);

	public static Material RotTexture = MaterialPool.MatFrom("Things/Buildings/CMC_shield_Beam", ShaderDatabase.TransparentPostLight);

	public static Material PulseTexture = MaterialPool.MatFrom("Things/Buildings/CMC_ShieldPlasmaPulse", ShaderDatabase.MoteGlow);

	private float rotatorAngle = 0f;

	public CompFullProjectileInterceptor CompFullProjectileInterceptor
	{
		get
		{
			CompFullProjectileInterceptor result;
			if ((result = compFullProjectileInterceptor) == null)
			{
				result = (compFullProjectileInterceptor = this.TryGetComp<CompFullProjectileInterceptor>());
			}
			return result;
		}
	}

	public void DrawScreen(Vector3 vec)
	{
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = DrawPos + Altitudes.AltIncVect;
		pos.y = AltitudeLayer.BuildingBelowTop.AltitudeFor() - 0.1f;
		matrix.SetTRS(pos, Quaternion.identity, vec);
		Graphics.DrawMesh(MeshPool.plane10, matrix, ScreenTexture, 0);
	}

	public void DrawRot(Vector3 vec)
	{
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = DrawPos + Altitudes.AltIncVect;
		pos.y = AltitudeLayer.BuildingBelowTop.AltitudeFor() - 0.1f;
		pos.z += 0.8554f;
		matrix.SetTRS(pos, Quaternion.AngleAxis(rotatorAngle, Vector3.up), vec);
		Graphics.DrawMesh(MeshPool.plane10, matrix, RotTexture, 0);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		compFullProjectileInterceptor = GetComp<CompFullProjectileInterceptor>();
	}

	protected override void Tick()
	{
		if (compFullProjectileInterceptor != null)
		{
			rotatorAngle = (rotatorAngle + 6f * (compFullProjectileInterceptor.energy / (float)compFullProjectileInterceptor.EnergyMax) * (compFullProjectileInterceptor.energy / (float)compFullProjectileInterceptor.EnergyMax)) % 360f;
		}
		else
		{
			compFullProjectileInterceptor = GetComp<CompFullProjectileInterceptor>();
		}
		base.Tick();
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Vector3 vec = new Vector3(3.3f, 1f, 3.3f);
		if (compFullProjectileInterceptor != null && compFullProjectileInterceptor.Active)
		{
			DrawScreen(vec);
			DrawRot(vec);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		yield return new Command_Action
		{
			defaultLabel = "CMC.ChangeSize".Translate(),
			defaultDesc = "CMC.ChangeSizeDesc".Translate(),
			icon = ContentFinder<Texture2D>.Get("UI/UI_ShieldSize"),
			action = delegate
			{
				Window_ShieldSize window = new Window_ShieldSize("CMC.ChangeSizeTitle".Translate(), "CMC.ChangeSizeDescription".Translate(compFullProjectileInterceptor.radius.ToString("F1")), this);
				Find.WindowStack.Add(window);
			}
		};
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref CompFullProjectileInterceptor.radius, "CMCShield.radius", 20f);
	}

	public override string GetInspectString()
	{
		string text = base.GetInspectString();
		if (!text.NullOrEmpty())
		{
			text += "\n";
		}
		return text + "CMC.ShieldSize".Translate(compFullProjectileInterceptor.radius.ToString("F1")).Colorize(ColorLibrary.Cyan);
	}
}
