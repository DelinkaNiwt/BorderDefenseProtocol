using RimWorld;
using Verse;

namespace AncotLibrary;

public class DeploymentPack_Pawn : Apparel
{
	private CompApparelReloadable_DeployPawn comp => this.TryGetComp<CompApparelReloadable_DeployPawn>();

	public override void Notify_BulletImpactNearby(BulletImpactData impactData)
	{
		base.Notify_BulletImpactNearby(impactData);
		Pawn wearer = base.Wearer;
		if (wearer != null && !wearer.Dead && comp.aiCanDeployNow && impactData.bullet.Launcher != null && impactData.bullet.Launcher.HostileTo(base.Wearer) && wearer.Spawned && !wearer.IsColonist)
		{
			Verb_DeployPawn.DeployPawn(comp);
		}
	}
}
