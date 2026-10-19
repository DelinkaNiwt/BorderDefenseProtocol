using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Xml;
using RimWorld;
using RimWorld.IO;
using Verse;

/// <summary>验证实际任务的重复准入和原版语言加载；地图生成、袭击与成功信号由开发入口实测。</summary>
internal static class RoyalArrivalTests
{
    /// <summary>真实游戏和模组依赖目录。</summary>
    private static string[] paths;
    /// <summary>查询正式节点的私有准入成员。</summary>
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>注册游戏依赖解析后执行行为验证。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>仅解析当前安装的依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string file = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(file)) return Assembly.LoadFrom(file);
        }
        return null;
    }

    /// <summary>同类待接受和进行中任务阻止重复，已经失败／成功和其他任务不阻止。</summary>
    private static void Run()
    {
        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        Current.Game.questManager = new QuestManager();
        Type type = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll")).GetType(
            "BDP.Content.Royal.Quest.QuestNode_Root_RoyalArrival", true);
        object node = Activator.CreateInstance(type);
        MethodInfo check = type.GetMethod("HasPendingQuest", InstanceMembers);
        Check(!(bool)check.Invoke(node, null), "没有同类任务时不阻止入口");
        var pending = (Quest)FormatterServices.GetUninitializedObject(typeof(Quest));
        pending.root = new QuestScriptDef { defName = "BDP_RoyalArrival", root = (RimWorld.QuestGen.QuestNode)node };
        pending.acceptanceExpireTick = -1;
        pending.acceptanceTick = -1;
        Current.Game.questManager.QuestsListForReading.Add(pending);
        Check((bool)check.Invoke(node, null), "待接受的同类任务也必须防重复");
        pending.acceptanceTick = 0;
        Check((bool)check.Invoke(node, null), "进行中的同类任务必须防重复");
        pending.root.defName = "BDP_RoyalArrivalOpening";
        pending.acceptanceTick = -1;
        Check((bool)check.Invoke(node, null), "待接受的首发任务也必须阻止随机入口重复生成");
        pending.acceptanceTick = 0;
        Check((bool)check.Invoke(node, null), "首发入口与随机入口必须共用防重复规则");
        typeof(Quest).GetField("ended", InstanceMembers).SetValue(pending, true);
        foreach (QuestEndOutcome outcome in new[] { QuestEndOutcome.Fail, QuestEndOutcome.Success, QuestEndOutcome.Unknown })
        {
            typeof(Quest).GetField("endOutcome", InstanceMembers).SetValue(pending, outcome);
            Check(!(bool)check.Invoke(node, null), "已经结束的任务不永久阻断再取得入口");
        }
        pending.root = new QuestScriptDef { defName = "OtherQuest" };
        typeof(Quest).GetField("ended", InstanceMembers).SetValue(pending, false);
        Check(!(bool)check.Invoke(node, null), "其他进行中任务不能封锁本入口");
        Console.WriteLine("通过：实际皇室任务的待接受／进行中防重复、结束后放行及其他任务隔离。完整地图流程仍待实测。");
        CheckTaskLanguageFiles();
    }

    /// <summary>按真实加载目录优先级执行原版文件去重、键读取和译文查询，防止开发文件屏蔽正式文案。</summary>
    private static void CheckTaskLanguageFiles()
    {
        string modRoot = Directory.GetParent(paths[2]).Parent.FullName;
        var loadFolders = new XmlDocument();
        loadFolders.Load(Path.Combine(modRoot, "LoadFolders.xml"));
        var roots = new List<string>();
        foreach (XmlNode entry in loadFolders.SelectNodes("/loadFolders/v1.6/li"))
            roots.Add(Path.GetFullPath(Path.Combine(modRoot, entry.InnerText.Trim('/'))));
        roots.Reverse();
        var language = (LoadedLanguage)FormatterServices.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "ChineseSimplified (简体中文)";
        language.loadErrors = new List<string>();
        foreach (string fieldName in new[] { "tmpAlreadyLoadedFiles", "keyedReplacements" })
        {
            FieldInfo field = typeof(LoadedLanguage).GetField(fieldName, InstanceMembers);
            field.SetValue(language, Activator.CreateInstance(field.FieldType));
        }
        typeof(LoadedLanguage).GetField("dataIsLoaded", InstanceMembers).SetValue(language, true);
        var mod = (ModContentPack)FormatterServices.GetUninitializedObject(typeof(ModContentPack));
        MethodInfo loadKeyed = typeof(LoadedLanguage).GetMethod("LoadFromFile_Keyed", InstanceMembers);
        foreach (string root in roots)
        {
            string keyedDirectory = Path.Combine(root, "Languages", language.folderName, "Keyed");
            if (!Directory.Exists(keyedDirectory)) continue;
            foreach (string file in Directory.GetFiles(keyedDirectory, "*RoyalArrival*.xml"))
            {
                var directory = new Tuple<VirtualDirectory, ModContentPack, string>(null, mod, root);
                if (!language.TryRegisterFileIfNew(directory, file)) continue;
                VirtualFile virtualFile = AbstractFilesystem.GetDirectory(root).GetFile(file.Substring(root.Length + 1));
                loadKeyed.Invoke(language, new object[] { virtualFile, File.ReadAllText(file) });
            }
        }
        Check(language.loadErrors.Count == 0, "皇室任务语言文件读取失败");
        TaggedString text;
        Check(language.TryGetTextFromKey("BDP_RoyalArrivalName", out text) && text.ToString() == "皇室求援",
            "正式任务名称被开发语言文件覆盖，原版无法取得中文译文");
        foreach (string key in new[] { "BDP_RoyalArrivalDescription", "BDP_RoyalArrivalArrivedLabel",
            "BDP_RoyalArrivalArrivedText", "BDP_RoyalArrivalRaidLabel", "BDP_RoyalArrivalRaidText",
            "BDP_RoyalArrivalPursuerInitializationFailed", "BDP_Dev_RoyalArrivalMenu",
            "BDP_Dev_RoyalArrivalGenerate", "BDP_Dev_RoyalArrivalUnavailable", "BDP_Dev_RoyalArrivalGenerated" })
            Check(language.TryGetTextFromKey(key, out text), "原版未加载皇室任务语言键：" + key);
        Console.WriteLine("通过：原版加载优先级、同路径去重、键读取与查询；正式任务和开发菜单中文同时可用。");
    }

    /// <summary>失败时输出对应准入边界。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
