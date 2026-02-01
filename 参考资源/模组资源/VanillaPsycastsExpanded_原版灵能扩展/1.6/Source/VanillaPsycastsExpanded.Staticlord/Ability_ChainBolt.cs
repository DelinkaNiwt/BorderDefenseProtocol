using RimWorld;
using UnityEngine;
using VEF.Abilities;

namespace VanillaPsycastsExpanded.Staticlord;

public class Ability_ChainBolt : Ability_ShootProjectile
{
	public override float GetPowerForPawn()
	{
		return ((Ability)this).def.power + (float)Mathf.FloorToInt((((Ability)this).pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 1f) * 4f);
	}
}
