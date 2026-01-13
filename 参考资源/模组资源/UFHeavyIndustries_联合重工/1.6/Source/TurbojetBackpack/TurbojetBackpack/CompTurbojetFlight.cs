using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

public class CompTurbojetFlight : ThingComp
{
	private TurbojetMode currentMode = TurbojetMode.Off;

	private bool isCombatMode = false;

	private int ticksFlying = 0;

	private float currentHeight = 0f;

	private bool wasRoofed = false;

	private bool wasDrafted = false;

	private Command_Action cachedFlightCmd;

	private Command_Toggle cachedCombatCmd;

	public TurbojetMode FlightMode => currentMode;

	public bool IsCombatMode => isCombatMode;

	public float CurrentHeight => currentHeight;

	public bool IsAirborne => currentHeight > 0.1f;

	public TurbojetExtension Extension => parent.def.GetModExtension<TurbojetExtension>();

	private bool IsPawnMoving
	{
		get
		{
			Pawn pawn = (parent as Apparel)?.Wearer;
			return pawn != null && pawn.pather != null && pawn.pather.Moving;
		}
	}

	public bool ShouldBeFlying
	{
		get
		{
			Pawn pawn = (parent as Apparel)?.Wearer;
			if (pawn == null || !pawn.Drafted)
			{
				return false;
			}
			if (currentMode == TurbojetMode.Off)
			{
				return false;
			}
			if (currentMode == TurbojetMode.HoverAlways)
			{
				return true;
			}
			if (currentMode == TurbojetMode.HoverMoving && IsPawnMoving)
			{
				return true;
			}
			return false;
		}
	}

	public void ForceSetHeight(float h)
	{
		currentHeight = h;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref currentMode, "currentMode", TurbojetMode.Off);
		Scribe_Values.Look(ref isCombatMode, "isCombatMode", defaultValue: false);
		Scribe_Values.Look(ref currentHeight, "currentHeight", 0f);
		Scribe_Values.Look(ref wasRoofed, "wasRoofed", defaultValue: false);
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		Pawn wearer = (parent as Apparel)?.Wearer;
		if (wearer == null || !wearer.IsColonistPlayerControlled || !wearer.Drafted)
		{
			yield break;
		}
		if (cachedFlightCmd == null)
		{
			cachedFlightCmd = new Command_Action();
			cachedFlightCmd.action = delegate
			{
				SwitchMode(wearer);
			};
			UpdateFlightGizmoState();
		}
		UpdateFlightGizmoState();
		yield return cachedFlightCmd;
		if (cachedCombatCmd == null)
		{
			string combatIconPath = Extension?.combatModeIconPath;
			Texture2D combatIcon = ((!combatIconPath.NullOrEmpty()) ? ContentFinder<Texture2D>.Get(combatIconPath, reportFailure: false) : TexCommand.Attack);
			cachedCombatCmd = new Command_Toggle
			{
				defaultLabel = "Turbojet_CombatMode_Label".Translate(),
				defaultDesc = "Turbojet_CombatMode_Desc".Translate(),
				icon = combatIcon,
				isActive = () => isCombatMode,
				toggleAction = delegate
				{
					isCombatMode = !isCombatMode;
				}
			};
		}
		yield return cachedCombatCmd;
	}

	private void SwitchMode(Pawn wearer)
	{
		switch (currentMode)
		{
		case TurbojetMode.Off:
			currentMode = TurbojetMode.HoverMoving;
			break;
		case TurbojetMode.HoverMoving:
			currentMode = TurbojetMode.HoverAlways;
			break;
		case TurbojetMode.HoverAlways:
			currentMode = TurbojetMode.Off;
			break;
		}
		TurbojetGlobal.ClearCache(wearer.thingIDNumber);
		UpdateFlightGizmoState();
		if (wearer.pather != null && wearer.pather.Moving)
		{
			wearer.pather.StartPath(wearer.pather.Destination, PathEndMode.Touch);
		}
	}

	private void UpdateFlightGizmoState()
	{
		if (cachedFlightCmd != null)
		{
			string text = Extension?.flightModeIconPath ?? parent.def.graphicData?.texPath;
			cachedFlightCmd.icon = ((text != null) ? ContentFinder<Texture2D>.Get(text, reportFailure: false) : null);
			switch (currentMode)
			{
			case TurbojetMode.HoverMoving:
				cachedFlightCmd.defaultLabel = "Turbojet_Mode_HoverMove_Label".Translate();
				cachedFlightCmd.defaultDesc = "Turbojet_Mode_HoverMove_Desc".Translate();
				break;
			case TurbojetMode.HoverAlways:
				cachedFlightCmd.defaultLabel = "Turbojet_Mode_HoverAlways_Label".Translate();
				cachedFlightCmd.defaultDesc = "Turbojet_Mode_HoverAlways_Desc".Translate();
				break;
			default:
				cachedFlightCmd.defaultLabel = "Turbojet_Mode_Off_Label".Translate();
				cachedFlightCmd.defaultDesc = "Turbojet_Mode_Off_Desc".Translate();
				break;
			}
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		Pawn pawn = (parent as Apparel)?.Wearer;
		if (pawn == null || !pawn.Spawned || pawn.Map == null)
		{
			return;
		}
		if (pawn.Drafted != wasDrafted)
		{
			wasDrafted = pawn.Drafted;
			TurbojetGlobal.ClearCache(pawn.thingIDNumber);
		}
		TurbojetMode effectiveMode = TurbojetGlobal.GetEffectiveMode(pawn);
		if (effectiveMode == TurbojetMode.Off)
		{
			ProcessHeight(pawn);
			if (currentHeight <= 0.01f)
			{
				ticksFlying = 0;
			}
			return;
		}
		bool flag = pawn.Map.roofGrid.Roofed(pawn.Position);
		if (ShouldBeFlying && wasRoofed != flag && pawn.pather.Moving)
		{
			pawn.pather.StartPath(pawn.pather.Destination, PathEndMode.Touch);
		}
		wasRoofed = flag;
		ProcessHeight(pawn);
		if (currentHeight > 0.1f && !pawn.Downed && !pawn.InBed())
		{
			ticksFlying++;
			if (pawn.IsHashIntervalTick(10))
			{
				SpawnGroundDust(pawn);
			}
			int interval = Extension?.shadowMoteInterval ?? 5;
			if (pawn.IsHashIntervalTick(interval))
			{
				TurboJumpUtility.SpawnShadowMote(pawn, Extension, currentHeight + GetBreathingOffset());
			}
			TurboJumpUtility.SpawnThrustMotes(pawn.Map, pawn.DrawPos, pawn.Rotation, Extension, ticksFlying);
			return;
		}
		if (pawn.Drafted && effectiveMode == TurbojetMode.HoverMoving && !pawn.Downed && !pawn.InBed() && !IsPawnMoving)
		{
			int interval2 = Extension?.standbyMoteInterval ?? 15;
			if (pawn.IsHashIntervalTick(interval2))
			{
				TurboJumpUtility.SpawnStandbyMotes(pawn, Extension);
			}
		}
		if (currentHeight <= 0.01f)
		{
			ticksFlying = 0;
		}
	}

	private void SpawnGroundDust(Pawn wearer)
	{
		ThingDef thingDef = Extension?.flightMote;
		if (thingDef == null)
		{
			return;
		}
		float num = currentHeight + GetBreathingOffset();
		Vector3 drawPos = wearer.DrawPos;
		drawPos.z -= num;
		if (drawPos.ToIntVec3().InBounds(wearer.Map))
		{
			float num2 = Extension?.flightHeight ?? 1.2f;
			float num3 = Mathf.Clamp01(currentHeight / num2);
			float a = Extension?.flightSmokeScaleMin ?? 1.2f;
			float b = Extension?.flightSmokeScaleMax ?? 3.6f;
			float num4 = Mathf.Lerp(a, b, num3);
			if (num3 >= 0.95f)
			{
				num4 *= Rand.Range(0.9f, 1.1f);
			}
			TurboJumpUtility.SpawnMoteBurst(wearer.Map, drawPos, thingDef, 1, num4, 0.3f);
		}
	}

	private void ProcessHeight(Pawn p)
	{
		float num = 0f;
		float num2 = 0.01f;
		if (ShouldBeFlying && !p.Downed && !p.InBed())
		{
			num = Extension?.flightHeight ?? 1.2f;
			num2 = Extension?.takeoffSpeed ?? 0.08f;
		}
		else
		{
			num = 0f;
			num2 = Extension?.landingSpeed ?? 0.05f;
		}
		if (Mathf.Abs(currentHeight - num) > 0.001f)
		{
			currentHeight = Mathf.MoveTowards(currentHeight, num, num2);
		}
	}

	public float GetBreathingOffset()
	{
		if (currentHeight <= 0.01f)
		{
			return 0f;
		}
		float num = ((Extension != null) ? Extension.hoverAmplitude : 0.15f);
		float num2 = ((Extension != null) ? Extension.hoverFrequency : 2f);
		return Mathf.Sin((float)ticksFlying / 60f * (float)Math.PI * num2) * num;
	}
}
