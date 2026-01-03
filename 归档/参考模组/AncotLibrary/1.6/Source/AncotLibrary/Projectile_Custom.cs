using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Projectile_Custom : Bullet
{
	public Projectile_Custom_Extension Props => def.GetModExtension<Projectile_Custom_Extension>();

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		base.Impact(hitThing, blockedByShield);
		if (Props?.impactEffecter != null)
		{
			Props.impactEffecter.Spawn().Trigger(new TargetInfo(ExactPosition.ToIntVec3(), launcher.Map), launcher);
		}
	}

	protected override void Tick()
	{
		base.Tick();
		LeaveTrail();
	}

	private void LeaveTrail()
	{
		if (Props?.trailFleck != null && base.Map != null && GenTicks.TicksGame % Props.trailFreauency == 0)
		{
			float num = new FloatRange(0f, 360f).RandomInRange;
			if (Props.fixedTrailRotation)
			{
				num = (destination - origin).ToAngleFlat() + 90f;
			}
			Map map = base.Map;
			FleckDef trailFleck = Props.trailFleck;
			Vector3 drawPos = DrawPos;
			Color trailColor = Props.trailColor;
			float rotation = num;
			AncotFleckMaker.CustomFleckThrow(map, trailFleck, drawPos, trailColor, default(Vector3), 1f, 0f, 0f, 0f, rotation);
		}
	}
}
