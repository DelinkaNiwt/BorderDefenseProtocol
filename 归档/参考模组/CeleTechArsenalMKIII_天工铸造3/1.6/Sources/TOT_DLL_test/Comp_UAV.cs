using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Comp_UAV : ThingComp, IAttackTargetSearcher
{
	private const int StartShootIntervalTicks = 10;

	private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/UI_TargetRange");

	public Thing gun;

	public float curRotation;

	protected int burstCooldownTicksLeft;

	protected int burstWarmupTicksLeft;

	public LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

	private bool fireAtWill = true;

	private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

	private int lastAttackTargetTick;

	public Material Mat;

	public Material Mat2;

	public int LastAttackTick = 0;

	public GraphicData graphicData;

	public float PosX;

	public float PosY;

	public float rand = Rand.Range(0f, 200f);

	public CompApparelReloadable CompApparelReloadable;

	public CompProperties_UAV Props => (CompProperties_UAV)props;

	public bool IsApparel => parent is Apparel;

	public Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	public Thing Thing => PawnOwner;

	public Verb CurrentEffectiveVerb => AttackVerb;

	public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

	public int LastAttackTargetTick => lastAttackTargetTick;

	public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

	public Verb AttackVerb => GunCompEq.PrimaryVerb;

	private bool WarmingUp => burstWarmupTicksLeft > 0;

	public bool CanShoot
	{
		get
		{
			if (PawnOwner == null)
			{
				return false;
			}
			if (!fireAtWill)
			{
				return false;
			}
			if (CompApparelReloadable != null && CompApparelReloadable.RemainingCharges < 1)
			{
				return false;
			}
			if ((PawnOwner.Faction.IsPlayer && PawnOwner.Drafted) || (!PawnOwner.Faction.IsPlayer && !PawnOwner.DeadOrDowned))
			{
				return true;
			}
			return false;
		}
	}

	public bool AutoAttack => Props.autoAttack;

	public Thing ReloadableThing => parent;

	public int BaseReloadTicks => 10;

	public override void Notify_Equipped(Pawn pawn)
	{
		base.PostPostMake();
		if (IsApparel)
		{
			MakeGun();
		}
		CompApparelReloadable = parent.TryGetComp<CompApparelReloadable>();
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
	}

	private void MakeGun()
	{
		gun = ThingMaker.MakeThing(Props.turretDef);
		UpdateGunVerbs();
	}

	private void UpdateGunVerbs()
	{
		List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
		for (int i = 0; i < allVerbs.Count; i++)
		{
			Verb verb = allVerbs[i];
			verb.caster = PawnOwner;
			verb.castCompleteCallback = delegate
			{
				burstCooldownTicksLeft = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
			};
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		PosX = Mathf.PingPong(((float)Find.TickManager.TicksGame + rand) * Props.BobSpeed, Props.BobDistance) * Props.Xoffset;
		PosY = Mathf.Sin(((float)Find.TickManager.TicksGame + rand) * 0.006f) * Props.Yoffset;
		if (!CanShoot)
		{
			return;
		}
		if (currentTarget.IsValid)
		{
			curRotation = (currentTarget.Cell.ToVector3Shifted() - PawnOwner.DrawPos).AngleFlat() + Props.angleOffset;
		}
		AttackVerb.VerbTick();
		if (AttackVerb.state == VerbState.Bursting)
		{
			return;
		}
		if (WarmingUp)
		{
			burstWarmupTicksLeft--;
			if (burstWarmupTicksLeft == 0)
			{
				bool flag = AttackVerb.TryStartCastOn(currentTarget, surpriseAttack: false, canHitNonTargetPawns: true, preventFriendlyFire: false, nonInterruptingSelfCast: true);
				lastAttackTargetTick = Find.TickManager.TicksGame;
				lastAttackedTarget = currentTarget;
				if (CompApparelReloadable != null && flag)
				{
					CompApparelReloadable.UsedOnce();
				}
			}
			return;
		}
		if (burstCooldownTicksLeft > 0)
		{
			burstCooldownTicksLeft--;
		}
		if (burstCooldownTicksLeft <= 0 && PawnOwner.IsHashIntervalTick(10))
		{
			currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable);
			if (currentTarget.IsValid)
			{
				burstWarmupTicksLeft = 1;
			}
			else
			{
				ResetCurrentTarget();
			}
		}
	}

	private void ResetCurrentTarget()
	{
		currentTarget = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = 0;
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (!IsApparel)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		if (PawnOwner.Faction == Faction.OfPlayer && PawnOwner.Drafted)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "CommandToggleTurret".Translate(),
				defaultDesc = "CommandToggleTurretDesc".Translate(),
				isActive = () => fireAtWill,
				icon = ToggleTurretIcon.Texture,
				toggleAction = delegate
				{
					fireAtWill = !fireAtWill;
					PawnOwner.Drawer.renderer.renderTree.SetDirty();
				}
			};
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		if (Props.turretDef != null)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "Turret".Translate(), Props.turretDef.LabelCap, "Stat_Thing_TurretDesc".Translate(), 5600, null, Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(Props.turretDef)));
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
		Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
		Scribe_Deep.Look(ref gun, "gun");
		Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (gun == null)
			{
				Log.Error("CompTurrentGun had null gun after loading. Recreating.");
				MakeGun();
			}
			else
			{
				UpdateGunVerbs();
			}
		}
	}
}
