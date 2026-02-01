using UnityEngine;
using Verse;

namespace NCLProjectiles;

[StaticConstructorOnStartup]
public class UIAssets
{
	public static readonly Material ProjectileShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
}
