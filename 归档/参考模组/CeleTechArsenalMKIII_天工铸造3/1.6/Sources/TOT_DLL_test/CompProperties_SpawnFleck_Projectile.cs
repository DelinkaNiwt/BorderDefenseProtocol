using Verse;

namespace TOT_DLL_test;

public class CompProperties_SpawnFleck_Projectile : CompProperties
{
	public FleckDef FleckDef;

	public int Fleck_MakeFleckTickMax = 10;

	public IntRange Fleck_MakeFleckNum;

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(1f, 2f);

	public FloatRange Fleck_Speed = new FloatRange(5f, 7f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public CompProperties_SpawnFleck_Projectile()
	{
		compClass = typeof(Comp_SpawnFleck_Projectile);
	}
}
