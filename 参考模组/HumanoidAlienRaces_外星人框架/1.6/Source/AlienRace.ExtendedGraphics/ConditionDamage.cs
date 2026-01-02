namespace AlienRace.ExtendedGraphics;

public class ConditionDamage : Condition
{
	public new const string XmlNameParseKey = "Damage";

	public float damage;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.IsPartBelowHealthThreshold(data.bodyPart, data.bodyPartLabel, damage);
	}
}
