using RimWorld;
using Verse;

namespace AncotLibrary;

public class DeploymentPack_Thing : Apparel
{
	private CompApparelReloadable_DeployThing comp => this.TryGetComp<CompApparelReloadable_DeployThing>();

	public override void Notify_BulletImpactNearby(BulletImpactData impactData)
	{
		base.Notify_BulletImpactNearby(impactData);
		Pawn wearer = base.Wearer;
		if (wearer != null && !wearer.Dead && comp.aiCanDeployNow && impactData.bullet.Launcher != null && impactData.bullet.Launcher.HostileTo(base.Wearer) && wearer.Spawned && !wearer.IsColonist)
		{
			Verb_DeployThing.DeployThing(comp);
		}
	}
}
