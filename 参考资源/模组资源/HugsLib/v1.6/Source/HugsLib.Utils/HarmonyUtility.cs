using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace HugsLib.Utils;

/// <summary>
/// Tools for working with the Harmony library.
/// </summary>
public static class HarmonyUtility
{
	private struct NameMethodPair
	{
		public readonly string MethodName;

		public readonly MethodBase Method;

		public NameMethodPair(string methodName, MethodBase method)
		{
			MethodName = methodName;
			Method = method;
		}
	}

	private const int DefaultPatchPriority = 400;

	/// <summary>
	/// Produces a human-readable list of all methods patched by all Harmony instances and their respective patches.
	/// </summary>
	public static string DescribeAllPatchedMethods()
	{
		try
		{
			return DescribePatchedMethodsList(Harmony.GetAllPatchedMethods());
		}
		catch (Exception ex)
		{
			return "Could not retrieve patched methods from the Harmony library:\n" + ex;
		}
	}

	/// <summary>
	/// Produces a human-readable list of all methods patched by a single Harmony instance and their respective patches.
	/// </summary>
	/// <param name="instance">A Harmony instance that can be queried for patch information.</param>
	public static string DescribePatchedMethods(Harmony instance)
	{
		try
		{
			return DescribePatchedMethodsList(instance.GetPatchedMethods());
		}
		catch (Exception arg)
		{
			return $"Could not retrieve patched methods from Harmony instance (id: {instance.Id}):\n{arg}";
		}
	}

	/// <summary>
	/// Produces a human-readable list of Harmony patches on a given set of methods.
	/// </summary>
	public static string DescribePatchedMethodsList(IEnumerable<MethodBase> patchedMethods)
	{
		try
		{
			List<MethodBase> list = patchedMethods.ToList();
			List<NameMethodPair> list2 = new List<NameMethodPair>(list.Count);
			foreach (MethodBase item in list)
			{
				if (!(item == null))
				{
					string nestedMemberName = GetNestedMemberName(item);
					list2.Add(new NameMethodPair(nestedMemberName, item));
				}
			}
			if (list2.Count == 0)
			{
				return "No patches have been reported.";
			}
			list2.Sort((NameMethodPair m1, NameMethodPair m2) => string.Compare(m1.MethodName, m2.MethodName, StringComparison.Ordinal));
			StringBuilder stringBuilder = new StringBuilder();
			foreach (NameMethodPair item2 in list2)
			{
				stringBuilder.Append(item2.MethodName);
				stringBuilder.Append(": ");
				HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(item2.Method);
				if (HasActivePatches(patchInfo))
				{
					if (patchInfo.Prefixes != null && patchInfo.Prefixes.Count > 0)
					{
						stringBuilder.Append("PRE: ");
						AppendPatchList(patchInfo.Prefixes, stringBuilder);
					}
					if (patchInfo.Postfixes != null && patchInfo.Postfixes.Count > 0)
					{
						EnsureEndsWithSpace(stringBuilder);
						stringBuilder.Append("post: ");
						AppendPatchList(patchInfo.Postfixes, stringBuilder);
					}
					if (patchInfo.Transpilers != null && patchInfo.Transpilers.Count > 0)
					{
						EnsureEndsWithSpace(stringBuilder);
						stringBuilder.Append("TRANS: ");
						AppendPatchList(patchInfo.Transpilers, stringBuilder);
					}
				}
				else
				{
					stringBuilder.Append("(no patches)");
				}
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			return "An exception occurred while collating patch data:\n" + ex;
		}
	}

	/// <summary>
	/// Produces a human-readable list of all Harmony versions present and their respective owners.
	/// </summary>
	/// <param name="instance">A Harmony instance that can be queried for version information.</param>
	/// <returns></returns>
	public static string DescribeHarmonyVersions(Harmony instance)
	{
		try
		{
			Version currentVersion;
			Dictionary<string, Version> source = Harmony.VersionInfo(out currentVersion);
			return "Harmony versions present: " + (from kv in source
				group kv.Key by kv.Value into grp
				orderby grp.Key descending
				select string.Format("{0}: {1}", grp.Key, grp.Join(", "))).Join("; ");
		}
		catch (Exception ex)
		{
			return "An exception occurred while collating Harmony version data:\n" + ex;
		}
	}

	internal static string GetNestedMemberName(MemberInfo member, int maxParentTypes = 10)
	{
		StringBuilder stringBuilder = new StringBuilder(member.Name);
		int num = 0;
		Type declaringType = member.DeclaringType;
		while (declaringType != null && num < maxParentTypes)
		{
			stringBuilder.Insert(0, '.');
			stringBuilder.Insert(0, declaringType.Name);
			declaringType = declaringType.DeclaringType;
			num++;
		}
		return stringBuilder.ToString();
	}

	private static void AppendPatchList(IEnumerable<Patch> patchList, StringBuilder builder)
	{
		List<Patch> list = new List<Patch>(patchList);
		list.Sort();
		bool flag = true;
		foreach (Patch item in list)
		{
			if (!flag)
			{
				builder.Append(", ");
			}
			flag = false;
			if (item.priority != 400)
			{
				builder.AppendFormat("[{0}]", item.priority);
			}
			builder.Append(item.PatchMethod.FullName());
		}
	}

	/// <summary>
	/// Logs an error if any issues with Harmony patches are detected
	/// </summary>
	public static void LogHarmonyPatchIssueErrors()
	{
		LogObsoleteMethodPatchErrors();
	}

	private static void LogObsoleteMethodPatchErrors()
	{
		foreach (var (text, source) in EnumerateObsoleteMethodPatchOwners())
		{
			Log.Warning("[" + text + "] Patches on methods annotated as Obsolete were detected by HugsLib: " + source.Distinct().Select(HugsLibUtility.FullName).ListElements());
		}
	}

	private static IEnumerable<(string owner, IEnumerable<MethodBase> methods)> EnumerateObsoleteMethodPatchOwners()
	{
		return from method in Harmony.GetAllPatchedMethods()
			select (method: method, info: Harmony.GetPatchInfo(method)) into pair
			where HasActivePatches(pair.info) && pair.method.HasAttribute<ObsoleteAttribute>()
			from owner in pair.info.Owners
			select (owner: owner, method: pair.method) into pair
			group pair.method by pair.owner into grp
			select ((string Key, IEnumerable<MethodBase>))(Key: grp.Key, grp);
	}

	private static bool HasActivePatches(HarmonyLib.Patches patches)
	{
		return patches != null && ((patches.Prefixes != null && patches.Prefixes.Count != 0) || (patches.Postfixes != null && patches.Postfixes.Count != 0) || (patches.Transpilers != null && patches.Transpilers.Count != 0));
	}

	private static void EnsureEndsWithSpace(StringBuilder builder)
	{
		if (builder[builder.Length - 1] != ' ')
		{
			builder.Append(" ");
		}
	}
}
