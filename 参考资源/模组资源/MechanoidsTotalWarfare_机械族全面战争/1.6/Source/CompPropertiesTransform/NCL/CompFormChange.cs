using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class CompFormChange : ThingComp
{
	public int revertTickCounter;

	public int cooldownNow;

	public int cooldownMax;

	public CompPropertiesWeaponSwitch Props => (CompPropertiesWeaponSwitch)props;

	public ThingComp ThingComp(Type t)
	{
		for (int i = 0; i < parent.AllComps.Count; i++)
		{
			if (parent.AllComps[i].GetType() == t)
			{
				return parent.AllComps[i];
			}
		}
		return null;
	}

	public void TryTransformInto(Pawn pawn, TransformData tsd)
	{
		ThingWithComps thingWithComps = ((!tsd.thingDef.MadeFromStuff) ? ((ThingWithComps)ThingMaker.MakeThing(tsd.thingDef)) : ((ThingWithComps)ThingMaker.MakeThing(tsd.thingDef, parent.Stuff)));
		thingWithComps.HitPoints = parent.HitPoints;
		List<ThingComp> list = new List<ThingComp>();
		list.AddRange(thingWithComps.AllComps);
		foreach (ThingComp item2 in list)
		{
			if (Props.SharedCompsResolved.Contains(item2.GetType()))
			{
				ThingComp item = ThingComp(item2.GetType());
				thingWithComps.AllComps.Remove(item2);
				thingWithComps.AllComps.Add(item);
				parent.AllComps.Remove(item);
				parent.AllComps.Add(item2);
			}
		}
		foreach (ThingComp allComp in thingWithComps.AllComps)
		{
			allComp.parent = thingWithComps;
		}
		CompFormChange compFormChange = thingWithComps.TryGetComp<CompFormChange>();
		compFormChange.cooldownNow = tsd.transformCooldown;
		compFormChange.cooldownMax = tsd.transformCooldown;
		IThingHolder parentHolder = base.ParentHolder;
		Map map = parent.Map;
		Vector3 drawPos = parent.DrawPos;
		parent.Destroy();
		if (pawn == null)
		{
			if (map != null)
			{
				GenSpawn.Spawn(thingWithComps, drawPos.ToIntVec3(), map);
				if (tsd.moteOnTransform != null)
				{
					MoteMaker.MakeStaticMote(drawPos, map, tsd.moteOnTransform);
				}
			}
			else
			{
				parentHolder.GetDirectlyHeldThings().TryAdd(thingWithComps);
			}
		}
		else
		{
			pawn.equipment.AddEquipment(thingWithComps);
			drawPos = pawn.DrawPos;
			map = pawn.Map;
			if (tsd.moteOnTransform != null)
			{
				MoteMaker.MakeStaticMote(drawPos, map, tsd.moteOnTransform);
			}
		}
		if (map != null && tsd.soundOnTransform != null)
		{
			tsd.soundOnTransform.PlayOneShot(SoundInfo.InMap(new TargetInfo(drawPos.ToIntVec3(), map)));
		}
	}

	public override void CompTick()
	{
		CooldownTick();
	}

	public Pawn GetEquipper()
	{
		IThingHolder parentHolder = base.ParentHolder;
		if (parentHolder == null || !(parentHolder is Pawn_EquipmentTracker { pawn: var pawn }))
		{
			return null;
		}
		return pawn;
	}

	public void CooldownTick()
	{
		cooldownNow = Mathf.Max(cooldownNow - 1, 0);
		revertTickCounter++;
		if (Props.revertData != null && Props.revertData.revertAfterTicks <= revertTickCounter)
		{
			TryTransformInto(GetEquipper(), Props.revertData);
		}
	}

	public IEnumerable<Gizmo> HeldGizmos(Pawn pawn)
	{
		foreach (TransformData transformData in Props.transformData)
		{
			bool flag = true;
			if (transformData.needApparel != null)
			{
				flag = false;
				foreach (Apparel item in pawn.apparel.WornApparel)
				{
					if (item.def == transformData.needApparel)
					{
						flag = true;
					}
				}
			}
			TransformData tsdP = transformData;
			Texture2D icon = transformData.thingDef.uiIcon;
			float iconDrawScale = transformData.thingDef.uiIconScale;
			if (!transformData.iconPath.NullOrEmpty())
			{
				icon = ContentFinder<Texture2D>.Get(transformData.iconPath);
				iconDrawScale = transformData.iconSize;
			}
			if (flag)
			{
				yield return new Command_Transform_Action
				{
					defaultLabel = transformData.label,
					defaultDesc = transformData.description,
					compFormChange = this,
					transformData = tsdP,
					icon = icon,
					iconDrawScale = iconDrawScale,
					Disabled = (cooldownNow > 0 || !flag),
					disabledReason = "",
					action = delegate
					{
						TryTransformInto(pawn, tsdP);
					}
				};
			}
			else
			{
				yield return new Command_Transform_Action
				{
					defaultLabel = transformData.label,
					defaultDesc = transformData.description,
					compFormChange = this,
					transformData = tsdP,
					icon = icon,
					iconDrawScale = iconDrawScale,
					Disabled = (cooldownNow > 0 || !flag),
					disabledReason = "appActive".Translate(transformData.needApparel.label),
					action = delegate
					{
						TryTransformInto(pawn, tsdP);
					}
				};
			}
		}
		if (Props.revertData != null)
		{
			TransformData revertData = Props.revertData;
			Texture2D icon2 = revertData.thingDef.uiIcon;
			float iconDrawScale2 = revertData.thingDef.uiIconScale;
			if (!revertData.iconPath.NullOrEmpty())
			{
				icon2 = ContentFinder<Texture2D>.Get(revertData.iconPath);
				iconDrawScale2 = revertData.iconSize;
			}
			yield return new Command_AutoReversion_Action
			{
				defaultLabel = revertData.label,
				defaultDesc = revertData.description,
				compFormChange = this,
				transformData = revertData,
				icon = icon2,
				iconDrawScale = iconDrawScale2,
				Disabled = true,
				disabledReason = ""
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref cooldownNow, "cooldownNow", 0);
		Scribe_Values.Look(ref cooldownMax, "cooldownMax", 0);
		Scribe_Values.Look(ref revertTickCounter, "revertTickCounter", 0);
	}
}
