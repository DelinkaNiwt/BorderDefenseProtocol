using UnityEngine;
using Verse;

namespace NCLProjectiles;

[StaticConstructorOnStartup]
public class WeaponAttachment : IExposable
{
	protected struct CachedDirectionalMaterials
	{
		public Material north;

		public Material east;

		public Material west;

		public Material south;
	}

	protected static readonly MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

	public WeaponWithAttachments weapon;

	public WeaponAttachmentConfiguration config;

	protected WeaponOrientationData orientationData;

	protected CachedDirectionalMaterials materials;

	protected Vector3 lastRenderedPosition;

	public WeaponOrientationData OrientationData => orientationData;

	public Vector3 LastRenderedPosition => lastRenderedPosition;

	public virtual float EquippedAngleOffset => config.angleOffset ?? weapon.def.equippedAngleOffset;

	public WeaponAttachment(WeaponWithAttachments weapon, WeaponAttachmentConfiguration config)
	{
		this.weapon = weapon;
		this.config = config;
	}

	public virtual void PostInitialize()
	{
	}

	public virtual WeaponOrientationData InitializeOrientationData(Thing parent, Vector3 location, float aimAngle, bool applyOffsets = true)
	{
		WeaponOrientationData weaponOrientationData = weapon.orientationData.Clone();
		if (!weaponOrientationData.initialized || config.forceRecalculateOrientation)
		{
			(Mesh, Vector3, float) tuple = WeaponUtility.CalculateEquipmentAiming(parent, weapon, location, aimAngle, EquippedAngleOffset, config.useRecoil, config.idle);
			Mesh item = tuple.Item1;
			Vector3 item2 = tuple.Item2;
			float item3 = tuple.Item3;
			weaponOrientationData.mesh = item;
			weaponOrientationData.aimAngle = aimAngle;
			weaponOrientationData.drawAngle = item3;
			if (applyOffsets)
			{
				weaponOrientationData.rotation = CalculateDrawRotation(parent, item2, item3);
				weaponOrientationData.position = CalculateDrawPosition(parent, item2, item3);
			}
			else
			{
				weaponOrientationData.rotation = Quaternion.Euler(0f, item3, 0f);
				weaponOrientationData.position = item2;
			}
			weaponOrientationData.initialized = true;
		}
		return weaponOrientationData;
	}

	public virtual void CalculateRenderingPosition(Thing parent, Vector3 location, float aimAngle)
	{
		lastRenderedPosition = CalculateDrawPosition(parent, location, aimAngle);
	}

	public virtual bool Draw(Thing parent, Vector3 location, float aimAngle)
	{
		CalculateRenderingPosition(parent, location, aimAngle);
		if (config.hasTexture)
		{
			DrawInternal(MeshPool.plane10, GetMaterial(parent, location, aimAngle), lastRenderedPosition, CalculateDrawRotation(parent, location, aimAngle), CalculateDrawSize(parent, location, aimAngle));
		}
		return true;
	}

	protected Material GetCachedMaterial(Rot4 rotation, ThingWithComps weapon)
	{
		return rotation.AsInt switch
		{
			0 => GetOrCreateMaterial(ref materials.north, rotation, weapon), 
			1 => GetOrCreateMaterial(ref materials.east, rotation, weapon), 
			3 => GetOrCreateMaterial(ref materials.west, rotation, weapon), 
			_ => GetOrCreateMaterial(ref materials.south, rotation, weapon), 
		};
	}

	private Material GetOrCreateMaterial(ref Material materialField, Rot4 rotation, ThingWithComps weapon)
	{
		if (materialField == null)
		{
			materialField = config.GetMaterial(rotation, weapon);
		}
		return materialField;
	}

	protected virtual Material GetMaterial(Thing parent, Vector3 location, float aimAngle)
	{
		if (config.useParentGraphic)
		{
			return weapon.Graphic.MatSingleFor(weapon);
		}
		return GetCachedMaterial(parent.Rotation, weapon);
	}

	protected virtual Vector3 CalculateDrawPosition(Thing parent, Vector3 location, float drawAngle)
	{
		Vector3 vector = config.GetDrawOffset(parent.Rotation);
		if (config.scaleWithParentSize)
		{
			vector *= weapon.pawnScaleFactor;
		}
		if (config.alignOffsetWithWeaponAngle)
		{
			vector = Quaternion.Euler(0f, drawAngle, 0f) * vector;
		}
		return (config.alignWithParentPosition ? (parent.DrawPosHeld ?? location) : location) + vector;
	}

	protected virtual Quaternion CalculateDrawRotation(Thing parent, Vector3 location, float aimAngle)
	{
		float drawAngle = config.GetDrawAngle(parent.Rotation, aimAngle, WeaponWithAttachments.isAiming);
		return Quaternion.Euler(0f, drawAngle, 0f);
	}

	protected virtual Vector3 CalculateDrawSize(Thing parent, Vector3 location, float aimAngle)
	{
		Vector3 result = ((!config.useParentGraphic) ? config.GetDrawSize(parent.Rotation) : weapon.DrawSize.ToRimWorldVector3());
		if (config.scaleWithParentSize)
		{
			result *= weapon.pawnScaleFactor;
		}
		return result;
	}

	protected virtual void DrawInternal(Mesh mesh, Material material, Vector3 position, Quaternion quaternion, Vector3 size)
	{
		if (material != null)
		{
			Matrix4x4 matrix = Matrix4x4.TRS(position, quaternion, size);
			Graphics.DrawMesh(mesh, matrix, material, 0);
		}
	}

	protected virtual void DrawInternal(Mesh mesh, Material material, Vector3 position, Quaternion quaternion, Vector3 size, Color color)
	{
		if (material != null)
		{
			materialPropertyBlock.Clear();
			materialPropertyBlock.SetColor("_Color", color);
			Matrix4x4 matrix = Matrix4x4.TRS(position, quaternion, size);
			Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
		}
	}

	public virtual void EquippedTick()
	{
	}

	public virtual void ExposeData()
	{
	}

	public virtual void SendWeaponSignal(string signal, object value)
	{
	}
}
