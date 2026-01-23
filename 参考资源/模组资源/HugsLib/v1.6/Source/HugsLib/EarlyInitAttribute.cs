using System;

namespace HugsLib;

/// <summary>
/// Used to indicate that a <see cref="T:HugsLib.ModBase" /> type should be instantiated at the earliest moment possible.
/// Specifically, when <see cref="T:Verse.Mod" /> classes are instantiated (see <see cref="T:Verse.PlayDataLoader" />.DoPlayLoad()).
/// If <see cref="P:HugsLib.ModBase.HarmonyAutoPatch" /> is true, Harmony patching will also happen at that time.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EarlyInitAttribute : Attribute
{
}
