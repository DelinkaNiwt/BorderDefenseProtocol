using UnityEngine;
using Verse;

namespace FreeElectronOrbitalLaser;

[StaticConstructorOnStartup]
public static class TextureCache
{
	public static Texture2D iconBeam = ContentFinder<Texture2D>.Get("Things/LaserConsole/FreeElectronOrbitalLaser", reportFailure: false);
}
