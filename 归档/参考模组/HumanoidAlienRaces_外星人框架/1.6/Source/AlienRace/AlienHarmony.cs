using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AlienRace;

public class AlienHarmony
{
	public readonly Harmony harmony = new Harmony(id);

	public string PatchReport
	{
		get
		{
			List<Patches> patchInfos = harmony.GetPatchedMethods().Select(Harmony.GetPatchInfo).ToList();
			int prefixCount = patchInfos.SelectMany((Patches p) => p.Prefixes).Count((Patch p) => p.owner == harmony.Id);
			int postfixCount = patchInfos.SelectMany((Patches p) => p.Postfixes).Count((Patch p) => p.owner == harmony.Id);
			int transpilerCount = patchInfos.SelectMany((Patches p) => p.Transpilers).Count((Patch p) => p.owner == harmony.Id);
			return $"{prefixCount + postfixCount + transpilerCount} patches ({prefixCount} pre, {postfixCount} post, {transpilerCount} trans)";
		}
	}

	public AlienHarmony(string id)
	{
	}

	public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
	{
		if (original == null)
		{
			Log.Error(string.Format("{0} is null for: pre {1} | post: {2} | trans: {3}", "original", prefix?.method, postfix?.method, transpiler?.method));
		}
		else if (prefix?.method == null && postfix?.method == null && transpiler?.method == null)
		{
			Log.Error($"Patches are null for {original}");
		}
		else
		{
			try
			{
				return harmony.Patch(original, prefix, postfix, transpiler, finalizer);
			}
			catch (Exception ex)
			{
				Log.Error($"Error during patching {original.DeclaringType?.FullName} :: {original} with: pre {prefix?.method} | post: {postfix?.method} | trans: {transpiler?.method}\n{ex}");
			}
		}
		return null;
	}
}
