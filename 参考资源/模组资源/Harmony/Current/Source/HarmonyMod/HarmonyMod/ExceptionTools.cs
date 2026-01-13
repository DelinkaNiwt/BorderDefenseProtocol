using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace HarmonyMod;

internal static class ExceptionTools
{
	private delegate void GetFullNameForStackTraceDelegate(StackTrace instance, StringBuilder sb, MethodBase mi, bool needsNewLine, out bool skipped, out bool isAsync);

	private delegate uint GetMethodIndexDelegate(StackFrame instance);

	private delegate string GetSecureFileNameDelegate(StackFrame instance);

	private delegate string GetAotIdDelegate();

	private static readonly AccessTools.FieldRef<StackTrace, StackTrace[]> captured_traces = SafeFieldRefAccess<StackTrace, StackTrace[]>("captured_traces");

	private static readonly AccessTools.FieldRef<StackFrame, string> internalMethodName = SafeFieldRefAccess<StackFrame, string>("internalMethodName");

	private static readonly AccessTools.FieldRef<StackFrame, long> methodAddress = SafeFieldRefAccess<StackFrame, long>("methodAddress");

	private static readonly GetFullNameForStackTraceDelegate GetFullNameForStackTrace = GetDelegate<GetFullNameForStackTraceDelegate>(typeof(StackTrace), "GetFullNameForStackTrace");

	private static readonly GetMethodIndexDelegate GetMethodIndex = GetDelegate<GetMethodIndexDelegate>(typeof(StackFrame), "GetMethodIndex");

	private static readonly GetSecureFileNameDelegate GetSecureFileName = GetDelegate<GetSecureFileNameDelegate>(typeof(StackFrame), "GetSecureFileName");

	private static readonly GetAotIdDelegate GetAotId = GetDelegate<GetAotIdDelegate>(typeof(StackTrace), "GetAotId");

	internal static readonly ConcurrentDictionary<int, int> seenStacktraces = new ConcurrentDictionary<int, int>();

	private const int MaxSeenStacktraces = 4096;

	private static T GetDelegate<T>(Type type, string name) where T : Delegate
	{
		try
		{
			return AccessTools.MethodDelegate<T>(AccessTools.Method(type, name) ?? throw new MissingMethodException(type.FullName, name));
		}
		catch (Exception ex)
		{
			Log.Error($"Harmony: Failed to create delegate for {type}.{name}: {ex.Message}");
			return null;
		}
	}

	private static AccessTools.FieldRef<TInst, TField> SafeFieldRefAccess<TInst, TField>(string fieldName)
	{
		try
		{
			return AccessTools.FieldRefAccess<TInst, TField>(fieldName);
		}
		catch (Exception ex)
		{
			Log.Warning("Harmony: Unable to access field " + typeof(TInst).FullName + "." + fieldName + ": " + ex.Message);
			return null;
		}
	}

	private static int ComputeStableHash(string s)
	{
		uint num = 2166136261u;
		for (int i = 0; i < s.Length; i++)
		{
			num ^= s[i];
			num *= 16777619;
		}
		return (int)num;
	}

	internal static string ExtractHarmonyEnhancedStackTrace(StackTrace trace, bool forceRefresh, out int hash)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StackTrace[] array = ((captured_traces != null) ? captured_traces(trace) : null);
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (stringBuilder.AddHarmonyFrames(array[i]))
				{
					stringBuilder.Append("\n--- End of stack trace from previous location where exception was thrown ---\n");
				}
			}
		}
		stringBuilder.AddHarmonyFrames(trace);
		string text = stringBuilder.ToString();
		hash = ComputeStableHash(text);
		if (Settings.noStacktraceCaching)
		{
			return text;
		}
		string text2 = $"[Ref {hash:X}]";
		if (forceRefresh)
		{
			return text2 + "\n" + text;
		}
		if (seenStacktraces.Count > 4096)
		{
			Log.Warning("Harmony: Clearing stacktrace cache to preserve memory");
			seenStacktraces.Clear();
		}
		if (seenStacktraces.AddOrUpdate(hash, 1, (int k, int v) => v + 1) > 1)
		{
			return text2 + " Duplicate stacktrace, see ref for original";
		}
		return text2 + "\n" + text;
	}

	private static bool AddHarmonyFrames(this StringBuilder sb, StackTrace trace)
	{
		if (trace == null || trace.FrameCount == 0)
		{
			return false;
		}
		for (int i = 0; i < trace.FrameCount; i++)
		{
			try
			{
				StackFrame frame = trace.GetFrame(i);
				if (i > 0)
				{
					sb.Append('\n');
				}
				MethodBase originalMethodFromStackframe = Harmony.GetOriginalMethodFromStackframe(frame);
				if (originalMethodFromStackframe == null)
				{
					string value = ((internalMethodName != null) ? internalMethodName(frame) : null);
					if (!string.IsNullOrEmpty(value))
					{
						sb.Append(value);
						continue;
					}
					MethodBase methodBase = frame?.GetMethod();
					if (methodBase != null)
					{
						AppendMethodSignature(sb, methodBase);
					}
					else
					{
						sb.AppendFormat("<0x{0:x5} + 0x{1:x5}> <unknown method>", SafeMethodAddress(frame), frame.GetNativeOffset());
					}
					continue;
				}
				if (GetFullNameForStackTrace != null)
				{
					GetFullNameForStackTrace(trace, sb, originalMethodFromStackframe, needsNewLine: false, out var _, out var _);
				}
				else
				{
					AppendMethodSignature(sb, originalMethodFromStackframe);
				}
				if (frame.GetILOffset() == -1)
				{
					sb.AppendFormat(" <0x{0:x5} + 0x{1:x5}>", SafeMethodAddress(frame), frame.GetNativeOffset());
					if (GetMethodIndex != null)
					{
						uint num = GetMethodIndex(frame);
						if (num != 16777215)
						{
							sb.AppendFormat(" {0}", num);
						}
					}
				}
				else
				{
					sb.AppendFormat(" [0x{0:x5}]", frame.GetILOffset());
				}
				string text = ((GetSecureFileName != null) ? GetSecureFileName(frame) : frame.GetFileName());
				if (string.IsNullOrEmpty(text))
				{
					text = "<unknown>";
				}
				if (text.Length > 0 && text[0] == '<')
				{
					string text2 = originalMethodFromStackframe.Module.ModuleVersionId.ToString("N");
					string text3 = ((GetAotId != null) ? GetAotId() : null);
					text = ((frame.GetILOffset() == -1 && text3 != null) ? ("<" + text2 + "#" + text3 + ">") : ("<" + text2 + ">"));
				}
				sb.AppendFormat(" in {0}:{1} ", text, frame.GetFileLineNumber());
				Patches patchInfo = Harmony.GetPatchInfo(originalMethodFromStackframe);
				if (patchInfo != null)
				{
					sb.AppendPatch(originalMethodFromStackframe, patchInfo.Transpilers, "TRANSPILER");
					sb.AppendPatch(originalMethodFromStackframe, patchInfo.Prefixes, "PREFIX");
					sb.AppendPatch(originalMethodFromStackframe, patchInfo.Postfixes, "POSTFIX");
					sb.AppendPatch(originalMethodFromStackframe, patchInfo.Finalizers, "FINALIZER");
				}
			}
			catch (Exception ex)
			{
				sb.Append("[Harmony: failed to render frame: " + ex.GetType().Name + "]");
			}
		}
		return true;
	}

	private static long SafeMethodAddress(StackFrame frame)
	{
		if (methodAddress == null)
		{
			return 0L;
		}
		return methodAddress(frame);
	}

	private static void AppendMethodSignature(StringBuilder sb, MethodBase mi)
	{
		Type declaringType = mi.DeclaringType;
		if (declaringType != null)
		{
			sb.Append(declaringType.FullName).Append('.');
		}
		sb.Append(mi.Name);
		if (mi is MethodInfo { IsGenericMethod: not false } methodInfo)
		{
			Type[] genericArguments = methodInfo.GetGenericArguments();
			if (genericArguments.Length != 0)
			{
				sb.Append('<').Append(string.Join(", ", genericArguments.Select((Type a) => a.Name))).Append('>');
			}
		}
		ParameterInfo[] parameters = mi.GetParameters();
		sb.Append('(').Append(string.Join(", ", parameters.Select((ParameterInfo p) => p.ParameterType.Name + " " + p.Name))).Append(')');
	}

	private static void AppendPatch(this StringBuilder sb, MethodBase method, IEnumerable<Patch> fixes, string name)
	{
		if (fixes == null)
		{
			return;
		}
		object obj = fixes as IList<Patch>;
		if (obj == null)
		{
			obj = new List<Patch>();
			((List<Patch>)obj).AddRange(fixes);
		}
		IList<Patch> list = (IList<Patch>)obj;
		if (list.Count == 0)
		{
			return;
		}
		Dictionary<MethodInfo, string> dictionary = new Dictionary<MethodInfo, string>(list.Count);
		foreach (Patch item in list)
		{
			if (item?.PatchMethod != null && !dictionary.ContainsKey(item.PatchMethod))
			{
				dictionary[item.PatchMethod] = item.owner ?? "<unknown>";
			}
		}
		foreach (MethodInfo sortedPatchMethod in PatchProcessor.GetSortedPatchMethods(method, list.ToArray()))
		{
			dictionary.TryGetValue(sortedPatchMethod, out var value);
			if (value == null)
			{
				value = sortedPatchMethod.DeclaringType?.Assembly?.GetName().Name ?? "<unknown>";
			}
			ParameterInfo[] parameters = sortedPatchMethod.GetParameters();
			string text = string.Join(", ", parameters.Select((ParameterInfo p) => p.ParameterType.Name + " " + p.Name));
			sb.AppendFormat("\n    - {0} {1}: {2} {3}:{4}({5})", name, value, sortedPatchMethod.ReturnType?.Name ?? "void", sortedPatchMethod.DeclaringType?.FullName ?? "<UnknownType>", sortedPatchMethod.Name, text);
		}
	}
}
