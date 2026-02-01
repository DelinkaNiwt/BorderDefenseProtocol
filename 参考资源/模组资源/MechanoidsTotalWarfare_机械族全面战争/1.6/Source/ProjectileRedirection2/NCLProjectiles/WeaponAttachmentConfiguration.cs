using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class WeaponAttachmentConfiguration
{
	public string label;

	public string parent;

	public Type attachmentClass = typeof(WeaponAttachment);

	public bool drawWhileWielded = true;

	public bool drawWhileNotWielded;

	public bool forceRecalculateOrientation;

	public Vector3 drawOffset = Vector3.zero;

	public WeaponDirectionalOffsets directionalOffsets;

	public WeaponDirectionalOffsets aimingOffsets;

	public WeaponDirectionalOffsets idleOffsets;

	public WeaponDirectionalIdleConfiguration idle;

	public bool alignWithParentPosition;

	public bool alignOffsetWithWeaponAngle;

	public Vector3 drawSize = Vector3.one;

	public bool scaleWithParentSize = true;

	public float? angleOffset;

	public bool alignWithAimAngle;

	public bool useRecoil;

	[Unsaved(false)]
	public bool hasTexture;

	public bool useParentGraphic;

	public GraphicData graphicData;

	public WeaponDirectionalGraphics directionalGraphicData;

	public ShaderTypeDef shaderType;

	public Color color = Color.white;

	public Material material;

	public string materialPath;

	public WeaponDirectionalMaterials directionalMaterials;

	public virtual void Initialize(ModExtension_WeaponAttachments parentExtension)
	{
		Shader shader = shaderType?.Shader ?? ShaderDatabase.Cutout;
		if (!materialPath.NullOrEmpty())
		{
			material = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get(materialPath), shader, color);
			hasTexture = true;
		}
		if (directionalMaterials != null)
		{
			if (!directionalMaterials.north.NullOrEmpty())
			{
				directionalMaterials.northMaterial = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get(directionalMaterials.north), shader, color);
				hasTexture = true;
			}
			if (!directionalMaterials.east.NullOrEmpty())
			{
				directionalMaterials.eastMaterial = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get(directionalMaterials.east), shader, color);
				hasTexture = true;
			}
			if (!directionalMaterials.west.NullOrEmpty())
			{
				directionalMaterials.westMaterial = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get(directionalMaterials.west), shader, color);
				hasTexture = true;
			}
			if (!directionalMaterials.south.NullOrEmpty())
			{
				directionalMaterials.southMaterial = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get(directionalMaterials.south), shader, color);
				hasTexture = true;
			}
		}
		if (useParentGraphic || graphicData != null || directionalGraphicData != null)
		{
			hasTexture = true;
		}
	}

	public Material GetMaterial(Rot4 rotation, Thing equipment)
	{
		if (directionalGraphicData != null)
		{
			if (rotation == Rot4.North && directionalGraphicData.north != null)
			{
				return directionalGraphicData.north.Graphic.GetColoredVersion(directionalGraphicData.north.Graphic.Shader, equipment.DrawColor, equipment.DrawColorTwo).MatSingleFor(equipment);
			}
			if (rotation == Rot4.East && directionalGraphicData.east != null)
			{
				return directionalGraphicData.east.Graphic.GetColoredVersion(directionalGraphicData.east.Graphic.Shader, equipment.DrawColor, equipment.DrawColorTwo).MatSingleFor(equipment);
			}
			if (rotation == Rot4.West && directionalGraphicData.west != null)
			{
				return directionalGraphicData.west.Graphic.GetColoredVersion(directionalGraphicData.west.Graphic.Shader, equipment.DrawColor, equipment.DrawColorTwo).MatSingleFor(equipment);
			}
			if (rotation == Rot4.South && directionalGraphicData.south != null)
			{
				return directionalGraphicData.south.Graphic.GetColoredVersion(directionalGraphicData.south.Graphic.Shader, equipment.DrawColor, equipment.DrawColorTwo).MatSingleFor(equipment);
			}
		}
		if (graphicData != null)
		{
			return graphicData.Graphic.GetColoredVersion(graphicData.Graphic.Shader, equipment.DrawColor, equipment.DrawColorTwo).MatSingleFor(equipment);
		}
		if (directionalMaterials == null)
		{
			return material;
		}
		return rotation.AsInt switch
		{
			0 => directionalMaterials.northMaterial ?? material, 
			1 => directionalMaterials.eastMaterial ?? material, 
			3 => directionalMaterials.westMaterial ?? material, 
			_ => directionalMaterials.southMaterial ?? material, 
		};
	}

	public Vector3 GetDrawSize(Rot4 rotation)
	{
		Vector3 a = graphicData?.drawSize.ToRimWorldVector3() ?? Vector3.one;
		a = a.MultipliedBy(drawSize);
		if (directionalOffsets != null)
		{
			a = a.MultipliedBy(directionalOffsets.GetSize(rotation));
		}
		if (WeaponWithAttachments.isAiming)
		{
			if (aimingOffsets != null)
			{
				a = a.MultipliedBy(aimingOffsets.GetSize(rotation));
			}
		}
		else if (idleOffsets != null)
		{
			a = a.MultipliedBy(idleOffsets.GetSize(rotation));
		}
		return a;
	}

	public Vector3 GetDrawOffset(Rot4 rotation)
	{
		Vector3 result = drawOffset;
		if (directionalOffsets != null)
		{
			result += directionalOffsets.GetOffset(rotation);
		}
		if (WeaponWithAttachments.isAiming)
		{
			if (aimingOffsets != null)
			{
				result += aimingOffsets.GetOffset(rotation);
			}
		}
		else if (idleOffsets != null)
		{
			result += idleOffsets.GetOffset(rotation);
		}
		return result;
	}

	public float GetDrawAngle(Rot4 rotation, float aimAngle, bool isAiming = false)
	{
		float num = angleOffset.GetValueOrDefault();
		if (alignWithAimAngle)
		{
			num += aimAngle;
		}
		if (directionalOffsets != null)
		{
			num += directionalOffsets.GetAngle(rotation);
		}
		if (isAiming)
		{
			if (aimingOffsets != null)
			{
				num += aimingOffsets.GetAngle(rotation);
			}
		}
		else if (idleOffsets != null)
		{
			num += idleOffsets.GetAngle(rotation);
		}
		return num;
	}

	public virtual WeaponAttachment CreateInstance(ThingWithComps weapon)
	{
		return (WeaponAttachment)Activator.CreateInstance(attachmentClass, weapon, this);
	}
}
