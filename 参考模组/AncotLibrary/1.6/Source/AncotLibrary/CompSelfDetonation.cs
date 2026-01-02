using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompSelfDetonation : ThingComp
{
	private CompProperties_SelfDetonation Props => (CompProperties_SelfDetonation)props;

	public override void Notify_UsedVerb(Pawn pawn, Verb verb)
	{
		Vector3 v = (verb.CurrentTarget.Thing.Position - parent.Position).ToVector3();
		v.Normalize();
		float num = v.ToAngleFlat();
		if (Props.angle != 0f)
		{
			if (num - 30f < -180f || num + 30f > 180f)
			{
				if (num < 0f)
				{
					IntVec3 positionHeld = parent.PositionHeld;
					Map map = parent.Map;
					float radius = Props.radius;
					DamageDef damageDef = Props.damageDef;
					ThingWithComps instigator = parent;
					int damageAmount = Props.damageAmount;
					float armorPenetration = Props.armorPenetration;
					FloatRange? affectedAngle = new FloatRange(-180f, num + Props.angle);
					GenExplosion.DoExplosion(positionHeld, map, radius, damageDef, instigator, damageAmount, armorPenetration, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, affectedAngle);
					IntVec3 positionHeld2 = parent.PositionHeld;
					Map map2 = parent.Map;
					float radius2 = Props.radius;
					DamageDef damageDef2 = Props.damageDef;
					ThingWithComps instigator2 = parent;
					int damageAmount2 = Props.damageAmount;
					float armorPenetration2 = Props.armorPenetration;
					affectedAngle = new FloatRange(360f + num - Props.angle, 180f);
					GenExplosion.DoExplosion(positionHeld2, map2, radius2, damageDef2, instigator2, damageAmount2, armorPenetration2, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, affectedAngle);
				}
				else
				{
					IntVec3 positionHeld3 = parent.PositionHeld;
					Map map3 = parent.Map;
					float radius3 = Props.radius;
					DamageDef damageDef3 = Props.damageDef;
					ThingWithComps instigator3 = parent;
					int damageAmount3 = Props.damageAmount;
					float armorPenetration3 = Props.armorPenetration;
					FloatRange? affectedAngle = new FloatRange(num - Props.angle, 180f);
					GenExplosion.DoExplosion(positionHeld3, map3, radius3, damageDef3, instigator3, damageAmount3, armorPenetration3, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, affectedAngle);
					IntVec3 positionHeld4 = parent.PositionHeld;
					Map map4 = parent.Map;
					float radius4 = Props.radius;
					DamageDef damageDef4 = Props.damageDef;
					ThingWithComps instigator4 = parent;
					int damageAmount4 = Props.damageAmount;
					float armorPenetration4 = Props.armorPenetration;
					affectedAngle = new FloatRange(-180f, -360f + num + Props.angle);
					GenExplosion.DoExplosion(positionHeld4, map4, radius4, damageDef4, instigator4, damageAmount4, armorPenetration4, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, affectedAngle);
				}
			}
			else
			{
				IntVec3 positionHeld5 = parent.PositionHeld;
				Map map5 = parent.Map;
				float radius5 = Props.radius;
				DamageDef damageDef5 = Props.damageDef;
				ThingWithComps instigator5 = parent;
				int damageAmount5 = Props.damageAmount;
				float armorPenetration5 = Props.armorPenetration;
				FloatRange? affectedAngle = new FloatRange(num - Props.angle, num + Props.angle);
				GenExplosion.DoExplosion(positionHeld5, map5, radius5, damageDef5, instigator5, damageAmount5, armorPenetration5, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, affectedAngle);
			}
		}
		else
		{
			GenExplosion.DoExplosion(parent.PositionHeld, parent.Map, Props.radius, Props.damageDef, parent, Props.damageAmount, Props.armorPenetration);
		}
		parent.Kill();
	}
}
