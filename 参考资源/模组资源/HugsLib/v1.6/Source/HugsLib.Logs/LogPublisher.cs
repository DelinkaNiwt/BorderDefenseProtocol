using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace HugsLib.Logs;

/// <summary>
/// Collects the game logs and loaded mods and posts the information on GitHub as a gist.
/// </summary>
public class LogPublisher
{
	public enum PublisherStatus
	{
		Ready,
		Uploading,
		Shortening,
		Done,
		Error
	}

	private const string RequestUserAgent = "HugsLib_log_uploader";

	private const string OutputLogFilename = "output_log.txt";

	private const string GistApiUrl = "https://api.github.com/gists";

	private const string ShortenerUrl = "https://git.io/";

	private const string GistPayloadJson = "{{\"description\":\"{0}\",\"public\":{1},\"files\":{{\"{2}\":{{\"content\":\"{3}\"}}}}}}";

	private const string GistDescription = "Rimworld output log published using HugsLib";

	private const int MaxLogLineCount = 10000;

	private const float PublishRequestTimeout = 90f;

	private const bool DenyPublicUpload = false;

	private readonly string GitHubAuthToken = "RuEvo2u9gsaCeKA9Bamh4sa57FOikUYkHhLH_phg".Reverse().Join("");

	private readonly Regex UploadResponseUrlMatch = new Regex("\"html_url\":\\s?\"(https://gist\\.github\\.com/[^\"]+)\"");

	private static SettingHandle<LogPublisherOptions> optionsHandle;

	private LogPublisherOptions publishOptions;

	private bool userAborted;

	private UnityWebRequest activeRequest;

	private Thread mockThread;

	public PublisherStatus Status { get; private set; }

	public string ErrorMessage { get; private set; }

	public string ResultUrl { get; private set; }

	public void ShowPublishPrompt()
	{
		if (PublisherIsReady())
		{
			UpdateCustomOptionsUsage();
			Find.WindowStack.Add(new Dialog_PublishLogsOptions("HugsLib_logs_shareConfirmTitle".Translate(), "HugsLib_logs_shareConfirmMessage".Translate(), optionsHandle.Value)
			{
				OnUpload = OnPublishConfirmed,
				OnCopy = CopyToClipboard,
				OnOptionsToggled = UpdateCustomOptionsUsage,
				OnPostClose = delegate
				{
					optionsHandle.ForceSaveChanges();
				}
			});
		}
		else
		{
			ShowPublishDialog();
		}
	}

	private void UpdateCustomOptionsUsage()
	{
		publishOptions = (optionsHandle.Value.UseCustomOptions ? optionsHandle.Value : new LogPublisherOptions());
	}

	public void AbortUpload()
	{
		if (Status == PublisherStatus.Uploading || Status == PublisherStatus.Shortening)
		{
			userAborted = true;
			if (activeRequest != null && !activeRequest.isDone)
			{
				activeRequest.Abort();
			}
			activeRequest = null;
			if (mockThread != null && mockThread.IsAlive)
			{
				mockThread.Interrupt();
			}
			if (Status == PublisherStatus.Shortening)
			{
				FinalizeUpload(success: true);
				return;
			}
			ErrorMessage = "Aborted by user";
			FinalizeUpload(success: false);
		}
	}

	public void BeginUpload()
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		if (!PublisherIsReady())
		{
			return;
		}
		Status = PublisherStatus.Uploading;
		ErrorMessage = null;
		userAborted = false;
		string text = PrepareLogData();
		if (text == null)
		{
			ErrorMessage = "Failed to collect data";
			FinalizeUpload(success: false);
			return;
		}
		Action<Exception> action = delegate(Exception ex)
		{
			if (!userAborted)
			{
				OnRequestError(ex.Message);
				HugsLibController.Logger.Warning("Exception during log publishing (gist creation): " + ex);
			}
		};
		try
		{
			text = CleanForJSON(text);
			bool flag = !string.IsNullOrWhiteSpace(publishOptions.AuthToken);
			string text2 = (flag ? publishOptions.AuthToken.Trim() : GitHubAuthToken);
			string text3 = (flag ? "false" : "true");
			bool flag2 = false;
			string s = string.Format("{{\"description\":\"{0}\",\"public\":{1},\"files\":{{\"{2}\":{{\"content\":\"{3}\"}}}}}}", "Rimworld output log published using HugsLib", text3, "output_log.txt", text);
			activeRequest = new UnityWebRequest("https://api.github.com/gists", "POST");
			activeRequest.SetRequestHeader("Authorization", "token " + text2);
			activeRequest.SetRequestHeader("User-Agent", "HugsLib_log_uploader");
			activeRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(s))
			{
				contentType = "application/json"
			};
			activeRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
			HugsLibUtility.AwaitUnityWebResponse(activeRequest, OnUploadComplete, action, HttpStatusCode.Created, 90f);
		}
		catch (Exception obj)
		{
			action(obj);
		}
	}

	public void CopyToClipboard()
	{
		UpdateCustomOptionsUsage();
		HugsLibUtility.CopyToClipboard(PrepareLogData());
	}

	private void MockUpload()
	{
		mockThread = new Thread((ThreadStart)delegate
		{
			Thread.Sleep(1500);
			Status = PublisherStatus.Shortening;
			Thread.Sleep(1500);
			ResultUrl = "copied to clipboard";
			FinalizeUpload(success: true);
		});
		mockThread.Start();
	}

	private void OnPublishConfirmed()
	{
		BeginUpload();
		ShowPublishDialog();
	}

	private void ShowPublishDialog()
	{
		Find.WindowStack.Add(new Dialog_PublishLogs());
	}

	private void OnRequestError(string errorMessage)
	{
		ErrorMessage = errorMessage;
		FinalizeUpload(success: false);
		HugsLibController.Logger.Error(errorMessage + "\n" + Environment.StackTrace);
	}

	private void OnUploadComplete(string response)
	{
		string text = TryExtractGistUrlFromUploadResponse(response);
		if (text == null)
		{
			OnRequestError("Failed to parse response");
			return;
		}
		ResultUrl = text;
		if (publishOptions.UseUrlShortener)
		{
			BeginUrlShortening();
		}
		else
		{
			FinalizeUpload(success: true);
		}
	}

	private void BeginUrlShortening()
	{
		Status = PublisherStatus.Shortening;
		Action<Exception> action = delegate(Exception ex)
		{
			if (!userAborted)
			{
				FinalizeUpload(success: true);
				HugsLibController.Logger.Warning("Exception during log publishing (url shortening): " + ex);
			}
		};
		try
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string> { { "url", ResultUrl } };
			activeRequest = UnityWebRequest.Post("https://git.io/", dictionary);
			activeRequest.SetRequestHeader("User-Agent", "HugsLib_log_uploader");
			HugsLibUtility.AwaitUnityWebResponse(activeRequest, OnUrlShorteningComplete, action, HttpStatusCode.Created);
		}
		catch (Exception obj)
		{
			action(obj);
		}
	}

	private void OnUrlShorteningComplete(string shortUrl)
	{
		ResultUrl = activeRequest.GetResponseHeader("Location");
		FinalizeUpload(success: true);
	}

	private void FinalizeUpload(bool success)
	{
		Status = (success ? PublisherStatus.Done : PublisherStatus.Error);
		activeRequest = null;
		mockThread = null;
	}

	private string TryExtractGistUrlFromUploadResponse(string response)
	{
		Match match = UploadResponseUrlMatch.Match(response);
		if (!match.Success)
		{
			return null;
		}
		return match.Groups[1].ToString();
	}

	private bool PublisherIsReady()
	{
		return Status == PublisherStatus.Ready || Status == PublisherStatus.Done || Status == PublisherStatus.Error;
	}

	private string PrepareLogData()
	{
		try
		{
			string logFileContents = GetLogFileContents();
			logFileContents = NormalizeLineEndings(logFileContents);
			logFileContents = RedactRimworldPaths(logFileContents);
			logFileContents = RedactPlayerConnectInformation(logFileContents);
			logFileContents = RedactRendererInformation(logFileContents);
			logFileContents = RedactHomeDirectoryPaths(logFileContents);
			logFileContents = RedactSteamId(logFileContents);
			logFileContents = RedactUselessLines(logFileContents);
			logFileContents = TrimExcessLines(logFileContents);
			return MakeLogTimestamp() + ListActiveMods() + "\n" + ListHarmonyPatches() + "\n" + ListPlatformInfo() + "\n" + logFileContents;
		}
		catch (Exception e)
		{
			HugsLibController.Logger.ReportException(e);
		}
		return null;
	}

	private string NormalizeLineEndings(string log)
	{
		return log.Replace("\r\n", "\n");
	}

	private string TrimExcessLines(string log)
	{
		if (publishOptions.AllowUnlimitedLogSize)
		{
			return log;
		}
		int num = IndexOfOccurence(log, '\n', 10000);
		if (num >= 0)
		{
			log = $"{log.Substring(0, num + 1)}(log trimmed to {10000:N0} lines. Use publishing options to upload the full log)";
		}
		return log;
	}

	private int IndexOfOccurence(string s, char match, int occurence)
	{
		int i = 1;
		int num = 0;
		for (; i <= occurence; i++)
		{
			if ((num = s.IndexOf(match, num + 1)) == -1)
			{
				break;
			}
			if (i == occurence)
			{
				return num;
			}
		}
		return -1;
	}

	private string RedactUselessLines(string log)
	{
		log = Regex.Replace(log, "Non platform assembly:.+\n", "");
		log = Regex.Replace(log, "Platform assembly: .+\n", "");
		log = Regex.Replace(log, "Fallback handler could not load library.+\n", "");
		log = Regex.Replace(log, "- Completed reload, in [\\d\\. ]+ seconds\n", "");
		log = Regex.Replace(log, "UnloadTime: [\\d\\. ]+ ms\n", "");
		log = Regex.Replace(log, "<RI> Initializing input\\.\r\n", "");
		log = Regex.Replace(log, "<RI> Input initialized\\.\r\n", "");
		log = Regex.Replace(log, "<RI> Initialized touch support\\.\r\n", "");
		log = Regex.Replace(log, "\\(Filename: C:/buildslave.+\n", "");
		log = Regex.Replace(log, "\n \n", "\n");
		return log;
	}

	private string RedactSteamId(string log)
	{
		return Regex.Replace(log, "Steam_SetMinidumpSteamID.+", "[Steam Id redacted]");
	}

	private string RedactHomeDirectoryPaths(string log)
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Regex.Replace(log, Regex.Escape(folderPath), "[Home_dir]", RegexOptions.IgnoreCase);
	}

	private string RedactRimworldPaths(string log)
	{
		string fullPath = Path.GetFullPath(Application.dataPath);
		List<string> list = fullPath.Split(Path.DirectorySeparatorChar).ToList();
		list.RemoveAt(list.Count - 1);
		fullPath = list.Join(Path.DirectorySeparatorChar.ToString());
		log = log.Replace(fullPath, "[Rimworld_dir]");
		if (Path.DirectorySeparatorChar != '/')
		{
			fullPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
			log = log.Replace(fullPath, "[Rimworld_dir]");
		}
		return log;
	}

	private string RedactRendererInformation(string log)
	{
		if (publishOptions.IncludePlatformInfo)
		{
			return log;
		}
		for (int i = 0; i < 5; i++)
		{
			string text = RedactString(log, "GfxDevice: ", "\nBegin MonoManager", "[Renderer information redacted]");
			if (log.Length == text.Length)
			{
				break;
			}
			log = text;
		}
		return log;
	}

	private string RedactPlayerConnectInformation(string log)
	{
		return RedactString(log, "PlayerConnection ", "Initialize engine", "[PlayerConnect information redacted]\n");
	}

	private string GetLogFileContents()
	{
		string text = HugsLibUtility.TryGetLogFilePath();
		if (text.NullOrEmpty() || !File.Exists(text))
		{
			throw new FileNotFoundException("Log file not found:" + text);
		}
		string tempFileName = Path.GetTempFileName();
		File.Delete(tempFileName);
		File.Copy(text, tempFileName);
		string text2 = File.ReadAllText(tempFileName);
		File.Delete(tempFileName);
		return "Log file contents:\n" + text2;
	}

	private string MakeLogTimestamp()
	{
		return "Log uploaded on " + DateTime.Now.ToLongDateString() + ", " + DateTime.Now.ToLongTimeString() + "\n";
	}

	private string RedactString(string original, string redactStart, string redactEnd, string replacement)
	{
		int num = original.IndexOf(redactStart, StringComparison.Ordinal);
		int num2 = original.IndexOf(redactEnd, StringComparison.Ordinal);
		string result = original;
		if (num >= 0 && num2 >= 0)
		{
			string text = original.Substring(num2);
			result = original.Substring(0, num + redactStart.Length);
			result += replacement;
			result += text;
		}
		return result;
	}

	private string ListHarmonyPatches()
	{
		Harmony harmonyInst = HugsLibController.Instance.HarmonyInst;
		string text = HarmonyUtility.DescribeAllPatchedMethods();
		return "Active Harmony patches:\n" + text + (text.EndsWith("\n") ? "" : "\n") + HarmonyUtility.DescribeHarmonyVersions(harmonyInst) + "\n";
	}

	private string ListPlatformInfo()
	{
		if (publishOptions.IncludePlatformInfo)
		{
			return "Platform information: " + "\nCPU: " + SystemInfo.processorType + "\nOS: " + SystemInfo.operatingSystem + "\nMemory: " + SystemInfo.systemMemorySize + " MB" + "\n";
		}
		return "Platform information: (hidden, use publishing options to include)\n";
	}

	private string ListActiveMods()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Loaded mods:\n");
		foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
		{
			stringBuilder.AppendFormat("{0}({1})", runningMod.Name, runningMod.PackageIdPlayerFacing);
			TryAppendOverrideVersion(stringBuilder, runningMod);
			TryAppendManifestVersion(stringBuilder, runningMod);
			stringBuilder.Append(": ");
			bool flag = true;
			bool flag2 = false;
			foreach (Assembly loadedAssembly in runningMod.assemblies.loadedAssemblies)
			{
				if (!flag)
				{
					stringBuilder.Append(", ");
				}
				flag = false;
				stringBuilder.Append(loadedAssembly.GetName().Name);
				stringBuilder.AppendFormat("({0})", AssemblyVersionInfo.ReadModAssembly(loadedAssembly, runningMod));
				flag2 = true;
			}
			if (!flag2)
			{
				stringBuilder.Append("(no assemblies)");
			}
			stringBuilder.Append("\n");
		}
		return stringBuilder.ToString();
	}

	private static void TryAppendOverrideVersion(StringBuilder builder, ModContentPack modContentPack)
	{
		VersionFile versionFile = VersionFile.TryParseVersionFile(modContentPack);
		if (versionFile != null && versionFile.OverrideVersion != null)
		{
			builder.AppendFormat("[ov:{0}]", versionFile.OverrideVersion);
		}
	}

	private static void TryAppendManifestVersion(StringBuilder builder, ModContentPack modContentPack)
	{
		ManifestFile manifestFile = ManifestFile.TryParse(modContentPack);
		if (manifestFile != null && manifestFile.Version != null)
		{
			builder.AppendFormat("[mv:{0}]", manifestFile.Version);
		}
	}

	private static string CleanForJSON(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		int length = s.Length;
		StringBuilder stringBuilder = new StringBuilder(length + 4);
		for (int i = 0; i < length; i++)
		{
			char c = s[i];
			switch (c)
			{
			case '"':
			case '\\':
				stringBuilder.Append('\\');
				stringBuilder.Append(c);
				continue;
			case '/':
				stringBuilder.Append('\\');
				stringBuilder.Append(c);
				continue;
			case '\b':
				stringBuilder.Append("\\b");
				continue;
			case '\t':
				stringBuilder.Append("\\t");
				continue;
			case '\n':
				stringBuilder.Append("\\n");
				continue;
			case '\f':
				stringBuilder.Append("\\f");
				continue;
			case '\r':
				stringBuilder.Append("\\r");
				continue;
			}
			if (c < ' ')
			{
				string text = "000X";
				stringBuilder.Append("\\u" + text.Substring(text.Length - 4));
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	internal static void RegisterSettings(ModSettingsPack pack)
	{
		optionsHandle = pack.GetHandle<LogPublisherOptions>("logPublisherSettings", "HugsLib_setting_logPublisherSettings_label".Translate(), null);
		optionsHandle.NeverVisible = true;
		optionsHandle.ValueChanged += EnsureNonNullHandleValue;
		EnsureNonNullHandleValue(null);
		static void EnsureNonNullHandleValue(SettingHandle _)
		{
			if (optionsHandle.Value == null)
			{
				optionsHandle.Value = new LogPublisherOptions();
				optionsHandle.HasUnsavedChanges = false;
			}
		}
	}
}
