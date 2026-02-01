using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

[StaticConstructorOnStartup]
public static class TexHurricane
{
	public static readonly Material HurricaneOverlay = MaterialPool.MatFrom("Effects/Staticlord/Hurricane/VPEHurricaneWorldOverlay", ShaderDatabase.WorldOverlayTransparent);
}
