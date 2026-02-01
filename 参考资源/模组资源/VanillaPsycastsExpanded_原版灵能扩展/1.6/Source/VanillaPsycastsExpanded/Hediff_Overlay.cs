using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public abstract class Hediff_Overlay : Hediff_Ability
{
	public MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

	private Material material;

	public Material OverlayMat
	{
		get
		{
			if (material == null)
			{
				material = MaterialPool.MatFrom(OverlayPath, ShaderDatabase.MoteGlow);
			}
			return material;
		}
	}

	public virtual float OverlaySize => 1f;

	public virtual string OverlayPath { get; }

	public override void PostAdd(DamageInfo? dinfo)
	{
		((HediffWithComps)this).PostAdd(dinfo);
		((Hediff)this).pawn.MapHeld?.GetComponent<MapComponent_PsycastsManager>().hediffsToDraw.Add(this);
	}

	public virtual void Draw()
	{
	}
}
