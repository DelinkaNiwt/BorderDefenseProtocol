using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Comp_AdvancedAmmo : ThingComp
{
	public int selectedProjectileType = 0;

	private bool usingSecondaryProjectile = false;

	public CompProperties_AdvancedAmmo Props => (CompProperties_AdvancedAmmo)props;

	protected virtual Pawn GetUser => (base.ParentHolder is Pawn_EquipmentTracker) ? ((Pawn)base.ParentHolder.ParentHolder) : null;

	public virtual ThingDef FindDefaultAmmo
	{
		get
		{
			foreach (VerbProperties verb in parent.def.Verbs)
			{
				if (verb.defaultProjectile != null)
				{
					return verb.defaultProjectile;
				}
			}
			return null;
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			GG_Properties_RandomProjectile defaultProjectile = new GG_Properties_RandomProjectile();
			List<GG_Properties_RandomProjectile> list = new List<GG_Properties_RandomProjectile> { defaultProjectile };
			if (Props.projectile1?.projectile != null)
			{
				list.Add(Props.projectile1);
			}
			if (Props.projectile2?.projectile != null)
			{
				list.Add(Props.projectile2);
			}
			if (Props.projectile3?.projectile != null)
			{
				list.Add(Props.projectile3);
			}
			GG_Properties_RandomProjectile selectedProjectile = list.RandomElementByWeight((GG_Properties_RandomProjectile p) => p.weight);
			if (selectedProjectile == Props.projectile1)
			{
				selectedProjectileType = 1;
			}
			else if (selectedProjectile == Props.projectile2)
			{
				selectedProjectileType = 2;
			}
			else if (selectedProjectile == Props.projectile3)
			{
				selectedProjectileType = 3;
			}
			else
			{
				selectedProjectileType = 0;
			}
		}
	}

	public ThingDef GetPrimaryProjectile()
	{
		int num = selectedProjectileType;
		if (1 == 0)
		{
		}
		ThingDef result = num switch
		{
			1 => Props.projectile1?.projectile, 
			2 => Props.projectile2?.projectile, 
			3 => Props.projectile3?.projectile, 
			_ => FindDefaultAmmo, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public ThingDef GetSecondaryProjectile()
	{
		int num = selectedProjectileType;
		if (1 == 0)
		{
		}
		ThingDef result = num switch
		{
			0 => Props.secondaryProjectile0, 
			1 => Props.secondaryProjectile1, 
			2 => Props.secondaryProjectile2, 
			3 => Props.secondaryProjectile3, 
			_ => null, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public int GetPrimaryProjectileCount()
	{
		int num = selectedProjectileType;
		if (1 == 0)
		{
		}
		int result = num switch
		{
			0 => Props.primaryProjectileCount0, 
			1 => Props.primaryProjectileCount1, 
			2 => Props.primaryProjectileCount2, 
			3 => Props.primaryProjectileCount3, 
			_ => 1, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public int GetSecondaryProjectileCount()
	{
		int num = selectedProjectileType;
		if (1 == 0)
		{
		}
		int result = num switch
		{
			0 => Props.secondaryProjectileCount0, 
			1 => Props.secondaryProjectileCount1, 
			2 => Props.secondaryProjectileCount2, 
			3 => Props.secondaryProjectileCount3, 
			_ => 0, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public bool HasSecondaryProjectile()
	{
		return GetSecondaryProjectile() != null;
	}

	public void SetUsingSecondaryProjectile(bool value)
	{
		usingSecondaryProjectile = value;
	}

	public bool IsUsingSecondaryProjectile()
	{
		return usingSecondaryProjectile;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref selectedProjectileType, "selectedProjectileType", 0);
		Scribe_Values.Look(ref usingSecondaryProjectile, "usingSecondaryProjectile", defaultValue: false);
	}

	public void SetProjectileType(int newProjectileType)
	{
		selectedProjectileType = newProjectileType;
		usingSecondaryProjectile = false;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		Pawn pawn = GetUser;
		if (pawn?.Faction != Faction.OfPlayer || Find.Selector.SingleSelectedThing != pawn)
		{
			yield break;
		}
		string defaultAmmoLabel = "GG_Keyed_DefaultAmmo".Translate();
		switch (selectedProjectileType)
		{
		case 0:
			if (Props.projectile1?.projectile != null)
			{
				yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath1)) ? Props.customTexturePath1 : FindDefaultAmmo.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + defaultAmmoLabel, action: delegate
				{
					SetProjectileType(1);
				});
			}
			break;
		case 1:
			if (Props.projectile2?.projectile != null)
			{
				yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath2)) ? Props.customTexturePath2 : Props.projectile1.projectile.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + Props.projectile1.projectile.label.Translate(), action: delegate
				{
					SetProjectileType(2);
				});
			}
			if (Props.projectile3?.projectile != null && (Props.projectile2 == null || Props.projectile2.projectile == null))
			{
				yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath3)) ? Props.customTexturePath3 : Props.projectile1.projectile.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + Props.projectile1.projectile.label.Translate(), action: delegate
				{
					SetProjectileType(3);
				});
			}
			yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath0)) ? Props.customTexturePath0 : Props.projectile1.projectile.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + Props.projectile1.projectile.label.Translate(), action: delegate
			{
				SetProjectileType(0);
			});
			break;
		case 2:
			if (Props.projectile3?.projectile != null)
			{
				yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath3)) ? Props.customTexturePath3 : Props.projectile2.projectile.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + Props.projectile2.projectile.label.Translate(), action: delegate
				{
					SetProjectileType(3);
				});
			}
			yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath0)) ? Props.customTexturePath0 : Props.projectile2.projectile.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + Props.projectile2.projectile.label.Translate(), action: delegate
			{
				SetProjectileType(0);
			});
			break;
		case 3:
			yield return CreateGizmo(iconPath: (!string.IsNullOrEmpty(Props.customTexturePath0)) ? Props.customTexturePath0 : Props.projectile3.projectile.graphicData.texPath, label: "GG_Keyed_ChooseAmmo".Translate() + Props.projectile3.projectile.label.Translate(), action: delegate
			{
				SetProjectileType(0);
			});
			break;
		}
	}

	private Command_Action CreateGizmo(string label, string iconPath, Action action)
	{
		return new Command_Action
		{
			defaultLabel = label,
			icon = ContentFinder<Texture2D>.Get(iconPath),
			action = action
		};
	}
}
