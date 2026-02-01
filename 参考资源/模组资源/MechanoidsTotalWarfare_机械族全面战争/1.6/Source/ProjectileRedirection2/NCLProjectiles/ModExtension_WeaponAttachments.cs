using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class ModExtension_WeaponAttachments : DefModExtension
{
	private static readonly Dictionary<int, int> originIncrementers = new Dictionary<int, int>();

	public bool tickWeaponWhileEquipped;

	public bool drawWeaponNormally = true;

	public bool drawWhileNotWielded;

	public bool drawNorthIdleMirrored;

	public Vector3 originOffset = Vector3.zero;

	public List<Vector3> originOffsets;

	public float originDistance;

	public bool alignOriginOffsetWithDirection;

	public List<WeaponAttachmentConfiguration> attachments;

	public ModExtension_WeaponAttachments()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			foreach (WeaponAttachmentConfiguration attachment in attachments)
			{
				attachment.Initialize(this);
			}
		});
	}

	public Vector3 GetOriginOffsetFor(Thing thing)
	{
		if (thing == null || originOffsets.NullOrEmpty())
		{
			return originOffset;
		}
		int num = 0;
		if (originIncrementers.TryGetValue(thing.thingIDNumber, out var value))
		{
			num = value;
		}
		if (num >= originOffsets.Count)
		{
			num = 0;
		}
		Vector3 result = originOffsets[num++];
		originIncrementers[thing.thingIDNumber] = num;
		return result;
	}
}
