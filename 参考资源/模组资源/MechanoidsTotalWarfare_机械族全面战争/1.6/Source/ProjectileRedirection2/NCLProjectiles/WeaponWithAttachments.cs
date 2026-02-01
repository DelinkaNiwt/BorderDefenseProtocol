using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class WeaponWithAttachments : ThingWithComps
{
	public static bool isAiming;

	public const string SignalEquipped = "Equipped";

	public const string SignalUnequipped = "Unequipped";

	public const string SignalDrafted = "Drafted";

	public const string SignalMeleeAttack = "MeleeAttack";

	public const string SignalWarmupStarted = "WarmupStarted";

	public const string SignalWarmupCompleted = "WarmupFinished";

	public const string SignalShotCast = "ShotCast";

	public const string SignalCooldownStarted = "CooldownStarted";

	public const string SignalAmmoChanged = "AmmoChanged";

	public const string SignalAbilityWarmupStarted = "AbilityWarmupStarted";

	public const string SignalAbilityCast = "AbilityCast";

	private const float DEFAULT_HUMAN_PAWN_SIZE = 1.5f;

	public List<WeaponAttachment> weaponAttachments;

	private ModExtension_WeaponAttachments cachedWeaponExtension;

	public readonly WeaponOrientationData orientationData = new WeaponOrientationData();

	public float pawnScaleFactor = 1f;

	protected bool scaleWithPawnSize;

	protected Pawn lastEquippedPawn;

	protected Vector3 lastRenderedPosition;

	protected int lastRenderedTick;

	protected readonly Dictionary<string, Func<int, bool, Vector3>> cachedAttachmentPositionGetters = new Dictionary<string, Func<int, bool, Vector3>>();

	public ModExtension_WeaponAttachments AttachmentExtension
	{
		get
		{
			ModExtension_WeaponAttachments result;
			if ((result = cachedWeaponExtension) == null)
			{
				result = (cachedWeaponExtension = def.GetModExtension<ModExtension_WeaponAttachments>());
			}
			return result;
		}
	}

	public bool ShouldDrawNormally => AttachmentExtension.drawWeaponNormally;

	public bool TickWeaponWhileEquipped => AttachmentExtension.tickWeaponWhileEquipped;

	public bool DrawNorthIdleMirrored => AttachmentExtension.drawNorthIdleMirrored;

	public Pawn Wielder
	{
		get
		{
			if (!(base.ParentHolder is Pawn_EquipmentTracker { pawn: var pawn }))
			{
				return null;
			}
			return pawn;
		}
	}

	public bool IsAiming
	{
		get
		{
			object obj = Wielder?.stances?.curStance;
			return obj is Stance_Busy stance_Busy && !stance_Busy.neverAimWeapon;
		}
	}

	public Vector3 LastRenderedPosition => lastRenderedPosition;

	public override void PostMake()
	{
		base.PostMake();
		InitializeAttachments();
	}

	private void InitializeAttachments()
	{
		ModExtension_WeaponAttachments attachmentExtension = AttachmentExtension;
		if (attachmentExtension == null || attachmentExtension.attachments.NullOrEmpty())
		{
			return;
		}
		weaponAttachments = new List<WeaponAttachment>();
		for (int i = 0; i < attachmentExtension.attachments.Count; i++)
		{
			WeaponAttachment item = attachmentExtension.attachments[i].CreateInstance(this);
			weaponAttachments.Add(item);
			if (attachmentExtension.attachments[i].scaleWithParentSize)
			{
				scaleWithPawnSize = true;
			}
		}
		for (int j = 0; j < weaponAttachments.Count; j++)
		{
			weaponAttachments[j].PostInitialize();
		}
	}

	protected void CheckPawnScale(Thing parent)
	{
		if (!scaleWithPawnSize)
		{
			return;
		}
		if (parent is Pawn pawn)
		{
			if (lastEquippedPawn != pawn)
			{
				GraphicMeshSet humanlikeBodySetForPawn = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn);
				Vector3? vector;
				if (humanlikeBodySetForPawn == null)
				{
					vector = null;
				}
				else
				{
					Mesh mesh = humanlikeBodySetForPawn.MeshAt(pawn.Rotation);
					vector = ((mesh != null) ? new Vector3?(mesh.bounds.size) : ((Vector3?)null));
				}
				Vector3? vector2 = vector;
				pawnScaleFactor = (vector2.HasValue ? (vector2.Value.z / 1.5f) : 1f);
				lastEquippedPawn = pawn;
			}
		}
		else
		{
			pawnScaleFactor = 1f;
		}
	}

	public virtual void CalculateRenderingPosition(Thing parent, Vector3 drawPosition, float aimAngle)
	{
		lastRenderedPosition = drawPosition;
		CheckPawnScale(parent);
		if (!ShouldDrawNormally)
		{
			orientationData.initialized = false;
		}
		else
		{
			var (mesh, position, num) = WeaponUtility.CalculateEquipmentAiming(parent, this, drawPosition, aimAngle, def.equippedAngleOffset, useRecoil: true);
			orientationData.mesh = mesh;
			orientationData.aimAngle = aimAngle;
			orientationData.drawAngle = num;
			orientationData.position = position;
			orientationData.rotation = Quaternion.Euler(0f, num, 0f);
			orientationData.initialized = true;
		}
		if (weaponAttachments != null)
		{
			for (int i = 0; i < weaponAttachments.Count; i++)
			{
				weaponAttachments[i].CalculateRenderingPosition(parent, drawPosition, aimAngle);
			}
		}
	}

	public virtual bool DrawAttachments(Thing parent, Vector3 drawPosition, float aimAngle, bool openlyCarrying = true)
	{
		lastRenderedPosition = drawPosition;
		lastRenderedTick = Find.TickManager.TicksGame;
		if (weaponAttachments != null)
		{
			CheckPawnScale(parent);
			orientationData.initialized = false;
			bool flag = ShouldDrawNormally;
			for (int i = 0; i < weaponAttachments.Count; i++)
			{
				WeaponAttachment weaponAttachment = weaponAttachments[i];
				if ((!openlyCarrying || weaponAttachment.config.drawWhileWielded) && (openlyCarrying || weaponAttachment.config.drawWhileNotWielded))
				{
					flag &= weaponAttachment.Draw(parent, drawPosition, aimAngle);
				}
			}
			return flag;
		}
		return true;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			InitializeAttachments();
		}
		if (weaponAttachments == null || !Scribe.EnterNode("weaponAttachments"))
		{
			return;
		}
		try
		{
			for (int i = 0; i < weaponAttachments.Count; i++)
			{
				weaponAttachments[i].ExposeData();
			}
		}
		finally
		{
			Scribe.ExitNode();
		}
	}

	public virtual void EquippedTick()
	{
		if (weaponAttachments != null)
		{
			for (int i = 0; i < weaponAttachments.Count; i++)
			{
				weaponAttachments[i].EquippedTick();
			}
		}
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		if (pawn.Spawned && TickWeaponWhileEquipped)
		{
			pawn.Map?.EccentricProjectilesEffectComp().RegisterTickingWeapon(this);
		}
		SendWeaponSignal("Equipped", pawn);
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		base.Notify_Unequipped(pawn);
		if (TickWeaponWhileEquipped)
		{
			pawn.Map?.EccentricProjectilesEffectComp().DeregisterTickingWeapon(this);
		}
		SendWeaponSignal("Unequipped", pawn);
	}

	public virtual void SendWeaponSignal(string signal, object value)
	{
		if (weaponAttachments != null)
		{
			for (int i = 0; i < weaponAttachments.Count; i++)
			{
				weaponAttachments[i].SendWeaponSignal(signal, value);
			}
		}
	}

	protected virtual void CheckRenderingPositions()
	{
		int ticksGame = Find.TickManager.TicksGame;
		if (lastRenderedTick < ticksGame)
		{
			Pawn wielder = Wielder;
			if (wielder != null)
			{
				var (drawPosition, aimAngle) = WeaponUtility.CalculateEquipmentOrientation(this, wielder);
				CalculateRenderingPosition(wielder, drawPosition, aimAngle);
			}
			lastRenderedTick = ticksGame;
		}
	}

	public virtual Vector3 GetAttachmentPosition(string label, int index = -1, bool random = false)
	{
		CheckRenderingPositions();
		if (weaponAttachments == null || label == null)
		{
			return LastRenderedPosition;
		}
		if (cachedAttachmentPositionGetters.TryGetValue(label, out var value))
		{
			return value(index, random);
		}
		List<WeaponAttachment> attachments = new List<WeaponAttachment>();
		foreach (WeaponAttachment weaponAttachment in weaponAttachments)
		{
			if (weaponAttachment.config.label == label)
			{
				attachments.Add(weaponAttachment);
			}
		}
		if (attachments.Count == 1)
		{
			WeaponAttachment singleAttachment = attachments[0];
			cachedAttachmentPositionGetters[label] = (int _, bool __) => singleAttachment.LastRenderedPosition;
		}
		else if (attachments.Count > 1)
		{
			int counter = -1;
			object lockObj = new object();
			cachedAttachmentPositionGetters[label] = delegate(int idx, bool rand)
			{
				if (rand)
				{
					return attachments.RandomElement().LastRenderedPosition;
				}
				if (idx <= -1)
				{
					lock (lockObj)
					{
						counter = (counter + 1) % attachments.Count;
						return attachments[counter].LastRenderedPosition;
					}
				}
				return attachments[idx % attachments.Count].LastRenderedPosition;
			};
		}
		else
		{
			cachedAttachmentPositionGetters[label] = (int _, bool __) => LastRenderedPosition;
		}
		return cachedAttachmentPositionGetters[label](index, random);
	}
}
