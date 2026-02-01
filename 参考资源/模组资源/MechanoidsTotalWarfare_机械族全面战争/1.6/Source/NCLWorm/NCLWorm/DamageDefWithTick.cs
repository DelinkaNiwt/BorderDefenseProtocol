using Verse;

namespace NCLWorm;

public class DamageDefWithTick : IExposable
{
	public DamageDef damageDef;

	public int tick = 1;

	public string Label => damageDef.label;

	public string LabelCap => damageDef.LabelCap;

	public DamageDefWithTick()
	{
	}

	public DamageDefWithTick(DamageDef damageDef, int ttick)
	{
		if (ttick < 0)
		{
			Log.Warning("Tried to set DamageDefWithTick tick to " + ttick + ". thingDef=" + damageDef);
			ttick = 0;
		}
		this.damageDef = damageDef;
		tick = ttick;
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref damageDef, "damageDef");
		Scribe_Values.Look(ref tick, "tick", 0);
	}

	public override string ToString()
	{
		return "(" + damageDef?.ToString() + "x" + tick + "Tick)";
	}

	public override int GetHashCode()
	{
		return damageDef.shortHash + tick << 16;
	}
}
